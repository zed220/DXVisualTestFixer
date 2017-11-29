using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DXVisualTestFixer.Core {
    public class TestInfo {
        public string Name { get; set; }
        public Team Team { get; set; }
        public string Theme { get; set; }
        public string Fixture { get; set; }
        public string Version { get; set; }
        public int Dpi { get; set; }
        public byte[] ImageBeforeArr { get; set; }
        public byte[] ImageCurrentArr { get; set; }
        public byte[] ImageDiffArr { get; set; }
        public string TextBefore { get; set; }
        public string TextCurrent { get; set; }
        public string TextDiff { get; set; }
        public bool CommitChange { get; set; }

        public string ToLog() {
            return String.Format("Team: {0}, Version: {1}, Test: {2}, Theme: {3}", Team.Name, Version, Name, Theme);
        }
    }
}
