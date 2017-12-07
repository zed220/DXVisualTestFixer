using DevExpress.XtraEditors.DXErrorProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class Repository {
        public static readonly string[] Versions = new string[] { "16.2", "17.1", "17.2", "18.1" };

        public string Version { get; set; }
        public string Path { get; set; }

        public string GetTaskName() {
            return String.Format("Test.v{0} WPF.Functional VisualTests UnoptimizedMode", Version);
        }
        public string GetTaskName_Optimized() {
            return String.Format("Test.v{0} WPF.Functional VisualTests", Version);
        }
    }
}
