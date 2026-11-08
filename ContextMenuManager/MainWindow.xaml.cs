using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContextMenuManager
{
    public partial class MainWindow : Window
    {

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private string backupFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ContextMenuBackups");

        private string registryPath = @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\";
        private string registryClassesRoot = @"*\shellex\ContextMenuHandlers\";
        private string targetName = "all";
        private string restoreTarget = "all";

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += (s, e) => ApplyDarkTitleBar();
            this.Loaded += (s, e) =>
            {
                LoadContextMenus();
                LoadRestoreFiles();
            };
            if (IsRunningAsAdmin())
            {
                this.Title = "Context Menu Manager (Administrator)";
            }
            SystemEvents.UserPreferenceChanged += (s, e) =>
            {
                if (e.Category == UserPreferenceCategory.General)
                {
                    Dispatcher.Invoke(ApplyDarkTitleBar);
                }
            };
        }

        private void OnRefreshButtonPressed(object sender, RoutedEventArgs e)
        {
            LoadContextMenus();
            LoadRestoreFiles();
        }

        private void OnDisableButtonPressed(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ContextMenuList.SelectedItem as string))
            {
                MessageBox.Show("Select an item first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to disable '{ContextMenuList.SelectedItem as string}'?\n\nA backup will be created so you can restore it later.", "Confirm Disable", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string fullPath = registryPath + (ContextMenuList.SelectedItem as string);

                    if (!System.IO.Directory.Exists(backupFolder))
                    {
                        System.IO.Directory.CreateDirectory(backupFolder);
                    }

                    string backupFile = System.IO.Path.Combine(backupFolder, $"backup_{targetName}_{ContextMenuList.SelectedItem as string}.reg");

                    System.Diagnostics.Process.Start("reg.exe", $"export \"{fullPath}\" \"{backupFile}\" /y").WaitForExit();
                    Registry.ClassesRoot.DeleteSubKeyTree(registryClassesRoot + (ContextMenuList.SelectedItem as string));
                    MessageBox.Show($"Disabled '{ContextMenuList.SelectedItem as string}'.\n\nBackup saved to Documents\\ContextMenuBackups.", $"Disabled '{ContextMenuList.SelectedItem as string}'", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadContextMenus();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadContextMenus()
        {
            if (ContextMenuList == null || PathLabel == null) return;
            PathLabel.Text = "Loading...";

            List<string> menuItems = new List<string>();

            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(registryClassesRoot))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            menuItems.Add(subKeyName);
                        }
                    }
                }

                ContextMenuList.ItemsSource = menuItems;
                PathLabel.Text = $"{menuItems.Count} items";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error reading registry. Try running as Administrator:\n\n" + ex.Message, "Run as Administrator", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyDarkTitleBar()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int useImmersiveDarkMode = 0;

            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value != null && (int)value == 0)
                    {
                        useImmersiveDarkMode = 1;
                        StatusBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
                    }
                    else
                    {
                        useImmersiveDarkMode = 0;
                        StatusBar.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F0F0"));
                    }
                }
            }

            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
            }
        }

        private void OnTargetComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TargetComboBox == null || registryPath == null) return;

            switch (TargetComboBox.SelectedIndex)
            {
                case 0:
                    registryPath = @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\";
                    registryClassesRoot = @"*\shellex\ContextMenuHandlers\";
                    targetName = "all";
                    break;

                case 1:
                    registryPath = @"HKEY_CLASSES_ROOT\Directory\shellex\ContextMenuHandlers\";
                    registryClassesRoot = @"Directory\shellex\ContextMenuHandlers\";
                    targetName = "directory";
                    break;

                case 2:
                    registryPath = @"HKEY_CLASSES_ROOT\Drive\shellex\ContextMenuHandlers\";
                    registryClassesRoot = @"Drive\shellex\ContextMenuHandlers\";
                    targetName = "drive";
                    break;
            }

            LoadContextMenus();
        }

        private void OnViewRegeditPressed(object sender, RoutedEventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit", true))
            {
                if (key != null)
                {
                    key.SetValue("LastKey", @"Computer\" + registryClassesRoot);
                }
            }

            System.Diagnostics.Process.Start("regedit.exe");
        }

        private void OnOpenBackupsPressed(object sender, RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(backupFolder))
            {
                System.Diagnostics.Process.Start("explorer.exe", backupFolder);
            }
            else
            {
                System.IO.Directory.CreateDirectory(backupFolder);
                System.Diagnostics.Process.Start("explorer.exe", backupFolder);
            }
        }

        private void OnListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisableButton.IsEnabled = ContextMenuList.SelectedItem != null;
        }

        private void OnRestoreListSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RestoreButton.IsEnabled = RestoreMenuList.SelectedItem != null;
        }

        private void LoadRestoreFiles()
        {
            if (!Directory.Exists(backupFolder))
            {
                Directory.CreateDirectory(backupFolder);
            }

            if (RestoreMenuList == null || PathLabel == null) return;
            PathLabel.Text = "Loading...";

            string[] files = Directory.GetFiles(backupFolder, $"backup_{restoreTarget}_*.reg");
            List<string> menuItems = new List<string>();

            try
            {
                foreach (string file in files)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                    fileName = fileName.Substring(7);

                    if (fileName.StartsWith("all_")) {
                        fileName = fileName.Substring(4);
                    }
                    else if (fileName.StartsWith("directory_")) {
                        fileName = fileName.Substring(10);
                    }
                    else if (fileName.StartsWith("drive_")) {
                        fileName = fileName.Substring(6);
                    }

                    menuItems.Add(fileName);
                }
                RestoreMenuList.ItemsSource = menuItems;
                PathLabel.Text = $"{menuItems.Count} items";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error reading files. Try restarting the application:\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRestoreComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RestoreComboBox == null || restoreTarget == null) return;

            switch (RestoreComboBox.SelectedIndex)
            {
                case 0:
                    restoreTarget = "all";
                    break;

                case 1:
                    restoreTarget = "directory";
                    break;

                case 2:
                    restoreTarget = "drive";
                    break;
            }

            LoadRestoreFiles();
        }

        private void OnRestoreButtonPressed(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RestoreMenuList.SelectedItem as string))
            {
                MessageBox.Show("Select an item first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to restore '{RestoreMenuList.SelectedItem as string}'?", "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string fullPath = backupFolder + @"\backup_" + restoreTarget + "_" + (RestoreMenuList.SelectedItem as string) + ".reg";

                    System.Diagnostics.Process.Start("reg.exe", $"import \"{fullPath}\"").WaitForExit();
                    MessageBox.Show($"Restored '{RestoreMenuList.SelectedItem as string}'.", $"Restored '{RestoreMenuList.SelectedItem as string}'", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadRestoreFiles();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private bool IsRunningAsAdmin()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }
    }
}