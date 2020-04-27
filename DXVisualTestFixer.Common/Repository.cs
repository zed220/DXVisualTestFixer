using System.IO;
using JetBrains.Annotations;

namespace DXVisualTestFixer.Common {
	public class Repository {
		string _version;

		[UsedImplicitly]
		public Repository() {
			
		}

		Repository(string version, string forkName, string path, bool readOnly) {
			Version = version;
			VersionAndFork = version == forkName ? forkName : $"{version}({forkName})";
			Path = path;
			ReadOnly = readOnly;
		}
		
		public string VersionAndFork { get; private set; }

		public string Version {
			get => _version;
			set {
				_version = value;
				VersionAndFork = _version;
			}
		}

		public string Path { get; set; }
		public bool ReadOnly { get; }
		public string MinioPath { get; set; }

		public bool IsDownloaded() => File.Exists(System.IO.Path.Combine(Path, "VisualTestsConfig.xml"));

		public static Repository CreateRegular(string version, string path) => new Repository(version, version, path, false);
		public static Repository CreateFork(string version, string forkName, string minioPath, string path) => new Repository(version, forkName, path, true) { MinioPath = minioPath };
	}
}