using System;

namespace DXVisualTestFixer.Common {
    public class TimingInfo {
        public TimingInfo(Repository repository, DateTime? sources, DateTime? tests) {
            Repository = repository;
            Sources = sources;
            Tests = tests;
        }
        public Repository Repository { get; }
        public DateTime? Sources { get; }
        public DateTime? Tests { get; }
    }
}
