using System;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for ProgressReport.xaml
    /// </summary>
    public partial class BackgroundProcess : Window
    {
        private bool run;
        private BackgroundWorker worker;

        public BackgroundProcess(string Task, Action<BackgroundWorker> Callback)
        {
            InitializeComponent();
            task.Text = Task;
            cancel.Click += (o, e) => worker.CancelAsync();

            worker = new BackgroundWorker();
            worker.DoWork += (o, e) => Callback(worker);
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (o, e) => progress.Value = e.ProgressPercentage;
            worker.RunWorkerCompleted += (o, e) => Close();

            worker.RunWorkerAsync();
        }

        public static void Run(string Task, Action<BackgroundWorker> Callback)
        {
            BackgroundProcess process = new BackgroundProcess(Task, Callback);
            process.ShowDialog();
        }
    }
}
