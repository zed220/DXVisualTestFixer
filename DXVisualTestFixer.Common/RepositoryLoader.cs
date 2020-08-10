using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.Common {
	public class RepositoryLoader : FileStringLoaderBase {
		RepositoryLoader(IPlatformInfo platform) : base(@"\\corp\internal\common\visualTests_squirrel\" + platform.VersionsFileName) { }

		public static string[] GetVersions(IPlatformInfo platform) => new RepositoryLoader(platform).Result.ToArray();
		protected override List<string> LoadIfFileNotFound() => new List<string> { "20.1", "20.2" };
	}
}