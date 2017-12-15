using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.Farm;
using DXVisualTestFixer.Mif;
using DXVisualTestFixer.Services;
using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using ThoughtWorks.CruiseControl.Remote;

namespace DXVisualTestFixer.ViewModels {
    public interface IMainViewModel {

    }

    public enum ProgramStatus {
        Idle,
        Loading,
    }

    public class MainViewModel : ViewModelBase, IMainViewModel {
        public Config Config { get; private set; }

        public List<TestInfoWrapper> Tests {
            get { return GetProperty(() => Tests); }
            set { SetProperty(() => Tests, value, OnTestsChanged); }
        }
        public TestInfoWrapper CurrentTest {
            get { return GetProperty(() => CurrentTest); }
            set { SetProperty(() => CurrentTest, value, OnCurrentTestChanged); }
        }
        public ProgramStatus Status {
            get { return GetProperty(() => Status); }
            set { SetProperty(() => Status, value); }
        }

        public MainViewModel() {
            InstallUpdateSyncWithInfo(false);
            UpdateConfig();
        }

        void FarmRefreshed(FarmRefreshedEventArgs args) {
            if(args == null) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    FarmIntegrator.Stop();
                    UpdateAllTests();
                }));
                return;
            }
        }
        List<FarmTaskInfo> GetAllTasks() {
            List<FarmTaskInfo> result = new List<FarmTaskInfo>();
            foreach(var repository in Config.Repositories) {
                if(FarmIntegrator.GetTaskStatus(repository.GetTaskName()).BuildStatus == IntegrationStatus.Failure) {
                    result.Add(new FarmTaskInfo(repository, FarmIntegrator.GetTaskUrl(repository.GetTaskName())));
                }
                if(FarmIntegrator.GetTaskStatus(repository.GetTaskName_Optimized()).BuildStatus == IntegrationStatus.Failure) {
                    result.Add(new FarmTaskInfo(repository, FarmIntegrator.GetTaskUrl(repository.GetTaskName_Optimized())));
                }
            }
            return result;
        }

        void UpdateAllTests() {
            Task.Factory.StartNew(() => {
                var tests = TestsService.LoadParrallel(GetAllTasks()).Select(t => new TestInfoWrapper(t)).ToList();
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    Tests = tests;
                    Status = ProgramStatus.Idle;
                }));
            });
        }


        void UpdateConfig() {
            var config = ConfigSerializer.GetConfig();
            if(Config != null && ConfigSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
            UpdateContent();
        }

        void UpdateContent() {
            if(Config.Repositories == null || Config.Repositories.Length == 0) {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    GetService<IMessageBoxService>()?.ShowMessage("Add repositories in settings", "Add repositories in settings", MessageButton.OK, MessageIcon.Information);
                    ShowSettings();
                }));
                return;
            }
            RefreshTestList();
        }

        void OnCurrentTestChanged() {
            ModuleManager.DefaultManager.Clear(Regions.TestInfo);
            if(CurrentTest != null)
                ModuleManager.DefaultManager.InjectOrNavigate(Regions.TestInfo, Modules.TestInfo,
                    new TestInfoModel() { TestInfo = CurrentTest, MoveNextRow = new Action(MoveNextCore), MovePrevRow = new Action(MovePrevCore) });
        }
        void OnTestsChanged() {
            CurrentTest = Tests?.FirstOrDefault();
        }

        public void ShowSettings() {
            ModuleManager.DefaultWindowManager.Show(Regions.Settings, Modules.Settings);
            ModuleManager.DefaultWindowManager.Clear(Regions.Settings);
            UpdateConfig();
        }
        public void ApplyChanges() {
            List<TestInfoWrapper> changedTests = Tests.Where(t => t.CommitChange).ToList();
            if(changedTests.Count == 0) {
                GetService<IMessageBoxService>()?.ShowMessage("Nothing to commit", "Nothing to commit", MessageButton.OK, MessageIcon.Information);
                return;
            }
            ChangedTestsModel changedTestsModel = new ChangedTestsModel() { Tests = changedTests };
            ModuleManager.DefaultWindowManager.Show(Regions.ApplyChanges, Modules.ApplyChanges, changedTestsModel);
            ModuleManager.DefaultWindowManager.Clear(Regions.ApplyChanges);
            if(!changedTestsModel.Apply)
                return;
            changedTests.ForEach(ApplyTest);
            UpdateContent();
        }
        bool ShowCheckOutMessageBox(string text) {
            MessageResult? result = GetService<IMessageBoxService>()?.ShowMessage("Please checkout file in DXVCS \n" + text, "Please checkout file in DXVCS", MessageButton.OKCancel, MessageIcon.Information);
            return result.HasValue && result.Value == MessageResult.OK;
        }
        void ApplyTest(TestInfoWrapper testWrapper) {
            if(!TestsService.ApplyTest(testWrapper.TestInfo, ShowCheckOutMessageBox))
                GetService<IMessageBoxService>()?.ShowMessage("Test not fixed \n" + testWrapper.ToLog(), "Test not fixed", MessageButton.OK, MessageIcon.Information);
        }
        public void RefreshTestList() {
            Tests = null;
            Status = ProgramStatus.Loading;
            FarmIntegrator.Start(FarmRefreshed);
        }

        void MoveNextCore() {
            MoveNext?.Invoke(this, EventArgs.Empty);
        }
        void MovePrevCore() {
            MovePrev?.Invoke(this, EventArgs.Empty);
        }

        public void InstallUpdateSyncWithInfo(bool informNoUpdate) {
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                UpdateAppService.Update(GetService<IMessageBoxService>(), informNoUpdate);
            }));
        }

        public event EventHandler MoveNext;
        public event EventHandler MovePrev;
    }
}
