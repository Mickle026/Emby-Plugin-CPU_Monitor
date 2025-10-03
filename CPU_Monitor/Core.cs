using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;


namespace CPUMonitor
{
    public partial class CPUMonitorCore : IService
    {
        public class CPUCoreUsage
        {
            public int Core { get; set; }
            public float Usage { get; set; }
        }

        [Route("/CPUMonitorByMick/GetCPUState", "GET", IsHidden = false, Summary = "Retrieves current CPU usage data")]
        [Authenticated(Roles = "Admin")]
        public class GetCPUUsage2 : IReturn<object>
        {
            public string Text { get; set; }
        }

        public object Get(GetCPUUsage2 request)
        {
            
            //Logfile = $"CPUMonitor-{timestamp}.log";
            //Log(Logfile, "CPUMonitor Plugin started").Wait(); // Debugging
            try
            {
                List<CPUCoreUsage> coreUsageList = GetCpuUsageData();

                return JsonSerializer.SerializeToString(new { cores = coreUsageList });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving CPU usage data: {ex.Message}");
                return new List<CPUCoreUsage>();
            }
        }

        private List<CPUCoreUsage> GetCpuUsageData()
        {
            List<CPUCoreUsage> coreUsageList = new List<CPUCoreUsage>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                coreUsageList = GetWindowsCpuUsage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                coreUsageList = GetLinuxCpuUsage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                coreUsageList = GetMacCpuUsage();
            }

            return coreUsageList;
        }
        private List<CPUCoreUsage> GetWindowsCpuUsage()
        {
            List<CPUCoreUsage> usageData = new List<CPUCoreUsage>();

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command \"Get-Counter -Counter '\\Processor(*)\\% Processor Time' | Select-Object -ExpandProperty CounterSamples\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split('\n');
                foreach (var line in lines)
                {
                    //Log(Logfile, $"Processing Line: {line}").Wait(); // Debugging
                    if (line.ToLower().Contains("\\processor(") && !line.ToLower().Contains("_total")) // Ignore _Total, keep per-core
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5) // Ensure we have enough elements
                        {
                            string coreName = parts[3]; // InstanceName (core number)

                            float usage;
                            if (float.TryParse(parts[4], out usage)) // CookedValue (CPU usage)
                            {
                                usageData.Add(new CPUCoreUsage { Core = int.Parse(coreName), Usage = usage });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log(Logfile, $"Error retrieving CPU usage via PowerShell: {ex.Message}").Wait();
            }

            return usageData;
        }


        public static List<CPUCoreUsage> GetLinuxCpuUsage()
        {
            List<CPUCoreUsage> usageData = new List<CPUCoreUsage>();
            // /usr/bin/mpstat -P ALL 1 1 | awk 'NR>3 {print "Core " $2 ": " 100 - $NF "%"}'
            // Try mpstat first
            string mpstatOutput = LinuxRunCommand("which mpstat && mpstat -P ALL 1 1 | awk 'NR>3 {print \"Core \" $2 \": \" 100 - $NF \"%\"}'");
            if (!string.IsNullOrWhiteSpace(mpstatOutput) && mpstatOutput.Contains("CPU"))
            {
                usageData = ParseMpstat(mpstatOutput);
            }
            else
            {
                // Fallback to htop
                //string htopOutput = LinuxRunCommand("htop -d 1 -b | head -20"); // Run htop in batch mode
                string htopOutput = LinuxRunCommand("htop -d 1 -b | grep \"CPU\" | awk '{print NR-1, $NF}'");

                usageData = ParseHtop(htopOutput);
            }
            
            return usageData;
        }
        private static List<CPUCoreUsage> ParseMpstat(string output)
        {
            List<CPUCoreUsage> usageData = new List<CPUCoreUsage>();
            string[] lines = output.Split('\n');

            foreach (var line in lines)
            {
                string[] values = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Ensure we have enough columns and skip header rows
                if (values.Length >= 10 && values[0] != "CPU" && values[0] != "all")
                {
                    if (int.TryParse(values[1], out int core))  // Extract core index correctly
                    {
                        if (float.TryParse(values[9], out float idle))  // Idle percentage is at index 9
                        {
                            float usage = 100 - idle;  // Calculate CPU usage
                            usageData.Add(new CPUCoreUsage { Core = core, Usage = usage });
                        }
                    }
                }
            }
            return usageData;
        }
        private static List<CPUCoreUsage> ParseHtop(string output)
        {
            List<CPUCoreUsage> usageData = new List<CPUCoreUsage>();
            string[] lines = output.Split('\n');

            foreach (var line in lines)
            {
                string[] values = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length >= 2 && values[0] != "CPU" && values[0] != "all")
                {
                    values[1] = values[1].Replace(":", ""); // Clean up the core index
                    if (int.TryParse(values[1], out int core))  // Extract core index correctly
                    {
                        values[2] = values[2].Replace("%", ""); // Clean up the core index
                        if (float.TryParse(values[2], out float usage))  // Idle percentage is at index 9
                        {
                            usageData.Add(new CPUCoreUsage { Core = core, Usage = usage });
                        }
                    }
                }
            }

            return usageData;
        }

        private static string LinuxRunCommand(string command)
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return result;
        }
        // Helper method to execute shell commands
        private string WindowsRunCommand(string fileName, string arguments)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running command ({fileName} {arguments}): {ex.Message}");
                return string.Empty;
            }
        }
        // The following method is causing the CS1022 error:
        // The error message indicates that there is an unexpected end of file or a missing closing brace.
        // This usually happens when there is a mismatch in the number of opening and closing braces.
        // The issue is caused by an extra closing brace '}' at the end of the file.
        // This brace does not correspond to any opening brace and is causing the CS1022 error.
        // The fix is to remove the extra closing brace.
        private List<CPUCoreUsage> GetMacCpuUsage()
        {
            List<CPUCoreUsage> usageData = new List<CPUCoreUsage>();

            Process process = new Process();
            process.StartInfo.FileName = "top";
            process.StartInfo.Arguments = "-l 1 | grep 'CPU usage'";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] parts = output.Split(',');
            foreach (var part in parts)
            {
                if (part.Contains("user") || part.Contains("sys"))
                {
                    string[] elements = part.Split('%');

                    // Replace `.Last()` with manual indexing
                    string[] splitParts = elements[0].Trim().Split(' ');
                    float usage = float.Parse(splitParts[splitParts.Length - 1]); // Get the last element manually

                    usageData.Add(new CPUCoreUsage { Core = usageData.Count, Usage = usage });
                }
            }

            return usageData;
        }
    }
}


