using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.FarmIntegrator2 {
	class FarmTaskInfo : IFarmTaskInfo {
		public FarmTaskInfo(Repository repository, string url) {
			Repository = repository;
			Url = url;
		}

		public Repository Repository { get; }
		public string Url { get; }
		public bool Success { get; set; }

		public override bool Equals(object obj) {
			if(obj == null || GetType() != obj.GetType())
				return false;
			var other = (FarmTaskInfo) obj;
			return Repository.Path == other.Repository.Path && Repository.Version == other.Repository.Version;
		}

		public override int GetHashCode() {
			return Repository.GetHashCode() ^ Repository.Version.GetHashCode();
		}
	}
}