using System.Configuration;
using System.Data;
using System.Windows;
using ControlzEx.Theming;
using MahApps.Metro.Controls;

namespace ContextMenuManager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
            ThemeManager.Current.SyncTheme();

            this.DispatcherUnhandledException += (s, args) =>
            {
                Exception inner = args.Exception;
                while (inner.InnerException != null) inner = inner.InnerException;
                MessageBox.Show(inner.Message + "\n\nStack Trace:\n" + inner.StackTrace, "Crash Handler", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}