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
            "tia_capabilities" => tia.GetCapabilities(),
            "tia_environment_status" => tia.GetEnvironmentStatus(),
            "tia_analyze_export_folder" => tia.AnalyzeExportFolder(ReadString(args, "folderPath"), ReadNullableInt(args, "maxFiles") ?? 200),
            "tia_parse_block_xml" => tia.ParseBlockXml(ReadString(args, "filePath")),
            "tia_summarize_scl" => tia.SummarizeScl(ReadString(args, "filePath")),
            "tia_generate_export_documentation" => tia.GenerateExportDocumentation(ReadString(args, "folderPath")),
            "tia_prepare_manual_import_checklist" => tia.PrepareManualImportChecklist(ReadString(args, "folderPath")),
            "tia_generate_axis_control_pack" => tia.GenerateAxisControlPack(ReadString(args, "outputFolder"), ReadNullableString(args, "projectName"), ReadNullableString(args, "axisName"), ReadNullableString(args, "userProfile"), ReadNullableString(args, "tiaVersion")),
            "tia_generate_plc_tag_table_csv" => tia.GeneratePlcTagTableCsv(ReadString(args, "outputFolder"), ReadNullableString(args, "projectName"), ReadNullableString(args, "axisName"), ReadNullableString(args, "userProfile"), ReadNullableString(args, "tiaVersion")),
            "tia_generate_hmi_plan" => tia.GenerateHmiPlan(ReadString(args, "outputFolder"), ReadNullableString(args, "projectName"), ReadNullableString(args, "axisName"), ReadNullableString(args, "userProfile"), ReadNullableString(args, "tiaVersion")),
            "tia_generate_logic_template_pack" => tia.GenerateLogicTemplatePack(ReadString(args, "outputFolder"), ReadNullableString(args, "projectName"), ReadNullableString(args, "axisName"), ReadNullableString(args, "tiaVersion"), ReadNullableBool(args, "includeHmi") ?? true),
            "tia_generate_ui_agent_plan" => tia.GenerateUiAgentPlan(ReadString(args, "outputFolder"), ReadNullableString(args, "projectPath"), ReadNullableString(args, "importPackFolder"), ReadNullableString(args, "tiaVersion"), ReadNullableString(args, "automationProfile")),
            "tia_analyze_project_texts_xlsx" => tia.AnalyzeProjectTextsXlsx(ReadString(args, "filePath")),
            "tia_analyze_webserver_bindings" => tia.AnalyzeWebServerBindings(ReadString(args, "path")),
            "tia_analyze_db_source" => tia.AnalyzeDbSource(ReadString(args, "filePath")),
            "tia_analyze_pdf_printout_text" => tia.AnalyzePdfPrintoutText(ReadString(args, "filePath")),
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

    private static string? ReadNullableString(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            return null;
        }

        var text = value.GetValue<string>();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool? ReadNullableBool(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            return null;
        }

        return value.GetValueKind() switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetValue<string>(), out var result) => result,
            _ => null
        };
    }

    private static string ReadString(JsonObject args, string name)
    {
        if (!args.TryGetPropertyValue(name, out var value) || value is null)
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return value.GetValue<string>();
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
            name = "tia_capabilities",
            title = "TIA Capabilities",
            description = "Determine whether this installation can run full agentic Openness workflows, semi-agentic export-file workflows, or advisory-only workflows. Use this before choosing TIA Portal automation steps.",
            inputSchema = new
            {
                type = "object",
                properties = new { },
                additionalProperties = false
            }
        },
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
            name = "tia_analyze_export_folder",
            title = "Analyze TIA Export Folder",
            description = "Analyze a folder of manually exported TIA Portal artifacts such as XML, SCL, AWL, CSV, and Excel files. Use this for semi-agentic mode when Openness permissions are unavailable.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    folderPath = new { type = "string", description = "Absolute or relative path to a folder containing TIA Portal exports." },
                    maxFiles = new { type = "integer", description = "Maximum number of supported files to summarize. Default 200." }
                },
                required = new[] { "folderPath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_parse_block_xml",
            title = "Parse TIA Block XML",
            description = "Parse a manually exported TIA Portal XML file and summarize root element, likely artifact name, common element counts, and interesting attributes/text.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    filePath = new { type = "string", description = "Path to an exported TIA Portal XML file." }
                },
                required = new[] { "filePath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_summarize_scl",
            title = "Summarize SCL Source",
            description = "Summarize an SCL/AWL source file: declarations, variables, calls, comments, and warnings.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    filePath = new { type = "string", description = "Path to an SCL or AWL source file." }
                },
                required = new[] { "filePath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_generate_export_documentation",
            title = "Generate Export Documentation",
            description = "Generate a Markdown documentation draft from a folder of exported TIA Portal artifacts.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    folderPath = new { type = "string", description = "Path to a folder containing exported TIA Portal artifacts." }
                },
                required = new[] { "folderPath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_prepare_manual_import_checklist",
            title = "Prepare Manual Import Checklist",
            description = "Create a Markdown checklist for manually importing generated/exported artifacts into TIA Portal when Openness is unavailable.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    folderPath = new { type = "string", description = "Path to a folder containing candidate import artifacts." }
                },
                required = new[] { "folderPath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_generate_axis_control_pack",
            title = "Generate Axis Control Import Pack",
            description = "Generate a semi-agentic TIA Portal engineering pack for a single position-control axis: SCL UDTs, DB, FB, OB1 call example, suggested PLC/HMI tags CSV, HMI screen plan, report, manifest, and manual import checklist. This does not modify a TIA project automatically.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputFolder = new { type = "string", description = "Folder where generated import artifacts will be written." },
                    projectName = new { type = "string", description = "Optional project name used in documentation. Default TiaProject." },
                    axisName = new { type = "string", description = "Optional axis identifier used for block/tag names. Default Axis1." },
                    userProfile = new { type = "string", description = "Optional user profile: self, student, or plc_engineer. Default self." },
                    tiaVersion = new { type = "string", description = "Optional TIA Portal target version such as V16, V17, V18, V19, V20, or V21. Default V16." }
                },
                required = new[] { "outputFolder" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_generate_plc_tag_table_csv",
            title = "Generate PLC Tag Table CSV",
            description = "Generate a suggested PLC/HMI tag table CSV and notes for manual adaptation/import in TIA Portal. Hardware addresses are intentionally left blank for human assignment.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputFolder = new { type = "string", description = "Folder where generated tag artifacts will be written." },
                    projectName = new { type = "string", description = "Optional project name used in documentation. Default TiaProject." },
                    axisName = new { type = "string", description = "Optional axis identifier used for tag names. Default Axis1." },
                    userProfile = new { type = "string", description = "Optional user profile: self, student, or plc_engineer. Default self." },
                    tiaVersion = new { type = "string", description = "Optional TIA Portal target version. Default V16." }
                },
                required = new[] { "outputFolder" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_generate_hmi_plan",
            title = "Generate HMI Plan",
            description = "Generate a Markdown HMI/WinCC screen plan with tags, alarms, operator behavior, and validation steps for a single axis.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputFolder = new { type = "string", description = "Folder where HMI_Screen_Plan.md will be written." },
                    projectName = new { type = "string", description = "Optional project name used in documentation. Default TiaProject." },
                    axisName = new { type = "string", description = "Optional axis identifier. Default Axis1." },
                    userProfile = new { type = "string", description = "Optional user profile: self, student, or plc_engineer. Default self." },
                    tiaVersion = new { type = "string", description = "Optional TIA Portal target version. Default V16." }
                },
                required = new[] { "outputFolder" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_generate_logic_template_pack",
            title = "Generate LAD/FBD/HMI Template Pack",
            description = "Generate neutral LAD, FBD, and optional HMI template artifacts for a TIA Portal axis workflow. These are template IR files and readable network plans; real TIA XML rendering requires seed exports from the same TIA Portal major version.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputFolder = new { type = "string", description = "Folder where template artifacts will be written." },
                    projectName = new { type = "string", description = "Optional project name used in documentation. Default TiaProject." },
                    axisName = new { type = "string", description = "Optional axis identifier used for block/tag names. Default Axis1." },
                    tiaVersion = new { type = "string", description = "Optional TIA Portal target version. Default V16." },
                    includeHmi = new { type = "boolean", description = "Whether to include neutral HMI screen templates. Default true." }
                },
                required = new[] { "outputFolder" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_generate_ui_agent_plan",
            title = "Generate TIA UI Agent Plan",
            description = "Generate a runnable plan for experimental TIA Portal desktop automation without Openness. The plan is consumed by scripts/ui-agent/tia-ui-agent.ps1 and can detect/open/focus TIA, prepare import packs, and capture state.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    outputFolder = new { type = "string", description = "Folder where UI-agent plan files will be written." },
                    projectPath = new { type = "string", description = "Optional path to a TIA project file such as .ap16. The UI agent opens it through Windows file association." },
                    importPackFolder = new { type = "string", description = "Optional folder containing generated SCL/CSV/XML/template files to import." },
                    tiaVersion = new { type = "string", description = "Optional TIA Portal target version. Default V16." },
                    automationProfile = new { type = "string", description = "dry-run, guided, or aggressive. Default guided." }
                },
                required = new[] { "outputFolder" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_analyze_project_texts_xlsx",
            title = "Analyze TIA Project Texts XLSX",
            description = "Analyze a TIA Portal project texts workbook exported as XLSX. Extracts category counts, HMI screen names, HMI object types, PLC block references, and sample texts without requiring Excel or Openness.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    filePath = new { type = "string", description = "Path to TIAProjectTexts.xlsx or a similar TIA Portal text export workbook." }
                },
                required = new[] { "filePath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_analyze_webserver_bindings",
            title = "Analyze TIA Web Server Bindings",
            description = "Analyze TIA Portal web-server HTML/TXT files and extract PLC tag bindings of the form :=\"DB\".Tag:. Useful for HMI/web template generation without Openness.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string", description = "Path to an HTML/TXT file or a folder containing TIA web-server files." }
                },
                required = new[] { "path" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_analyze_db_source",
            title = "Analyze TIA DB Source",
            description = "Analyze a generated/exported TIA Portal DATA_BLOCK source file (.db/.scl): block name, optimized access marker, sections, and variables.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    filePath = new { type = "string", description = "Path to a TIA DATA_BLOCK source file." }
                },
                required = new[] { "filePath" },
                additionalProperties = false
            }
        },
        new
        {
            name = "tia_analyze_pdf_printout_text",
            title = "Analyze TIA PDF Printout Text",
            description = "Analyze text extracted from a TIA Portal PDF printout. Extracts HMI object type counts, event names, variable references, and screen names. Pass a .txt file created from the PDF text, not the binary PDF.",
            inputSchema = new
            {
                type = "object",
                properties = new
                {
                    filePath = new { type = "string", description = "Path to a text file containing extracted TIA Portal PDF printout text." }
                },
                required = new[] { "filePath" },
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
