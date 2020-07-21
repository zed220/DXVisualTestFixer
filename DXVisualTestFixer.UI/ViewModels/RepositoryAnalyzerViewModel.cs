using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using JetBrains.Annotations;
using BindableBase = Prism.Mvvm.BindableBase;
using INotification = Prism.Interactivity.InteractionRequest.INotification;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class RepositoryAnalyzerViewModel : BindableBase, INotification {
		IReadOnlyCollection<TimingModel> _currentTimings;
		string _currentVersion;

		public RepositoryAnalyzerViewModel(ITestsService testsService) {
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService(), MessageResult.OK);
			ElapsedTimes = new Dictionary<string, IReadOnlyCollection<TimingModel>>();
			var versions = new List<string>();
			if(testsService.SelectedState.ElapsedTimes == null || testsService.SelectedState.ElapsedTimes.Count == 0)
				return;
			foreach(var et in testsService.SelectedState.ElapsedTimes) {
				ElapsedTimes.Add(et.Key.Version, et.Value.Select(eti => new TimingModel(eti.Name, eti.Time)).ToReadOnlyCollection());
				versions.Add(et.Key.Version);
			}
			versions.Sort();
			Versions = versions.ToReadOnlyCollection();
			CurrentVersion = versions.Last();
		}

		[PublicAPI] public Dictionary<string, IReadOnlyCollection<TimingModel>> ElapsedTimes { get; }
		[PublicAPI] public IReadOnlyCollection<string> Versions { get; }

		[PublicAPI] public string CurrentVersion {
			get => _currentVersion;
			set => SetProperty(ref _currentVersion, value, OnCurrentVersionChanged);
		}

		[PublicAPI] public IReadOnlyCollection<TimingModel> CurrentTimings {
			get => _currentTimings;
			set => SetProperty(ref _currentTimings, value);
		}

		[PublicAPI] public IEnumerable<UICommand> Commands { get; }
		public string Title { get; set; } = "Repository Analyzer";
		public object Content { get; set; }

		void OnCurrentVersionChanged() {
			if(string.IsNullOrEmpty(CurrentVersion)) {
				CurrentTimings = null;
				return;
			}

			CurrentTimings = ElapsedTimes[CurrentVersion];
		}
	}
}