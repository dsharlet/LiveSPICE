using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static new App Current { get { return (App)Application.Current; } }

        private Settings settings = new Settings();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            settings.Save();
            base.OnExit(e);
        }

        public List<string> Mru { get { return settings.Mru.ToList(); } set { settings.Mru = value.ToArray(); } }
        public void Used(string Filename)
        {
            List<string> mru = Mru;
            mru.Remove(Filename);
            mru.Insert(0, Filename);
            if (mru.Count > 20)
                mru.RemoveRange(20, mru.Count - 20);
            Mru = mru;
        }
        public void RemoveFromMru(string Filename)
        {
            List<string> mru = Mru;
            mru.Remove(Filename);
            Mru = mru;
        }


        void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textbox = (TextBox)sender;
            if (e.Key == Key.Enter & textbox.AcceptsReturn == false) 
            {
                BindingExpression be = textbox.GetBindingExpression(TextBox.TextProperty);
                be.UpdateSource();

                Window.GetWindow(textbox).Focus();
            }
        }
    }
}
