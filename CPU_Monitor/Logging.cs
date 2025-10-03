using MediaBrowser.Common.Configuration;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CPUMonitor
{
    public partial class CPUMonitorCore
    {
        
        public async Task Log(string logname, string message)
        {

            await Task.Run(() =>
            {
                try
                {
                    string logFilePath = Path.Combine(config.CommonApplicationPaths.LogDirectoryPath, logname);
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine($"{DateTime.Now}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing to log file: {ex.Message}");
                }
            });
        }

    }
}

