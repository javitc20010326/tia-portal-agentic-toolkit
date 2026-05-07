using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TiaPortalAgenticToolkit.Openness;

public sealed record TemplatePackResult(
    string PackKind,
    string OutputFolder,
    string ProjectName,
    string AxisName,
    string TiaVersion,
    IReadOnlyList<GeneratedFileSummary> Files,
    IReadOnlyList<string> RequiredSeedTemplates,
    IReadOnlyList<string> Warnings);

public sealed class TemplatePackGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public TemplatePackResult GenerateLogicTemplatePack(
        string outputFolder,
        string? projectName,
        string? axisName,
        string? tiaVersion,
        bool includeHmi)
    {
        var request = NormalizeRequest(outputFolder, projectName, axisName, tiaVersion);
        Directory.CreateDirectory(request.OutputFolder);

        var files = new List<GeneratedFileSummary>();
        Write(files, request, "logic-ir.json", "logic-ir", "Neutral PLC logic representation for SCL/LAD/FBD generation.", BuildLogicIr(request, includeHmi));
        Write(files, request, "LAD_AxisInterlock.template.json", "lad-template-ir", "Ladder-style network template expressed as neutral contacts/coils.", BuildLadTemplate(request));
        Write(files, request, "FBD_AxisMode.template.json", "fbd-template-ir", "Function-block-diagram-style network template expressed as neutral blocks/signals.", BuildFbdTemplate(request));
        Write(files, request, "LAD_Networks.md", "lad-readable-spec", "Readable LAD network plan for humans and future template rendering.", BuildLadMarkdown(request));
        Write(files, request, "FBD_Networks.md", "fbd-readable-spec", "Readable FBD network plan for humans and future template rendering.", BuildFbdMarkdown(request));
        Write(files, request, "EXPERIMENTAL_LAD_AxisInterlock.robot.xml", "experimental-robot-lad-xml", "Built-in ladder template XML understood by the toolkit UI robot. Not guaranteed to be directly importable by TIA Portal.", BuildExperimentalLadRobotXml(request));
        Write(files, request, "EXPERIMENTAL_FBD_AxisMode.robot.xml", "experimental-robot-fbd-xml", "Built-in FBD template XML understood by the toolkit UI robot. Not guaranteed to be directly importable by TIA Portal.", BuildExperimentalFbdRobotXml(request));
        Write(files, request, "EXPERIMENTAL_IMPORT_MAP.json", "experimental-import-map", "Automation hints that tell the UI robot what generated artifacts exist and how to treat them.", BuildExperimentalImportMap(request, includeHmi));
        if (includeHmi)
        {
            Write(files, request, "HMI_AxisOverview.template.json", "hmi-template-ir", "Neutral HMI screen template with objects, bindings, alarms, and layout.", BuildHmiTemplate(request));
            Write(files, request, "EXPERIMENTAL_HMI_AxisOverview.robot.json", "experimental-robot-hmi-json", "Built-in HMI template understood by the toolkit UI robot. Requires renderer or UI automation to create real TIA/WinCC objects.", BuildExperimentalHmiRobotTemplate(request));
        }
        Write(files, request, "EXPERIMENTAL_BASE_TEMPLATES.md", "experimental-template-notes", "Notes for using built-in fallback templates when no user seed exports are available.", BuildExperimentalBaseTemplateNotes(request, includeHmi));
        Write(files, request, "SEED_TEMPLATE_REQUEST.md", "seed-template-request", "Exact exports needed to convert neutral templates into real TIA XML for this TIA version.", BuildSeedTemplateRequest(request, includeHmi));
        Write(files, request, "TEMPLATE_PACK_MANIFEST.json", "manifest", "Machine-readable template-pack metadata.", BuildManifest(request, files, includeHmi));

        return new TemplatePackResult(
            PackKind: includeHmi ? "lad_fbd_hmi_templates" : "lad_fbd_templates",
            OutputFolder: request.OutputFolder,
            ProjectName: request.ProjectName,
            AxisName: request.AxisName,
            TiaVersion: request.TiaVersion,
            Files: files,
            RequiredSeedTemplates: BuildSeedTemplateList(includeHmi),
            Warnings:
            [
                "Neutral LAD/FBD/HMI templates are not guaranteed importable until mapped to exported TIA XML seed templates from the same TIA Portal major version.",
                "Built-in experimental robot templates are included so users can start without seed exports, but direct TIA import may fail until validated on the target TIA version.",
                "SCL generation is currently the most reliable no-Openness path.",
                "UI-agent import/compile can automate the desktop, but it is more brittle than Openness and must be tested on a copied project."
            ]);
    }

    private static string BuildLogicIr(NormalizedTemplateRequest request, bool includeHmi)
    {
        var ir = new
        {
            schema = "tia-portal-agentic-toolkit.logic-ir.v1",
            request.ProjectName,
            request.AxisName,
            request.TiaVersion,
            targetLanguages = includeHmi ? new[] { "SCL", "LAD", "FBD", "HMI" } : new[] { "SCL", "LAD", "FBD" },
            signals = new[]
            {
                Signal("Command.Enable", "Bool", "Operator enable"),
                Signal("Command.Reset", "Bool", "Fault reset"),
                Signal("Command.Stop", "Bool", "Controlled stop"),
                Signal("Command.JogPositive", "Bool", "Positive jog"),
                Signal("Command.JogNegative", "Bool", "Negative jog"),
                Signal("DriveReady", "Bool", "Drive ready feedback"),
                Signal("DriveFault", "Bool", "Drive fault feedback"),
                Signal("PositiveLimit", "Bool", "Positive limit switch"),
                Signal("NegativeLimit", "Bool", "Negative limit switch"),
                Signal("Status.Ready", "Bool", "Axis ready"),
                Signal("Status.Error", "Bool", "Axis fault"),
                Signal("EnableDrive", "Bool", "Drive enable output"),
                Signal("MovePositive", "Bool", "Positive movement output"),
                Signal("MoveNegative", "Bool", "Negative movement output"),
                Signal("StopDrive", "Bool", "Stop output")
            },
            networks = new object[]
            {
                new
                {
                    id = "N001",
                    title = "Ready permissive",
                    languageHint = "LAD",
                    expression = "Status.Ready := DriveReady AND NOT DriveFault AND NOT Status.Error"
                },
                new
                {
                    id = "N002",
                    title = "Drive enable",
                    languageHint = "LAD",
                    expression = "EnableDrive := Command.Enable AND Status.Ready AND NOT Command.Stop"
                },
                new
                {
                    id = "N003",
                    title = "Positive jog interlock",
                    languageHint = "LAD",
                    expression = "MovePositive := EnableDrive AND Command.JogPositive AND NOT PositiveLimit AND NOT Command.Stop"
                },
                new
                {
                    id = "N004",
                    title = "Negative jog interlock",
                    languageHint = "LAD",
                    expression = "MoveNegative := EnableDrive AND Command.JogNegative AND NOT NegativeLimit AND NOT Command.Stop"
                },
                new
                {
                    id = "N005",
                    title = "Stop priority",
                    languageHint = "FBD",
                    expression = "StopDrive := Command.Stop OR DriveFault OR Status.Error"
                }
            }
        };

        return JsonSerializer.Serialize(ir, JsonOptions);
    }

    private static string BuildLadTemplate(NormalizedTemplateRequest request)
    {
        var template = new
        {
            schema = "tia-portal-agentic-toolkit.lad-template-ir.v1",
            blockName = $"FB_{request.AxisName}_LAD_Interlocks",
            networks = new object[]
            {
                LadNetwork("N001", "Ready permissive", "Status.Ready", Contact("DriveReady"), Contact("DriveFault", normallyClosed: true), Contact("Status.Error", normallyClosed: true)),
                LadNetwork("N002", "Drive enable", "EnableDrive", Contact("Command.Enable"), Contact("Status.Ready"), Contact("Command.Stop", normallyClosed: true)),
                LadNetwork("N003", "Positive jog", "MovePositive", Contact("EnableDrive"), Contact("Command.JogPositive"), Contact("PositiveLimit", normallyClosed: true), Contact("Command.Stop", normallyClosed: true)),
                LadNetwork("N004", "Negative jog", "MoveNegative", Contact("EnableDrive"), Contact("Command.JogNegative"), Contact("NegativeLimit", normallyClosed: true), Contact("Command.Stop", normallyClosed: true)),
                LadNetwork("N005", "Stop output", "StopDrive", Branch(Contact("Command.Stop"), Contact("DriveFault"), Contact("Status.Error")))
            }
        };

        return JsonSerializer.Serialize(template, JsonOptions);
    }

    private static string BuildFbdTemplate(NormalizedTemplateRequest request)
    {
        var template = new
        {
            schema = "tia-portal-agentic-toolkit.fbd-template-ir.v1",
            blockName = $"FB_{request.AxisName}_FBD_Mode",
            networks = new object[]
            {
                FbdNetwork("N001", "Ready permissive", "AND", new[] { "DriveReady", "NOT DriveFault", "NOT Status.Error" }, "Status.Ready"),
                FbdNetwork("N002", "Drive enable", "AND", new[] { "Command.Enable", "Status.Ready", "NOT Command.Stop" }, "EnableDrive"),
                FbdNetwork("N003", "Stop priority", "OR", new[] { "Command.Stop", "DriveFault", "Status.Error" }, "StopDrive"),
                FbdNetwork("N004", "Movement mutual exclusion", "AND", new[] { "NOT MovePositive", "NOT MoveNegative" }, "Status.NotMoving")
            }
        };

        return JsonSerializer.Serialize(template, JsonOptions);
    }

    private static string BuildLadMarkdown(NormalizedTemplateRequest request) =>
        $"""
# LAD Network Plan: {request.AxisName}

These networks are neutral ladder logic. To become real TIA Portal LAD XML, they need one exported LAD seed block from the same TIA Portal version.

## N001 Ready Permissive

`DriveReady` series `NOT DriveFault` series `NOT Status.Error` drives coil `Status.Ready`.

## N002 Drive Enable

`Command.Enable` series `Status.Ready` series `NOT Command.Stop` drives coil `EnableDrive`.

## N003 Positive Jog

`EnableDrive` series `Command.JogPositive` series `NOT PositiveLimit` series `NOT Command.Stop` drives coil `MovePositive`.

## N004 Negative Jog

`EnableDrive` series `Command.JogNegative` series `NOT NegativeLimit` series `NOT Command.Stop` drives coil `MoveNegative`.

## N005 Stop Output

Parallel branch of `Command.Stop`, `DriveFault`, and `Status.Error` drives coil `StopDrive`.
""";

    private static string BuildFbdMarkdown(NormalizedTemplateRequest request) =>
        $"""
# FBD Network Plan: {request.AxisName}

These networks are neutral FBD logic. To become real TIA Portal FBD XML, they need one exported FBD seed block from the same TIA Portal version.

## Blocks

- `AND3` for ready permissive.
- `AND3` for drive enable.
- `OR3` for stop priority.
- `NOT` blocks for fault, error, stop, and movement exclusion.

## Signal Flow

- `DriveReady`, `NOT DriveFault`, and `NOT Status.Error` feed `Status.Ready`.
- `Command.Enable`, `Status.Ready`, and `NOT Command.Stop` feed `EnableDrive`.
- `Command.Stop`, `DriveFault`, and `Status.Error` feed `StopDrive`.
""";

    private static string BuildHmiTemplate(NormalizedTemplateRequest request)
    {
        var template = new
        {
            schema = "tia-portal-agentic-toolkit.hmi-template-ir.v1",
            screenName = $"{request.AxisName}_Overview",
            size = new { width = 1280, height = 720 },
            objects = new object[]
            {
                HmiObject("title", "Text", 24, 20, 500, 40, request.AxisName),
                HmiObject("stateText", "TextField", 24, 80, 360, 36, $"DB_{request.AxisName}_Data.Status.StateText"),
                HmiObject("fault", "Indicator", 410, 80, 120, 36, $"DB_{request.AxisName}_Data.Status.Error"),
                HmiObject("enable", "Button", 24, 150, 140, 44, $"DB_{request.AxisName}_Data.Command.Enable"),
                HmiObject("stop", "Button", 180, 150, 140, 44, $"DB_{request.AxisName}_Data.Command.Stop"),
                HmiObject("reset", "Button", 336, 150, 140, 44, $"DB_{request.AxisName}_Data.Command.Reset"),
                HmiObject("jogPositive", "Button", 24, 220, 180, 44, $"DB_{request.AxisName}_Data.Command.JogPositive"),
                HmiObject("jogNegative", "Button", 220, 220, 180, 44, $"DB_{request.AxisName}_Data.Command.JogNegative"),
                HmiObject("targetPosition", "NumericInput", 24, 300, 220, 44, $"DB_{request.AxisName}_Data.Command.TargetPosition"),
                HmiObject("actualPosition", "NumericOutput", 270, 300, 220, 44, $"DB_{request.AxisName}_Data.Status.ActualPosition"),
                HmiObject("errorCode", "NumericOutput", 24, 370, 220, 44, $"DB_{request.AxisName}_Data.Status.ErrorCode")
            },
            alarms = new[]
            {
                new { name = $"{request.AxisName}_DriveFault", trigger = $"DB_{request.AxisName}_Data.Status.ErrorCode = 16#0100" },
                new { name = $"{request.AxisName}_PositiveLimit", trigger = $"DB_{request.AxisName}_Data.Status.ErrorCode = 16#0201" },
                new { name = $"{request.AxisName}_NegativeLimit", trigger = $"DB_{request.AxisName}_Data.Status.ErrorCode = 16#0202" }
            }
        };

        return JsonSerializer.Serialize(template, JsonOptions);
    }

    private static string BuildExperimentalLadRobotXml(NormalizedTemplateRequest request) =>
        $$"""
<?xml version="1.0" encoding="utf-8"?>
<TiaPortalAgenticToolkitRobotTemplate schema="tia-portal-agentic-toolkit.robot-lad.v1"
                                      tiaVersion="{{request.TiaVersion}}"
                                      projectName="{{request.ProjectName}}"
                                      blockName="FB_{{request.AxisName}}_LAD_Interlocks"
                                      language="LAD"
                                      importConfidence="experimental">
  <Notice>This is toolkit robot XML, not certified Siemens XML. The UI agent can use it as a construction recipe. A TIA XML renderer may transform it later.</Notice>
  <Interface>
    <Signal name="Command.Enable" type="Bool" direction="Input" />
    <Signal name="Command.Stop" type="Bool" direction="Input" />
    <Signal name="Command.JogPositive" type="Bool" direction="Input" />
    <Signal name="Command.JogNegative" type="Bool" direction="Input" />
    <Signal name="DriveReady" type="Bool" direction="Input" />
    <Signal name="DriveFault" type="Bool" direction="Input" />
    <Signal name="PositiveLimit" type="Bool" direction="Input" />
    <Signal name="NegativeLimit" type="Bool" direction="Input" />
    <Signal name="Status.Ready" type="Bool" direction="Output" />
    <Signal name="EnableDrive" type="Bool" direction="Output" />
    <Signal name="MovePositive" type="Bool" direction="Output" />
    <Signal name="MoveNegative" type="Bool" direction="Output" />
    <Signal name="StopDrive" type="Bool" direction="Output" />
  </Interface>
  <Networks>
    <Network id="N001" title="Ready permissive">
      <Series>
        <Contact signal="DriveReady" normallyClosed="false" />
        <Contact signal="DriveFault" normallyClosed="true" />
        <Contact signal="Status.Error" normallyClosed="true" />
      </Series>
      <Coil signal="Status.Ready" />
    </Network>
    <Network id="N002" title="Drive enable">
      <Series>
        <Contact signal="Command.Enable" normallyClosed="false" />
        <Contact signal="Status.Ready" normallyClosed="false" />
        <Contact signal="Command.Stop" normallyClosed="true" />
      </Series>
      <Coil signal="EnableDrive" />
    </Network>
    <Network id="N003" title="Positive jog">
      <Series>
        <Contact signal="EnableDrive" normallyClosed="false" />
        <Contact signal="Command.JogPositive" normallyClosed="false" />
        <Contact signal="PositiveLimit" normallyClosed="true" />
        <Contact signal="Command.Stop" normallyClosed="true" />
      </Series>
      <Coil signal="MovePositive" />
    </Network>
    <Network id="N004" title="Negative jog">
      <Series>
        <Contact signal="EnableDrive" normallyClosed="false" />
        <Contact signal="Command.JogNegative" normallyClosed="false" />
        <Contact signal="NegativeLimit" normallyClosed="true" />
        <Contact signal="Command.Stop" normallyClosed="true" />
      </Series>
      <Coil signal="MoveNegative" />
    </Network>
    <Network id="N005" title="Stop output">
      <Parallel>
        <Contact signal="Command.Stop" normallyClosed="false" />
        <Contact signal="DriveFault" normallyClosed="false" />
        <Contact signal="Status.Error" normallyClosed="false" />
      </Parallel>
      <Coil signal="StopDrive" />
    </Network>
  </Networks>
</TiaPortalAgenticToolkitRobotTemplate>
""";

    private static string BuildExperimentalFbdRobotXml(NormalizedTemplateRequest request) =>
        $$"""
<?xml version="1.0" encoding="utf-8"?>
<TiaPortalAgenticToolkitRobotTemplate schema="tia-portal-agentic-toolkit.robot-fbd.v1"
                                      tiaVersion="{{request.TiaVersion}}"
                                      projectName="{{request.ProjectName}}"
                                      blockName="FB_{{request.AxisName}}_FBD_Mode"
                                      language="FBD"
                                      importConfidence="experimental">
  <Notice>This is toolkit robot XML, not certified Siemens XML. The UI agent can use it as a construction recipe. A TIA XML renderer may transform it later.</Notice>
  <Networks>
    <Network id="N001" title="Ready permissive">
      <Block type="AND" name="AND_Ready">
        <Input signal="DriveReady" />
        <Input signal="NOT DriveFault" />
        <Input signal="NOT Status.Error" />
        <Output signal="Status.Ready" />
      </Block>
    </Network>
    <Network id="N002" title="Drive enable">
      <Block type="AND" name="AND_Enable">
        <Input signal="Command.Enable" />
        <Input signal="Status.Ready" />
        <Input signal="NOT Command.Stop" />
        <Output signal="EnableDrive" />
      </Block>
    </Network>
    <Network id="N003" title="Stop priority">
      <Block type="OR" name="OR_Stop">
        <Input signal="Command.Stop" />
        <Input signal="DriveFault" />
        <Input signal="Status.Error" />
        <Output signal="StopDrive" />
      </Block>
    </Network>
    <Network id="N004" title="Movement mutual exclusion">
      <Block type="AND" name="AND_NotMoving">
        <Input signal="NOT MovePositive" />
        <Input signal="NOT MoveNegative" />
        <Output signal="Status.NotMoving" />
      </Block>
    </Network>
  </Networks>
</TiaPortalAgenticToolkitRobotTemplate>
""";

    private static string BuildExperimentalHmiRobotTemplate(NormalizedTemplateRequest request)
    {
        var template = new
        {
            schema = "tia-portal-agentic-toolkit.robot-hmi.v1",
            tiaVersion = request.TiaVersion,
            projectName = request.ProjectName,
            screenName = $"{request.AxisName}_Overview",
            importConfidence = "experimental",
            note = "Toolkit robot HMI recipe. It is not a certified WinCC/TIA screen export.",
            actions = new object[]
            {
                new { action = "createScreen", name = $"{request.AxisName}_Overview", width = 1280, height = 720 },
                new { action = "addText", id = "title", text = request.AxisName, x = 24, y = 20, width = 500, height = 40 },
                new { action = "addTextField", id = "stateText", tag = $"DB_{request.AxisName}_Data.Status.StateText", x = 24, y = 80, width = 360, height = 36 },
                new { action = "addIndicator", id = "fault", tag = $"DB_{request.AxisName}_Data.Status.Error", x = 410, y = 80, width = 120, height = 36 },
                new { action = "addButton", id = "enable", label = "Enable", tag = $"DB_{request.AxisName}_Data.Command.Enable", mode = "toggle", x = 24, y = 150, width = 140, height = 44 },
                new { action = "addButton", id = "stop", label = "Stop", tag = $"DB_{request.AxisName}_Data.Command.Stop", mode = "momentary", x = 180, y = 150, width = 140, height = 44 },
                new { action = "addButton", id = "reset", label = "Reset", tag = $"DB_{request.AxisName}_Data.Command.Reset", mode = "momentary", x = 336, y = 150, width = 140, height = 44 },
                new { action = "addNumericInput", id = "targetPosition", tag = $"DB_{request.AxisName}_Data.Command.TargetPosition", x = 24, y = 300, width = 220, height = 44 },
                new { action = "addNumericOutput", id = "actualPosition", tag = $"DB_{request.AxisName}_Data.Status.ActualPosition", x = 270, y = 300, width = 220, height = 44 },
                new { action = "addAlarm", name = $"{request.AxisName}_DriveFault", trigger = $"DB_{request.AxisName}_Data.Status.ErrorCode = 16#0100" }
            }
        };

        return JsonSerializer.Serialize(template, JsonOptions);
    }

    private static string BuildExperimentalImportMap(NormalizedTemplateRequest request, bool includeHmi)
    {
        var map = new
        {
            schema = "tia-portal-agentic-toolkit.experimental-import-map.v1",
            tiaVersion = request.TiaVersion,
            projectName = request.ProjectName,
            axisName = request.AxisName,
            preferredFlow = "SCL first, robot LAD/FBD/HMI recipes second, real TIA XML renderer when available.",
            files = new object[]
            {
                new { path = "EXPERIMENTAL_LAD_AxisInterlock.robot.xml", role = "ladRobotRecipe", directTiaImport = false, uiAgentCanRead = true },
                new { path = "EXPERIMENTAL_FBD_AxisMode.robot.xml", role = "fbdRobotRecipe", directTiaImport = false, uiAgentCanRead = true },
                new { path = "LAD_AxisInterlock.template.json", role = "ladNeutralIr", directTiaImport = false, uiAgentCanRead = true },
                new { path = "FBD_AxisMode.template.json", role = "fbdNeutralIr", directTiaImport = false, uiAgentCanRead = true },
                new { path = "HMI_AxisOverview.template.json", role = includeHmi ? "hmiNeutralIr" : "notGenerated", directTiaImport = false, uiAgentCanRead = includeHmi }
            },
            robotInstructions = new[]
            {
                "Prefer generated SCL artifacts when direct import is needed.",
                "Use .robot.xml/.robot.json files as UI construction recipes.",
                "If TIA import rejects an experimental XML file, switch to guided UI construction or request real exports from the target TIA version.",
                "Never download generated logic to hardware automatically."
            }
        };

        return JsonSerializer.Serialize(map, JsonOptions);
    }

    private static string BuildExperimentalBaseTemplateNotes(NormalizedTemplateRequest request, bool includeHmi)
    {
        var hmiLine = includeHmi
            ? "- `EXPERIMENTAL_HMI_AxisOverview.robot.json`: HMI screen construction recipe."
            : "- HMI recipe was not generated because includeHmi=false.";

        return $"""
# Experimental Built-In Base Templates

These files exist so the toolkit can work without asking every user for seed templates first.

They are intentionally marked experimental:

- They are understood by the toolkit and UI robot.
- They are not guaranteed to be directly importable by TIA Portal.
- They are safe as construction recipes and documentation.
- Real TIA exports from `{request.TiaVersion}` can replace or calibrate them later.

Generated built-in templates:

- `EXPERIMENTAL_LAD_AxisInterlock.robot.xml`: LAD construction recipe.
- `EXPERIMENTAL_FBD_AxisMode.robot.xml`: FBD construction recipe.
{hmiLine}
- `EXPERIMENTAL_IMPORT_MAP.json`: tells the UI robot how to treat the generated files.

Best practical path:

1. Generate SCL/CSV import packs for immediate use.
2. Use robot templates for guided UI construction.
3. When available, feed exported TIA XML/CSV files back into the toolkit to create a real renderer for this TIA version.
""";
    }

    private static string BuildSeedTemplateRequest(NormalizedTemplateRequest request, bool includeHmi)
    {
        var hmiLine = includeHmi
            ? "- One exported HMI screen containing a text, button, numeric input/output, indicator, and alarm if your TIA/WinCC setup allows exporting it."
            : "- HMI seed export is not required for this pack.";

        return $"""
# Seed Template Request

To convert neutral templates into real TIA Portal XML, provide exports from the same TIA Portal major version: `{request.TiaVersion}`.

Needed seed exports:

- One tiny LAD FB exported as XML with one normally-open contact, one normally-closed contact, one parallel branch, and one coil.
- One tiny FBD FB exported as XML with AND, OR, and NOT blocks.
- One PLC tag table exported as CSV or XML.
{hmiLine}

Recommended names:

- `Seed_LAD_Contacts.xml`
- `Seed_FBD_Blocks.xml`
- `Seed_TagTable.csv`
- `Seed_HMI_Screen.xml`

Once these are available, the toolkit can map neutral template nodes to the exact Siemens XML shape used by this TIA Portal version.
""";
    }

    private static string BuildManifest(NormalizedTemplateRequest request, IReadOnlyList<GeneratedFileSummary> files, bool includeHmi)
    {
        var manifest = new
        {
            schema = "tia-portal-agentic-toolkit.template-pack.v1",
            generatedAtUtc = DateTimeOffset.UtcNow,
            request.ProjectName,
            request.AxisName,
            request.TiaVersion,
            includes = includeHmi ? new[] { "LAD", "FBD", "HMI" } : new[] { "LAD", "FBD" },
            files = files.Select(file => new { path = Path.GetFileName(file.Path), file.Kind, file.Purpose }),
            builtInFallbackTemplates = new
            {
                generated = true,
                directTiaImportGuaranteed = false,
                uiAgentReadable = true,
                files = includeHmi
                    ? new[] { "EXPERIMENTAL_LAD_AxisInterlock.robot.xml", "EXPERIMENTAL_FBD_AxisMode.robot.xml", "EXPERIMENTAL_HMI_AxisOverview.robot.json", "EXPERIMENTAL_IMPORT_MAP.json" }
                    : new[] { "EXPERIMENTAL_LAD_AxisInterlock.robot.xml", "EXPERIMENTAL_FBD_AxisMode.robot.xml", "EXPERIMENTAL_IMPORT_MAP.json" }
            },
            seedTemplatesRequired = BuildSeedTemplateList(includeHmi)
        };

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static IReadOnlyList<string> BuildSeedTemplateList(bool includeHmi)
    {
        var list = new List<string>
        {
            "Exported LAD FB XML from the same TIA Portal major version.",
            "Exported FBD FB XML from the same TIA Portal major version.",
            "Exported PLC tag table CSV/XML from the same TIA Portal major version."
        };

        if (includeHmi)
        {
            list.Add("Exported HMI screen/template from the same TIA Portal/WinCC generation, if export is available.");
        }

        return list;
    }

    private static void Write(List<GeneratedFileSummary> files, NormalizedTemplateRequest request, string fileName, string kind, string purpose, string content)
    {
        var path = Path.Combine(request.OutputFolder, fileName);
        File.WriteAllText(path, content, Encoding.UTF8);
        files.Add(new GeneratedFileSummary(path, kind, purpose));
    }

    private static object Signal(string name, string dataType, string comment) => new { name, dataType, comment };

    private static object Contact(string signal, bool normallyClosed = false) => new { type = "contact", signal, normallyClosed };

    private static object Branch(params object[] paths) => new { type = "parallelBranch", paths };

    private static object LadNetwork(string id, string title, string coil, params object[] elements) => new { id, title, elements, coil = new { type = "coil", signal = coil } };

    private static object FbdNetwork(string id, string title, string block, string[] inputs, string output) => new { id, title, block, inputs, output };

    private static object HmiObject(string id, string type, int x, int y, int width, int height, string bindingOrText) => new { id, type, x, y, width, height, bindingOrText };

    private static NormalizedTemplateRequest NormalizeRequest(string outputFolder, string? projectName, string? axisName, string? tiaVersion)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new ArgumentException("Missing required argument: outputFolder");
        }

        return new NormalizedTemplateRequest(
            OutputFolder: Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder)),
            ProjectName: NormalizeIdentifier(projectName, "TiaProject"),
            AxisName: NormalizeIdentifier(axisName, "Axis1"),
            TiaVersion: NormalizeTiaVersion(tiaVersion));
    }

    private static string NormalizeIdentifier(string? value, string fallback)
    {
        var candidate = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        candidate = Regex.Replace(candidate, @"[^\w]", "_");
        candidate = Regex.Replace(candidate, @"_+", "_").Trim('_');
        if (candidate.Length == 0)
        {
            candidate = fallback;
        }
        return char.IsDigit(candidate[0]) ? "_" + candidate : candidate;
    }

    private static string NormalizeTiaVersion(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "V16";
        }

        var normalized = value.Trim().ToUpperInvariant();
        return normalized.StartsWith('V') ? normalized : "V" + normalized;
    }

    private sealed record NormalizedTemplateRequest(string OutputFolder, string ProjectName, string AxisName, string TiaVersion);
}
