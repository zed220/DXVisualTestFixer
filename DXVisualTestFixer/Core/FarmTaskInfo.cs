using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class FarmTaskInfo {
        public FarmTaskInfo(Repository repository, string url) {
            Repository = repository;
            Url = url;
        }

        public Repository Repository { get; private set; }
        public string Url { get; private set; }
    }
}
