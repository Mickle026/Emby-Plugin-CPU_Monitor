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
    public partial class CPUMonitorCore
    {
        [Route("/StorageSpaceByMick/GetStorageSpace", "GET", IsHidden = false, Summary = "Retrieves current storage space data")]
        [Authenticated(Roles = "Admin")]
        public class GetStorageSpace : IReturn<object>
        {
            public string Text { get; set; }
        }

        public object Get(GetStorageSpace request)
        {
            try
            {
                List<DiskSpaceInfo> diskSpaceList = GetDiskSpaceData();
                return JsonSerializer.SerializeToString(new { disks = diskSpaceList });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving storage space data: {ex.Message}");
                return new List<DiskSpaceInfo>();
            }
        }

        private List<DiskSpaceInfo> GetDiskSpaceData()
        {
            List<DiskSpaceInfo> diskSpaceList = new List<DiskSpaceInfo>();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                diskSpaceList = GetWindowsDiskSpace();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                diskSpaceList = GetLinuxDiskSpace();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                //diskSpaceList = GetMacDiskSpace();
            }
            return diskSpaceList;
        }

        /// <summary>
        /// Retrieves disk space information for Windows systems.
        /// </summary>
        /// <returns>
        /// Name  Total(GB)  Free(GB)
        /// C     500        120
        /// D     1000       450
        /// E     250        80
        /// </returns>
        /// 
        /// PS C:\Users\mickl> Get-PSDrive | Where-Object {$_.Used -ne $null} | Select-Object Name, @{Name="Total(GB)"; Expression={$_.Used + $_.Free}}, @{Name="Free(GB)"; Expression={$_.Free}}
        ///
        //// Name Total(GB)      Free(GB)
        /// ----      ---------      --------
        /// C      959390412800  732764909568
        /// D      480102051840  404663791616
        /// E      480101003264  381472813056
        /// F      999211573248  122920161280
        /// J     1947335331840 1155903430656
        /// Y    11992417959936  217044090880
        /// Z    17988626939904   21099343872
        private List<DiskSpaceInfo> GetWindowsDiskSpace()
        {
            List<DiskSpaceInfo> diskData = new List<DiskSpaceInfo>();

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command \"Get-PSDrive | Where-Object {$_.Used -ne $null} | Select-Object Name, @{Name='TotalGB'; Expression={$_.Used + $_.Free}}, @{Name='FreeGB'; Expression={$_.Free}}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string[] lines = output.Split('\n');
                foreach (var line in lines)
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        diskData.Add(new DiskSpaceInfo
                        {
                            DriveLetter = parts[0],
                            TotalGB = float.Parse(parts[1]),
                            FreeGB = float.Parse(parts[2])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving disk space via PowerShell: {ex.Message}");
            }

            return diskData;
        }

        /// <summary>
        /// Retrieves disk space information for Linux systems.
        /// </summary>
        /// <returns>
        /// Filesystem: /dev/sda1  Size: 500G  Used: 380G  Available: 120G  Usage: 76%  Mounted on: /
        /// Filesystem: /dev/sdb1 Size: 1T Used: 550G Available: 450G Usage: 55%  Mounted on: /mnt/data
        /// </returns>
        private static List<DiskSpaceInfo> GetLinuxDiskSpace()
        {
            List<DiskSpaceInfo> diskData = new List<DiskSpaceInfo>();

            Process process = new Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = "-c \"df -h\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string[] lines = output.Split('\n');

            foreach (var line in lines)
            {
                string[] values = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length >= 6 && values[0] != "Filesystem") // Skip header row
                {
                    diskData.Add(new DiskSpaceInfo
                    {
                        Filesystem = values[0],  // Drive name (e.g., /dev/sda1)
                        Size = values[1],        // Total size (e.g., 500G)
                        Used = values[2],        // Used space (e.g., 380G)
                        Available = values[3],   // Free space (e.g., 120G)
                        UsagePercentage = values[4], // Usage % (e.g., 76%)
                        MountedOn = values[5]    // Mount point (e.g., /)
                    });
                }
            }

            return diskData;
        }

        public class DiskSpaceInfo
        {
            public string Filesystem { get; set; }
            public string Size { get; set; }
            public string Used { get; set; }
            public string Available { get; set; }
            public string UsagePercentage { get; set; }
            public string MountedOn { get; set; }
            public string DriveLetter { get; set; } // For Windows
            public float TotalGB { get; set; } // For Windows
            public float FreeGB { get; set; } // For Windows
            public string DriveName { get; set; } // For Linux
            public string DriveSize { get; set; } // For Linux
            public string DriveUsed { get; set; } // For Linux
            public string DriveAvailable { get; set; } // For Linux
            public string DriveUsagePercentage { get; set; } // For Linux
            public string DriveMountedOn { get; set; } // For Linux
            public string DriveType { get; set; } // For Linux
            public string DriveMountOptions { get; set; } // For Linux
            public string DriveUUID { get; set; } // For Linux
            public string DriveLabel { get; set; } // For Linux
            public string DriveFileSystem { get; set; } // For Linux
            public string DriveInodes { get; set; } // For Linux
            public string DriveInodesUsed { get; set; } // For Linux
            public string DriveInodesFree { get; set; } // For Linux
            public string DriveInodesPercentage { get; set; } // For Linux
            public string DriveInodesMountedOn { get; set; } // For Linux
            public string DriveInodesType { get; set; } // For Linux
            public string DriveInodesMountOptions { get; set; } // For Linux
            public string DriveInodesUUID { get; set; } // For Linux
            public string DriveInodesLabel { get; set; } // For Linux
            public string DriveInodesFileSystem { get; set; } // For Linux
            public string DriveInodesSize { get; set; } // For Linux
            public string DriveInodesAvailable { get; set; } // For Linux
            public string DriveInodesUsagePercentage { get; set; } // For Linux


        }
    }
}
