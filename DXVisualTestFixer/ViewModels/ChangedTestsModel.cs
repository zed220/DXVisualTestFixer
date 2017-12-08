using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public class ChangedTestsModel {
        public List<TestInfoWrapper> Tests { get; set; }
        public bool Apply { get; set; }
    }
}
