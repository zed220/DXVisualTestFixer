using DevExpress.Data.Filtering;
using DXVisualTestFixer.Core.Configuration;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.Farm;
using DXVisualTestFixer.UI.Views;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DXVisualTestFixer.Common;
using ThoughtWorks.CruiseControl.Remote;
using Regions = DXVisualTestFixer.Common.Regions;

namespace DXVisualTestFixer.UI.ViewModels {
    public enum ProgramStatus {
        Idle,
        Loading,
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

    public class ViewModelBase : BindableBase {
        protected readonly IUnityContainer unityContainer;

        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        public ViewModelBase(IUnityContainer unityContainer) {
            this.unityContainer = unityContainer;
        }

        protected bool CheckConfirmation(InteractionRequest<IConfirmation> service, string title, string content, MessageBoxImage image = MessageBoxImage.Warning) {
            IDXConfirmation confirmation = unityContainer.Resolve<IDXConfirmation>();
            SetupNotification(confirmation, title, content, image);
            service.Raise(confirmation);
            return confirmation.Confirmed;
        }
        protected void DoNotification(string title, string content, MessageBoxImage image = MessageBoxImage.Information) {
            IDXNotification confirmation = unityContainer.Resolve<IDXNotification>();
            SetupNotification(confirmation, title, content, image);
            NotificationRequest.Raise(confirmation);
        }
        void SetupNotification(IDXNotification notification, string title, string content, MessageBoxImage image) {
            notification.Title = title;
            notification.Content = content;
            notification.ImageType = image;
        }
    }

    public class MainViewModel : ViewModelBase, IMainViewModel {

        readonly IRegionManager regionManager;
        readonly ILoggingService loggingService;
        readonly Dispatcher Dispatcher;

        #region private properties
        IConfig Config;

        List<ITestInfoWrapper> _Tests;
        ITestInfoWrapper _CurrentTest;
        ProgramStatus _Status;
        string _CurrentLogLine;
        TestViewType _TestViewType;
        MergerdTestViewType _MergerdTestViewType;
        int _TestsToCommitCount;
        CriteriaOperator _CurrentFilter;
        TestsService _TestService;
        Dictionary<Repository, List<string>> _UsedFiles;
        Dictionary<Repository, List<Team>> _Teams;
        Dictionary<Repository, List<IElapsedTimeInfo>> _ElapsedTimes;
        List<SolutionModel> _Solutions;
        #endregion

        public List<ITestInfoWrapper> Tests {
            get { return _Tests; }
            set { SetProperty(ref _Tests, value, OnTestsChanged); }
        }
        public ITestInfoWrapper CurrentTest {
            get { return _CurrentTest; }
            set { SetProperty(ref _CurrentTest, value, OnCurrentTestChanged); }
        }
        public ProgramStatus Status {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
        public string CurrentLogLine {
            get { return _CurrentLogLine; }
            set { SetProperty(ref _CurrentLogLine, value); }
        }
        public TestViewType TestViewType {
            get { return _TestViewType; }
            set { SetProperty(ref _TestViewType, value, OnTestViewTypeChanged); }
        }
        public MergerdTestViewType MergerdTestViewType {
            get { return _MergerdTestViewType; }
            set { SetProperty(ref _MergerdTestViewType, value); }
        }
        public int TestsToCommitCount {
            get { return _TestsToCommitCount; }
            set { SetProperty(ref _TestsToCommitCount, value); }
        }
        public CriteriaOperator CurrentFilter {
            get { return _CurrentFilter; }
            set { SetProperty(ref _CurrentFilter, value); }
        }
        public TestsService TestService {
            get { return _TestService; }
            set { SetProperty(ref _TestService, value); }
        }
        public Dictionary<Repository, List<string>> UsedFiles {
            get { return _UsedFiles; }
            set { SetProperty(ref _UsedFiles, value); }
        }
        public Dictionary<Repository, List<Team>> Teams {
            get { return _Teams; }
            set { SetProperty(ref _Teams, value); }
        }
        public Dictionary<Repository, List<IElapsedTimeInfo>> ElapsedTimes {
            get { return _ElapsedTimes; }
            set { SetProperty(ref _ElapsedTimes, value); }
        }
        public List<SolutionModel> Solutions {
            get { return _Solutions; }
            set { SetProperty(ref _Solutions, value); }
        }

        public LoadingProgressController LoadingProgressController { get; } = new LoadingProgressController();
        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

        public InteractionRequest<IConfirmation> SettingsRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<IConfirmation> ApplyChangesRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<IConfirmation> RepositoryOptimizerRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<INotification> RepositoryAnalyzerRequest { get; } = new InteractionRequest<INotification>();

        public MainViewModel(IUnityContainer container, IRegionManager regionManager, ILoggingService loggingService)
            : base(container) {
            Dispatcher = Dispatcher.CurrentDispatcher;
            this.regionManager = regionManager;
            this.loggingService = loggingService;
            TestService = new TestsService(LoadingProgressController);
            UpdateConfig();
            loggingService.MessageReserved += OnLoggingMessageReserved;
        }

        void OnLoggingMessageReserved(object sender, IMessageEventArgs args) {
            CurrentLogLine = args.Message;
        }

        async void FarmRefreshed(FarmRefreshedEventArgs args) {
            if(args == null) {
                await Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(async () => {
                    FarmIntegrator.Stop();
                    loggingService.SendMessage("Farm integrator succes");
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
            loggingService.SendMessage("Refreshing tests");
            LoadingProgressController.Start();
            List<FarmTaskInfo> failedTasks = GetAllTasks();
            var testInfoContainer = await TestService.LoadTestsAsync(failedTasks).ConfigureAwait(false);
            var tests = testInfoContainer.TestList.Where(t => t != null).Select(t => new TestInfoWrapper(this, t)).Cast<ITestInfoWrapper>().ToList();
            loggingService.SendMessage("");
            await Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
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
            loggingService.SendMessage("Checking config");
            var config = ConfigSerializer.GetConfig();
            if(Config != null && ConfigSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
            FillSolutions();
            ServiceLocator.Current.GetInstance<IAppearanceService>()?.SetTheme(Config.ThemeName);
            loggingService.SendMessage("Config loaded");
            UpdateContent();
        }

        void UpdateContent() {
            if(Config.Repositories == null || Config.Repositories.Length == 0) {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    DoNotification("Add repositories in settings", "Add repositories in settings");
                    ShowSettings();
                }));
                return;
            }
            foreach(var repoModel in Config.Repositories.Select(rep => new RepositoryModel(rep))) {
                if(!repoModel.IsValid()) {
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                        DoNotification("Invalid Settings", "Modify repositories in settings", MessageBoxImage.Warning);
                        ShowSettings();
                    }));
                    return;
                }
            }
            RefreshTestList();
        }

        void OnTestViewTypeChanged() {
            regionManager.Regions[Regions.TestInfo].RemoveAll();
            OnCurrentTestChanged();
        }
        void OnCurrentTestChanged() {
            if(CurrentTest == null)
                return;
            regionManager.RequestNavigate(Regions.TestInfo, TestViewType == TestViewType.Split ? "TestInfoView" : "MergedTestInfoView");
        }
        void OnTestsChanged() {
            if(Tests == null) {
                TestsToCommitCount = 0;
                CurrentTest = null;
                CurrentFilter = null;
                regionManager.Regions[Regions.FilterPanel].RemoveAll();
            }
            else {
                regionManager.AddToRegion(Regions.FilterPanel, unityContainer.Resolve<FilterPanelView>());
                CurrentTest = Tests.FirstOrDefault();
            }
        }

        public void SetFilter(CriteriaOperator op) {
            CurrentFilter = op;
        }

        public void ShowRepositoryOptimizer() {
            if(CheckHasUncommittedChanges() || CheckAlarmAdmin())
                return;
            IRepositoryOptimizerViewModel confirmation = unityContainer.Resolve<IRepositoryOptimizerViewModel>();
            RepositoryOptimizerRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            TestsToCommitCount = 0;
            UpdateContent();
        }
        public void ShowRepositoryAnalyzer() {
            RepositoryAnalyzerRequest.Raise(unityContainer.Resolve<IRepositoryAnalyzerViewModel>());
        }
        public void ShowSettings() {
            if(CheckHasUncommittedChanges())
                return;
            ISettingsViewModel confirmation = unityContainer.Resolve<ISettingsViewModel>();
            SettingsRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            TestsToCommitCount = 0;
            ConfigSerializer.SaveConfig(confirmation.Config);
            UpdateConfig();
        }
        public List<ITestInfoWrapper> GetChangedTests() {
            return Tests.Where(t => t.CommitChange).ToList();
        }
        public void ApplyChanges() {
            if(TestsToCommitCount == 0) {
                DoNotification("Nothing to commit", "Nothing to commit");
                return;
            }
            List<ITestInfoWrapper> changedTests = GetChangedTests();
            if(changedTests.Count == 0) {
                DoNotification("Nothing to commit", "Nothing to commit");
                return;
            }
            IApplyChangesViewModel confirmation = unityContainer.Resolve<IApplyChangesViewModel>();
            ApplyChangesRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            changedTests.ForEach(ApplyTest);
            TestsToCommitCount = 0;
            UpdateContent();
        }
        bool ShowCheckOutMessageBox(string text) {
            return CheckConfirmation(ConfirmationRequest, "Readonly file detected", "Please checkout file in DXVCS \n" + text);
        }
        void ApplyTest(ITestInfoWrapper testWrapper) {
            if(TestsService.ApplyTest(testWrapper.TestInfo, ShowCheckOutMessageBox))
                return;
            DoNotification("Test not fixed", "Test not fixed \n" + testWrapper.ToLog(), MessageBoxImage.Error);
        }
        bool CheckHasUncommittedChanges() {
            if(TestsToCommitCount == 0)
                return false;
            return !CheckConfirmation(ConfirmationRequest, "Uncommitted tests", "You has uncommitted tests! Do you want to refresh tests list and flush all uncommitted tests?");
        }
        bool CheckAlarmAdmin() {
            return !CheckConfirmation(ConfirmationRequest, "Warning", "This tool is powerful and dangerous. Unbridled using may cause repository errors! Are you really sure?");
        }
        public void RefreshTestList() {
            if(CheckHasUncommittedChanges())
                return;
            loggingService.SendMessage("Waiting response from farm integrator");
            Tests = null;
            Status = ProgramStatus.Loading;
            FarmIntegrator.Start(FarmRefreshed);
        }

        public void RaiseMoveNext() {
            MoveNext?.Invoke(this, EventArgs.Empty);
        }
        public void RaiseMovePrev() {
            MovePrev?.Invoke(this, EventArgs.Empty);
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

        public event EventHandler MoveNext;
        public event EventHandler MovePrev;
    }
}
