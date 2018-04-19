using DXVisualTestFixer.Services;
using System.Windows;
using Prism.Unity;
using Microsoft.Practices.Unity;
using Prism.Mvvm;
using DXVisualTestFixer.ViewModels;
using Prism.Regions;
using DXVisualTestFixer.Views;
using DevExpress.Xpf.Docking;
using DXVisualTestFixer.PrismCommon;

namespace DXVisualTestFixer {
    public class Bootstrapper : UnityBootstrapper {
        protected override DependencyObject CreateShell() {
            return Container.TryResolve(typeof(IShell)) as DependencyObject;
        }
        protected override void InitializeShell() {
            Application.Current.MainWindow.Show();
        }
        protected override void ConfigureContainer() {
            base.ConfigureContainer();
            RegisterTypeIfMissing(typeof(IShell), typeof(Shell), true);
            RegisterTypeIfMissing(typeof(ILoggingService), typeof(LoggingService), true);
            RegisterTypeIfMissing(typeof(IMainViewModel), typeof(MainViewModel), true);
            RegisterTypeIfMissing(typeof(ISettingsViewModel), typeof(SettingsViewModel), false);
            RegisterTypeIfMissing(typeof(ITestInfoViewModel), typeof(TestInfoViewModel), false);
            RegisterTypeIfMissing(typeof(IAppearanceService), typeof(AppearanceService), true);
            RegisterTypeIfMissing(typeof(IUpdateService), typeof(UpdateService), true);
            RegisterTypeIfMissing(typeof(IFilterPanelViewModel), typeof(FilterPanelViewModel), false);
        }

        protected override RegionAdapterMappings ConfigureRegionAdapterMappings() {
            var mappings = base.ConfigureRegionAdapterMappings();
            mappings.RegisterMapping(typeof(LayoutPanel), Container.Resolve<LayoutPanelRegionAdapter>());
            return mappings;
        }

        protected override void ConfigureViewModelLocator() {
            base.ConfigureViewModelLocator();
            ViewModelLocationProvider.Register(typeof(MergedTestInfoView).FullName, typeof(ITestInfoViewModel));
        }
    }

    //public static class Bootstrapper {
    //    static IUnityContainer RootContainer { get; } = new UnityContainer();

    //    static Bootstrapper() {
    //        RegisterTypes();
    //        RegisterServiceLocator();
    //        BuildMif();
    //    }

    //    static void RegisterTypes() {
    //        RootContainer.RegisterType<IMifRegistrator, RepositoriesViewMifRegistrator>(new ContainerControlledLifetimeManager());
    //        RootContainer.RegisterType<ITestInfoViewModel, TestInfoViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<ISettingsViewModel, SettingsViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IRepositoryOptimizerViewModel, RepositoryOptimizerViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IRepositoryAnalyzerViewModel, RepositoryAnalyzerViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IApplyChangesViewModel, ApplyChangesViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IFilterPanelViewModel, FilterPanelViewModel>(new TransientLifetimeManager());
    //    }
    //    static void RegisterServiceLocator() {
    //        ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(RootContainer));
    //    }
    //    static void BuildMif() {
    //        MifRegistrator.Register();
    //    }

    //    public static void RegisterExplicit<TFrom>(TFrom instance, LifetimeManager manager) {
    //        RootContainer.RegisterInstance<TFrom>(instance, manager);
    //    }

    //    public static void Run() {
    //        ServiceLocator.Current.GetInstance<IMifRegistrator>().RegisterUI();
    //        Application.Current.MainWindow = new Shell();
    //        Application.Current.MainWindow.Show();
    //    }
    //}
}
