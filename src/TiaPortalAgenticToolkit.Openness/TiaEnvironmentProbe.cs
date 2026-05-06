using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;

namespace TiaPortalAgenticToolkit.Openness;

public sealed record TiaEnvironmentStatus(
    string ToolkitVersion,
    bool IsWindows,
    bool IsUserInOpennessGroup,
    string CurrentUser,
    IReadOnlyList<OpennessVersionInfo> OpennessVersions,
    IReadOnlyList<string> EngineeringAssemblyCandidates,
    IReadOnlyList<RunningPortalProcess> RunningPortalProcesses,
    IReadOnlyList<string> Warnings);

public sealed record TiaCapabilities(
    string Mode,
    string? RecommendedVersion,
    IReadOnlyList<string> TiaPortalVersions,
    bool CanUseOpenness,
    bool CanUseExports,
    bool CanProvideAdvisory,
    bool UserInOpennessGroup,
    bool OpennessInstalled,
    bool EngineeringAssembliesFound,
    string NextAction,
    TiaEnvironmentStatus Status);

public sealed record OpennessVersionInfo(string Version, string RegistryPath, IReadOnlyDictionary<string, string> Values);

public sealed record RunningPortalProcess(int Id, string ProcessName, string? MainWindowTitle, string? FileName);

public sealed class TiaEnvironmentProbe
{
    private const string OpennessGroupName = "Siemens TIA Openness";

    public TiaEnvironmentStatus GetStatus()
    {
        var warnings = new List<string>();
        var isWindows = OperatingSystem.IsWindows();
        var currentUser = WindowsIdentity.GetCurrent()?.Name ?? Environment.UserName;
        var versions = isWindows ? ReadOpennessRegistry(warnings) : [];
        var assemblies = isWindows ? FindEngineeringAssemblies(versions) : [];
        var processes = isWindows ? FindRunningPortalProcesses() : [];
        var inGroup = isWindows && IsInOpennessGroup(warnings);

        if (!isWindows)
        {
            warnings.Add("TIA Portal Openness is Windows-only.");
        }

        if (isWindows && versions.Count == 0)
        {
            warnings.Add("No TIA Portal Openness registry entries were found under HKLM\\SOFTWARE\\Siemens\\Automation\\Openness.");
        }

        if (isWindows && !inGroup)
        {
            warnings.Add("The current user does not appear to be in the local Windows group 'Siemens TIA Openness'. Add the user and sign out/in before using Openness.");
        }

        return new TiaEnvironmentStatus(
            ToolkitVersion: ReadToolkitVersion(),
            IsWindows: isWindows,
            IsUserInOpennessGroup: inGroup,
            CurrentUser: currentUser,
            OpennessVersions: versions,
            EngineeringAssemblyCandidates: assemblies,
            RunningPortalProcesses: processes,
            Warnings: warnings);
    }

    public TiaCapabilities GetCapabilities()
    {
        var status = GetStatus();
        var versions = status.OpennessVersions
            .Select(v => NormalizeVersion(v.Version))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var opennessInstalled = status.OpennessVersions.Count > 0;
        var assembliesFound = status.EngineeringAssemblyCandidates.Count > 0;
        var canUseOpenness = status.IsWindows && opennessInstalled && assembliesFound && status.IsUserInOpennessGroup;

        string mode;
        string nextAction;
        if (canUseOpenness)
        {
            mode = "full_agentic";
            nextAction = "Open TIA Portal and a non-production project, then use read-only project inspection tools first.";
        }
        else if (status.IsWindows && (opennessInstalled || assembliesFound))
        {
            mode = "semi_agentic";
            nextAction = status.IsUserInOpennessGroup
                ? "Openness is partially detected but not fully usable. Continue with exported files while checking installation paths."
                : "Continue with exported files, or ask an administrator to add the user to Siemens TIA Openness for full agentic mode.";
        }
        else
        {
            mode = "advisory";
            nextAction = "Provide exported XML/SCL/CSV/Excel artifacts for semi-agentic analysis, or install TIA Portal Openness for full agentic mode.";
        }

        return new TiaCapabilities(
            Mode: mode,
            RecommendedVersion: versions.LastOrDefault(),
            TiaPortalVersions: versions,
            CanUseOpenness: canUseOpenness,
            CanUseExports: true,
            CanProvideAdvisory: true,
            UserInOpennessGroup: status.IsUserInOpennessGroup,
            OpennessInstalled: opennessInstalled,
            EngineeringAssembliesFound: assembliesFound,
            NextAction: nextAction,
            Status: status);
    }

    private static string NormalizeVersion(string version)
    {
        if (version.StartsWith("V", StringComparison.OrdinalIgnoreCase))
        {
            return version.ToUpperInvariant();
        }

        return version.Split('.')[0] is { Length: > 0 } major ? "V" + major : version;
    }

    private static string ReadToolkitVersion()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 6; i++)
        {
            var path = Path.Combine(dir, "VERSION");
            if (File.Exists(path))
            {
                return File.ReadAllText(path).Trim();
            }

            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                break;
            }

            dir = parent.FullName;
        }

        return "0.1.0";
    }

    private static bool IsInOpennessGroup(List<string> warnings)
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(OpennessGroupName);
        }
        catch (Exception ex)
        {
            warnings.Add($"Could not check Windows group membership: {ex.Message}");
            return false;
        }
    }

    private static List<OpennessVersionInfo> ReadOpennessRegistry(List<string> warnings)
    {
        var results = new List<OpennessVersionInfo>();
        try
        {
            using var root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Siemens\Automation\Openness");
            if (root is null)
            {
                return results;
            }

            foreach (var subKeyName in root.GetSubKeyNames())
            {
                using var sub = root.OpenSubKey(subKeyName);
                if (sub is null)
                {
                    continue;
                }

                var values = sub.GetValueNames()
                    .ToDictionary(name => name, name => Convert.ToString(sub.GetValue(name)) ?? string.Empty);

                results.Add(new OpennessVersionInfo(
                    Version: subKeyName,
                    RegistryPath: $@"HKEY_LOCAL_MACHINE\SOFTWARE\Siemens\Automation\Openness\{subKeyName}",
                    Values: values));
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Could not read Openness registry keys: {ex.Message}");
        }

        return results;
    }

    private static List<string> FindEngineeringAssemblies(IEnumerable<OpennessVersionInfo> versions)
    {
        var candidates = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var version in versions)
        {
            foreach (var value in version.Values.Values)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                AddAssemblyCandidate(candidates, value);
                if (Directory.Exists(value))
                {
                    foreach (var path in Directory.EnumerateFiles(value, "Siemens.Engineering.dll", SearchOption.AllDirectories))
                    {
                        candidates.Add(path);
                    }
                }
            }
        }

        foreach (var root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) })
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var siemensRoot = Path.Combine(root, "Siemens");
            if (!Directory.Exists(siemensRoot))
            {
                continue;
            }

            try
            {
                foreach (var path in Directory.EnumerateFiles(siemensRoot, "Siemens.Engineering.dll", SearchOption.AllDirectories))
                {
                    candidates.Add(path);
                }
            }
            catch
            {
                // Ignore inaccessible installation subfolders.
            }
        }

        return candidates.ToList();
    }

    private static void AddAssemblyCandidate(ISet<string> candidates, string value)
    {
        if (File.Exists(value) && Path.GetFileName(value).Equals("Siemens.Engineering.dll", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(value);
            return;
        }

        if (Directory.Exists(value))
        {
            var direct = Path.Combine(value, "Siemens.Engineering.dll");
            if (File.Exists(direct))
            {
                candidates.Add(direct);
            }
        }
    }

    private static List<RunningPortalProcess> FindRunningPortalProcesses()
    {
        var names = new[] { "Siemens.Automation.Portal" };
        var results = new List<RunningPortalProcess>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                var name = process.ProcessName;
                string? fileName = null;
                try
                {
                    fileName = process.MainModule?.FileName;
                }
                catch
                {
                    // Process module paths may be inaccessible without elevation.
                }

                var looksLikePortal = names.Any(candidate => name.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                    || (fileName?.Contains(@"\Siemens\", StringComparison.OrdinalIgnoreCase) == true
                        && fileName.Contains("Portal", StringComparison.OrdinalIgnoreCase));

                if (!looksLikePortal)
                {
                    continue;
                }

                results.Add(new RunningPortalProcess(process.Id, name, process.MainWindowTitle, fileName));
            }
            catch
            {
                // Ignore processes that disappear during enumeration.
            }
        }

        return results.OrderBy(p => p.ProcessName).ThenBy(p => p.Id).ToList();
    }
}
