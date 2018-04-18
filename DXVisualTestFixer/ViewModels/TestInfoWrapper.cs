using DevExpress.Mvvm;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public class TestInfoWrapper : BindableBase {
        MainViewModel ViewModel;

        public TestInfoWrapper(MainViewModel viewModel, TestInfo testInfo) {
            ViewModel = viewModel;
            TestInfo = testInfo;
        }

        public TestInfo TestInfo { get; private set; }
        public TestState Valid { get { return TestInfo.Valid; } }
        public string Version { get { return TestInfo.Version; } }
        public string TeamName { get { return TestInfo.Team.Name; } }
        public int Dpi { get { return TestInfo.Dpi; } }
        public bool ImageEquals { get { return TestInfo.ImageBeforeArr != null && TestInfo.ImageCurrentArr != null && TestInfo.ImageDiffArr == null; } }

        public bool CommitChange {
            get { return GetProperty(() => CommitChange); }
            set { SetProperty(() => CommitChange, value, OnChanged); }
        }

        void OnChanged() {
            if(CommitChange)
                ViewModel.TestsToCommitCount++;
            else
                ViewModel.TestsToCommitCount--;
        }

        public string ToLog() {
            return String.Format("Team: {0}, Version: {1}, Test: {2}, Theme: {3}", TestInfo?.Team.Name, TestInfo?.Version, TestInfo?.NameWithNamespace, TestInfo?.Theme);
        }
    }
}
