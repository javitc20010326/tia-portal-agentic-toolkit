using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TiaPortalAgenticToolkit.Openness;

public sealed record GeneratedFileSummary(
    string Path,
    string Kind,
    string Purpose);

public sealed record EngineeringPackResult(
    string PackKind,
    string OutputFolder,
    string ProjectName,
    string AxisName,
    string UserProfile,
    string TiaVersion,
    IReadOnlyList<GeneratedFileSummary> Files,
    IReadOnlyList<string> ManualImportOrder,
    IReadOnlyList<string> Warnings);

public sealed class ImportPackGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public EngineeringPackResult GenerateAxisControlPack(
        string outputFolder,
        string? projectName,
        string? axisName,
        string? userProfile,
        string? tiaVersion)
    {
        var request = NormalizeRequest(outputFolder, projectName, axisName, userProfile, tiaVersion);
        Directory.CreateDirectory(request.OutputFolder);

        var files = new List<GeneratedFileSummary>();
        WriteGeneratedFile(files, request, "01_UDT_AxisCommand.scl", "scl-udt", "Axis command user data type.", BuildAxisCommandUdt(request));
        WriteGeneratedFile(files, request, "02_UDT_AxisStatus.scl", "scl-udt", "Axis status user data type.", BuildAxisStatusUdt(request));
        WriteGeneratedFile(files, request, "03_DB_AxisData.scl", "scl-db", "Shared axis command/status data block.", BuildAxisDataDb(request));
        WriteGeneratedFile(files, request, "04_FB_AxisControl.scl", "scl-fb", "Defensive position-control state machine starter block.", BuildAxisControlFb(request));
        WriteGeneratedFile(files, request, "05_OB1_Call_Example.scl", "scl-snippet", "Example call for OB1 or a cyclic organization block.", BuildOb1CallExample(request));
        WriteGeneratedFile(files, request, "PLC_Tags_Suggested.csv", "tag-table-csv", "Suggested PLC/HMI tag table. Verify column mapping in TIA Portal before import.", BuildTagTableCsv(request));
        WriteGeneratedFile(files, request, "HMI_Screen_Plan.md", "hmi-plan", "WinCC/TIA HMI screen layout, tags, alarms, and operator behavior.", BuildHmiPlan(request));
        WriteGeneratedFile(files, request, "Practice_Report.md", "engineering-report", "Engineering explanation suitable for review or class/lab reporting.", BuildPracticeReport(request));
        WriteGeneratedFile(files, request, "MANUAL_IMPORT_CHECKLIST.md", "manual-checklist", "Manual import and validation checklist for semi-agentic mode.", BuildManualImportChecklist(request));
        WriteGeneratedFile(files, request, "manifest.json", "manifest", "Machine-readable pack metadata.", BuildManifest(request, files));

        return new EngineeringPackResult(
            PackKind: "axis_position_control",
            OutputFolder: request.OutputFolder,
            ProjectName: request.ProjectName,
            AxisName: request.AxisName,
            UserProfile: request.UserProfile,
            TiaVersion: request.TiaVersion,
            Files: files,
            ManualImportOrder: BuildManualImportOrder(),
            Warnings: BuildWarnings(request));
    }

    public EngineeringPackResult GeneratePlcTagTableCsv(
        string outputFolder,
        string? projectName,
        string? axisName,
        string? userProfile,
        string? tiaVersion)
    {
        var request = NormalizeRequest(outputFolder, projectName, axisName, userProfile, tiaVersion);
        Directory.CreateDirectory(request.OutputFolder);

        var files = new List<GeneratedFileSummary>();
        WriteGeneratedFile(files, request, "PLC_Tags_Suggested.csv", "tag-table-csv", "Suggested PLC/HMI tag table. Verify column mapping in TIA Portal before import.", BuildTagTableCsv(request));
        WriteGeneratedFile(files, request, "PLC_Tags_ReadMe.md", "tag-table-notes", "Notes for adapting the CSV to the exact TIA Portal import format.", BuildTagTableReadme(request));

        return new EngineeringPackResult(
            PackKind: "plc_tag_table",
            OutputFolder: request.OutputFolder,
            ProjectName: request.ProjectName,
            AxisName: request.AxisName,
            UserProfile: request.UserProfile,
            TiaVersion: request.TiaVersion,
            Files: files,
            ManualImportOrder: new[] { "Review the CSV columns against a tag table exported from your TIA Portal version.", "Import or copy the tags into a project copy.", "Compile and resolve duplicated names or address conflicts." },
            Warnings: BuildWarnings(request));
    }

    public DocumentationDraft GenerateHmiPlan(
        string outputFolder,
        string? projectName,
        string? axisName,
        string? userProfile,
        string? tiaVersion)
    {
        var request = NormalizeRequest(outputFolder, projectName, axisName, userProfile, tiaVersion);
        Directory.CreateDirectory(request.OutputFolder);
        var markdown = BuildHmiPlan(request);
        File.WriteAllText(Path.Combine(request.OutputFolder, "HMI_Screen_Plan.md"), markdown, Encoding.UTF8);
        return new DocumentationDraft("markdown", markdown);
    }

    private static NormalizedPackRequest NormalizeRequest(
        string outputFolder,
        string? projectName,
        string? axisName,
        string? userProfile,
        string? tiaVersion)
    {
        if (string.IsNullOrWhiteSpace(outputFolder))
        {
            throw new ArgumentException("Missing required argument: outputFolder");
        }

        var fullOutputFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(outputFolder));
        var normalizedProjectName = NormalizeIdentifier(projectName, "TiaProject");
        var normalizedAxisName = NormalizeIdentifier(axisName, "Axis1");
        var normalizedProfile = NormalizeProfile(userProfile);
        var normalizedVersion = NormalizeTiaVersion(tiaVersion);

        return new NormalizedPackRequest(
            OutputFolder: fullOutputFolder,
            ProjectName: normalizedProjectName,
            AxisName: normalizedAxisName,
            UserProfile: normalizedProfile,
            TiaVersion: normalizedVersion);
    }

    private static void WriteGeneratedFile(
        List<GeneratedFileSummary> files,
        NormalizedPackRequest request,
        string fileName,
        string kind,
        string purpose,
        string content)
    {
        var path = Path.Combine(request.OutputFolder, fileName);
        File.WriteAllText(path, content, Encoding.UTF8);
        files.Add(new GeneratedFileSummary(path, kind, purpose));
    }

    private static string BuildAxisCommandUdt(NormalizedPackRequest request)
    {
        var typeName = $"{request.AxisName}_Command";
        return $"""
TYPE "{typeName}"
VERSION : 0.1
   STRUCT
      Enable : Bool;          // Operator enables the axis command interface.
      Reset : Bool;           // Reset command for faults and latched states.
      JogPositive : Bool;     // Manual jog in positive direction.
      JogNegative : Bool;     // Manual jog in negative direction.
      MoveAbsolute : Bool;    // Start absolute move request.
      Stop : Bool;            // Controlled stop request.
      TargetPosition : Real;  // Engineering units.
      Velocity : Real;        // Engineering units per second.
      Acceleration : Real;    // Engineering units per second squared.
      Deceleration : Real;    // Engineering units per second squared.
   END_STRUCT;
END_TYPE
""";
    }

    private static string BuildAxisStatusUdt(NormalizedPackRequest request)
    {
        var typeName = $"{request.AxisName}_Status";
        return $"""
TYPE "{typeName}"
VERSION : 0.1
   STRUCT
      Ready : Bool;
      Busy : Bool;
      Done : Bool;
      Error : Bool;
      ErrorCode : Word;
      Enabled : Bool;
      Moving : Bool;
      AtTarget : Bool;
      ActualPosition : Real;
      TargetLatched : Real;
      State : Int;
      StateText : String[40];
   END_STRUCT;
END_TYPE
""";
    }

    private static string BuildAxisDataDb(NormalizedPackRequest request)
    {
        return $$"""
DATA_BLOCK "DB_{{request.AxisName}}_Data"
{ S7_Optimized_Access := 'TRUE' }
VERSION : 0.1
   VAR
      Command : "{{request.AxisName}}_Command";
      Status : "{{request.AxisName}}_Status";
   END_VAR
BEGIN
   Command.Velocity := 10.0;
   Command.Acceleration := 50.0;
   Command.Deceleration := 50.0;
END_DATA_BLOCK
""";
    }

    private static string BuildAxisControlFb(NormalizedPackRequest request)
    {
        return $$"""
FUNCTION_BLOCK "FB_{{request.AxisName}}_Control"
{ S7_Optimized_Access := 'TRUE' }
VERSION : 0.1
   VAR_INPUT
      Command : "{{request.AxisName}}_Command";
      ActualPosition : Real;
      DriveReady : Bool;
      DriveFault : Bool;
      PositiveLimit : Bool;
      NegativeLimit : Bool;
      CycleTime : Time := T#100ms;
   END_VAR

   VAR_OUTPUT
      Status : "{{request.AxisName}}_Status";
      EnableDrive : Bool;
      MovePositive : Bool;
      MoveNegative : Bool;
      StopDrive : Bool;
      TargetPosition : Real;
      TargetVelocity : Real;
   END_VAR

   VAR
      state : Int := 0;
      previousMoveAbsolute : Bool;
      moveAbsoluteRise : Bool;
      positionError : Real;
   END_VAR

BEGIN
   moveAbsoluteRise := Command.MoveAbsolute AND NOT previousMoveAbsolute;
   previousMoveAbsolute := Command.MoveAbsolute;

   Status.Ready := DriveReady AND NOT DriveFault AND NOT Status.Error;
   Status.ActualPosition := ActualPosition;
   Status.State := state;
   StopDrive := FALSE;
   MovePositive := FALSE;
   MoveNegative := FALSE;
   EnableDrive := Command.Enable AND NOT Status.Error;
   TargetVelocity := Command.Velocity;

   IF Command.Reset THEN
      Status.Error := FALSE;
      Status.ErrorCode := W#16#0000;
      Status.Done := FALSE;
      state := 0;
   END_IF;

   IF DriveFault THEN
      Status.Error := TRUE;
      Status.ErrorCode := W#16#0100;
      state := 900;
   END_IF;

   IF Command.JogPositive AND PositiveLimit THEN
      Status.Error := TRUE;
      Status.ErrorCode := W#16#0201;
      state := 900;
   END_IF;

   IF Command.JogNegative AND NegativeLimit THEN
      Status.Error := TRUE;
      Status.ErrorCode := W#16#0202;
      state := 900;
   END_IF;

   CASE state OF
      0:
         Status.StateText := 'Idle';
         Status.Busy := FALSE;
         Status.Done := FALSE;
         Status.Moving := FALSE;
         Status.AtTarget := FALSE;
         StopDrive := TRUE;

         IF Status.Ready AND Command.Enable THEN
            state := 10;
         END_IF;

      10:
         Status.StateText := 'Enabled';
         Status.Enabled := TRUE;
         StopDrive := FALSE;

         IF NOT Command.Enable THEN
            state := 0;
         ELSIF Command.Stop THEN
            state := 80;
         ELSIF Command.JogPositive AND NOT PositiveLimit THEN
            state := 20;
         ELSIF Command.JogNegative AND NOT NegativeLimit THEN
            state := 30;
         ELSIF moveAbsoluteRise THEN
            TargetPosition := Command.TargetPosition;
            Status.TargetLatched := Command.TargetPosition;
            state := 40;
         END_IF;

      20:
         Status.StateText := 'Jog positive';
         Status.Busy := TRUE;
         Status.Moving := TRUE;
         MovePositive := TRUE;

         IF NOT Command.JogPositive OR Command.Stop OR PositiveLimit THEN
            state := 80;
         END_IF;

      30:
         Status.StateText := 'Jog negative';
         Status.Busy := TRUE;
         Status.Moving := TRUE;
         MoveNegative := TRUE;

         IF NOT Command.JogNegative OR Command.Stop OR NegativeLimit THEN
            state := 80;
         END_IF;

      40:
         Status.StateText := 'Move absolute';
         Status.Busy := TRUE;
         Status.Moving := TRUE;
         positionError := Status.TargetLatched - ActualPosition;

         IF ABS(positionError) <= 0.01 THEN
            Status.Done := TRUE;
            Status.AtTarget := TRUE;
            state := 10;
         ELSIF Command.Stop THEN
            state := 80;
         ELSIF positionError > 0.0 AND NOT PositiveLimit THEN
            MovePositive := TRUE;
         ELSIF positionError < 0.0 AND NOT NegativeLimit THEN
            MoveNegative := TRUE;
         ELSE
            Status.Error := TRUE;
            Status.ErrorCode := W#16#0300;
            state := 900;
         END_IF;

      80:
         Status.StateText := 'Stopping';
         StopDrive := TRUE;
         Status.Busy := FALSE;
         Status.Moving := FALSE;

         IF NOT Command.Stop THEN
            state := 10;
         END_IF;

      900:
         Status.StateText := 'Fault';
         StopDrive := TRUE;
         EnableDrive := FALSE;
         Status.Busy := FALSE;
         Status.Moving := FALSE;

      ELSE
         Status.Error := TRUE;
         Status.ErrorCode := W#16#FFFF;
         state := 900;
   END_CASE;
END_FUNCTION_BLOCK
""";
    }

    private static string BuildOb1CallExample(NormalizedPackRequest request)
    {
        return $"""
// Copy this call into OB1 or another cyclic block after importing the UDTs, DB, and FB.
// Replace the hardware feedback and command outputs with real project tags.

"FB_{request.AxisName}_Control"(
   Command := "DB_{request.AxisName}_Data".Command,
   ActualPosition := "HMI_{request.AxisName}_ActualPosition",
   DriveReady := "I_{request.AxisName}_DriveReady",
   DriveFault := "I_{request.AxisName}_DriveFault",
   PositiveLimit := "I_{request.AxisName}_PositiveLimit",
   NegativeLimit := "I_{request.AxisName}_NegativeLimit",
   Status => "DB_{request.AxisName}_Data".Status,
   EnableDrive => "Q_{request.AxisName}_EnableDrive",
   MovePositive => "Q_{request.AxisName}_MovePositive",
   MoveNegative => "Q_{request.AxisName}_MoveNegative",
   StopDrive => "Q_{request.AxisName}_StopDrive",
   TargetPosition => "HMI_{request.AxisName}_TargetLatched",
   TargetVelocity => "HMI_{request.AxisName}_TargetVelocity");
""";
    }

    private static string BuildTagTableCsv(NormalizedPackRequest request)
    {
        var rows = new[]
        {
            new[] { $"HMI_{request.AxisName}_Enable", "Bool", "", "HMI command: enable axis." },
            new[] { $"HMI_{request.AxisName}_Reset", "Bool", "", "HMI command: reset axis fault." },
            new[] { $"HMI_{request.AxisName}_JogPositive", "Bool", "", "HMI command: jog positive." },
            new[] { $"HMI_{request.AxisName}_JogNegative", "Bool", "", "HMI command: jog negative." },
            new[] { $"HMI_{request.AxisName}_MoveAbsolute", "Bool", "", "HMI command: move absolute." },
            new[] { $"HMI_{request.AxisName}_Stop", "Bool", "", "HMI command: controlled stop." },
            new[] { $"HMI_{request.AxisName}_TargetPosition", "Real", "", "HMI setpoint: target position." },
            new[] { $"HMI_{request.AxisName}_TargetVelocity", "Real", "", "HMI display: target velocity." },
            new[] { $"HMI_{request.AxisName}_ActualPosition", "Real", "", "HMI display: actual position." },
            new[] { $"I_{request.AxisName}_DriveReady", "Bool", "", "Drive ready feedback. Assign real input address manually." },
            new[] { $"I_{request.AxisName}_DriveFault", "Bool", "", "Drive fault feedback. Assign real input address manually." },
            new[] { $"I_{request.AxisName}_PositiveLimit", "Bool", "", "Positive limit feedback. Assign real input address manually." },
            new[] { $"I_{request.AxisName}_NegativeLimit", "Bool", "", "Negative limit feedback. Assign real input address manually." },
            new[] { $"Q_{request.AxisName}_EnableDrive", "Bool", "", "Drive enable output. Assign real output address manually." },
            new[] { $"Q_{request.AxisName}_MovePositive", "Bool", "", "Positive movement command. Assign real output address manually." },
            new[] { $"Q_{request.AxisName}_MoveNegative", "Bool", "", "Negative movement command. Assign real output address manually." },
            new[] { $"Q_{request.AxisName}_StopDrive", "Bool", "", "Drive stop output. Assign real output address manually." },
        };

        var sb = new StringBuilder();
        sb.AppendLine("Name,DataType,LogicalAddress,Comment");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return sb.ToString();
    }

    private static string BuildHmiPlan(NormalizedPackRequest request)
    {
        var audienceLine = request.UserProfile switch
        {
            "student" => "Keep the screen easy to explain in class and make every command visible during commissioning.",
            "plc_engineer" => "Keep the screen compact, diagnosable, and consistent with plant naming standards.",
            _ => "Keep the screen practical for one engineer using it during tests and iteration."
        };

        return $"""
# HMI Screen Plan: {request.AxisName}

Project: `{request.ProjectName}`
TIA Portal target: `{request.TiaVersion}`
Profile: `{request.UserProfile}`

{audienceLine}

## Screen

Name: `{request.AxisName}_Overview`

Main zones:

- Header with axis name, current state, and fault indicator.
- Command area with enable, jog positive, jog negative, move absolute, stop, and reset.
- Setpoint area with target position, velocity, acceleration, and deceleration.
- Feedback area with actual position, at-target state, busy/done/error flags, and state text.
- Diagnostics area with error code, drive-ready feedback, limit switches, and last operator action.

## HMI Tags

- `DB_{request.AxisName}_Data.Command.Enable`
- `DB_{request.AxisName}_Data.Command.Reset`
- `DB_{request.AxisName}_Data.Command.JogPositive`
- `DB_{request.AxisName}_Data.Command.JogNegative`
- `DB_{request.AxisName}_Data.Command.MoveAbsolute`
- `DB_{request.AxisName}_Data.Command.Stop`
- `DB_{request.AxisName}_Data.Command.TargetPosition`
- `DB_{request.AxisName}_Data.Command.Velocity`
- `DB_{request.AxisName}_Data.Status.Ready`
- `DB_{request.AxisName}_Data.Status.Busy`
- `DB_{request.AxisName}_Data.Status.Done`
- `DB_{request.AxisName}_Data.Status.Error`
- `DB_{request.AxisName}_Data.Status.ErrorCode`
- `DB_{request.AxisName}_Data.Status.ActualPosition`
- `DB_{request.AxisName}_Data.Status.StateText`

## Operator Behavior

- Use momentary buttons for jog, stop, reset, and move absolute.
- Use a maintained toggle or explicit on/off pair for enable.
- Disable move commands when `Status.Ready = FALSE`.
- Make stop visible and reachable without changing screen.
- Show fault state with error code and reset action, but do not hide diagnostics after reset.

## Alarms

- Axis drive fault: `Status.Error = TRUE` and `Status.ErrorCode = 16#0100`.
- Positive limit reached during positive command: `Status.ErrorCode = 16#0201`.
- Negative limit reached during negative command: `Status.ErrorCode = 16#0202`.
- Target unreachable or blocked by limit: `Status.ErrorCode = 16#0300`.

## Validation

- Test all buttons with outputs disconnected or in simulation first.
- Confirm jog commands are momentary.
- Confirm stop has priority over move and jog commands.
- Confirm limit-switch behavior before connecting real motion hardware.
""";
    }

    private static string BuildPracticeReport(NormalizedPackRequest request)
    {
        return $"""
# Engineering Report: {request.AxisName} Position-Control Starter Pack

## Purpose

This pack creates a conservative starter architecture for a single axis in TIA Portal. It separates operator commands, axis status, cyclic control logic, HMI planning, and manual import checks.

## Generated Artifacts

- Two UDTs define command and status structures.
- One DB stores shared command/status data.
- One FB implements a defensive state machine for enable, jog, absolute move, stop, and fault handling.
- One OB1 call example shows how to wire the block to tags.
- One tag-table CSV proposes names and data types.
- One HMI plan defines screens, tags, alarms, and validation.

## Design Notes

- Hardware addresses are intentionally blank in the CSV. A human must assign them according to the real PLC wiring.
- The FB does not download to hardware automatically.
- The generated code should be compiled in a project copy before it is used with real drives or motion hardware.
- This pack is intended for `{request.TiaVersion}`, but generated SCL must still be checked by TIA Portal because Siemens import syntax can vary by version and project settings.
""";
    }

    private static string BuildManualImportChecklist(NormalizedPackRequest request)
    {
        return $"""
# Manual Import Checklist

Use this when Openness is unavailable or when you want to review every action manually.

## Before Import

- Work on a copy of the TIA Portal project, not the original.
- Confirm the target project opens correctly in `{request.TiaVersion}`.
- Keep real PLC/drive hardware offline unless a responsible engineer approves testing.
- If possible, export an empty tag table from your TIA Portal version and compare its CSV/XML columns with `PLC_Tags_Suggested.csv`.

## Import Order

1. Import or create `01_UDT_AxisCommand.scl`.
2. Import or create `02_UDT_AxisStatus.scl`.
3. Import or create `03_DB_AxisData.scl`.
4. Import or create `04_FB_AxisControl.scl`.
5. Add the call from `05_OB1_Call_Example.scl` to OB1 or a cyclic block.
6. Import or manually create tags from `PLC_Tags_Suggested.csv`.
7. Build the HMI screen from `HMI_Screen_Plan.md`.
8. Compile PLC software and resolve diagnostics.

## Tests

- Enable false: outputs must remain off.
- Drive fault true: block must enter fault state.
- Jog positive with positive limit true: block must block movement and set fault.
- Jog negative with negative limit true: block must block movement and set fault.
- Stop command: movement outputs must turn off.
- Move absolute: state should return to enabled when actual position reaches target tolerance.

## Human Approval

Do not download to a real PLC or connect a real drive until the generated artifacts compile and have been reviewed against the lab hardware.
""";
    }

    private static string BuildTagTableReadme(NormalizedPackRequest request)
    {
        return $"""
# PLC Tag Table Notes

`PLC_Tags_Suggested.csv` is a neutral CSV with name, data type, address, and comment columns.

TIA Portal CSV/XML import schemas can vary by version and export settings. For best results in `{request.TiaVersion}`:

1. Create a small empty/manual tag table in TIA Portal.
2. Export it from TIA Portal.
3. Compare the exported columns with this generated CSV.
4. Rename/reorder columns if needed.
5. Import into a project copy and compile.

The generated file intentionally leaves hardware addresses blank.
""";
    }

    private static string BuildManifest(NormalizedPackRequest request, IReadOnlyList<GeneratedFileSummary> files)
    {
        var manifest = new
        {
            schema = "tia-portal-agentic-toolkit.import-pack.v1",
            packKind = "axis_position_control",
            generatedAtUtc = DateTimeOffset.UtcNow,
            request.ProjectName,
            request.AxisName,
            request.UserProfile,
            request.TiaVersion,
            files = files.Select(file => new
            {
                path = Path.GetFileName(file.Path),
                file.Kind,
                file.Purpose
            }),
            safety = new
            {
                modifiesProjectAutomatically = false,
                hardwareDownload = false,
                requiresHumanImport = true,
                requiresCompileBeforeUse = true
            }
        };

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }

    private static IReadOnlyList<string> BuildManualImportOrder() =>
    [
        "Import UDTs first.",
        "Import DB after UDTs.",
        "Import FB after DB and UDTs.",
        "Add OB1 call only after compile names are valid.",
        "Import or manually create tag table.",
        "Create HMI screen from plan.",
        "Compile and test offline before hardware."
    ];

    private static IReadOnlyList<string> BuildWarnings(NormalizedPackRequest request)
    {
        var warnings = new List<string>
        {
            "This pack does not modify a TIA Portal project automatically. Manual import or Openness is required.",
            "Do not assign generated outputs to real hardware without engineering review.",
            "Generated SCL is a starter template. Compile in TIA Portal and feed diagnostics back to Codex for correction."
        };

        if (request.UserProfile == "student")
        {
            warnings.Add("Student profile: use only in lab/project copies unless a teacher approves hardware testing.");
        }

        return warnings;
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

        if (char.IsDigit(candidate[0]))
        {
            candidate = "_" + candidate;
        }

        return candidate;
    }

    private static string NormalizeProfile(string? value)
    {
        var candidate = (value ?? "self").Trim().ToLowerInvariant().Replace("-", "_");
        return candidate switch
        {
            "student" or "alumno" or "alumna" => "student",
            "engineer" or "plc_engineer" or "ingeniero" or "ingeniera" => "plc_engineer",
            _ => "self"
        };
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

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private sealed record NormalizedPackRequest(
        string OutputFolder,
        string ProjectName,
        string AxisName,
        string UserProfile,
        string TiaVersion);
}
