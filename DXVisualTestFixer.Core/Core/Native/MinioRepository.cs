using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Core {
	 class MinioRepository {
		public MinioRepository(Repository repository, string path) {
			Repository = repository;
			Path = path;
		}
		
		public Repository Repository { get; }
		public string Path { get; }
	}
}