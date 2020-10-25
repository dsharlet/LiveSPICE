using System.Text;
using System.Xml;
using Xceed.Wpf.AvalonDock;

namespace LiveSPICE
{
    static class DockingManagerExtensions
    {
        public static string SaveLayout(this DockingManager This)
        {
            Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(This);
            StringBuilder config = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(config))
            {
                serializer.Serialize(writer);
            }

            // http://avalondock.codeplex.com/discussions/400644
            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(XmlReader.Create(new System.IO.StringReader(config.ToString())));
            XmlNodeList projectNodes = configDoc.GetElementsByTagName("LayoutDocument");
            for (int i = projectNodes.Count - 1; i > -1; i--)
                projectNodes[i].ParentNode.RemoveChild(projectNodes[i]);

            config = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(config))
            {
                configDoc.WriteTo(writer);
            }
            return config.ToString();
        }

        public static void LoadLayout(this DockingManager This, string Config)
        {
            if (Config != "")
            {
                Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(This);
                serializer.Deserialize(XmlReader.Create(new System.IO.StringReader(Config)));
            }
        }
    }
}
