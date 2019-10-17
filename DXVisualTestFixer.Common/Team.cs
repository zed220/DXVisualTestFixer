using System.Collections.Generic;

namespace DXVisualTestFixer.Common {
	public class Team {
		public static readonly string ErrorName = "Error";
		
		public Team(string name, string version) {
			Name = name;
			Version = version;
		}
		
		public string Name { get; }
		public string Version { get; }
		public List<TeamInfo> TeamInfos { get; } = new List<TeamInfo>();
		
		public static Team CreateErrorTeam(string version) => new Team(ErrorName, version);
	}

	public class TeamInfo {
		public string ServerFolderName { get; set; }
		public string TestResourcesPath { get; set; }
		public string TestResourcesPathOptimized { get; set; }
		public int Dpi { get; set; }
		public bool? Optimized { get; set; }
	}
}