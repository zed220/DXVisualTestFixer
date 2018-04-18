using DXVisualTestFixer.Services;
using System.Windows;
using Prism.Unity;
using Microsoft.Practices.Unity;
using Prism.Mvvm;

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
    //        RootContainer.RegisterType<ILoggingService, LoggingService>(new ContainerControlledLifetimeManager());
    //        RootContainer.RegisterType<IMainViewModel, MainViewModel>(new ContainerControlledLifetimeManager());
    //        RootContainer.RegisterType<IAppearanceService, AppearanceService>(new ContainerControlledLifetimeManager());
    //        RootContainer.RegisterType<ITestInfoViewModel, TestInfoViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<ISettingsViewModel, SettingsViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IRepositoryOptimizerViewModel, RepositoryOptimizerViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IRepositoryAnalyzerViewModel, RepositoryAnalyzerViewModel>(new TransientLifetimeManager());
    //        RootContainer.RegisterType<IUpdateService, UpdateService>(new ContainerControlledLifetimeManager());
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
