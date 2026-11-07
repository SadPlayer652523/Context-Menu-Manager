using Microsoft.Win32;
using System;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MahApps.Metro.Controls;
using ControlzEx.Theming;

namespace ContextMenuManager {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += (s, e) => ApplyDarkTitleBar();
            SystemEvents.UserPreferenceChanged += (s, e) => {
                if (e.Category == UserPreferenceCategory.General)
                {
                    Dispatcher.Invoke(ApplyDarkTitleBar);
                }
            };
            LoadContextMenus();
        }

        private void OnRefreshButtonPressed(object sender, RoutedEventArgs e)
        {
            LoadContextMenus();
        }

        private void OnDisableButtonPressed(object sender, RoutedEventArgs e)
        {
            string selectedItem = ContextMenuList.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedItem))
            {
                MessageBox.Show("Select an item first", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Are you sure you want to disable '{selectedItem}'?\n\nA backup will be created so you can restore it later.", "Confirm Disable", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string fullPath = @"HKEY_CLASSES_ROOT\*\shellex\ContextMenuHandlers\" + selectedItem;
                    string backupFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ContextMenuBackups");

                    if (!System.IO.File.Exists(backupFolder))
                    {
                        System.IO.Directory.CreateDirectory(backupFolder);
                    }

                    string backupFile = System.IO.Path.Combine(backupFolder, $"backup_{selectedItem}.reg");

                    System.Diagnostics.Process.Start("reg.exe", $"export \"{fullPath}\" \"{backupFile}\" /y").WaitForExit();
                    Registry.ClassesRoot.DeleteSubKeyTree(@"*\shellex\ContextMenuHandlers\" + selectedItem);
                    MessageBox.Show($"Disabled '{selectedItem}'.\n\nBackup saved to Documents\\ContextMenuBackups.", $"Disabled '{selectedItem}'", MessageBoxButton.OK, MessageBoxImage.Information);

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
            List<string> menuItems = new List<string>();

            string registryPath = @"*\shellex\ContextMenuHandlers";

            try
            {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(registryPath))
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
                    }
                }
            }

            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useImmersiveDarkMode, sizeof(int));
            }
        }
    }
}