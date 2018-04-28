using DevExpress.Logify.WPF;
using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
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
            Process.Start(@"\\corp\internal\common\visualTests_squirrel\Setup.exe");
            Environment.Exit(0);
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            new Bootstrapper().Run();
        }
    }
}
