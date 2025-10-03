using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CPUMonitor
{
    public partial class CPUMonitorCore
    {

        [Route("/StorageIOByMick/GetStorageIO", "GET", IsHidden = false, Summary = "Retrieves current storage I/O data")]
        [Authenticated(Roles = "Admin")]
        public class GetStorageIO : IReturn<object>
        {
            public string Text { get; set; }
        }
        public object Get(GetStorageIO request)
        {
            try
            {
                List<StorageIOUsage> storageIOList = GetStorageIOData();
                return JsonSerializer.SerializeToString(new { storageIO = storageIOList });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving storage I/O data: {ex.Message}");
                return new List<StorageIOUsage>();
            }
        }
        private List<StorageIOUsage> GetStorageIOData()
        {
            List<StorageIOUsage> storageIOList = new List<StorageIOUsage>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                storageIOList = GetWindowsStorageIO();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                //storageIOList = GetLinuxStorageIO();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                //storageIOList = GetMacStorageIO();
            }
            return storageIOList;
        }
        /// <summary>
        /// `GetWindowsStorageIO` retrieves the storage I/O data for Windows.
        /// </summary>
        /// <returns>
        /// A list of `StorageIOUsage` objects containing disk name and transfers per second.
        /// 
        /// </returns>
        private List<StorageIOUsage> GetWindowsStorageIO()
        {
            List<StorageIOUsage> usageData = new List<StorageIOUsage>();

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command \"Get-Counter '\\PhysicalDisk(*)\\Disk Transfers/sec' | Select-Object -ExpandProperty CounterSamples\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.ToLower().Contains("\\physicaldisk(")) // Filter disk I/O data
                    {
                        string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            string diskName = parts[3];  // Disk identifier
                            float ioRate;
                            if (float.TryParse(parts[4], out ioRate))
                            {
                                usageData.Add(new StorageIOUsage { DiskName = diskName, TransfersPerSecond = ioRate });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving storage I/O via PowerShell: {ex.Message}");
            }

            return usageData;
        }

        public class StorageIOUsage
        {
            public string DiskName { get; set; }
            public float TransfersPerSecond { get; set; }
        }
    }
}
