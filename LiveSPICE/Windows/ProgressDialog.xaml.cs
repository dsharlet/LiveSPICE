using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public partial class ProgressDialog : Window
    {
        private BackgroundWorker worker;

        public ProgressDialog(string Task, Action<Func<bool>, Action<double>> Callback, bool SupportsCancel, bool SupportsProgress)
        {
            InitializeComponent();
            task.Text = Task;
            if (SupportsCancel)
                cancel.Click += (o, e) => worker.CancelAsync();
            else
                cancel.Visibility = Visibility.Collapsed;

            if (!SupportsProgress)
                progress.IsIndeterminate = true;

            worker = new BackgroundWorker();
            worker.DoWork += (o, e) => Callback(
                () => worker.CancellationPending, 
                p => worker.ReportProgress((int)(100 * p)));

            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (o, e) => progress.Value = e.ProgressPercentage;
            worker.RunWorkerCompleted += (o, e) => Close();

            worker.RunWorkerAsync();
        }

        public ProgressDialog(string Task, Action Callback)
        {
            InitializeComponent();
            task.Text = Task;
            cancel.Visibility = Visibility.Collapsed;

            progress.IsIndeterminate = true;

            worker = new BackgroundWorker();
            worker.DoWork += (o, e) => Dispatcher.InvokeAsync(() => Callback());

            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += (o, e) => progress.Value = e.ProgressPercentage;
            worker.RunWorkerCompleted += (o, e) => Close();

            worker.RunWorkerAsync();
        }

        public static void RunAsync(Window Owner, string Task, Action<Func<bool>, Action<double>> Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, Callback, true, true) { Owner = Owner };
            process.ShowDialog();
        }

        public static void RunAsync(Window Owner, string Task, Action<Action<double>> Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, (x, y) => Callback(y), false, true) { Owner = Owner };
            process.ShowDialog();
        }

        public static void RunAsync(Window Owner, string Task, Action Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, (x, y) => Callback(), false, false) { Owner = Owner };
            process.ShowDialog();
        }

        public static void Run(Window Owner, string Task, Action Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, Callback) { Owner = Owner };
            process.ShowDialog();
        }
    }
}
