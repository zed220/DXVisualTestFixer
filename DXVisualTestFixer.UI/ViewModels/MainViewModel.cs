using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Data.Filtering;
using DevExpress.Mvvm.Native;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using DXVisualTestFixer.UI.Native;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;
using Prism.Interactivity.InteractionRequest;
using Prism.Regions;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class MainViewModel : ViewModelBase {
		#region Readonly fields

		readonly IConfigSerializer configSerializer;
		readonly Dispatcher dispatcher;
		readonly IActiveService isActiveService;
		readonly ILoggingService loggingService;
		readonly INotificationService notificationService;
		readonly RepositoryObsolescenceTracker obsolescenceTracker;
		readonly IPlatformProvider platformProvider;
		readonly IGitWorker gitWorker;
		readonly ITestsService testService;

		#endregion

		#region Constructor

		public MainViewModel(INotificationService notificationService,
			ILoggingService loggingService,
			IConfigSerializer configSerializer,
			ILoadingProgressController loadingProgressController,
			ITestsService testsService,
			IGitWorker gitWorker,
			IPlatformProvider platformProvider,
			IActiveService isActiveService, ITestsService testService) {
			dispatcher = Dispatcher.CurrentDispatcher;
			this.notificationService = notificationService;
			this.loggingService = loggingService;
			this.configSerializer = configSerializer;
			this.platformProvider = platformProvider;
			this.isActiveService = isActiveService;
			this.testService = testService;
			this.gitWorker = gitWorker;
			obsolescenceTracker = new RepositoryObsolescenceTracker(this.gitWorker, () => _config.Repositories, NoticeRepositoryObsolescenceAsync);
			LoadingProgressController = loadingProgressController;
			testService = testsService;
			testService.PropertyChanged += TestService_PropertyChanged;
			loggingService.MessageReserved += OnLoggingMessageReserved;
		}

		#endregion

		#region Requests

		[UsedImplicitly] public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();
		[UsedImplicitly] public InteractionRequest<IConfirmation> SettingsRequest { get; } = new InteractionRequest<IConfirmation>();
		[UsedImplicitly] public InteractionRequest<IConfirmation> ApplyChangesRequest { get; } = new InteractionRequest<IConfirmation>();
		[UsedImplicitly] public InteractionRequest<IConfirmation> RepositoryOptimizerRequest { get; } = new InteractionRequest<IConfirmation>();
		[UsedImplicitly] public InteractionRequest<INotification> RepositoryAnalyzerRequest { get; } = new InteractionRequest<INotification>();
		[UsedImplicitly] public InteractionRequest<INotification> ViewResourcesRequest { get; } = new InteractionRequest<INotification>();

		#endregion

		#region Fields

		IConfig _config;
		List<ITestInfoModel> _tests;
		ITestInfoModel _currentTest;
		ProgramStatus _status;
		string _currentLogLine;
		int _testsToCommitCount;
		CriteriaOperator _currentFilter;
		List<SolutionModel> _solutions;
		List<TimingInfo> _timingInfo = new List<TimingInfo>();
		string _selectedStateName;
		List<string> _availableStates;
		bool _isReadOnly;
		string _defaultPlatform;

		#endregion

		#region Public properties

		[UsedImplicitly] public ILoadingProgressController LoadingProgressController { get; }

		[UsedImplicitly]
		public List<ITestInfoModel> Tests {
			get => _tests;
			set => SetProperty(ref _tests, value, OnTestsChanged);
		}

		[UsedImplicitly]
		public ITestInfoModel CurrentTest {
			get => _currentTest;
			set => SetProperty(ref _currentTest, value);
		}

		[UsedImplicitly]
		public ProgramStatus Status {
			get => _status;
			set => SetProperty(ref _status, value);
		}

		[UsedImplicitly]
		public string CurrentLogLine {
			get => _currentLogLine;
			set => SetProperty(ref _currentLogLine, value);
		}

		[UsedImplicitly]
		public int TestsToCommitCount {
			get => _testsToCommitCount;
			set => SetProperty(ref _testsToCommitCount, value);
		}

		[UsedImplicitly]
		public CriteriaOperator CurrentFilter {
			get => _currentFilter;
			set => SetProperty(ref _currentFilter, value);
		}

		[UsedImplicitly]
		public List<SolutionModel> Solutions {
			get => _solutions;
			set => SetProperty(ref _solutions, value);
		}

		[UsedImplicitly]
		public List<TimingInfo> TimingInfo {
			get => _timingInfo;
			set => SetProperty(ref _timingInfo, value);
		}

		[UsedImplicitly]
		public string SelectedStateName {
			get => _selectedStateName;
			set {
				_selectedStateName = value;
				RaisePropertyChanged();
				RefreshTestList();
			}
		}

		[UsedImplicitly]
		public List<string> AvailableStates {
			get => _availableStates;
			private set {
				_availableStates = value;
				RaisePropertyChanged();
			}
		}

		[UsedImplicitly]
		public bool IsReadOnly {
			get => _isReadOnly;
			set {
				_isReadOnly = value;
				RaisePropertyChanged();
			}
		}

		#endregion

		#region Public Methods

		[UsedImplicitly]
		public async void InitializeAsync() {
			Status = ProgramStatus.Loading;
			await Task.Delay(1).ConfigureAwait(false);
			loggingService.SendMessage("Checking config");
			UpdateConfigAndCheckUpdated();
			var preventExecution = false;
			await dispatcher.InvokeAsync(() => {
				if(!ValidateConfigCheckChanged()) return;
				_config = null;
				InitializeAsync();
				preventExecution = true;
			}).Task.ConfigureAwait(false);
			if(preventExecution)
				return;

			var configChanged = UpdateConfigAndCheckUpdated();
			await dispatcher.InvokeAsync(() => { SelectedStateName = _defaultPlatform; }).Task.ConfigureAwait(false);
			await FillSolutionsAsync().ConfigureAwait(false);
			loggingService.SendMessage("Config loaded");
			await dispatcher.InvokeAsync(() => {
				return Status = ProgramStatus.Idle;
			}).Task.ConfigureAwait(false);
			RefreshTestList();
		}

		[UsedImplicitly]
		public void ShowRepositoryOptimizer() {
			if(CheckHasUncommittedChanges() || CheckAlarmAdmin())
				return;
			var confirmation = ServiceLocator.Current.TryResolve<RepositoryOptimizerViewModel>();
			RepositoryOptimizerRequest.Raise(confirmation);
			if(!confirmation.Confirmed)
				return;
			TestsToCommitCount = 0;
			InitializeAsync();
		}

		[UsedImplicitly]
		public void ShowRepositoryAnalyzer() => RepositoryAnalyzerRequest.Raise(ServiceLocator.Current.TryResolve<RepositoryAnalyzerViewModel>());

		[UsedImplicitly]
		public void ShowViewResources() => ViewResourcesRequest.Raise(ServiceLocator.Current.TryResolve<ViewResourcesViewModel>());

		[UsedImplicitly]
		public void ShowSettings() {
			if(ShowSettingsCore())
				InitializeAsync();
		}

		[UsedImplicitly]
		public void StageCurrentTest() {
			if(CurrentTest == null || CurrentTest.Valid == TestState.Error)
				return;
			CurrentTest.CommitChange = true;
		}

		[UsedImplicitly]
		public void UnstageCurrentTest() {
			if(CurrentTest == null)
				return;
			CurrentTest.CommitChange = false;
		}

		[UsedImplicitly]
		public void ApplyChanges() {
			Status = ProgramStatus.Loading;
			obsolescenceTracker.Stop();
			if(TestsToCommitCount == 0) {
				notificationService.DoNotification("Nothing to commit", "Nothing to commit");
				obsolescenceTracker.Start();
				Status = ProgramStatus.Idle;
				return;
			}

			if(testService.SelectedState.ChangedTests.Count == 0) {
				notificationService.DoNotification("Nothing to commit", "Nothing to commit");
				obsolescenceTracker.Start();
				Status = ProgramStatus.Idle;
				return;
			}

			var confirmation = ServiceLocator.Current.TryResolve<ApplyChangesViewModel>();
			ApplyChangesRequest.Raise(confirmation);
			if(!confirmation.Confirmed) {
				obsolescenceTracker.Start();
				Status = ProgramStatus.Idle;
				return;
			}

			Task.Factory.StartNew(() => ApplyChangesCore(confirmation.IsAutoCommit, confirmation.CommitCaption));
		}

		[PublicAPI]
		public async void RefreshTestList() {
			if(Status == ProgramStatus.Loading)
				return;
			var preventUpdate = false;
			await dispatcher.InvokeAsync(() => {
				Status = ProgramStatus.Loading;
				if(CheckHasUncommittedChanges()) {
					preventUpdate = true;
					return;
				}
				obsolescenceTracker.Stop();
				Tests = null;
			}).Task.ConfigureAwait(false);
			if(preventUpdate)
				return;
			loggingService.SendMessage("Waiting response from minio");
			try {
				await ActualizeRepositories();
				await UpdateAllTests();
			}
			catch(Exception e) {
				await dispatcher.InvokeAsync(() => {
					notificationService.DoNotification("Error", e.Message, MessageBoxImage.Error);
					Application.Current.MainWindow.Close();
				}).Task.ConfigureAwait(false);
			}
			await dispatcher.InvokeAsync(() => {
				Status = ProgramStatus.Idle; 
				obsolescenceTracker.Start();	
			}).Task.ConfigureAwait(false);
		}

		[PublicAPI]
		public void ClearCommits() {
			if(TestsToCommitCount == 0)
				return;
			foreach(var test in Tests)
				test.CommitChange = false;
			testService.SelectedState?.ChangedTests?.Clear();
		}

		[PublicAPI]
		public void CommitTest(TestInfoModel testInfoModel) {
			TestsToCommitCount++;
			testService.SelectedState.ChangedTests.Add(testInfoModel.TestInfo);
		}

		[PublicAPI]
		public void UndoCommitTest(TestInfoModel testInfoModel) {
			TestsToCommitCount--;
			testService.SelectedState.ChangedTests.Remove(testInfoModel.TestInfo);
		}

		#endregion

		bool ShowSettingsCore() {
			if(CheckHasUncommittedChanges())
				return false;
			var settingsViewModel = ServiceLocator.Current.TryResolve<ISettingsViewModel>();
			SettingsRequest.Raise(settingsViewModel);
			if(!settingsViewModel.Confirmed)
				return false;
			TestsToCommitCount = 0;
			configSerializer.SaveConfig(settingsViewModel.Config);
			return true;
		}

		async Task NoticeRepositoryObsolescenceAsync() {
			if(!isActiveService.IsActive) {
				obsolescenceTracker.Stop();
				isActiveService.PropertyChanged += LinqExtensions.WithReturnValue<PropertyChangedEventHandler>(x => {
					return async (sender, args) => {
						if(args.PropertyName != nameof(IActiveService.IsActive))
							return;
						isActiveService.PropertyChanged -= x.Value;
						await NoticeRepositoryObsolescenceCoreAsync();
					};
				});
				return;
			}

			await NoticeRepositoryObsolescenceCoreAsync();
		}

		async Task NoticeRepositoryObsolescenceCoreAsync() {
			await Task.Delay(TimeSpan.FromSeconds(1));
			obsolescenceTracker.Start();
			if(!CheckConfirmation(ConfirmationRequest, "Repositories outdated", "Repositories outdated. Refresh tests list?"))
				return;
			RefreshTestList();
		}

		void TestService_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if(e.PropertyName == nameof(ITestsService.CurrentFilter))
				CurrentFilter = CriteriaOperator.Parse(testService.CurrentFilter);
		}

		void OnLoggingMessageReserved(object sender, IMessageEventArgs args) => CurrentLogLine = args.Message;

		async Task UpdateAllTests() {
			loggingService.SendMessage("Refreshing tests");
			LoadingProgressController.Start();
			await testService.SelectState(_defaultPlatform, SelectedStateName).ConfigureAwait(false);
			var testInfoContainer = testService.SelectedState;
			IsReadOnly = !testInfoContainer.AllowEditing;
			var tests = testInfoContainer.TestList.Where(t => t != null).Select(t => new TestInfoModel(this, t)).Cast<ITestInfoModel>().ToList();
			loggingService.SendMessage("");
			await dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => {
				Tests = tests;
				AvailableStates = testService.States.Keys.ToList();
				TimingInfo = new List<TimingInfo>(testInfoContainer.Timings);
				Status = ProgramStatus.Idle;
				LoadingProgressController.Stop();
			}));
		}

		async Task FillSolutionsAsync() {
			var actualSolutions = new List<SolutionModel>();
			if(_config.Repositories != null)
				actualSolutions = await DoAsync(FillSolutionsCore).ConfigureAwait(false);
			await dispatcher.InvokeAsync(() => Solutions = actualSolutions).Task.ConfigureAwait(false);
		}

		List<SolutionModel> FillSolutionsCore() {
			var actualSolutions = new List<SolutionModel>();
			foreach(var repository in _config.GetLocalRepositories().Where(s => s.Platform == _defaultPlatform)) {
				actualSolutions.Add(new SolutionModel(repository.Version, repository.Path));
				RepositoryModel.InitializeBinIfNeed(repository.Path, repository.Version);
			}

			return actualSolutions;
		}

		bool UpdateConfigAndCheckUpdated() {
			var config = configSerializer.GetConfig(false);
			_defaultPlatform = config.DefaultPlatform;
			if(_config != null && configSerializer.IsConfigEquals(config, _config))
				return false;
			_config = config;
			return true;
		}

		async Task<T> DoAsync<T>(Func<T> func) => await Task.Run(func);
		async Task DoAsync(Action action) => await Task.Run(action);

		bool ValidateConfigCheckChanged() {
			if(_config.GetLocalRepositories().ToList().Count == 0) {
				notificationService.DoNotification("Add repositories in settings", "Add repositories in settings");
				var settingsChanged = ShowSettingsCore();
				if(_config.GetLocalRepositories().ToList().Count == 0) 
					notificationService.DoNotification("Add repositories in settings", "Add repositories in settings");
				return settingsChanged;
			}

			if(string.IsNullOrWhiteSpace(_config.DefaultPlatform)) {
				notificationService.DoNotification("Default Platform Does Not Set", "Select default platform in Settings", MessageBoxImage.Warning);
				return ShowSettingsCore();
			}

			foreach(var repo in _config.Repositories.Where(r => r.Platform == _config.DefaultPlatform))
				if(!repo.IsDownloaded()) {
					notificationService.DoNotification("Missing Repositories", "Download repositories in Settings", MessageBoxImage.Warning);
					return ShowSettingsCore();
				}

			return false;
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

		async Task ApplyChangesCore(bool commitIntoGitRepo, string commitCaption) {
			if(commitIntoGitRepo && !await ActualizeRepositories()) {
				obsolescenceTracker.Start();
				Status = ProgramStatus.Idle;
				return;
			}

			await Task.Factory.StartNew(() => testService.SelectedState.ChangedTests.ForEach(ApplyTest));
			if(commitIntoGitRepo && !await PushTestsInRepository(commitCaption)) {
				obsolescenceTracker.Start();
				Status = ProgramStatus.Idle;
				return;
			}

			TestsToCommitCount = 0;
			RefreshTestList();
		}

		async Task<bool> ActualizeRepositories() {
			_defaultPlatform ??= _config.DefaultPlatform;

			foreach(var repo in _config.GetLocalRepositories()) {
				if(!gitWorker.SetHttpRepository(platformProvider.PlatformInfos.Single(p => p.Name == repo.Platform).GitRepository, repo)) {
					notificationService.DoNotification("Updating repository source failed", $"Can't update source (origin or upstream) for repository {repo.Version} that located at {repo.Path}", MessageBoxImage.Error);
					return await Task.FromResult(false);
				}

				if(await gitWorker.Update(repo) != GitUpdateResult.Error) continue;
				notificationService.DoNotification("Updating failed", $"Repository {repo.Version} in {repo.Path} can't update", MessageBoxImage.Error);
				return await Task.FromResult(false);
			}

			return await Task.FromResult(true);
		}

		async Task<bool> PushTestsInRepository(string commitCaption) {
			foreach(var group in testService.SelectedState.ChangedTests.GroupBy(t => t.Repository)) {
				var commitResult = await gitWorker.Commit(group.Key, commitCaption);
				if(commitResult != GitCommitResult.Error) continue;
				dispatcher.Invoke(() => notificationService.DoNotification("Pushing failed", $"Can't push repository {group.Key.Version} that located at {group.Key.Path}", MessageBoxImage.Error));
				return await Task.FromResult(false);
			}

			return await Task.FromResult(true);
		}

		bool ShowCheckOutMessageBox(string text) => CheckConfirmation(ConfirmationRequest, "Readonly file detected", "Please remove readonly for \n" + text);

		void ApplyTest(TestInfo testInfo) {
			if(testService.ApplyTest(testInfo, ShowCheckOutMessageBox))
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
	}
}