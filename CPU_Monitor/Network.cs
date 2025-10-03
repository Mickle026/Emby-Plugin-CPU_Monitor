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
        private List<NetworkAdapterUsage> GetWindowsNetworkUsage()
        {
            List<NetworkAdapterUsage> usageData = new List<NetworkAdapterUsage>();

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command \"Get-Counter '\\Network Interface(*)\\Bytes Total/sec' | Select-Object -ExpandProperty CounterSamples\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.ToLower().Contains("\\network interface(")) // Filter network adapter data
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            string adapterName = parts[3];  // Network adapter name
                            float bytesPerSec;
                            if (float.TryParse(parts[4], out bytesPerSec))
                            {
                                usageData.Add(new NetworkAdapterUsage { AdapterName = adapterName, BytesPerSecond = bytesPerSec });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving network usage via PowerShell: {ex.Message}");
            }

            return usageData;
        }

        public class NetworkAdapterUsage
        {
            public string AdapterName { get; set; }
            public float BytesPerSecond { get; set; }
        }
    }
}
