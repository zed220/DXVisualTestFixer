using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using Prism.Interactivity.InteractionRequest;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.UI.ViewModels {
	public class ApplyChangesViewModel : BindableBase, IConfirmation {
		const string DefaultCommitCaption = "Update tests";
		readonly ITestsService testsService;

		List<TestInfo> _ChangedTests;
		string _CommitCaption = DefaultCommitCaption;
		bool _IsAutoCommit = true;

		public ApplyChangesViewModel(ITestsService testsService) {
			this.testsService = testsService;
			Title = "Settings";
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
			Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => {
				if(string.IsNullOrWhiteSpace(CommitCaption))
					_CommitCaption = DefaultCommitCaption;
				if(CommitCaption.Length > 255)
					_CommitCaption = CommitCaption.Substring(0, 255);
				Confirmed = true;
			});
			ChangedTests = testsService.ActualState.ChangedTests;
		}

		public IEnumerable<UICommand> DialogCommands { get; set; }

		public List<TestInfo> ChangedTests {
			get => _ChangedTests;
			set => SetProperty(ref _ChangedTests, value);
		}

		public bool IsAutoCommit {
			get => _IsAutoCommit;
			set => SetProperty(ref _IsAutoCommit, value);
		}

		public string CommitCaption {
			get => _CommitCaption;
			set => SetProperty(ref _CommitCaption, value);
		}

		public IEnumerable<UICommand> Commands { get; }

		public bool Confirmed { get; set; }
		public string Title { get; set; }
		public object Content { get; set; }
	}
}