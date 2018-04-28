using DevExpress.Logify.WPF;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() {
            LogifyAlert.Instance.ApiKey = "1CFEC5BD43E34C5AB6A58911736E8360";
            LogifyAlert.Instance.ConfirmSendReport = true;
            LogifyAlert.Instance.Run();
            string DXVisualTestFixerDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DXVisualTestFixer");
            var existApp = Path.Combine(DXVisualTestFixerDir, "DXVisualTestFixer.exe");
            if(File.Exists(existApp)) {
                if(Directory.Exists(Path.Combine(DXVisualTestFixerDir, "packages"))) {
                    if(Directory.Exists(Path.Combine(DXVisualTestFixerDir, "packages", "SquirrelTemp"))) {
                        Process.Start(existApp);
                        Environment.Exit(0);
                        return;
                    }
                }
            }
            Process.Start(@"\\corp\internal\common\visualTests_squirrel\Setup.exe");
            Environment.Exit(0);
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            new Bootstrapper().Run();
        }
    }
}
