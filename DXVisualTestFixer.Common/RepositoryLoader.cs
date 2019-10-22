using System.Collections.Generic;

namespace DXVisualTestFixer.Common {
	public class RepositoryLoader : FileStringLoaderBase {
		RepositoryLoader() : base(@"\\corp\internal\common\visualTests_squirrel\versions.xml") { }

		public static string[] GetVersions() => new RepositoryLoader().Result.ToArray();
		protected override List<string> LoadIfFileNotFound() => new List<string> {"18.2", "19.1", "19.2"};
	}
}