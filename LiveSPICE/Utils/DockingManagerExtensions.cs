using AvalonDock;
using AvalonDock.Layout.Serialization;
using System.Text;
using System.Xml;

namespace LiveSPICE
{
    static class DockingManagerExtensions
    {
        public static string SaveLayout(this DockingManager This)
        {
            var serializer = new XmlLayoutSerializer(This);
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
                var serializer = new XmlLayoutSerializer(This);
                serializer.Deserialize(XmlReader.Create(new System.IO.StringReader(Config)));
            }
        }
    }
}
