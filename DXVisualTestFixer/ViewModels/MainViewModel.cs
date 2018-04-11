using CommonServiceLocator;
using DevExpress.Data.Filtering;
using DevExpress.Logify.WPF;
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
using System.Diagnostics;
using System.IO;
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

    public class UnusedFiltesContainer {
        public UnusedFiltesContainer(Dictionary<Repository, List<string>> usedFiles, Dictionary<Repository, List<Team>> teams) {
            UsedFiles = usedFiles;
            Teams = teams;
        }

        public Dictionary<Repository, List<string>> UsedFiles { get; }
        public Dictionary<Repository, List<Team>> Teams { get; }
    }

    public class SolutionModel {
        public SolutionModel(string version, string path) {
            Version = version;
            Path = GetRealPath(path);
        }

        public string Version { get; }
        public string Path { get; }
        public void OpenSolution() {
            var solutionFilePath = Directory.EnumerateFiles(Path, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if(solutionFilePath == null || !File.Exists(solutionFilePath))
                return;
            string openSolutionPath = @"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe";
            if(!File.Exists(openSolutionPath))
                return;
            ProcessStartInfo info = new ProcessStartInfo(openSolutionPath, solutionFilePath);
            info.Verb = "runas";
            Process.Start(info);
        }
        public void OpenFolder() {
            Process.Start(Path);
        }
        string GetRealPath(string path) {
            string folderName = Repository.InNewVersion(Version) ? "VisualTests" : "DevExpress.Xpf.VisualTests";
            return System.IO.Path.Combine(path, folderName);
        }
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
        public string CurrentLogLine {
            get { return GetProperty(() => CurrentLogLine); }
            set { SetProperty(() => CurrentLogLine, value); }
        }
        public TestViewType TestViewType {
            get { return GetProperty(() => TestViewType); }
            set { SetProperty(() => TestViewType, value, OnTestViewTypeChanged); }
        }
        public MergerdTestViewType MergerdTestViewType {
            get { return GetProperty(() => MergerdTestViewType); }
            set { SetProperty(() => MergerdTestViewType, value); }
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
        public TestsService TestService {
            get { return GetProperty(() => TestService); }
            set { SetProperty(() => TestService, value); }
        }
        public Dictionary<Repository, List<string>> UsedFiles {
            get { return GetProperty(() => UsedFiles); }
            set { SetProperty(() => UsedFiles, value); }
        }
        public Dictionary<Repository, List<Team>> Teams {
            get { return GetProperty(() => Teams); }
            set { SetProperty(() => Teams, value); }
        }
        public Dictionary<Repository, List<ElapsedTimeInfo>> ElapsedTimes {
            get { return GetProperty(() => ElapsedTimes); }
            set { SetProperty(() => ElapsedTimes, value); }
        }
        public List<SolutionModel> Solutions {
            get { return GetProperty(() => Solutions); }
            set { SetProperty(() => Solutions, value); }
        }

        public MainViewModel() {
            LoadingProgressController = new LoadingProgressController();
            TestService = new TestsService(LoadingProgressController);
            UpdateConfig();
            ServiceLocator.Current.GetInstance<ILoggingService>().MessageReserved += OnLoggingMessageReserved;
            ServiceLocator.Current.GetInstance<IUpdateService>().Start();
        }

        void OnLoggingMessageReserved(object sender, IMessageEventArgs args) {
            CurrentLogLine = args.Message;
        }

        async void FarmRefreshed(FarmRefreshedEventArgs args) {
            if(args == null) {
                await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(async () => {
                    FarmIntegrator.Stop();
                    ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Farm integrator succes");
                    await UpdateAllTests().ConfigureAwait(false);
                }));
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

        async Task UpdateAllTests() {
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Refreshing tests");
            LoadingProgressController.Start();
            List<FarmTaskInfo> failedTasks = GetAllTasks();
            var testInfoContainer = await TestService.LoadTestsAsync(failedTasks).ConfigureAwait(false);
            var tests = testInfoContainer.TestList.Where(t => t != null).Select(t => new TestInfoWrapper(this, t)).ToList();
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("");
            await App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                Tests = tests;
                UsedFiles = testInfoContainer.UsedFiles;
                ElapsedTimes = testInfoContainer.ElapsedTimes;
                Teams = testInfoContainer.Teams;
                Status = ProgramStatus.Idle;
                LoadingProgressController.Stop();
            }));
        }

        void FillSolutions() {
            List<SolutionModel> newSolutions = new List<SolutionModel>();
            foreach(var repository in Config.Repositories)
                newSolutions.Add(new SolutionModel(repository.Version, repository.Path));
            Solutions = newSolutions;
        }

        void UpdateConfig() {
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Checking config");
            var config = ConfigSerializer.GetConfig();
            if(Config != null && ConfigSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
            FillSolutions();
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
            TestInfoModel testInfoModel = new TestInfoModel(MergerdTestViewType) {
                TestInfo = CurrentTest,
                MoveNextRow = new Action(MoveNextCore),
                MovePrevRow = new Action(MovePrevCore),
                SetMergerdTestViewType = vt => MergerdTestViewType = vt
            };
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

        public void ShowRepositoryOptimizer() {
            if(CheckHasUncommittedChanges() || CheckAlarmAdmin())
                return;
            TestsToCommitCount = 0;
            ModuleManager.DefaultWindowManager.Show(Regions.RepositoryOptimizer, Modules.RepositoryOptimizer, new UnusedFiltesContainer(UsedFiles, Teams));
            ModuleManager.DefaultWindowManager.Clear(Regions.RepositoryOptimizer);
            UpdateContent();
        }
        public void ShowRepositoryAnalyzer() {
            ModuleManager.DefaultWindowManager.Show(Regions.RepositoryAnalyzer, Modules.RepositoryAnalyzer, ElapsedTimes);
            ModuleManager.DefaultWindowManager.Clear(Regions.RepositoryAnalyzer);
        }
        public void ShowSettings() {
            if(CheckHasUncommittedChanges())
                return;
            TestsToCommitCount = 0;
            ModuleManager.DefaultWindowManager.Show(Regions.Settings, Modules.Settings);
            ModuleManager.DefaultWindowManager.Clear(Regions.Settings);
            UpdateConfig();
        }
        public void ApplyChanges() {
            if(TestsToCommitCount == 0) {
                GetService<IMessageBoxService>()?.ShowMessage("Nothing to commit", "Nothing to commit", MessageButton.OK, MessageIcon.Information);
                return;
            }
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
            TestsToCommitCount = 0;
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
        bool CheckHasUncommittedChanges() {
            if(TestsToCommitCount == 0)
                return false;
            MessageResult? result = GetService<IMessageBoxService>()?.ShowMessage("You has uncommitted tests! Do you want to refresh tests list and flush all uncommitted tests?", "Uncommitted tests", MessageButton.OKCancel, MessageIcon.Information);
            if(!result.HasValue || result.Value != MessageResult.OK)
                return true;
            return false;
        }
        bool CheckAlarmAdmin() {
            MessageResult? result = GetService<IMessageBoxService>()?.ShowMessage("Warning! This tool is powerful and dangerous. Unbridled using may cause repository errors! Are you really sure?", "Warning", MessageButton.OKCancel, MessageIcon.Warning);
            if(!result.HasValue || result.Value != MessageResult.OK)
                return true;
            return false;
        }
        public void RefreshTestList() {
            if(CheckHasUncommittedChanges())
                return;
            ServiceLocator.Current.GetInstance<ILoggingService>().SendMessage("Waiting response from farm integrator");
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

        public void ClearCommits() {
            if(TestsToCommitCount == 0)
                return;
            foreach(var test in Tests)
                test.CommitChange = false;
        }
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
