using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xceed.Wpf.AvalonDock;

namespace LiveSPICE
{
    static class DockingManagerExtensions
    {
        public static void SaveLayout(this DockingManager This, string Config)
        {
            Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(This);
            serializer.Serialize(Config);

            // http://avalondock.codeplex.com/discussions/400644
            XmlDocument configDoc = new XmlDocument();
            configDoc.Load(Config);
            XmlNodeList projectNodes = configDoc.GetElementsByTagName("LayoutDocument");
            for (int i = projectNodes.Count - 1; i > -1; i--)
            {
                projectNodes[i].ParentNode.RemoveChild(projectNodes[i]);
            }
            configDoc.Save(Config);
        }

        public static void LoadLayout(this DockingManager This, string Config)
        {
            Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer serializer = new Xceed.Wpf.AvalonDock.Layout.Serialization.XmlLayoutSerializer(This);
            serializer.Deserialize(Config);
        }
    }
}
