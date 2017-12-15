using DevExpress.Mvvm;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public class TestInfoWrapper : BindableBase {
        public TestInfoWrapper(TestInfo testInfo) {
            TestInfo = testInfo;
            Valid = TestsService.TestValid(testInfo);
        }

        public TestInfo TestInfo { get; private set; }

        public bool CommitChange {
            get { return GetProperty(() => CommitChange); }
            set { SetProperty(() => CommitChange, value); }
        }
        public bool Valid { get; private set; }

        public string ToLog() {
            return String.Format("Team: {0}, Version: {1}, Test: {2}, Theme: {3}", TestInfo?.Team.Name, TestInfo?.Version, TestInfo?.Name, TestInfo?.Theme);
        }
    }
}
