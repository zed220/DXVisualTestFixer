using System;

namespace DXVisualTestFixer.Common {
    public class TimingInfo {
        public TimingInfo(DateTime? sources, DateTime? tests) {
            Sources = sources;
            Tests = tests;
        }
        public DateTime? Sources { get; }
        public DateTime? Tests { get; }
    }
}
