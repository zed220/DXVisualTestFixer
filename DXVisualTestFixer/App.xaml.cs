using DevExpress.Xpf.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public App() {
            ApplicationThemeHelper.UseLegacyDefaultTheme = true;
        }

        protected override void OnStartup(StartupEventArgs e) {
            Bootstrapper.Run();
        }
    }
}
