#region ================= Namespaces

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using mxd.SQL2.Games;

#endregion

namespace mxd.SQL2
{
    public partial class App : Application
    {
        #region ================= Variables

        private static string appname;  // SQLauncher
        private static string version;
        private static string gamepath;     // c:\Games\Quake\
        private static string inipath;      // c:\Games\Quake\SQLauncher.ini
        private static Random random;       // 42. Or 667. Or 1?

        #endregion

        #region ================= Properties

        public static string AppName => appname;
        public static string Version => version;
        public static string GamePath => gamepath;
        public static string IniPath => inipath;
        public static Random Random => random;

        public static string ErrorMessageTitle = "Serious Error!";

        #endregion

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            //Application.Current.DispatcherUnhandledException += delegate(object o, DispatcherUnhandledExceptionEventArgs args) { throw args.Exception; };
            //AppDomain.CurrentDomain.UnhandledException += delegate(object o, UnhandledExceptionEventArgs args) { throw new Exception(args.ExceptionObject.ToString()); };

            // Store application path, version, game path and program name
            AssemblyName thisasm = Assembly.GetExecutingAssembly().GetName();
            version = thisasm.Version.Major + "." + thisasm.Version.Minor;
            appname = Path.GetFileNameWithoutExtension(thisasm.CodeBase);
            Uri localpath = new Uri(Path.GetDirectoryName(thisasm.CodeBase));

            string apppath = Uri.UnescapeDataString(localpath.AbsolutePath);
            gamepath = ((e.Args.Length == 1 && Directory.Exists(e.Args[0])) ? e.Args[0] : apppath);
            inipath = Path.Combine(gamepath, appname + ".ini");

            random = new Random();

            if(!GameHandler.Create(gamepath))
            {
                MessageBox.Show("No supported game files detected in the game directory (" + gamepath
                    + ")\n\nMake sure you are running this program from your " + GameHandler.SupportedGames + " directory!", ErrorMessageTitle);
                Application.Current.Shutdown();
                return;
            }

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture; // Set CultureInfo

            var mainwindow = new MainWindow();
            mainwindow.Show();
        }
    }
}
