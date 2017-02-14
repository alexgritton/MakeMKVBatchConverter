using System;
using System.Collections.Generic;
using System.IO;
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
using Telerik.Windows.Controls;

namespace MakeMKV_Batch_Converter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindowVM VM;
        public MainWindow()
        {
            StyleManager.ApplicationTheme = new Windows8Theme();
            VM = new MainWindowVM();
            this.DataContext = VM;
            InitializeComponent();
        }

        private void SaveUserSettings()
        {
            if (VM.AppSource.Length > 1)
                Properties.Settings.Default.MKVSource = VM.AppSource;
            Properties.Settings.Default.SameAsSource = VM.SameAsSource;
            Properties.Settings.Default.OutputDir = VM.OutputDir;
            Properties.Settings.Default.NormalizeNameChecked = VM.NormalizeNameChecked;
            Properties.Settings.Default.StandardNameChecked = VM.StandardNameChecked;
            Properties.Settings.Default.AllTitles = VM.AllTitles;
            Properties.Settings.Default.MainTitle = VM.MainTitle;
            Properties.Settings.Default.MinSec = VM.MinSec;
            Properties.Settings.Default.ShutDown = VM.ShutDown;
            Properties.Settings.Default.RunProgram = VM.RunProgram;
            Properties.Settings.Default.RunProgramLocation = VM.FinishProgram;
            Properties.Settings.Default.FinalSound = VM.FinalSound;
            Properties.Settings.Default.ApplicationLayout = SaveLayoutAsString();
            Properties.Settings.Default.Save();
        }

        private string SaveLayoutAsString()
        {
            MemoryStream stream = new MemoryStream();
            this.Docking.SaveLayout(stream);

            stream.Seek(0, SeekOrigin.Begin);

            StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (VM.p != null && !VM.p.HasExited)
                VM.p.Kill();
            SaveUserSettings();

            base.OnClosing(e);
        }

        private void MainWindow_OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    VM.FindAllFiles(file);
                }
            }
        }

        private void MainWindow_OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
        }

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(Properties.Settings.Default.ApplicationLayout))
                {
                    using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(Properties.Settings.Default.ApplicationLayout)))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        this.Docking.LoadLayout(stream);
                    }
                }
            }
            catch (Exception ex)
            {
            }

        }
    }
}
