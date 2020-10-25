using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace LiveSPICE
{
    public class MruMenuItem : MenuItem
    {
        public MruMenuItem()
        {
            // Add a dummy menu item so the sub menu exists.
            Items.Add(new MenuItem());
        }

        private List<RoutedEventHandler> mruClick = new List<RoutedEventHandler>();
        public event RoutedEventHandler MruClick
        {
            add { mruClick.Add(value); }
            remove { mruClick.Remove(value); }
        }

        protected override void OnSubmenuOpened(RoutedEventArgs e)
        {
            Items.Clear();
            string[] mru = App.Current.Settings.Mru;

            if (mru.Length == 0)
            {
                Items.Add(new MenuItem() { Header = "Empty" });
                return;
            }

            for (int i = 0; i < mru.Length && i < 10; ++i)
            {
                MenuItem item = new MenuItem()
                {
                    ToolTip = mru[i],
                    Header = CompactPath(mru[i], 50),
                    Tag = mru[i],
                };

                foreach (RoutedEventHandler j in mruClick)
                    item.Click += j;

                Items.Add(item);
            }
        }

        public static string CompactPath(string path, int length)
        {
            List<string> paths = path.Split('\\').ToList();
            for (int i = 1; i < paths.Count - 1 && paths.Sum(j => j.Length) > length; ++i)
            {
                paths[i] = "...";
                for (int j = 0; j < paths.Count - 1; ++j)
                    if (paths[j] == "..." && paths[j + 1] == "...")
                        paths.RemoveAt(j);
            }
            return String.Join("\\", paths);
        }
    }
}
