using System;
using CommonServiceLocator;
using Unity;
using Unity.ServiceLocation;
using DXVisualTestFixer.Mif;
using Unity.Lifetime;
using DXVisualTestFixer.ViewModels;

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
            RootContainer.RegisterType<ITestInfoViewModel, TestInfoViewModel>(new TransientLifetimeManager());
            RootContainer.RegisterType<ISettingsViewModel, SettingsViewModel>(new TransientLifetimeManager());
            RootContainer.RegisterType<IApplyChangesViewModel, ApplyChangesViewModel>(new TransientLifetimeManager());
        }
        static void RegisterServiceLocator() {
            ServiceLocator.SetLocatorProvider(() => new UnityServiceLocator(RootContainer));
        }
        static void BuildMif() {
            MifRegistrator.Register();
        }

        public static void Run() {
            ServiceLocator.Current.GetInstance<IMifRegistrator>().RegisterUI();
        }
    }
}
