using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm.Native;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using DXVisualTestFixer.UI.Native;
using Microsoft.Practices.ServiceLocation;
using Prism.Interactivity.InteractionRequest;
using Prism.Regions;

namespace DXVisualTestFixer.UI.ViewModels {
	public enum ProgramStatus {
		Idle,
		Loading
	}

	public class MainViewModel : ViewModelBase {
		public static string NavigationParameter_Test = "Test";
		readonly IConfigSerializer configSerializer;
		readonly Dispatcher Dispatcher;
		readonly IFarmIntegrator farmIntegrator;
		readonly IActiveService isActiveService;
		readonly ILoggingService loggingService;

		readonly INotificationService notificationService;
		readonly RepositoryObsolescenceTracker obsolescenceTracker;
		readonly IRegionManager regionManager;

		public MainViewModel(INotificationService notificationService,
			IRegionManager regionManager,
			ILoggingService loggingService,
			IFarmIntegrator farmIntegrator,
			IConfigSerializer configSerializer,
			ILoadingProgressController loadingProgressController,
			ITestsService testsService,
			IGitWorker gitWorker,
			IActiveService isActiveService) {
			Dispatcher = Dispatcher.CurrentDispatcher;
			this.notificationService = notificationService;
			this.regionManager = regionManager;
			this.loggingService = loggingService;
			this.farmIntegrator = farmIntegrator;
			this.configSerializer = configSerializer;
			this.isActiveService = isActiveService;
			obsolescenceTracker = new RepositoryObsolescenceTracker(NoticeRepositoryObsolescence);
			LoadingProgressController = loadingProgressController;
			TestService = testsService;
			_GitWorker = gitWorker;
			TestService.PropertyChanged += TestService_PropertyChanged;
			loggingService.MessageReserved += OnLoggingMessageReserved;
			UpdateConfig();
		}

		public List<ITestInfoModel> Tests {
			get => _Tests;
			set => SetProperty(ref _Tests, value, OnTestsChanged);
		}

		public ITestInfoModel CurrentTest {
			get => _CurrentTest;
			set => SetProperty(ref _CurrentTest, value);
		}

		public ProgramStatus Status {
			get => _Status;
			set => SetProperty(ref _Status, value);
		}

		public string CurrentLogLine {
			get => _CurrentLogLine;
			set => SetProperty(ref _CurrentLogLine, value);
		}

		public int TestsToCommitCount {
			get => _TestsToCommitCount;
			set => SetProperty(ref _TestsToCommitCount, value);
		}

		public CriteriaOperator CurrentFilter {
			get => _CurrentFilter;
			set => SetProperty(ref _CurrentFilter, value);
		}

		public ITestsService TestService {
			get => _TestService;
			set => SetProperty(ref _TestService, value);
		}

		public List<SolutionModel> Solutions {
			get => _Solutions;
			set => SetProperty(ref _Solutions, value);
		}

		public List<TimingInfo> TimingInfo {
			get => _TimingInfo;
			set => SetProperty(ref _TimingInfo, value);
		}

		public ILoadingProgressController LoadingProgressController { get; }

		public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();
		public InteractionRequest<IConfirmation> SettingsRequest { get; } = new InteractionRequest<IConfirmation>();
		public InteractionRequest<IConfirmation> ApplyChangesRequest { get; } = new InteractionRequest<IConfirmation>();
		public InteractionRequest<IConfirmation> RepositoryOptimizerRequest { get; } = new InteractionRequest<IConfirmation>();
		public InteractionRequest<INotification> RepositoryAnalyzerRequest { get; } = new InteractionRequest<INotification>();
		public InteractionRequest<INotification> ViewResourcesRequest { get; } = new InteractionRequest<INotification>();

		void NoticeRepositoryObsolescence() {
			if(!isActiveService.IsActive) {
				obsolescenceTracker.Stop();
				isActiveService.PropertyChanged += LinqExtensions.WithReturnValue<PropertyChangedEventHandler>(x => {
					return (sender, args) => {
						if(args.PropertyName != nameof(IActiveService.IsActive))
							return;
						isActiveService.PropertyChanged -= x.Value;
						NoticeRepositoryObsolescenceCore();
					};
				});
				return;
			}

			NoticeRepositoryObsolescenceCore();
		}

		void NoticeRepositoryObsolescenceCore() {
			obsolescenceTracker.Start();
			if(CheckConfirmation(ConfirmationRequest, "Repositories may be outdated", "Repositories may be outdated. Refresh tests list?"))
				UpdateContent();
		}

		void TestService_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if(e.PropertyName == nameof(ITestsService.CurrentFilter))
				CurrentFilter = CriteriaOperator.Parse(TestService.CurrentFilter);
		}

		void OnLoggingMessageReserved(object sender, IMessageEventArgs args) {
			CurrentLogLine = args.Message;
		}

		async void FarmRefresh() {
			try {
				await ActualizeRepositories();
				await UpdateAllTests();
			}
			catch(Exception e) {
				Dispatcher.Invoke(() => {
					notificationService.DoNotification("Error", e.Message, MessageBoxImage.Error);
					Application.Current.MainWindow.Close();
				});
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
				TimingInfo = new List<TimingInfo>(testInfoContainer.Timings);
				Status = ProgramStatus.Idle;
				LoadingProgressController.Stop();
			}));
		}

		void FillSolutions() {
			if(Config.Repositories == null) {
				Solutions = new List<SolutionModel>();
				return;
			}

			var actualSolutions = new List<SolutionModel>();
			foreach(var repository in Config.GetLocalRepositories())
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
			ServiceLocator.Current.GetInstance<IAppearanceService>()?.SetTheme( /*Config.ThemeName*/ "Office2019Colorful", "DarkLilac");
			loggingService.SendMessage("Config loaded");
			UpdateContent();
		}

		void UpdateContent() {
			if(Config.GetLocalRepositories().ToList().Count == 0) {
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
					notificationService.DoNotification("Add repositories in settings", "Add repositories in settings");
					ShowSettings();
					if(Config.GetLocalRepositories().ToList().Count == 0) {
						notificationService.DoNotification("Add repositories in settings", "Add repositories in settings");
						Status = ProgramStatus.Idle;
						return;
					}

					RefreshTestList();
				}));
				return;
			}

			foreach(var repo in Config.Repositories)
				if(!repo.IsDownloaded()) {
					Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
						notificationService.DoNotification("Missing Repositories", "Download repositories in Settings", MessageBoxImage.Warning);
						ShowSettings();
						RefreshTestList();
					}));
					Status = ProgramStatus.Idle;
					return;
				}

			RefreshTestList();
		}

		void OnTestsChanged() {
			if(Tests == null) {
				TestsToCommitCount = 0;
				CurrentTest = null;
				CurrentFilter = null;
			}
			else {
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
			var confirmation = ServiceLocator.Current.TryResolve<ISettingsViewModel>();
			SettingsRequest.Raise(confirmation);
			if(!confirmation.Confirmed)
				return;
			TestsToCommitCount = 0;
			configSerializer.SaveConfig(confirmation.Config);
			UpdateConfig();
		}

		public void CommitCurrentTest() {
			if(CurrentTest == null || CurrentTest.Valid == TestState.Error)
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
			Status = ProgramStatus.Loading;
			Task.Factory.StartNew(() => ApplyChangesCore(confirmation.IsAutoCommit, confirmation.CommitCaption));
		}

		async Task ApplyChangesCore(bool commitIntoGitRepo, string commitCaption) {
			if(commitIntoGitRepo && !await ActualizeRepositories())
				return;
			await Task.Factory.StartNew(() => TestService.ActualState.ChangedTests.ForEach(ApplyTest));
			if(commitIntoGitRepo && !await PushTestsInRepository(commitCaption))
				return;
			TestsToCommitCount = 0;
			UpdateContent();
		}

		async Task<bool> ActualizeRepositories() {
			foreach(var repo in Config.GetLocalRepositories()) {
				if(!_GitWorker.SetHttpRepository(repo)) {
					notificationService.DoNotification("Updating repository source failed", $"Can't update source (origin or upstream) for repository {repo.Version} that located at {repo.Path}", MessageBoxImage.Error);
					return await Task.FromResult(false);
				}

				if(await _GitWorker.Update(repo) == GitUpdateResult.Error) {
					notificationService.DoNotification("Updating failed", $"Repository {repo.Version} in {repo.Path} can't update", MessageBoxImage.Error);
					return await Task.FromResult(false);
				}
			}

			return await Task.FromResult(true);
		}

		async Task<bool> PushTestsInRepository(string commitCaption) {
			foreach(var group in TestService.ActualState.ChangedTests.GroupBy(t => t.Repository)) {
				var commitResult = await _GitWorker.Commit(group.Key, commitCaption);
				if(commitResult == GitCommitResult.Error) {
					Dispatcher.Invoke(() => notificationService.DoNotification("Pushing failed", $"Can't push repository {group.Key.Version} that located at {group.Key.Path}", MessageBoxImage.Error));
					return await Task.FromResult(false);
				}
			}

			return await Task.FromResult(true);
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
			obsolescenceTracker.Stop();
			loggingService.SendMessage("Waiting response from farm integrator");
			Tests = null;
			Status = ProgramStatus.Loading;
			Task.Factory.StartNew(FarmRefresh).ConfigureAwait(false);
			obsolescenceTracker.Start();
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
		readonly IGitWorker _GitWorker;
		List<TimingInfo> _TimingInfo = new List<TimingInfo>();

		#endregion
	}
}