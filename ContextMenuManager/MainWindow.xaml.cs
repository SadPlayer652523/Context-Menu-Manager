using Microsoft.Win32;
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
using System.Collections.Generic;

namespace ContextMenuManager {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}