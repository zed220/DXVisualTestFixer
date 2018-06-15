using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.UI.ViewModels {

    public class RepositoryAnalyzerViewModel : BindableBase, Prism.Interactivity.InteractionRequest.INotification {
        string _CurrentVersion;
        List<TimingModel> _CurrentTimings;

        public Dictionary<string, List<TimingModel>> ElapsedTimes { get; }
        public List<string> Versions { get; }
        public string Title { get; set; } = "Repository Analyzer";
        public object Content { get; set; }

        public string CurrentVersion {
            get { return _CurrentVersion; }
            set { SetProperty(ref _CurrentVersion, value, OnCurrentVersionChanged); }
        }
        public List<TimingModel> CurrentTimings {
            get { return _CurrentTimings; }
            set { SetProperty(ref _CurrentTimings, value); }
        }

        public IEnumerable<UICommand> Commands { get; }

        public RepositoryAnalyzerViewModel(ITestsService testsService) {
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService(), MessageResult.OK);
            ElapsedTimes = new Dictionary<string, List<TimingModel>>();
            Versions = new List<string>();
            if(testsService.ActualState.ElapsedTimes == null || testsService.ActualState.ElapsedTimes.Count == 0)
                return;
            foreach(var et in testsService.ActualState.ElapsedTimes) {
                ElapsedTimes.Add(et.Key.Version, et.Value.Select(eti => new TimingModel(eti.Name, eti.Time)).ToList());
                Versions.Add(et.Key.Version);
            }
            CurrentVersion = Versions.Last();
        }

        void OnCurrentVersionChanged() {
            if(String.IsNullOrEmpty(CurrentVersion)) {
                CurrentTimings = null;
                return;
            }
            CurrentTimings = ElapsedTimes[CurrentVersion];
        }
    }
}
