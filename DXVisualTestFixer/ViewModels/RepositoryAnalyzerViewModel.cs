using DXVisualTestFixer.Core;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.ViewModels {
    public interface IRepositoryAnalyzerViewModel : INotification { }

    public class TimingModel {
        public TimingModel(string fullName, TimeSpan time) {
            FullName = fullName;
            Time = time;
            PopulateAttributes();
        }

        void PopulateAttributes() {
            string[] splitted = FullName.Split(new string[] { "_", "-" }, StringSplitOptions.RemoveEmptyEntries);
            if(splitted.Length < 1)
                return;
            Prefix = splitted[0];
            if(splitted.Length < 2)
                return;
            Team = splitted[1];
            if(splitted.Length < 3)
                return;
            Dpi = splitted[2];
            if(splitted.Length < 4)
                return;
            Part = splitted[3];
        }

        public string FullName { get; }
        public string Prefix { get; private set; }
        public string Team { get; private set; }
        public string Dpi { get; private set; }
        public string Part { get; private set; }

        public TimeSpan Time { get; }
    }

    public class RepositoryAnalyzerViewModel : BindableBase, IRepositoryAnalyzerViewModel {
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

        public RepositoryAnalyzerViewModel(IMainViewModel mainViewModel) {
            ElapsedTimes = new Dictionary<string, List<TimingModel>>();
            Versions = new List<string>();
            if(mainViewModel.ElapsedTimes == null || mainViewModel.ElapsedTimes.Count == 0)
                return;
            foreach(var et in mainViewModel.ElapsedTimes) {
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
