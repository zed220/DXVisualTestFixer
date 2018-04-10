using DevExpress.Mvvm;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface IRepositoryAnalyzerViewModel : ISupportParameter { }

    public class TimingModel {
        public TimingModel(string name, TimeSpan time) {
            Name = name;
            Time = time;
        }

        public string Name { get; }
        public TimeSpan Time { get; }
    }

    public class RepositoryAnalyzerViewModel : ViewModelBase, IRepositoryAnalyzerViewModel {
        public Dictionary<string, List<TimingModel>> ElapsedTimes {
            get { return GetProperty(() => ElapsedTimes); }
            set { SetProperty(() => ElapsedTimes, value); }
        }
        public List<string> Versions {
            get { return GetProperty(() => Versions); }
            set { SetProperty(() => Versions, value); }
        }
        public string CurrentVersion {
            get { return GetProperty(() => CurrentVersion); }
            set { SetProperty(() => CurrentVersion, value, OnCurrentVersionChanged); }
        }

        public List<TimingModel> CurrentTimings {
            get { return GetProperty(() => CurrentTimings); }
            set { SetProperty(() => CurrentTimings, value); }
        }

        void OnCurrentVersionChanged() {
            if(String.IsNullOrEmpty(CurrentVersion)) {
                CurrentTimings = null;
                return;
            }
            CurrentTimings = ElapsedTimes[CurrentVersion];
        }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            ElapsedTimes = new Dictionary<string, List<TimingModel>>();
            Versions = new List<string>();
            var elapsedTimes = parameter as Dictionary<Repository, List<ElapsedTimeInfo>>;
            if(elapsedTimes == null || elapsedTimes.Count == 0)
                return;
            foreach(var et in elapsedTimes) {
                ElapsedTimes.Add(et.Key.Version, et.Value.Select(eti => new TimingModel(eti.Name, eti.Time)).ToList());
                Versions.Add(et.Key.Version);
            }
            CurrentVersion = Versions.Last();
        }
    }
}
