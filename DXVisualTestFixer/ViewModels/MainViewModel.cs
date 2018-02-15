using CommonServiceLocator;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
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
        public string CorrentLogLine {
            get { return GetProperty(() => CorrentLogLine); }
            set { SetProperty(() => CorrentLogLine, value); }
        }
        public TestViewType TestViewType {
            get { return GetProperty(() => TestViewType); }
            set { SetProperty(() => TestViewType, value, OnTestViewTypeChanged); }
        }
        public LoadingProgressController LoadingProgressController {
            get { return GetProperty(() => LoadingProgressController); }
            set { SetProperty(() => LoadingProgressController, value); }
        }
        public int TestsToCommitCount {
            get { return GetProperty(() => TestsToCommitCount); }
            set { SetProperty(() => TestsToCommitCount, value); }
        }
        public CriteriaOperator CurrentFilter {
            get { return GetProperty(() => CurrentFilter); }
            set { SetProperty(() => CurrentFilter, value); }
        }
        public IUpdateService UpdateService {
            get { return GetProperty(() => UpdateService); }
            set { SetProperty(() => UpdateService, value); }
        }

        public MainViewModel() {
            LoadingProgressController = new LoadingProgressController();
            UpdateService = ServiceLocator.Current.GetInstance<IUpdateService>();
            UpdateService.Start();
            UpdateConfig();
            ServiceLocator.Current.GetInstance<ILoggingService>().MessageReserved += OnLoggingMessageReserved;
        }

        void OnLoggingMessageReserved(object sender, IMessageEventArgs args) {
            CorrentLogLine = args.Message;
        }

        void FarmRefreshed(FarmRefreshedEventArgs args) {
            if(args == null) {
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    FarmIntegrator.Stop();
                    ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Farm integrator succes");
                    UpdateAllTests();
                }));
                return;
            }
        }
        List<FarmTaskInfo> GetAllTasks() {
            List<FarmTaskInfo> result = new List<FarmTaskInfo>();
            foreach(var repository in Config.Repositories) {
                if(Repository.InNewVersion(repository.Version)) {
                    if(FarmIntegrator.GetTaskStatus(repository.GetTaskName_New()).BuildStatus == IntegrationStatus.Failure) {
                        result.Add(new FarmTaskInfo(repository, FarmIntegrator.GetTaskUrl(repository.GetTaskName_New())));
                    }
                    continue;
                }
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
                ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Start refreshing tests");
                LoadingProgressController.Start();
                List<FarmTaskInfo> failedTasks = GetAllTasks();
                var tests = TestsService.LoadParrallel(failedTasks, LoadingProgressController).Select(t => new TestInfoWrapper(this, t)).ToList();
                ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("");
                App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    Tests = tests;
                    Status = ProgramStatus.Idle;
                    LoadingProgressController.Stop();
                }));
            });
        }

        void UpdateConfig() {
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Checking config");
            var config = ConfigSerializer.GetConfig();
            if(Config != null && ConfigSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
            ServiceLocator.Current.GetInstance<IAppearanceService>()?.SetTheme(Config.ThemeName);
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Config loaded");
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
            foreach(var repoModel in Config.Repositories.Select(rep => new RepositoryModel(rep))) {
                if(!repoModel.IsValid()) {
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                        GetService<IMessageBoxService>()?.ShowMessage("Some repositories has wrong settings", "Modify repositories in settings", MessageButton.OK, MessageIcon.Information);
                        ShowSettings();
                    }));
                    return;
                }
            }
            RefreshTestList();
        }

        void OnCurrentTestChanged() {
            if(CurrentTest == null) {
                ModuleManager.DefaultManager.Clear(Regions.TestInfo);
                return;
            }
            TestInfoModel testInfoModel = new TestInfoModel() { TestInfo = CurrentTest, MoveNextRow = new Action(MoveNextCore), MovePrevRow = new Action(MovePrevCore) };
            var vm = ModuleManager.DefaultManager.GetRegion(Regions.TestInfo).GetViewModel(Modules.TestInfo);
            if(vm == null) {
                ModuleManager.DefaultManager.InjectOrNavigate(Regions.TestInfo, Modules.TestInfo, testInfoModel);
                return;
            }
            ((ISupportParameter)ModuleManager.DefaultManager.GetRegion(Regions.TestInfo).GetViewModel(Modules.TestInfo)).Parameter = testInfoModel;
        }
        void OnTestsChanged() {
            if(Tests == null) {
                TestsToCommitCount = 0;
                CurrentTest = null;
                CurrentFilter = null;
                ModuleManager.DefaultManager.Clear(Regions.FilterPanel);
            }
            else {
                CurrentTest = Tests.FirstOrDefault();
                ModuleManager.DefaultManager.InjectOrNavigate(Regions.FilterPanel, Modules.FilterPanel,
                    new FilterPanelViewModelParameter(Tests, SetFilter));
            }
        }

        void SetFilter(CriteriaOperator op) {
            CurrentFilter = op;
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
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Start farm integrator");
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

        //public void InstallUpdateSyncWithInfo(bool informNoUpdate) {
        //    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
        //        ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Check application updates");
        //        UpdateAppService.Update(GetService<IMessageBoxService>(), informNoUpdate);
        //        ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Finish check application updates");
        //    }));
        //}
        public void ChangeTestViewType(TestViewType testViewType) {
            TestViewType = testViewType;
        }

        void OnTestViewTypeChanged() {
            ModuleManager.DefaultManager.Clear(Regions.TestInfo);
            MifRegistrator.InitializeTestInfo(TestViewType);
            OnCurrentTestChanged();
        }

        public event EventHandler MoveNext;
        public event EventHandler MovePrev;
    }
}
