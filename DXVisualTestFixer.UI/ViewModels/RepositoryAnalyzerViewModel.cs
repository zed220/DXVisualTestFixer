using System.Collections.Generic;
using System.Linq;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using JetBrains.Annotations;
using BindableBase = Prism.Mvvm.BindableBase;
using INotification = Prism.Interactivity.InteractionRequest.INotification;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class RepositoryAnalyzerViewModel : BindableBase, INotification {
		List<TimingModel> _CurrentTimings;
		string _CurrentVersion;

		public RepositoryAnalyzerViewModel(ITestsService testsService) {
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService(), MessageResult.OK);
			ElapsedTimes = new Dictionary<string, List<TimingModel>>();
			Versions = new List<string>();
			if(testsService.SelectedState.ElapsedTimes == null || testsService.SelectedState.ElapsedTimes.Count == 0)
				return;
			foreach(var et in testsService.SelectedState.ElapsedTimes) {
				ElapsedTimes.Add(et.Key.Version, et.Value.Select(eti => new TimingModel(eti.Name, eti.Time)).ToList());
				Versions.Add(et.Key.Version);
			}

			CurrentVersion = Versions.Last();
		}

		public Dictionary<string, List<TimingModel>> ElapsedTimes { get; }
		public List<string> Versions { get; }

		public string CurrentVersion {
			get => _CurrentVersion;
			set => SetProperty(ref _CurrentVersion, value, OnCurrentVersionChanged);
		}

		public List<TimingModel> CurrentTimings {
			get => _CurrentTimings;
			set => SetProperty(ref _CurrentTimings, value);
		}

		public IEnumerable<UICommand> Commands { get; }
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