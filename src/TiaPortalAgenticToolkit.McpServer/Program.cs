using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using TiaPortalAgenticToolkit.Openness;

var server = new McpServer(new TiaPortalSession());
await server.RunAsync();

internal sealed class McpServer(TiaPortalSession tia)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task RunAsync()
    {
        var stdout = Console.OpenStandardOutput();

        while (true)
        {
            var message = await ReadMessageAsync(Console.In);
            if (message is null)
            {
                return;
            }

            JsonNode? response;
            try
            {
                response = Handle(message);
            }
            catch (Exception ex)
            {
                response = Error(message["id"], -32603, ex.Message);
            }

            if (response is not null)
            {
                await WriteMessageAsync(stdout, response);
            }
        }
    }

    private JsonNode? Handle(JsonNode request)
    {
        var method = request["method"]?.GetValue<string>();
        var id = request["id"];

        return method switch
        {
            "initialize" => Result(id, new
            {
                protocolVersion = "2025-06-18",
                capabilities = new
                {
                    tools = new { }
                },
                serverInfo = new
                {
                    name = "tia-portal-agentic-toolkit",
                    version = "0.1.0"
                }
            }),
            "notifications/initialized" => null,
            "tools/list" => Result(id, new
            {
                tools = ToolDefinitions.All
            }),
            "tools/call" => CallTool(id, request["params"]?.AsObject()),
            _ => Error(id, -32601, $"Unknown method: {method}")
        };
    }

    private JsonNode CallTool(JsonNode? id, JsonObject? parameters)
    {
        var name = parameters?["name"]?.GetValue<string>() ?? "";
        var args = parameters?["arguments"]?.AsObject() ?? new JsonObject();

        object result = name switch
        {
            "tia_environment_status" => tia.GetEnvironmentStatus(),
            "tia_attach_running_portal" => tia.AttachToRunningPortal(ReadNullableInt(args, "processId")),
            _ => new { error = $"Unknown tool: {name}" }
        };

        return Result(id, new
        {
            content = new object[]
            {
                new
                {
                    type = "text",
                    text = JsonSerializer.Serialize(result, JsonOptions)
                }
            }
        });
    }

    private static int? ReadNullableInt(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            return null;
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.Number when value.GetValue<int>() is var i => i,
            JsonValueKind.String when int.TryParse(value.GetValue<string>(), out var i) => i,
            _ => null
        };
    }

    private static JsonNode Result(JsonNode? id, object result)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneOrNull(id),
            ["result"] = JsonSerializer.SerializeToNode(result, JsonOptions)
        };

        return response;
    }

    private static JsonNode Error(JsonNode? id, int code, string message)
    {
        var response = new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = CloneOrNull(id),
            ["error"] = JsonSerializer.SerializeToNode(new { code, message }, JsonOptions)
        };

        return response;
    }

    private static JsonNode? CloneOrNull(JsonNode? node) =>
        node is null ? null : JsonNode.Parse(node.ToJsonString());

    private static Task<JsonNode?> ReadMessageAsync(TextReader input)
    {
        var headers = new List<string>();
        while (true)
        {
            var line = input.ReadLine();
            if (line is null)
            {
                return Task.FromResult<JsonNode?>(null);
            }

            if (line.Length == 0)
            {
                break;
            }

            headers.Add(line);
        }

        var contentLength = headers
            .Select(header => header.Split(':', 2))
            .Where(parts => parts.Length == 2 && parts[0].Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            .Select(parts => int.TryParse(parts[1].Trim(), out var value) ? value : 0)
            .FirstOrDefault();

        if (contentLength <= 0)
        {
            return Task.FromResult<JsonNode?>(null);
        }

        var buffer = new char[contentLength];
        var offset = 0;
        while (offset < contentLength)
        {
            var read = input.Read(buffer, offset, contentLength - offset);
            if (read == 0)
            {
                return Task.FromResult<JsonNode?>(null);
            }

            offset += read;
        }

        return Task.FromResult(JsonNode.Parse(new string(buffer)));
    }

    private static async Task WriteMessageAsync(Stream output, JsonNode message)
    {
        var json = message.ToJsonString(JsonOptions);
        var payload = Encoding.UTF8.GetBytes(json);
        var header = Encoding.ASCII.GetBytes($"Content-Length: {payload.Length}\r\n\r\n");
        await output.WriteAsync(header);
        await output.WriteAsync(payload);
        await output.FlushAsync();
    }
}

internal static class ToolDefinitions
{
    public static readonly object[] All =
    [
        new
        {
            name = "tia_environment_status",
            title = "TIA Environment Status",
            description = "Inspect the local Windows/TIA Portal Openness environment: Openness registry keys, Siemens.Engineering.dll candidates, user group membership, running TIA Portal processes, and warnings.",
            inputSchema = new
            {
                type = "object",
                properties = new { },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_attach_running_portal",
            title = "Attach Running TIA Portal",
            description = "Prepare to attach to an existing TIA Portal process. In v0.1.0 this validates prerequisites and reports the selected process; project mutation is not implemented yet.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    processId = new
                    {
                        type = "integer",
                        description = "Optional TIA Portal process id. If omitted, the server will choose when attach support is implemented."
                    }
                },
                additionalProperties = false
            }
        }
    ];
}
