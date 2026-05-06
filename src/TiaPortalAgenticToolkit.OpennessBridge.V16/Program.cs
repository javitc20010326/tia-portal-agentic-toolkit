using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using Microsoft.Win32;

namespace TiaPortalAgenticToolkit.OpennessBridge.V16
{
    internal static class Program
    {
        private const string OpennessGroupName = "Siemens TIA Openness";

        private static int Main(string[] args)
        {
            try
            {
                var command = args.Length > 0 ? args[0] : "status";
                switch (command)
                {
                    case "status":
                        PrintStatus();
                        return 0;
                    case "assemblies":
                        foreach (var path in FindEngineeringAssemblies())
                        {
                            Console.WriteLine(path);
                        }
                        return 0;
                    default:
                        Console.Error.WriteLine("Unknown command. Supported: status, assemblies");
                        return 2;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static void PrintStatus()
        {
            Console.WriteLine("{");
            Console.WriteLine("  \"bridge\": \"TiaPortalAgenticToolkit.OpennessBridge.V16\",");
            Console.WriteLine("  \"user\": " + Json(Environment.UserDomainName + "\\" + Environment.UserName) + ",");
            Console.WriteLine("  \"isUserInOpennessGroup\": " + IsInOpennessGroup().ToString().ToLowerInvariant() + ",");
            Console.WriteLine("  \"opennessRegistryKeys\": [");
            var keys = ReadOpennessRegistryKeys().ToList();
            for (var i = 0; i < keys.Count; i++)
            {
                Console.Write("    " + Json(keys[i]));
                Console.WriteLine(i == keys.Count - 1 ? "" : ",");
            }
            Console.WriteLine("  ],");
            Console.WriteLine("  \"engineeringAssemblies\": [");
            var assemblies = FindEngineeringAssemblies().ToList();
            for (var i = 0; i < assemblies.Count; i++)
            {
                Console.Write("    " + Json(assemblies[i]));
                Console.WriteLine(i == assemblies.Count - 1 ? "" : ",");
            }
            Console.WriteLine("  ],");
            Console.WriteLine("  \"runningPortalProcesses\": [");
            var processes = FindPortalProcesses().ToList();
            for (var i = 0; i < processes.Count; i++)
            {
                Console.Write("    " + Json(processes[i]));
                Console.WriteLine(i == processes.Count - 1 ? "" : ",");
            }
            Console.WriteLine("  ]");
            Console.WriteLine("}");
        }

        private static bool IsInOpennessGroup()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                return new WindowsPrincipal(identity).IsInRole(OpennessGroupName);
            }
        }

        private static IEnumerable<string> ReadOpennessRegistryKeys()
        {
            using (var root = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Siemens\Automation\Openness"))
            {
                if (root == null)
                {
                    yield break;
                }

                foreach (var subKeyName in root.GetSubKeyNames())
                {
                    yield return @"HKEY_LOCAL_MACHINE\SOFTWARE\Siemens\Automation\Openness\" + subKeyName;
                }
            }
        }

        private static IEnumerable<string> FindEngineeringAssemblies()
        {
            var roots = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };

            foreach (var root in roots.Where(Directory.Exists))
            {
                var siemens = Path.Combine(root, "Siemens");
                if (!Directory.Exists(siemens))
                {
                    continue;
                }

                foreach (var path in SafeEnumerateFiles(siemens, "Siemens.Engineering.dll"))
                {
                    yield return path;
                }

                foreach (var path in SafeEnumerateFiles(siemens, "Siemens.Engineering.Base.dll"))
                {
                    yield return path;
                }
            }
        }

        private static IEnumerable<string> FindPortalProcesses()
        {
            foreach (var process in Process.GetProcesses())
            {
                string fileName = null;
                try
                {
                    fileName = process.MainModule != null ? process.MainModule.FileName : null;
                }
                catch
                {
                    // Ignore inaccessible processes.
                }

                var name = process.ProcessName ?? "";
                var isPortal = name.IndexOf("Siemens.Automation.Portal", StringComparison.OrdinalIgnoreCase) >= 0
                    || (fileName != null
                        && fileName.IndexOf(@"\Siemens\", StringComparison.OrdinalIgnoreCase) >= 0
                        && fileName.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0);

                if (isPortal)
                {
                    yield return process.Id + ":" + name + ":" + fileName;
                }
            }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string root, string pattern)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                string[] files;
                string[] dirs;
                try
                {
                    files = Directory.GetFiles(current, pattern);
                    dirs = Directory.GetDirectories(current);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                foreach (var dir in dirs)
                {
                    pending.Push(dir);
                }
            }
        }

        private static string Json(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }
    }
}
