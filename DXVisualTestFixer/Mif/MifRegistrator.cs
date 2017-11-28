using CommonServiceLocator;
using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using DXVisualTestFixer.ViewModels;
using DXVisualTestFixer.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer.Mif {
    public static class MifRegistrator {
        static MifRegistrator() {
            ViewModelLocator.Default = new VMLocator();
            ModuleManager.DefaultManager.Register(Regions.Main, new Module(Modules.Main, ServiceLocator.Current.GetInstance<IMainViewModel>, typeof(MainView)));
            ModuleManager.DefaultManager.Register(Regions.TestInfo, new Module(Modules.TestInfo, ServiceLocator.Current.GetInstance<ITestInfoViewModel>, typeof(TestInfoView)));
            ModuleManager.DefaultManager.Register(Regions.Settings, new Module(Modules.Settings, ServiceLocator.Current.GetInstance<ISettingsViewModel>, typeof(SettingsView)));
        }

        public static void Register() {
        }
    }

    public class VMLocator : ViewModelLocator, IViewModelLocator {
        public VMLocator() : base(Application.Current) { }
        object IViewModelLocator.ResolveViewModel(string name) {
            return ServiceLocator.Current.GetInstance(this.ResolveViewModelType(name));
        }
    }
}
