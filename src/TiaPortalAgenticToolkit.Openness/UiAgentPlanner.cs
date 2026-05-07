using System.Text;
using System.Text.Json;

namespace TiaPortalAgenticToolkit.Openness;

public sealed record UiAgentPlanResult(
    string PackKind,
    string OutputFolder,
    string? ProjectPath,
    string? ImportPackFolder,
    string TiaVersion,
    string AutomationProfile,
    IReadOnlyList<GeneratedFileSummary> Files,
    IReadOnlyList<string> AutomaticSteps,
    IReadOnlyList<string> HumanCheckpoints,
    IReadOnlyList<string> Warnings);

public sealed class UiAgentPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public UiAgentPlanResult GeneratePlan(
        string outputFolder,
        string? projectPath,
        string? importPackFolder,
        string? tiaVersion,
        string? automationProfile)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new ArgumentException("Missing required argument: outputFolder");
        }

        var request = new UiAgentRequest(
            OutputFolder: Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder)),
            ProjectPath: NormalizeNullablePath(projectPath),
            ImportPackFolder: NormalizeNullablePath(importPackFolder),
            TiaVersion: NormalizeTiaVersion(tiaVersion),
            AutomationProfile: NormalizeProfile(automationProfile));

        Directory.CreateDirectory(request.OutputFolder);

        var files = new List<GeneratedFileSummary>();
        Write(files, request.OutputFolder, "ui-agent-run.json", "ui-agent-plan", "Executable UI-agent plan for scripts/ui-agent/tia-ui-agent.ps1.", BuildPlanJson(request));
        Write(files, request.OutputFolder, "UI_AGENT_RUNBOOK.md", "ui-agent-runbook", "Runbook explaining the automation phases and checkpoints.", BuildRunbook(request));
        Write(files, request.OutputFolder, "UI_AGENT_LIMITS.md", "ui-agent-limits", "Boundaries and failure modes for TIA Portal UI automation without Openness.", BuildLimits(request));

        return new UiAgentPlanResult(
            PackKind: "tia_ui_agent_plan",
            OutputFolder: request.OutputFolder,
            ProjectPath: request.ProjectPath,
            ImportPackFolder: request.ImportPackFolder,
            TiaVersion: request.TiaVersion,
            AutomationProfile: request.AutomationProfile,
            Files: files,
            AutomaticSteps:
            [
                "Detect TIA Portal executable and installed Portal versions.",
                "Open the requested TIA Portal project through Windows file association or Portal executable.",
                "Bring the TIA Portal window to foreground.",
                "Prepare generated import artifacts for UI-driven import.",
                "Capture running Portal processes and window titles before and after each UI phase."
            ],
            HumanCheckpoints:
            [
                "Confirm a copied/offline project is open before import.",
                "Confirm generated blocks/tags compile before any hardware use.",
                "Approve any PLC download manually outside the UI agent."
            ],
            Warnings:
            [
                "UI Agent Mode is experimental and version/language/layout dependent.",
                "It is more automatic than manual import, but less reliable than Openness.",
                "It must not be used on a production PLC project without a copied project and visible supervision."
            ]);
    }

    private static string BuildPlanJson(UiAgentRequest request)
    {
        var plan = new
        {
            schema = "tia-portal-agentic-toolkit.ui-agent-plan.v1",
            request.TiaVersion,
            request.AutomationProfile,
            request.ProjectPath,
            request.ImportPackFolder,
            generatedAtUtc = DateTimeOffset.UtcNow,
            phases = new object[]
            {
                new { id = "detect", action = "status", required = true },
                new { id = "openProject", action = "open-project", required = !string.IsNullOrWhiteSpace(request.ProjectPath), projectPath = request.ProjectPath },
                new { id = "focus", action = "focus", required = true, windowTitleRegex = "TIA|Totally Integrated Automation|Portal" },
                new { id = "prepareImport", action = "prepare-import", required = !string.IsNullOrWhiteSpace(request.ImportPackFolder), importPackFolder = request.ImportPackFolder },
                new { id = "operatorImportAssist", action = "guided-sendkeys", required = false, profile = request.AutomationProfile },
                new { id = "collectDiagnostics", action = "capture-state", required = true }
            },
            safety = new
            {
                usesOpenness = false,
                editsProjectThroughApi = false,
                editsProjectThroughUi = request.AutomationProfile == "aggressive",
                downloadsToHardware = false,
                requiresCopiedProject = true
            }
        };

        return JsonSerializer.Serialize(plan, JsonOptions);
    }

    private static string BuildRunbook(UiAgentRequest request) =>
        $"""
# TIA Portal UI Agent Runbook

Target TIA version: `{request.TiaVersion}`
Automation profile: `{request.AutomationProfile}`
Project path: `{request.ProjectPath ?? "not set"}`
Import pack folder: `{request.ImportPackFolder ?? "not set"}`

This runbook is for machines with TIA Portal installed but without usable Openness permissions.

## Command

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\ui-agent\tia-ui-agent.ps1 -PlanPath "{Path.Combine(request.OutputFolder, "ui-agent-run.json")}"
```

## What The UI Agent Can Automate

- Locate installed TIA Portal executables.
- Open a TIA project file if a project path is provided.
- Bring TIA Portal to the foreground.
- Prepare generated SCL/CSV/template files for import.
- Capture process/window state and diagnostics text that is visible or copyable.

## What Needs Template Training

For reliable automatic LAD/FBD/HMI creation without Openness, provide seed exports from `{request.TiaVersion}`:

- one tiny LAD XML export,
- one tiny FBD XML export,
- one tag table export,
- one HMI screen export if available.

The toolkit can then map neutral template files to the exact XML shape used by this TIA version.
""";

    private static string BuildLimits(UiAgentRequest request) =>
        $"""
# UI Agent Limits

UI Agent Mode exists because some machines have TIA Portal but do not grant Openness access.

It is useful for:

- desktop-level automation,
- repeated import/compile workflows,
- capturing diagnostics,
- training template mappings for LAD/FBD/HMI exports.

It is not equivalent to Openness:

- window titles, menus, language, and layout can change,
- hidden dialogs can block automation,
- HMI editors are graphical and harder to automate,
- compile diagnostics may require OCR or user-visible text capture,
- project mutation happens through the visible TIA UI, not a stable engineering API.

Safe default: use `AutomationProfile = guided`. Use `aggressive` only on copied/offline projects after the guided flow is proven on that machine.
""";

    private static void Write(List<GeneratedFileSummary> files, string outputFolder, string fileName, string kind, string purpose, string content)
    {
        var path = Path.Combine(outputFolder, fileName);
        File.WriteAllText(path, content, Encoding.UTF8);
        files.Add(new GeneratedFileSummary(path, kind, purpose));
    }

    private static string? NormalizeNullablePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
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

    private static string NormalizeProfile(string? value)
    {
        var normalized = (value ?? "guided").Trim().ToLowerInvariant();
        return normalized switch
        {
            "aggressive" or "auto" or "automatic" => "aggressive",
            "dryrun" or "dry-run" or "dry_run" => "dry-run",
            _ => "guided"
        };
    }

    private sealed record UiAgentRequest(
        string OutputFolder,
        string? ProjectPath,
        string? ImportPackFolder,
        string TiaVersion,
        string AutomationProfile);
}
