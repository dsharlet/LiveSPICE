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
    /// <summary>
    /// Interaction logic for Output.xaml
    /// </summary>
    public partial class Log : UserControl, Circuit.ILog
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

        public void WriteLine(Circuit.MessageType Type, string Message, params object[] Format)
        {
            if (Type == Circuit.MessageType.Verbose)
                return;
            if (Type != Circuit.MessageType.Info)
                text.AppendText("[" + Type.ToString() + "]");
            text.AppendText(Message + "\r\n");
            text.ScrollToEnd();
        }

        void Circuit.ILog.WriteLine(Circuit.MessageType Type, string Message, params object[] Format)
        {
            WriteLine(Type, Message, Format);
        }
    }
}
