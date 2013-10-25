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
        public Settings Settings { get { return settings; } }

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
