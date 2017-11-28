using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.Mif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DXVisualTestFixer.ViewModels {
    public interface IMainViewModel {

    }

    public class MainViewModel : ViewModelBase, IMainViewModel {
        public Config Config { get; private set; }

        public List<TestInfo> Tests {
            get { return GetProperty(() => Tests); }
            set { SetProperty(() => Tests, value, OnTestsChanged); }
        }
        public TestInfo CurrentTest {
            get { return GetProperty(() => CurrentTest); }
            set { SetProperty(() => CurrentTest, value, OnCurrentTestChanged); }
        }

        public MainViewModel() {
            UpdateConfig();
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(UpdateContent), DispatcherPriority.Background);
        }

        void UpdateConfig() {
            var config = ConfigSerializer.GetConfig();
            if(Config != null && ConfigSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
        }

        void UpdateContent() {
            Tests = TestLoader.Load();
        }

        void OnCurrentTestChanged() {
            ModuleManager.DefaultManager.Clear(Regions.TestInfo);
            if(CurrentTest != null)
                ModuleManager.DefaultManager.InjectOrNavigate(Regions.TestInfo, Modules.TestInfo, CurrentTest);
        }
        void OnTestsChanged() {
            CurrentTest = Tests.FirstOrDefault();
        }

        public void ShowSettings() {
            ModuleManager.DefaultWindowManager.Show(Regions.Settings, Modules.Settings);
            ModuleManager.DefaultWindowManager.Clear(Regions.Settings);
            UpdateConfig();
        }
    }
}
