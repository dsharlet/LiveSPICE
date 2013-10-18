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
        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));
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
