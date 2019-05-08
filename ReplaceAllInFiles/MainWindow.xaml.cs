using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;

namespace ReplaceAllInFiles
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class ReplaceOptions
        {
            public string Source { get; set; }
            public string Dest { get; set; }
            public string FileList { get; set; }
            public bool ScanRecursively { get; set; }
            public bool CaseSensitive { get; set; }
        }

        private BackgroundWorker worker;

        public MainWindow()
        {
            InitializeComponent();
            Start.IsEnabled = true;
            Cancel.IsEnabled = false;
            ScanRecursively.IsChecked = true;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            Cancel.IsEnabled = true;

            FileList.Items.Clear();

            if (!string.IsNullOrEmpty(Source.Text) && !string.IsNullOrEmpty(Files.Text))
            {
                ReplaceOptions options = new ReplaceOptions();
                options.Source = Source.Text;
                options.Dest = Dest.Text;
                options.FileList = Files.Text;
                options.ScanRecursively = (ScanRecursively.IsChecked == true);
                options.CaseSensitive = (CaseSensitive.IsChecked == true);

                worker = new BackgroundWorker();
                worker.DoWork += new DoWorkEventHandler(worker_DoWork);
                worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
                worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
                worker.WorkerReportsProgress = true;
                worker.WorkerSupportsCancellation = true;
                worker.RunWorkerAsync(options);
            }
            else
            {
                Start.IsEnabled = true;
                Cancel.IsEnabled = false;
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Start.IsEnabled = true;
            Cancel.IsEnabled = false;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            FileList.Items.Add(e.UserState as string);
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            ReplaceOptions options = e.Argument as ReplaceOptions;

            try
            {
                IEnumerable<string> files = Directory.EnumerateFiles(Environment.CurrentDirectory, options.FileList, options.ScanRecursively ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                foreach (string file in files)
                {
                    string fileName = file.Trim();
                    string fileText = File.ReadAllText(fileName);

                    if (options.CaseSensitive)
                    {
                        fileText = fileText.Replace(options.Source, options.Dest);
                    }
                    else
                    {
                        while (true)
                        {
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                e.Result = null;
                                return;
                            }

                            int index = fileText.IndexOf(options.Source, StringComparison.InvariantCultureIgnoreCase);
                            if (index < 0)
                                break;

                            fileText = fileText.Substring(0, index) + options.Dest + fileText.Substring(index + options.Source.Length);
                        }
                    }

                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        e.Result = null;
                        return;
                    }

                    File.WriteAllText(fileName, fileText);
                    worker.ReportProgress(1, fileName);
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("Exception :-\n" + exp.ToString());
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = true;
            Cancel.IsEnabled = false;
        }
    }
}
