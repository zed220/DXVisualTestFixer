using DevExpress.Mvvm;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.ViewModels {
	public class TestInfoModel : BindableBase, ITestInfoModel {
		readonly MainViewModel ViewModel;

		public TestInfoModel(MainViewModel viewModel, TestInfo testInfo) {
			ViewModel = viewModel;
			TestInfo = testInfo;
		}

		public bool ImageEquals => TestInfo.ImageEquals;

		public TestInfo TestInfo { get; }
		public TestState Valid => TestInfo.Valid;
		public string Version => TestInfo.Version;
		public bool Optimized => TestInfo.Optimized;
		public string TeamName => TestInfo.Team.Name;
		public string Theme => TestInfo.Theme;
		public int Dpi => TestInfo.Dpi;
		public string AdditionalParameters => TestInfo.AdditionalParameters;
		public int Problem => TestInfo.Problem;
		public string ProblemName => TestInfo.ProblemName;

		public bool CommitChange {
			get => GetProperty(() => CommitChange);
			set => SetCommitChange(value);
		}

		public string ToLog() => $"Team: {TestInfo?.Team.Name}, Version: {TestInfo?.Version}, Test: {TestInfo?.NameWithNamespace}, Theme: {TestInfo?.Theme}";

		void SetCommitChange(bool value) {
			if(Valid == TestState.Error)
				return;
			SetProperty(() => CommitChange, value, OnChanged);
		}

		void OnChanged() {
			if(CommitChange)
				ViewModel.CommitTest(this);
			else
				ViewModel.UndoCommitTest(this);
		}
	}
}