using System;
using CommonServiceLocator;
using Unity;
using Unity.ServiceLocation;
using DXVisualTestFixer.Mif;
using Unity.Lifetime;
using DXVisualTestFixer.ViewModels;
using DXVisualTestFixer.Services;

namespace DXVisualTestFixer {
    public static class Bootstrapper {
        static IUnityContainer RootContainer { get; } = new UnityContainer();

        static Bootstrapper() {
            RegisterTypes();
            RegisterServiceLocator();
            BuildMif();
        }

        static void RegisterTypes() {
            RootContainer.RegisterType<IMifRegistrator, RepositoriesViewMifRegistrator>(new ContainerControlledLifetimeManager());
            RootContainer.RegisterType<ILoggingService, LoggingService>(new ContainerControlledLifetimeManager());
            RootContainer.RegisterType<IMainViewModel, MainViewModel>(new ContainerControlledLifetimeManager());
            RootContainer.RegisterType<IAppearanceService, AppearanceService>(new ContainerControlledLifetimeManager());
            RootContainer.RegisterType<ITestInfoViewModel, TestInfoViewModel>(new TransientLifetimeManager());
            RootContainer.RegisterType<ISettingsViewModel, SettingsViewModel>(new TransientLifetimeManager());
            RootContainer.RegisterType<IRepositoryOptimizerViewModel, RepositoryOptimizerViewModel>(new TransientLifetimeManager());
            RootContainer.RegisterType<IUpdateService, UpdateService>(new ContainerControlledLifetimeManager());
            RootContainer.RegisterType<IApplyChangesViewModel, ApplyChangesViewModel>(new TransientLifetimeManager());
            RootContainer.RegisterType<IFilterPanelViewModel, FilterPanelViewModel>(new TransientLifetimeManager());
        }
        static void RegisterServiceLocator() {
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(RootContainer));
        }
        static void BuildMif() {
            MifRegistrator.Register();
        }

        public static void RegisterExplicit<TFrom>(TFrom instance, LifetimeManager manager) {
            RootContainer.RegisterInstance<TFrom>(instance, manager);
        }

        public static void Run() {
            ServiceLocator.Current.GetInstance<IMifRegistrator>().RegisterUI();
        }
    }
}
