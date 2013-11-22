using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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

        public new MainWindow MainWindow { get { return (MainWindow)base.MainWindow; } }

        private Settings settings = new Settings();
        public Settings Settings { get { return settings; } }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("clearsettings"))
                settings.Reset();

            Dispatcher.UnhandledException += Dispatcher_UnhandledException;

            base.OnStartup(e);
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.KeyDownEvent, new KeyEventHandler(TextBox_KeyDown));
            EventManager.RegisterClassHandler(typeof(ComboBox), ComboBox.KeyDownEvent, new KeyEventHandler(ComboBox_KeyDown));

            LoadAssemblies();
        }

        void Dispatcher_UnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            if (UnhandledException.Show(e.Exception))
                e.Handled = true;
        }

        public DirectoryInfo UserDocuments 
        { 
            get 
            {
                string docs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LiveSPICE");
                return Directory.CreateDirectory(docs);
            } 
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
                if (be != null)
                    be.UpdateSource();

                Window.GetWindow(textbox).Focus();
            }
        }

        void ComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;
            if (e.Key == Key.Enter)
            {
                BindingExpression be = combo.GetBindingExpression(ComboBox.TextProperty);
                if (be != null)
                    be.UpdateSource();

                Window.GetWindow(combo).Focus();
            }
        }

        public static void LoadAssemblies()
        {
            foreach (string dll in Directory.GetFiles(System.AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
            {
                try
                {
                    Assembly.LoadFile(dll);
                }
                catch (FileLoadException) { } 
                catch (BadImageFormatException) { }
            }
        }
    }
}
