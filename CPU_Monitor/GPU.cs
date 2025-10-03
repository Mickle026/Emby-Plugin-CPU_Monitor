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
        [Route("/CPUMonitorByMick/GetGPUUsage", "GET", IsHidden = false, Summary = "Retrieves current GPU usage data")]
        [Authenticated(Roles = "Admin")]
        public class GetGPUUsage : IReturn<object>
        {
            public string Text { get; set; }
        }

        public object Get(GetGPUUsage request)
        {

            //Logfile = $"CPUMonitor-{timestamp}.log";
            //Log(Logfile, "CPUMonitor Plugin started").Wait(); // Debugging
            try
            {
                List<GPUUsage> coreUsageList = GetGPUUsageData();

                return JsonSerializer.SerializeToString(new { cores = coreUsageList });

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving GPU usage data: {ex.Message}");
                return new List<GPUUsage>();
            }
        }

        private List<GPUUsage> GetGPUUsageData()
        {
            List<GPUUsage> coreUsageList = new List<GPUUsage>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                coreUsageList = GetWindowsGpuUsage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //coreUsageList = GetLinuxCpuUsage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
               // coreUsageList = GetMacCpuUsage();
            }

            return coreUsageList;
        }
        private List<GPUUsage> GetWindowsGpuUsage()
        {
            List<GPUUsage> usageData = new List<GPUUsage>();

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command \"Get-Counter '\\GPU Engine(*)\\Utilization Percentage' | Select-Object -ExpandProperty CounterSamples\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.ToLower().Contains("\\gpu engine(")) // Filter relevant GPU lines
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5) // Ensure proper format
                        {
                            string engineName = parts[3]; // Engine name (e.g., Render or 3D)
                            float usage;
                            if (float.TryParse(parts[4], out usage)) // Extract usage percentage
                            {
                                usageData.Add(new GPUUsage { Engine = engineName, Usage = usage });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving GPU usage via PowerShell: {ex.Message}");
            }

            return usageData;
        }

        public class GPUUsage
        {
            public string Engine { get; set; }
            public float Usage { get; set; }
        }
    }
}
