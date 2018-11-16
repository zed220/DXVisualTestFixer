using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.ViewModels {
    public class TestInfoModel : BindableBase, ITestInfoModel {
        MainViewModel ViewModel;

        public TestInfoModel(MainViewModel viewModel, TestInfo testInfo) {
            ViewModel = viewModel;
            TestInfo = testInfo;
        }

        public TestInfo TestInfo { get; private set; }
        public TestState Valid { get { return TestInfo.Valid; } }
        public string Version { get { return TestInfo.Version; } }
        public string TeamName { get { return TestInfo.Team.Name; } }
        public int Dpi { get { return TestInfo.Dpi; } }
        public int Problem { get { return TestInfo.Problem; } }
        public string ProblemName { get { return TestInfo.ProblemName; } }
        public bool ImageEquals { get { return TestInfo.ImageEquals; } }

        public bool CommitChange {
            get { return GetProperty(() => CommitChange); }
            set { SetCommitChange(value); }
        }

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

        public string ToLog() {
            return String.Format("Team: {0}, Version: {1}, Test: {2}, Theme: {3}", TestInfo?.Team.Name, TestInfo?.Version, TestInfo?.NameWithNamespace, TestInfo?.Theme);
        }
    }
}
