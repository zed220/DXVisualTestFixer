using DXVisualTestFixer.Services;
using System.Windows;
using Prism.Unity;
using Prism.Mvvm;
using Prism.Regions;
using DevExpress.Xpf.Docking;
using DXVisualTestFixer.PrismCommon;
using DevExpress.Mvvm.UI;
using DevExpress.Xpf.Dialogs;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.UI.Views;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.PrismCommon;
using DXVisualTestFixer.UI.ViewModels;
using DXVisualTestFixer.UI.Services;
using DXVisualTestFixer.Farm;
using DXVisualTestFixer.Core.Configuration;
using DXVisualTestFixer.UI.Native;
using DXVisualTestFixer.UI.Common;
using DevExpress.Logify.WPF;
using Prism.Ioc;
using DevExpress.Xpf.Core;

namespace DXVisualTestFixer {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication {
        public App() {
            LogifyAlert.Instance.ApiKey = "1CFEC5BD43E34C5AB6A58911736E8360";
            LogifyAlert.Instance.ConfirmSendReport = true;
            LogifyAlert.Instance.Run();
            ApplicationThemeHelper.UseLegacyDefaultTheme = true;
        }

        protected override Window CreateShell() {
            return new Shell();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton<IConfigSerializer, ConfigSerializer>();
            containerRegistry.RegisterSingleton<IThemesProvider, ThemesProvider>();
            containerRegistry.RegisterSingleton<ILoadingProgressController, LoadingProgressController>();
            containerRegistry.RegisterSingleton<ILoggingService, LoggingService>();
            containerRegistry.RegisterSingleton<ITestsService, TestsService>();
            containerRegistry.RegisterSingleton<IFarmIntegrator, FarmIntegrator>();
            containerRegistry.RegisterSingleton<IVersionService, VersionService>();
            containerRegistry.RegisterSingleton<IAppearanceService, AppearanceService>();
            containerRegistry.RegisterSingleton<IUpdateService, SquirrelUpdateService>();
            containerRegistry.RegisterSingleton<INotificationService, UI.Services.NotificationService>();

            containerRegistry.Register<ISettingsViewModel, SettingsViewModel>();
            containerRegistry.Register<IFolderBrowserDialog, DXFolderBrowserDialog>();
            containerRegistry.Register<IDXNotification, DXNotification>();
            containerRegistry.Register<IDXConfirmation, DXConfirmation>();

            containerRegistry.RegisterForNavigation<SplitTestInfoView>();
            containerRegistry.RegisterForNavigation<MergedTestInfoView>();
        }

        protected override void ConfigureRegionAdapterMappings(RegionAdapterMappings regionAdapterMappings) {
            base.ConfigureRegionAdapterMappings(regionAdapterMappings);
            regionAdapterMappings.RegisterMapping(typeof(LayoutPanel), Container.Resolve<LayoutPanelRegionAdapter>());
            //Container.Resolve<IRegionManager>().RegisterViewWithRegion(Regions.Main, typeof(MainView));
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            ViewModelLocationProvider.Register<MergedTestInfoView, TestInfoViewModel>();
            ViewModelLocationProvider.Register<SplitTestInfoView, TestInfoViewModel>();
            ViewModelLocationProvider.Register<Shell, ShellViewModel>();
        }
    }
}
