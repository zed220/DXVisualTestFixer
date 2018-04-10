using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public class Team {
        public string Name { get; set; }
        public string Version { get; set; }
        public List<TeamInfo> TeamInfos { get; set; } = new List<TeamInfo>();
    }

    public class TeamInfo {
        public string ServerFolderName { get; set; }
        public string TestResourcesPath { get; set; }
        public int Dpi { get; set; }
        public bool? Optimized { get; set; }
    }
}
