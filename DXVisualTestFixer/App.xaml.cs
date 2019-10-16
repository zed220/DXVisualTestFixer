using System.Windows;
using DevExpress.Logify.WPF;
using DevExpress.Xpf.Core;

namespace DXVisualTestFixer {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
		public App() {
			LogifyAlert.Instance.ApiKey = "1CFEC5BD43E34C5AB6A58911736E8360";
			LogifyAlert.Instance.ConfirmSendReport = true;
			LogifyAlert.Instance.CollectBreadcrumbs = true;
			LogifyAlert.Instance.Run();
			ApplicationThemeHelper.UseLegacyDefaultTheme = true;
		}

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);
			new Bootstrapper().Run();
		}
	}
}