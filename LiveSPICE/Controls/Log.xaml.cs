using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LiveSPICE
{
    public enum LogType
    {
        Error,
        Warning,
        Info,
    };

    public interface ILog
    {
        void Begin();
        void Write(LogType Type, string Message);
        bool End();
    }

    /// <summary>
    /// Interaction logic for Output.xaml
    /// </summary>
    public partial class Log : UserControl, ILog
    {
        private static Log single;
        public static Log Instance { get { return single; } }

        public Log()
        {
            InitializeComponent();

            if (single != null)
                throw new InvalidOperationException("Multiple instances of Output");
            single = this;
        }

        public void Clear()
        {
            text.Text = "";
        }

        private bool error;
        public void Begin() { Clear(); error = false; }
        public void Write(LogType Type, string Message)
        {
            text.AppendText(Type.ToString() + ": " + Message + "\r\n");
            error = error || Type == LogType.Error;
        }
        public bool End() { return !error; }
    }
}
