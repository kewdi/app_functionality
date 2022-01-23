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
using Windows;
using Windows.Media;
using Windows.Media.Control;
using MediaManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;
using MediaSession = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;
using MediaProperties = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties;
using System.Windows.Forms;

namespace testingwpf2
{
    /// <summary>
    /// 
    /// 
    ///     Intended for twitch.tv/0vonix0
    /// 
    /// 
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() { InitializeComponent(); }

        public MediaManager SessionManager = null;
        public MediaSession CurrentSession = null;
        public string DefaultTextFilePath => System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"songinfo.txt");

        public static NotifyIcon Notif;
        internal static bool ManualCloseFlag = false;

        private async void Window_Loaded(object obj, RoutedEventArgs args)
        {
            cbDisplayUnicode.IsChecked = Properties.Settings.Default.DisplayUnicode;
            tbTextFilePath.Text =
                string.IsNullOrWhiteSpace(Properties.Settings.Default.SavedCustomFilePath)
                ? DefaultTextFilePath
                : Properties.Settings.Default.SavedCustomFilePath;

            Notif = new NotifyIcon()
            {
                Text = "init",
                Icon = System.Drawing.Icon.FromHandle(Properties.Resources.ico.GetHicon()),
                Visible = true
            };
            Notif.DoubleClick += (o, a) =>
                WindowState =
                    WindowState.Minimized == WindowState
                    ? WindowState.Normal
                    : WindowState.Minimized
            ;
            Notif.MouseClick += (o, a) =>
            {
                if (MouseButtons.Right == a.Button)
                {
                    ManualCloseFlag = true;
                    Close();
                }
            };

            SessionManager = await MediaManager.RequestAsync();
            CurrentSession = SessionManager.GetCurrentSession();
            CurrentSession.MediaPropertiesChanged +=
                (session, properties) => UpdateAudioInfo();
            
            UpdateAudioInfo();
        }

        private void UpdateAudioInfo(MediaProperties props)
        {
            Dispatcher.Invoke(()=> {
                string newContent =
                    ((bool)cbDisplayUnicode.IsChecked ?   "\U0001F3B5  " : "")          + props.Title + 
                    ((bool)cbDisplayUnicode.IsChecked ? "\n\U0001F3A4  " : "\nby " )    + props.Artist +
                    ((bool)cbDisplayUnicode.IsChecked ? "\n\U0001F4BF  " : "\non ")     + props.AlbumTitle
                ;
                lAudioInfo.Content = newContent;
                Notif.Text = newContent;
                DumpTextToFile(newContent);
           });
        }
        /// <summary>Identical to UpdateSong() in your source code.</summary>
        private async void UpdateAudioInfo() => UpdateAudioInfo(await CurrentSession.TryGetMediaPropertiesAsync());

        private void DumpTextToFile(string content)
        {
            //attempt, ignore all complaints
            try { 
                System.IO.File.WriteAllText(
                    string.IsNullOrWhiteSpace(Properties.Settings.Default.SavedCustomFilePath)
                    ? DefaultTextFilePath
                    : Properties.Settings.Default.SavedCustomFilePath
                , content);
            }
            catch { }
        }

        #region Settings UI

        private void SettingsMenu_MouseRightButtonUp(object obj, MouseButtonEventArgs args)
        {
            panelSettings.Visibility =
                panelSettings.Visibility == Visibility.Visible
                    ? Visibility.Hidden : Visibility.Visible;
        }

        private void tbTextFilePath_KeyDown(object obj, System.Windows.Input.KeyEventArgs args = null)
        {
            if (args == null || Key.Enter == args.Key)
            {
                Properties.Settings.Default.SavedCustomFilePath = tbTextFilePath.Text;
                Properties.Settings.Default.Save();
                UpdateAudioInfo();
                tbTextFilePath.Background = Brushes.LightSeaGreen;
            }
        }

        private void bRefresh_Click(object obj, RoutedEventArgs args) => UpdateAudioInfo();

        private void tbTextFilePath_TextChanged(object obj, TextChangedEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.SavedCustomFilePath))
                return;
            tbTextFilePath.Background =
                tbTextFilePath.Text == Properties.Settings.Default.SavedCustomFilePath
                ? Brushes.LightSeaGreen
                : Brushes.LightYellow
            ;
        }

        private void bDefaultPath_Click(object obj, RoutedEventArgs args)
        {
            tbTextFilePath.Text = DefaultTextFilePath;
            tbTextFilePath_KeyDown(null, null);
        }

        private void cbDisplayUnicode_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DisplayUnicode = (bool)cbDisplayUnicode.IsChecked;
            Properties.Settings.Default.Save();
        }

        #endregion


        protected override void OnClosing(System.ComponentModel.CancelEventArgs args)
        {
            //manual call acceptance (from NotifyIcon)
            if (!ManualCloseFlag)
            {
                args.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            base.OnClosing(args);
        }

        private void Window_StateChanged(object sender, EventArgs e) => ShowInTaskbar = WindowState.Minimized != WindowState;

    }
}
