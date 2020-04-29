using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.Common {
	public class RepositoryLoader : FileStringLoaderBase {
		RepositoryLoader() : base(ServiceLocator.Current.GetInstance<IPlatformInfo>().DeployPath + "versions.xml") { }

		public static string[] GetVersions() => new RepositoryLoader().Result.ToArray();
		protected override List<string> LoadIfFileNotFound() => new List<string> { "20.1" };
	}
}