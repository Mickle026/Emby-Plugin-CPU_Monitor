using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CPUMonitor
{

    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IHasThumbImage
    {

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IFileSystem fileSystem, ILogger logger)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

        private readonly Guid _id = new Guid("EBAB59A3-8C08-4A27-852B-998319387B21");
        public override Guid Id
        {
            get { return _id; }
        }

        public override string Name => "CPU Monitor";
        public Stream GetThumbImage()
        {
            var type = this.GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }


        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "CPUMonitor",
                    EmbeddedResourcePath = GetType().Namespace + ".CPUMonitor.html"
                },
                new PluginPageInfo
                {
                    Name = "CPUMonitor.js",
                    EmbeddedResourcePath = GetType().Namespace + ".CPUMonitor.js"
                }
            };
        }
    }
}




