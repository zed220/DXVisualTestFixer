using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class Team {
        public string Name { get; set; }
        public string TestResourcesPath { get; set; }
        public string ServerFolderName { get; set; }
        public bool SupportOptimized { get; set; }
    }
}
