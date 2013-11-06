using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public partial class Log : UserControl, Circuit.ILog, INotifyPropertyChanged
    {
        public Log()
        {
            InitializeComponent();
        }

        public void Clear() { text.Text = ""; }
        public void Clear_Click(object sender, EventArgs e) { Clear(); }

        private Circuit.MessageType verbosity = Circuit.MessageType.Info;
        public string Verbosity 
        {
            get { return verbosity.ToString(); } 
            set { verbosity = (Circuit.MessageType)Enum.Parse(typeof(Circuit.MessageType), value); NotifyChanged("Verbosity"); } 
        }

        public void WriteLine(Circuit.MessageType Type, string Message, params object[] Format)
        {
            if (Type > verbosity)
                return;
            Dispatcher.InvokeAsync(() =>
                {
                    bool atEnd = text.VerticalOffset + text.ViewportHeight >= text.ExtentHeight - 1.0;
                    if (Type != Circuit.MessageType.Info)
                        text.AppendText("[" + Type.ToString() + "] ");
                    text.AppendText(Message + "\r\n");
                    if (atEnd)
                        text.ScrollToEnd();
                });
        }

        void Circuit.ILog.WriteLine(Circuit.MessageType Type, string Message, params object[] Format)
        {
            WriteLine(Type, String.Format(Message, Format));
        }

        // INotifyPropertyChanged.
        private void NotifyChanged(string p)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(p));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
