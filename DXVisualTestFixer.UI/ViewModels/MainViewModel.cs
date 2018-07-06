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
    public class MainViewModel : ViewModelBase {
        public static string NavigationParameter_Test = "Test";

        readonly INotificationService notificationService;
        readonly IRegionManager regionManager;
        readonly ILoggingService loggingService;
        readonly Dispatcher Dispatcher;
        readonly IFarmIntegrator farmIntegrator;

        #region private properties
        IConfig Config;

        List<ITestInfoModel> _Tests;
        ITestInfoModel _CurrentTest;
        TestViewType _TestViewType;
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
            set { SetProperty(ref _CurrentTest, value, OnCurrentTestChanged); }
        }
        public TestViewType TestViewType {
            get { return _TestViewType; }
            set { SetProperty(ref _TestViewType, value, OnTestViewTypeChanged); }
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
            LoadingProgressController = loadingProgressController;
            TestService = testsService;
            TestService.PropertyChanged += TestService_PropertyChanged;
            Config = configSerializer.GetConfig();
            FillSolutions();
            RefreshTestList();
        }

        void TestService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(ITestsService.CurrentFilter))
                CurrentFilter = CriteriaOperator.Parse(TestService.CurrentFilter);
        }

        async void FarmRefreshed(IFarmRefreshedEventArgs args) {
            if(args == null) {
                await Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(async () => {
                    farmIntegrator.Stop();
                    loggingService.SendMessage("Farm integrator succes");
                    await UpdateAllTests().ConfigureAwait(false);
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
                LoadingProgressController.Status = ProgramStatus.Idle;
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

            appearanceService?.SetTheme(Config.ThemeName);
        void OnTestViewTypeChanged() {
            regionManager.Regions[Regions.TestInfo].RemoveAll();
            OnCurrentTestChanged();
        }
        void OnCurrentTestChanged() {
            if(CurrentTest == null)
                return;
            NavigationParameters p = new NavigationParameters();
            p.Add(NavigationParameter_Test, CurrentTest);
            regionManager.RequestNavigate(Regions.TestInfo, TestViewType == TestViewType.Split ? nameof(SplitTestInfoView) : nameof(MergedTestInfoView), p);
        }
        void OnTestsChanged() {
            if(Tests == null) {
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
            RefreshTestList();
        }
        public void ShowRepositoryAnalyzer() {
            RepositoryAnalyzerRequest.Raise(ServiceLocator.Current.TryResolve<RepositoryAnalyzerViewModel>());
        }
        public void ShowViewResources() {
            ViewResourcesRequest.Raise(ServiceLocator.Current.TryResolve<ViewResourcesViewModel>());
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
            if(TestService.ActualState.ChangedTests.Count == 0) {
                notificationService.DoNotification("Nothing to commit", "Nothing to commit");
                return;
            }
            var confirmation = ServiceLocator.Current.TryResolve<ApplyChangesViewModel>();
            ApplyChangesRequest.Raise(confirmation);
            if(!confirmation.Confirmed)
                return;
            TestService.ActualState.ChangedTests.ForEach(ApplyTest);
            RefreshTestList();
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
            if(TestService.ActualState == null || TestService.ActualState.ChangedTests.Count == 0)
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
            LoadingProgressController.Status = ProgramStatus.Loading;
            farmIntegrator.Start(FarmRefreshed);
        }

        public void ClearCommits() {
            if(TestService.ActualState.ChangedTests.Count == 0)
                return;
            foreach(var test in Tests)
                test.CommitChange = false;
            TestService.ActualState.ChangedTests.Clear();
        }
        public void ChangeTestViewType(TestViewType testViewType) {
            TestViewType = testViewType;
        }

        public void CommitTest(TestInfoModel testInfoModel) {
            TestService.ActualState.ChangedTests.Add(testInfoModel.TestInfo);
        }
        public void UndoCommitTest(TestInfoModel testInfoModel) {
            TestService.ActualState.ChangedTests.Remove(testInfoModel.TestInfo);
        }
    }
}
