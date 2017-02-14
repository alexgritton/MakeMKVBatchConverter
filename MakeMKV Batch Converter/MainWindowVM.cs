using System.Deployment.Application;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Management;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Media;
using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using PropertyChanged;
using Application = System.Windows.Application;

namespace MakeMKV_Batch_Converter
{
    [ImplementPropertyChanged]
    public class MainWindowVM
    {
        #region Properties


        public bool ButtonsEnabled { get; set; }

        public string MainDir{get;set;}

        public string OutputDir { get; set; }

        public string AppPath{get;set;}

        public string AppSource { get; set; }

        public string ConversionLabel { get; set; }

        public string ConversionBoxText { get; set; }

        public int IfoCount{get;set;}
        public int IsoCount{get;set;}
        public int BdCount{get;set;}

        public int MinSec { get; set; }

        public int ProgressBar { get; set; }

        public ObservableCollection<DvdWrapper> DvdFolders{get;set;}

        public string FolderInfoLabel { get; set; }

        public bool SameAsSource { get; set; }

        public bool NormalizeNameChecked { get; set; }

        public bool StandardNameChecked { get; set; }

        public bool AllTitles { get; set; }

        public bool MainTitle { get; set; }

        public bool ShutDown { get; set; }

        public bool RunProgram { get; set; }

        public string FinalSound { get; set; }

        public string FinishProgram { get; set; }

        public string ApplicationVersion { get; set; }

        private List<string> _fileExtensions = new List<string> { ".iso", ".m2ts", ".ifo" }; 

        #endregion

        public Process p;
        public MainWindowVM()
        {
            AppSource = string.Format(@"{0}", Properties.Settings.Default.MKVSource);
            SameAsSource = Properties.Settings.Default.SameAsSource;
            OutputDir = string.Format(@"{0}", Properties.Settings.Default.OutputDir);
            NormalizeNameChecked = Properties.Settings.Default.NormalizeNameChecked;
            StandardNameChecked = Properties.Settings.Default.StandardNameChecked;
            AllTitles = Properties.Settings.Default.AllTitles;
            MainTitle = Properties.Settings.Default.MainTitle;
            AppPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase).Substring(6);
            ShutDown = Properties.Settings.Default.ShutDown;
            RunProgram = Properties.Settings.Default.RunProgram;
            FinalSound = string.Format(@"{0}", Properties.Settings.Default.FinalSound);
            FinishProgram = string.Format(@"{0}", Properties.Settings.Default.RunProgramLocation);
            IfoCount = 0;
            IsoCount = 0;
            BdCount = 0;
            MinSec = Properties.Settings.Default.MinSec;
            DvdFolders = new ObservableCollection<DvdWrapper>();
            ButtonsEnabled = true;
            ApplicationVersion = "Debug";
            if(ApplicationDeployment.IsNetworkDeployed)
                ApplicationVersion = string.Format("Version {0}", ApplicationDeployment.CurrentDeployment.CurrentVersion);
        }

        private string NormalizeName(string name)
        {
            string temp_name = name;
            if(name.Contains(" "))
                temp_name = name.Replace(" ", "_");
            temp_name = temp_name.ToUpper();
            return temp_name;
        }

        private void AddAllFiles()
        {
            FolderBrowserDialog open = new FolderBrowserDialog();
            open.ShowNewFolderButton = false;
            open.ShowDialog();
            if (!string.IsNullOrEmpty(open.SelectedPath))
            {
                FindAllFiles(open.SelectedPath);
            }
        }

        public void AddFile(string fullFilePath)
        {
            DvdWrapper wrapper = DvdWrapper.Create(fullFilePath);
            if (wrapper == null) return;
            switch (wrapper.Type)
            {
                case FileType.Iso:
                    IsoCount++;
                    break;
                case FileType.BluRay:
                    BdCount++;
                    break;
                case FileType.VideoTs:
                    IfoCount++;
                    break;
            }
            DvdFolders.Add(wrapper);
        }

        public void FindAllFiles(string path)
        {
            if (!Directory.Exists(path))
            {
                if (_fileExtensions.Any(x => x.Equals(Path.GetExtension(path))))
                {
                    AddFile(path);
                }
                return;
            }

            List<string> allFiles = new List<string>();
            foreach (var fileExtension in _fileExtensions)
            {
                try
                {
                    string[] files = Directory.GetFiles(path, string.Format("*{0}", fileExtension));
                    if(!files.Any())continue;
                    if (files.All(x => Path.GetExtension(x).ToLower().Equals(".m2ts")))
                    {
                        allFiles.Add(files.First());
                    }
                    else
                    {
                         allFiles.AddRange(files);
                    }
                   
                }
                catch (Exception)
                {
                }
            }
            if (allFiles.Count > 0)
            {
                //TODO: Process video files.
                foreach (var file in allFiles)
                {
                    AddFile(file);
                }
            }

            string[] folders = Directory.GetDirectories(path);
            foreach (var folder in folders)
            {
                FindAllFiles(folder);
            }
            FolderInfoLabel = string.Format("Folders Found: {0} | IFO Files Found: {1} | ISO Files Found: {2} | Blu-Ray Files Found: {3}", DvdFolders.Count, IfoCount, IsoCount, BdCount);
        }

        private void ProcessDvd(object sender)
        {

            string date = string.Format("{0}-{1}-{2}--{3}-{4}-{5}.txt", DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Year, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            string logs = Path.Combine(AppPath, "Conversion Logs", date);
            try
            {
                if (!Directory.Exists(Path.Combine(AppPath, "Conversion Logs")))
                    Directory.CreateDirectory(Path.Combine(AppPath, "Conversion Logs"));

            }
            catch
            {
            }
            using (StreamWriter outfile = new StreamWriter(logs))
            {
                outfile.AutoFlush = true;
                outfile.WriteLine("Conversion Started: {0}", DateTime.Now);
                foreach (DvdWrapper dvd in DvdFolders)
                {
                    if (dvd.Progress == "Ready")
                    {
                        bool discOpenFailed = false;
                        dvd.MarkInProgress();
                        ConversionLabel = string.Format("Current Conversion: {0}", dvd.DisplayName);
                        Stopwatch timer = new Stopwatch();
                        timer.Start();
                        string tempOutputDir = OutputDir;
                        if (SameAsSource)
                        {
                            try
                            {
                                if (!Directory.Exists(Path.Combine(dvd.PathName, "batch_temp")))
                                    Directory.CreateDirectory(Path.Combine(dvd.PathName, "batch_temp"));
                                OutputDir = dvd.PathName;
                                tempOutputDir = Path.Combine(dvd.PathName, "batch_temp");
                            }
                            catch
                            {
                                string tempDest = string.Empty;
                                OutputDir = dvd.PathName;
                                ConversionBoxText += tempDest;
                                tempOutputDir = Path.Combine(tempDest, "batch_temp");
                                if (!Directory.Exists(tempOutputDir))
                                    Directory.CreateDirectory(tempOutputDir);
                            }
                        }
                        else
                        {
                            if (!Directory.Exists(Path.Combine(OutputDir, "batch_temp")))
                                Directory.CreateDirectory(Path.Combine(OutputDir, "batch_temp"));
                            tempOutputDir = Path.Combine(OutputDir, "batch_temp");
                        }
                        string arg = "--noscan --minlength=" + MinSec + " --progress=-same mkv ";
                        if (dvd.Type == FileType.Iso)
                        {
                            if (AllTitles)
                                arg += "iso:\"" + dvd.PathName + "\" all \"" + tempOutputDir + "\"";
                            else
                                arg += "iso:\"" + dvd.PathName + "\" 0 \"" + tempOutputDir + "\"";
                        }
                        else if (dvd.Type == FileType.BluRay)
                        {
                            if (AllTitles)
                                arg += "file:\"" + dvd.PathName + "\" all \"" + tempOutputDir + "\"";
                            else
                                arg += "file:\"" + dvd.PathName + "\" 0 \"" + tempOutputDir + "\"";
                        }
                        else
                        {
                            if (AllTitles)
                                arg += "file:\"" + dvd.PathName + "\" all \"" + tempOutputDir + "\"";
                            else
                                arg += "file:\"" + dvd.PathName + "\" 0 \"" + tempOutputDir + "\"";
                        }
                        outfile.WriteLine("Argument Line: {0}", arg);
                        p = Process.Start(new ProcessStartInfo { FileName = AppSource, Arguments = arg, UseShellExecute = false, RedirectStandardOutput = true, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true });
                        string currentline = null;
                        currentline = p.StandardOutput.ReadLine();
                        int titleSkipCount = 0;
                        int corruptedCount = 0;
                        while (currentline != null && corruptedCount < 10)
                        {
                            if (currentline.Contains("Current progress"))
                            {
                                int current, total;
                                currentline = currentline.Substring(19);
                                string currentcut = currentline.Substring(0, currentline.IndexOf('%'));
                                currentline = currentline.Substring(currentline.IndexOf('T'));
                                currentline = currentline.Substring(17, currentline.IndexOf('%') - 17);
                                total = Convert.ToInt32(currentline);
                                current = Convert.ToInt32(currentcut);
                                ProgressBar = total;
                            }
                            else
                            {
                                if (currentline.Contains("skipped"))
                                {
                                    titleSkipCount++;
                                }
                                else
                                {
                                    if (currentline.Contains("Decrypting"))
                                    {
                                        string skipped = string.Format("{0} Titles Skipped. They were shorter than minLength {1}.\n", titleSkipCount, MinSec);
                                        ConversionBoxText += skipped;
                                        outfile.WriteLine(skipped);
                                    }
                                    else if (!currentline.Contains("Scanning CD") && !currentline.Contains("Opening Blu-ray disc") && !currentline.Contains("advanced titleset"))
                                    {
                                        if (currentline.Contains("Failed to open disc"))
                                            discOpenFailed = true;
                                        ConversionBoxText += currentline + "\n";
                                    }
                                    else if(currentline.Contains("attempting to work around"))
                                        corruptedCount++;
                                    outfile.WriteLine(currentline);
                                }
                            }
                            currentline = p.StandardOutput.ReadLine();
                        }
                        if (corruptedCount == 10)
                            if (!p.HasExited)
                                p.Kill();
                        int failOut = 0;
                        if (NormalizeNameChecked)
                        {
                            dvd.DisplayName = NormalizeName(dvd.DisplayName);
                        }

                        string finalName = Path.GetFileNameWithoutExtension(dvd.DisplayName);
                        if (!AllTitles)
                        {
                            bool fail = true;
                            while (fail && failOut < 10)
                            {
                                try
                                {
                                    string source = tempOutputDir;
                                    string finalSource = Directory.GetFiles(string.Format(source)).First();
                                    if (failOut == 0)
                                    {
                                        File.Move(finalSource, string.Format(@"{0}\{1}.mkv", OutputDir, finalName));
                                    }
                                    else
                                    {
                                        File.Move(finalSource, string.Format(@"{0}\{1}{2}.mkv", OutputDir, finalName, failOut));
                                    }
                                    fail = false;
                                }
                                catch (Exception ex)
                                {
                                    if (!fail)
                                        outfile.WriteLine("Error Moving File. {0}", ex.Message);
                                    fail = true;
                                    failOut++;

                                }
                            }
                            try
                            {
                                if (Directory.Exists(tempOutputDir))
                                {
                                    string[] tempFiles = Directory.GetFiles(tempOutputDir);
                                    foreach (string temp in tempFiles)
                                    {
                                        File.Delete(Path.Combine(tempOutputDir, temp));
                                    }
                                    Directory.Delete(tempOutputDir);
                                }
                            }
                            catch (Exception ex)
                            {
                                outfile.WriteLine("Error Deleting Temp Files. {0}", ex.Message);
                            }
                        }
                        else
                        {
                            try
                            {
                                if (Directory.Exists(tempOutputDir))
                                {
                                    Directory.Move(tempOutputDir, OutputDir + "\\" + finalName);
                                }
                            }
                            catch (Exception ex)
                            {
                                outfile.WriteLine("Error Moving Directory {0} to {1}. {2}", tempOutputDir, (OutputDir + "\\" + finalName), ex.Message);
                            }
                        }

                        timer.Stop();
                        ConversionBoxText += string.Format("Conversion Process Completed In: {0}s\n", timer.Elapsed);
                        outfile.WriteLine("Conversion Process Completed In: {0}", timer.Elapsed);
                        ConversionLabel = "Current Conversion: ";
                        try
                        {
                            if (failOut == 10 || discOpenFailed || corruptedCount == 10)
                            {
                                dvd.MarkFailed();
                            }
                            else
                            {
                                dvd.MarkDone();
                            }
                        }
                        catch (Exception ex)
                        {
                            outfile.WriteLine("Error Code 311. Message: {0}", ex.Message);
                        }
                    }
                }
            }
            ButtonsEnabled = true;
            try
            {

                (new SoundPlayer(string.Format(@"{0}", FinalSound))).Play();
            }
            catch
            {
            }
            
            if (RunProgram)
                Process.Start(FinishProgram);
            if (ShutDown)
                Process.Start("shutdown", "/s /t 0");
            //MessageBox.Show("Conversion Complete!", "Conversion Complete", MessageBoxButtons.OK);
            
        }

        #region Commands

        public ICommand RemoveSelected
        {
            get
            {
                return new RelayCommand<object>(x =>
                {

                        IList<DvdWrapper> items = DvdFolders.Where(y => y.IsChecked).ToList();
                        foreach (DvdWrapper item in items)
                        {
                            if (item.Type == FileType.VideoTs && item.Progress != "NotReady")
                                IfoCount--;
                            else if (item.Type == FileType.Iso && item.Progress != "NotReady")
                                IsoCount--;
                            else if (item.Type == FileType.BluRay && item.Progress != "NotReady")
                                BdCount--;
                            DvdWrapper item1 = item;
                            Application.Current.Dispatcher.Invoke(()=>DvdFolders.Remove(item1));
                        }
                        FolderInfoLabel = string.Format("Folders Found: {0} | IFO Files Found: {1} | ISO Files Found: {2} | Blu-Ray Files Found: {3}", DvdFolders.Count, IfoCount, IsoCount, BdCount);
                });
            }
        }

        public ICommand RemoveAll
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    DialogResult result = MessageBox.Show("Are you sure?", "Remove All", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        DvdFolders.Clear();
                        IfoCount = 0;
                        IsoCount = 0;
                        BdCount = 0;
                        FolderInfoLabel = string.Format("Folders Found: {0} | IFO Files Found: {1} | ISO Files Found: {2} | Blu-Ray Files Found: {3}", DvdFolders.Count, IfoCount, IsoCount, BdCount);
                    }
                });
            }
        }

        public ICommand LoadAllFiles
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    AddAllFiles();
                });
            }
        }

        public ICommand ChangeFinishedProgramSource
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.Filter = "(*.exe)|*.exe";
                    open.ShowDialog();
                    FinishProgram = open.FileName;
                });
            }
        }

        public ICommand ChangeFinishedSoundSource
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.Filter = "(*.wav)|*.wav";
                    open.ShowDialog();
                    FinalSound = open.FileName;
                });
            }
        }

        public ICommand ChangeAppSource
        {
            get
            {
                return new RelayCommand<object>(x => 
                {
                    OpenFileDialog open = new OpenFileDialog();
                    open.Filter = "(*.exe)|*.exe";
                    open.ShowDialog();
                    AppSource = open.FileName;
                });
            }
        }

        public ICommand VerifyNameCheck
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    if (x.ToString() == "Normalize")
                    {
                        if (!NormalizeNameChecked)
                        {
                            NormalizeNameChecked = true;
                        }
                        StandardNameChecked = false;
                    }
                    else
                    {
                        if (!StandardNameChecked)
                        {
                            StandardNameChecked = true;
                        }
                        NormalizeNameChecked = false;
                    }
                });
            }
        }

        public ICommand VerifyTitleCheck
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    if (x.ToString() == "MainTitle")
                    {
                        MainTitle = true;
                        AllTitles = false;
                    }
                    else
                    {
                        AllTitles = true;
                        MainTitle = false;
                    }
                });
            }
        }

        public ICommand UpdateOutputDir
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    FolderBrowserDialog browser = new FolderBrowserDialog();
                    browser.ShowDialog();
                    SameAsSource = false;
                    OutputDir = browser.SelectedPath;
                });
            }
        }

        public ICommand StartDvdProcess
        {
            get
            {
                return new RelayCommand<object>(x =>
                {
                    if (OutputDir == string.Empty && !SameAsSource)
                        MessageBox.Show("No Destination Selected");
                    else if (MinSec == 0)
                        MessageBox.Show("No Minimum Title Length Set");
                    else if (DvdFolders.Count == 0)
                        MessageBox.Show("Nothing To Convert");
                    else if (!MainTitle && !AllTitles)
                        MessageBox.Show("No Title Option Selected");
                    else if (!StandardNameChecked && !NormalizeNameChecked)
                        MessageBox.Show("No Filename Option Selected");
                    else if (!File.Exists(AppSource))
                        MessageBox.Show("Invalid Location for MakeMKVCon.exe");
                    else
                    {
                        ButtonsEnabled = false;
                        ConversionBoxText += string.Format("Converting {0} Folders : {1} ISOs : {2} Blu-Rays\n", DvdFolders.Count(e => e.Type == FileType.VideoTs && e.Progress == "Ready"), DvdFolders.Count(e => e.Type == FileType.Iso && e.Progress == "Ready"), DvdFolders.Count(e => e.Type == FileType.BluRay && e.Progress == "Ready"));
                        ThreadPool.QueueUserWorkItem(ProcessDvd);
                    }
                });
            }
        }

        #endregion
    }
    [ImplementPropertyChanged]
    public class DvdWrapper
    {
        public bool IsChecked { get; set; }
        public string DisplayName { get; set; }
        public string PathName { get; set; }
        public FileType Type { get; set; }
        public string Progress { get; set; }
        public Brush Background { get; set; }
        public Brush AlternateBackground { get; set; }

        public DvdWrapper()
        {
            Initialize();
        }

        public static DvdWrapper Create(string fullFilePath)
        {
            DvdWrapper wrapper = new DvdWrapper();
            wrapper.Type = StringEnum.GetFileType(Path.GetExtension(fullFilePath));
            if (wrapper.Type == FileType.VideoTs)
            {
                try
                {
                    if (Path.GetFileName(fullFilePath).ToLower() != "video_ts.ifo") return null;
                    string rootPath = Path.GetDirectoryName(Path.GetDirectoryName(fullFilePath)); //EX: C:\\test\\BATMAN_3\\VIDEO_TS\Video_ts.ifo Turns into C:\\test\\BATMAN_3
                    if (rootPath != null)
                    {
                        string[] splitPath = rootPath.Split('\\');//EX: [c:, test, BATMAN_3]
                        wrapper.DisplayName = splitPath[splitPath.Length - 1];//EX: Gets BATMAN_3
                    }
                    wrapper.PathName = Path.GetDirectoryName(fullFilePath);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else if (wrapper.Type == FileType.BluRay)
            {
                try
                {
                    string rootPath = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(fullFilePath)));//EX: C:\\test\\the dark knight\bdmv\stream\file.m2ts
                    string[] splitPath = rootPath.Split('\\');//EX: [c:, test, the dark knight]
                    wrapper.DisplayName = splitPath[splitPath.Length - 1];
                    wrapper.PathName = rootPath;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                wrapper.DisplayName = Path.GetFileName(fullFilePath);
                wrapper.PathName = fullFilePath;
            }
            wrapper.Progress = "Ready";
            return wrapper;
        }

        private void Initialize()
        {
            Background = Application.Current.Resources["FlatWhiteBrush"] as Brush;
            AlternateBackground = Application.Current.Resources["FlatLightGreyBrush"] as Brush;
        }

        public void MarkDone()
        {
            Progress = "Complete";
            Background = Application.Current.Resources["FlatGreenBrush"] as Brush;
            AlternateBackground = Application.Current.Resources["FlatGreenBrush"] as Brush;
        }

        public void MarkFailed()
        {
            Progress = "Failed";
            Background = Application.Current.Resources["FlatRedBrush"] as Brush;
            AlternateBackground = Application.Current.Resources["FlatRedBrush"] as Brush;
        }

        public void MarkInProgress()
        {
            Progress = "In Progress";
            Background = Application.Current.Resources["FlatYellowBrush"] as Brush;
            AlternateBackground = Application.Current.Resources["FlatYellowBrush"] as Brush;
        }
    }
}
