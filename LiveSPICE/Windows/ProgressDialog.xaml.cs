using System;
using System.ComponentModel;
using System.Windows;

namespace LiveSPICE
{
    /// <summary>
    /// Interaction logic for ProgressDialog.xaml
    /// </summary>
    public partial class ProgressDialog : Window
    {
        private BackgroundWorker worker;

        private ProgressDialog(string Task)
        {
            InitializeComponent();
            task.Text = Task;

            Closing += (o, e) => e.Cancel = worker.IsBusy;
        }

        private ProgressDialog(string Task, bool Async, Action<Func<bool>, Action<double>> Callback, bool SupportsCancel, bool SupportsProgress)
            : this(Task)
        {
            // Set up the controls for cancel/progress.
            if (SupportsCancel)
                cancel.Click += (o, e) => worker.CancelAsync();
            else
                cancel.Visibility = Visibility.Collapsed;
            if (!SupportsProgress)
                progress.IsIndeterminate = true;

            // Create worker thread.
            worker = new BackgroundWorker()
            {
                WorkerSupportsCancellation = SupportsCancel,
                WorkerReportsProgress = SupportsProgress
            };

            if (Async)
            {
                worker.DoWork += (o, e) => Callback(
                    () => worker.CancellationPending,
                    p => worker.ReportProgress((int)(100 * p)));
            }
            else
            {
                worker.DoWork += (o, e) => Dispatcher.Invoke(() => Callback(() => false, x => { }));
            }
            worker.ProgressChanged += (o, e) => progress.Value = e.ProgressPercentage;
            worker.RunWorkerCompleted += (o, e) => Close();

            // Run the worker when the window is loaded.
            ContentRendered += (o, e) => worker.RunWorkerAsync();
        }

        /// <summary>
        /// Run a callback asynchronously with progress and cancel support.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Task"></param>
        /// <param name="Callback"></param>
        public static void RunAsync(Window Owner, string Task, Action<Func<bool>, Action<double>> Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, true, Callback, true, true) { Owner = Owner };
            process.ShowDialog();
        }

        /// <summary>
        /// Run a callback asynchronously with progress support.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Task"></param>
        /// <param name="Callback"></param>
        public static void RunAsync(Window Owner, string Task, Action<Action<double>> Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, true, (x, y) => Callback(y), false, true) { Owner = Owner };
            process.ShowDialog();
        }

        /// <summary>
        /// Run a callback asynchronously with a blocking progress dialog for the duration.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Task"></param>
        /// <param name="Callback"></param>
        public static void RunAsync(Window Owner, string Task, Action Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, true, (x, y) => Callback(), false, false) { Owner = Owner };
            process.ShowDialog();
        }

        /// <summary>
        /// Run a callback synchronously with a blocking progress dialog.
        /// </summary>
        /// <param name="Owner"></param>
        /// <param name="Task"></param>
        /// <param name="Callback"></param>
        public static void Run(Window Owner, string Task, Action Callback)
        {
            ProgressDialog process = new ProgressDialog(Task, false, (x, y) => Callback(), false, false) { Owner = Owner };
            process.ShowDialog();
        }
    }
}
