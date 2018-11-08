using DevExpress.Data.Filtering;
using DXVisualTestFixer.UI.Views;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Native;
using DXVisualTestFixer.UI.Models;

namespace DXVisualTestFixer.UI.ViewModels {
    public enum ProgramStatus {
        Idle,
        Loading,
    }
    public class MainViewModel : ViewModelBase {
        public static string NavigationParameter_Test = "Test";

        readonly INotificationService notificationService;
        readonly IRegionManager regionManager;
        readonly ILoggingService loggingService;
        readonly Dispatcher Dispatcher;
        readonly IFarmIntegrator farmIntegrator;
        readonly IConfigSerializer configSerializer;

        #region private properties
        IConfig Config;

        List<ITestInfoModel> _Tests;
        ITestInfoModel _CurrentTest;
        ProgramStatus _Status;
        string _CurrentLogLine;
        int _TestsToCommitCount;
        CriteriaOperator _CurrentFilter;
        ITestsService _TestService;
        List<SolutionModel> _Solutions;
        #endregion

        public List<ITestInfoModel> Tests {
            get { return _Tests; }
            set { SetProperty(ref _Tests, value, OnTestsChanged); }
        }
        public ITestInfoModel CurrentTest {
            get { return _CurrentTest; }
            set { SetProperty(ref _CurrentTest, value); }
        }
        public ProgramStatus Status {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }
        public string CurrentLogLine {
            get { return _CurrentLogLine; }
            set { SetProperty(ref _CurrentLogLine, value); }
        }
        public int TestsToCommitCount {
            get { return _TestsToCommitCount; }
            set { SetProperty(ref _TestsToCommitCount, value); }
        }
        public CriteriaOperator CurrentFilter {
            get { return _CurrentFilter; }
            set { SetProperty(ref _CurrentFilter, value); }
        }
        public ITestsService TestService {
            get { return _TestService; }
            set { SetProperty(ref _TestService, value); }
        }
        public List<SolutionModel> Solutions {
            get { return _Solutions; }
            set { SetProperty(ref _Solutions, value); }
        }
        public ILoadingProgressController LoadingProgressController { get; }

        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<IConfirmation> SettingsRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<IConfirmation> ApplyChangesRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<IConfirmation> RepositoryOptimizerRequest { get; } = new InteractionRequest<IConfirmation>();
        public InteractionRequest<INotification> RepositoryAnalyzerRequest { get; } = new InteractionRequest<INotification>();
        public InteractionRequest<INotification> ViewResourcesRequest { get; } = new InteractionRequest<INotification>();

        public MainViewModel(INotificationService notificationService, 
                             IRegionManager regionManager, 
                             ILoggingService loggingService, 
                             IFarmIntegrator farmIntegrator, 
                             IConfigSerializer configSerializer, 
                             ILoadingProgressController loadingProgressController, 
                             ITestsService testsService) {
            Dispatcher = Dispatcher.CurrentDispatcher;
            this.notificationService = notificationService;
            this.regionManager = regionManager;
            this.loggingService = loggingService;
            this.farmIntegrator = farmIntegrator;
            this.configSerializer = configSerializer;
            LoadingProgressController = loadingProgressController;
            TestService = testsService;
            TestService.PropertyChanged += TestService_PropertyChanged;
            loggingService.MessageReserved += OnLoggingMessageReserved;
            UpdateConfig();
        }

        void TestService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(ITestsService.CurrentFilter))
                CurrentFilter = CriteriaOperator.Parse(TestService.CurrentFilter);
        }

        void OnLoggingMessageReserved(object sender, IMessageEventArgs args) {
            CurrentLogLine = args.Message;
        }

        async void FarmRefreshed(IFarmRefreshedEventArgs args) {
            if(args == null) {
                await Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(async () => {
                    loggingService.SendMessage("Finishing farm integrator");
                    farmIntegrator.Stop();
                    loggingService.SendMessage("Farm integrator succes");
                    await UpdateAllTests().ConfigureAwait(false);
                }));
            }
            else {
                await Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(async () => {
                    loggingService.SendMessage("Retrying farm integrator");
                    farmIntegrator.Stop();
                    RefreshTestList();
                }));
            }
        }

        async Task UpdateAllTests() {
            loggingService.SendMessage("Refreshing tests");
            LoadingProgressController.Start();
            await TestService.UpdateTests(notificationService).ConfigureAwait(false);
            var testInfoContainer = TestService.ActualState;
            var tests = testInfoContainer.TestList.Where(t => t != null).Select(t => new TestInfoModel(this, t)).Cast<ITestInfoModel>().ToList();
            loggingService.SendMessage("");
            await Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                Tests = tests;
                Status = ProgramStatus.Idle;
                LoadingProgressController.Stop();
            }));
        }

        void FillSolutions() {
            if(Config.Repositories == null) {
                Solutions = new List<SolutionModel>();
                return;
            }
            List<SolutionModel> actualSolutions = new List<SolutionModel>();
            foreach(var repository in Config.Repositories)
                actualSolutions.Add(new SolutionModel(repository.Version, repository.Path));
            Solutions = actualSolutions;
        }

        void UpdateConfig() {
            loggingService.SendMessage("Checking config");
            var config = configSerializer.GetConfig(false);
            if(Config != null && configSerializer.IsConfigEquals(config, Config))
                return;
            Config = config;
            FillSolutions();
            ServiceLocator.Current.GetInstance<IAppearanceService>()?.SetTheme(/*Config.ThemeName*/ "VS2017Light");
            loggingService.SendMessage("Config loaded");
            UpdateContent();
        }

        void UpdateContent() {
            if(Config.Repositories == null || Config.Repositories.Length == 0) {
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                    notificationService.DoNotification("Add repositories in settings", "Add repositories in settings");
                    ShowSettings();
                    RefreshTestList();
                }));
                return;
            }
            foreach(var repoModel in Config.Repositories.Select(rep => new RepositoryModel(rep))) {
                if(!repoModel.IsValid()) {
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
                        notificationService.DoNotification("Invalid Settings", "Modify repositories in settings", MessageBoxImage.Warning);
                        ShowSettings();
                        RefreshTestList();
                    }));
                    return;
                }
            }
            RefreshTestList();
        }

        void OnTestsChanged() {
            if(Tests == null) {
                TestsToCommitCount = 0;
                CurrentTest = null;
                CurrentFilter = null;
                regionManager.Regions[Regions.FilterPanel].RemoveAll();
            }
            else {
                regionManager.AddToRegion(Regions.FilterPanel, ServiceLocator.Current.TryResolve<FilterPanelView>());
                CurrentTest = Tests.FirstOrDefault();
            }
        }

        public void ShowRepositoryOptimizer() {
            if(CheckHasUncommittedChanges() || CheckAlarmAdmin())
                return;
            var confirmation = ServiceLocator.Current.TryResolve<RepositoryOptimizerViewModel>();
            RepositoryOptimizerRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            TestsToCommitCount = 0;
            UpdateContent();
        }
        public void ShowRepositoryAnalyzer() {
            RepositoryAnalyzerRequest.Raise(ServiceLocator.Current.TryResolve<RepositoryAnalyzerViewModel>());
        }
        public void ShowViewResources() {
            ViewResourcesRequest.Raise(ServiceLocator.Current.TryResolve<ViewResourcesViewModel>());
        }

        public void ShowSettings() {
            if(CheckHasUncommittedChanges())
                return;
            ISettingsViewModel confirmation = ServiceLocator.Current.TryResolve<ISettingsViewModel>();
            SettingsRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            TestsToCommitCount = 0;
            configSerializer.SaveConfig(confirmation.Config);
            UpdateConfig();
        }
        public void CommitCurrentTest() {
            if(CurrentTest == null || CurrentTest.Valid != TestState.Valid)
                return;
            CurrentTest.CommitChange = true;
        }
        public void UncommitCurrentTest() {
            if(CurrentTest == null)
                return;
            CurrentTest.CommitChange = false;
        }

        public List<ITestInfoModel> GetChangedTests() {
            return Tests.Where(t => t.CommitChange).ToList();
        }
        public void ApplyChanges() {
            if(TestsToCommitCount == 0) {
                notificationService.DoNotification("Nothing to commit", "Nothing to commit");
                return;
            }
            if(TestService.ActualState.ChangedTests.Count == 0) {
                notificationService.DoNotification("Nothing to commit", "Nothing to commit");
                return;
            }
            var confirmation = ServiceLocator.Current.TryResolve<ApplyChangesViewModel>();
            ApplyChangesRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            TestService.ActualState.ChangedTests.ForEach(ApplyTest);
            TestsToCommitCount = 0;
            UpdateContent();
        }
        bool ShowCheckOutMessageBox(string text) {
            return CheckConfirmation(ConfirmationRequest, "Readonly file detected", "Please checkout file in DXVCS \n" + text);
        }
        void ApplyTest(TestInfo testInfo) {
            if(TestService.ApplyTest(testInfo, ShowCheckOutMessageBox))
                return;
            var testWrapper = Tests.FirstOrDefault(test => test.TestInfo == testInfo);
            notificationService.DoNotification("Test not fixed", "Test not fixed \n" + testWrapper != null ? testWrapper.ToLog() : testInfo.Name, MessageBoxImage.Error);
        }
        bool CheckHasUncommittedChanges() {
            if(TestsToCommitCount == 0)
                return false;
            return !CheckConfirmation(ConfirmationRequest, "Uncommitted tests", "You has uncommitted tests! Do you want to refresh tests list and flush all uncommitted tests?");
         }
        bool CheckAlarmAdmin() {
            return !CheckConfirmation(ConfirmationRequest, "Warning", "This tool is powerful and dangerous. Unbridled using may cause repository errors! Are you really sure?");
        }
        public void RefreshTestList(bool checkConfirmation = true) {
            if(checkConfirmation && CheckHasUncommittedChanges())
                return;
            loggingService.SendMessage("Waiting response from farm integrator");
            Tests = null;
            Status = ProgramStatus.Loading;
            farmIntegrator.Start(FarmRefreshed);
        }

        public void ClearCommits() {
            if(TestsToCommitCount == 0)
                return;
            foreach(var test in Tests)
                test.CommitChange = false;
            TestService.ActualState?.ChangedTests?.Clear();
        }

        public void CommitTest(TestInfoModel testInfoModel) {
            TestsToCommitCount++;
            TestService.ActualState.ChangedTests.Add(testInfoModel.TestInfo);
        }
        public void UndoCommitTest(TestInfoModel testInfoModel) {
            TestsToCommitCount--;
            TestService.ActualState.ChangedTests.Remove(testInfoModel.TestInfo);
        }
    }
}
