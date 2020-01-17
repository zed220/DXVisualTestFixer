using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using JetBrains.Annotations;
using Prism.Interactivity.InteractionRequest;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class ApplyChangesViewModel : BindableBase, IConfirmation {
		const string DefaultCommitCaption = "Update tests";
		readonly ITestsService testsService;

		List<TestInfo> _ChangedTests;
		string _CommitCaption = DefaultCommitCaption;
		bool _IsAutoCommit = true;
		bool _CanChangeAutoCommit = true;

		public ApplyChangesViewModel(ITestsService testsService) {
			this.testsService = testsService;
			Title = "Settings";
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
			Commands.Single(c => c.IsDefault).Command = new DelegateCommand(() => {
				if(string.IsNullOrWhiteSpace(CommitCaption))
					_CommitCaption = DefaultCommitCaption;
				if(CommitCaption.Length > 255)
					_CommitCaption = CommitCaption.Substring(0, 255);
				Confirmed = true;
			});
			ChangedTests = testsService.SelectedState.ChangedTests;
			if(ChangedTests.FirstOrDefault(x => x.Repository.ReadOnly) != null) {
				CanChangeAutoCommit = false;
				IsAutoCommit = false;
			}
		}

		[UsedImplicitly]
		public IEnumerable<UICommand> DialogCommands { get; set; }
		
		[UsedImplicitly]
		public List<TestInfo> ChangedTests {
			get => _ChangedTests;
			set => SetProperty(ref _ChangedTests, value);
		}

		public bool IsAutoCommit {
			get => _IsAutoCommit;
			[UsedImplicitly]
			set => SetProperty(ref _IsAutoCommit, value);
		}
		public bool CanChangeAutoCommit {
			get => _CanChangeAutoCommit;
			[UsedImplicitly]
			set => SetProperty(ref _CanChangeAutoCommit, value);
		}
		public string CommitCaption {
			get => _CommitCaption;
			[UsedImplicitly]
			set => SetProperty(ref _CommitCaption, value);
		}
		
		[UsedImplicitly]
		public IEnumerable<UICommand> Commands { get; }

		public bool Confirmed { get; set; }
		public string Title { get; set; }
		public object Content { get; set; }
	}
}