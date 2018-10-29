using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace File_Organiser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        List<int> years;
        Dictionary<int, List<string>> months;

        public MainWindow()
        {
            InitializeComponent();

            sourceDIR.Text = Properties.Settings.Default.source_directory;

            if(sourceDIR.Text == "")
            {
                sourceDIR.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            DirectoryCreationType.SelectedIndex = 0;
            FileTransferType.SelectedIndex = 0;
            FileProgress.Value = 0;

            scanFolder();
        }

        private void FindDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    sourceDIR.Text = folderBrowserDialog.SelectedPath;
                    Properties.Settings.Default.source_directory = sourceDIR.Text;
                    Properties.Settings.Default.Save();
                    scanFolder();
                }
            }
        }

        private void scanFolder()
        {

            years = new List<int>();
            months = new Dictionary<int, List<string>>();
            int monthcount = 0;

            var files = Directory.EnumerateFiles(sourceDIR.Text, "*.*", SearchOption.AllDirectories);
            ScanFilesInfo.Content = files.Count().ToString() + " files found.";

            DateTime earliestDate = DateTime.UtcNow;

            foreach(string file in files)
            {
                // DateTime fileModified = File.GetCreationTime(@file);
                DateTime fileModified = File.GetLastWriteTime(file);
                if (DateTime.Compare(earliestDate, fileModified) > 0) {earliestDate = fileModified;}
                int year = fileModified.Year;
                string month = fileModified.ToString("MMMM");

                if (!years.Contains(year))
                {
                    years.Add(year);
                    Debug.WriteLine("Year: " + year);
                }

                if (months.ContainsKey(year))
                {
                    if (!months[year].Contains(month))
                    {
                        months[year].Add(month);
                        Debug.WriteLine("-----------" + month);
                        monthcount++;
                    }
                } else
                {
                    months.Add(year, new List<string> {month});
                    Debug.WriteLine("-----------" + month);
                    monthcount++;
                }

            }
            ScanFilesInfo.Content = files.Count().ToString() + " files found.";
            if (years.Count > 0) ScanFilesInfo.Content += " " + years.Count.ToString() + " year"+(years.Count > 1 ? "s" : "")+" found";
            if (monthcount > 1) ScanFilesInfo.Content += "; " + monthcount + " different months";

        }

        private void StartOrganisation_Click(object sender, RoutedEventArgs e)
        {
            scanFolder();

            StartOrganisation.IsEnabled = false;

            IEnumerable<string> files = Directory.EnumerateFiles(sourceDIR.Text, "*.*", SearchOption.AllDirectories);

            FileProgress.Maximum = files.Count();
            FileProgress.Value = 0;

            List<string> copied = new List<string>();
            

            switch (DirectoryCreationType.SelectedIndex)
            {
                case 0:
                    // Create Year structure
                    foreach (int year in years)
                    {
                        Debug.WriteLine("Processing Year: " + year.ToString());
                        string currentDIR = sourceDIR.Text + "\\" + year.ToString();
                        System.IO.Directory.CreateDirectory(currentDIR);
                        foreach (string file in files)
                        {
                            DateTime fm = File.GetLastWriteTime(file);
                            int y = fm.Year;
                            string filename = System.IO.Path.GetFileName(file);

                            if ((y == year) && (!copied.Contains(filename)))
                            {

                                FileProgress.Dispatcher.Invoke(() => FileProgress.Value += 1, System.Windows.Threading.DispatcherPriority.Background);
                                copied.Add(filename);

                                ScanFilesInfo.Content = year.ToString() + ": " + filename;
                                ProgressBarText.Text = FileProgress.Value + " / " + FileProgress.Maximum;

                                switch (FileTransferType.SelectedIndex)
                                {
                                    case 0:
                                        System.IO.File.Copy(sourceDIR.Text + "\\" + filename, currentDIR + "\\" + filename, true);
                                        break;
                                    case 1:
                                        System.IO.File.Move(sourceDIR.Text + "\\" + filename, currentDIR + "\\" + filename);
                                        break;
                                }

                            }
                        }
                    }
                break;
                case 1:
                    // Create Year > Month directory structure.
                    foreach (int year in years)
                    {
                        string yearDIR = sourceDIR.Text + "\\" + year.ToString();
                        System.IO.Directory.CreateDirectory(yearDIR);
                        foreach (string month in months[year])
                        {
                            string monthDIR = yearDIR + "\\" + month;
                            System.IO.Directory.CreateDirectory(monthDIR);
                            foreach(string file in files)
                            {
                                DateTime fm = File.GetLastWriteTime(file);
                                int y = fm.Year;
                                string m = fm.ToString("MMMM");
                                string filename = System.IO.Path.GetFileName(file);

                                if ((year == y) && (month == m) && (!copied.Contains(filename)))
                                {
                                                                     
                                    FileProgress.Dispatcher.Invoke(() => FileProgress.Value += 1, System.Windows.Threading.DispatcherPriority.Background);
                                    copied.Add(filename);

                                    ScanFilesInfo.Content = year.ToString() + "/" + m + ": " + filename;
                                    ProgressBarText.Text = FileProgress.Value + " / " + FileProgress.Maximum;

                                    switch (FileTransferType.SelectedIndex)
                                    {
                                        case 0:
                                            System.IO.File.Copy(sourceDIR.Text + "\\" + filename, monthDIR + "\\" + filename,true);
                                            break;
                                        case 1:
                                            System.IO.File.Move(sourceDIR.Text + "\\" + filename, monthDIR + "\\" + filename);
                                            break;
                                    }

                                }
                            }
                        }
                    }
                break;
            }

            System.Media.SystemSounds.Asterisk.Play();
            StartOrganisation.IsEnabled = true;
        }
    }
}
