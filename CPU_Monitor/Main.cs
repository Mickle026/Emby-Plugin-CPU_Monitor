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
        private IJsonSerializer JsonSerializer { get; }
        private PerformanceCounter[] cpuCounters;
        private PerformanceCounter totalCpuCounter;
        private readonly IConfigurationManager config;
        string Logfile = "CPUMonitor.log";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"); // Avoid invalid characters

        public CPUMonitorCore(IJsonSerializer json, IConfigurationManager configuration)
        {
            JsonSerializer = json ?? throw new ArgumentNullException(nameof(json)); // Ensure Serializer isn't null
            config = configuration ?? throw new ArgumentNullException(nameof(configuration)); // Ensure Configuration isn't null
        }
    }
}
