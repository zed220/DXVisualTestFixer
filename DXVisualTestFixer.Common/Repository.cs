using System.IO;
using JetBrains.Annotations;

namespace DXVisualTestFixer.Common {
	public class Repository {
		string _version;

		[UsedImplicitly]
		public Repository() {
			
		}

		Repository(string platform, string version, string forkName, string path, bool readOnly) {
			Platform = platform;
			Version = version;
			VersionAndFork = version == forkName ? forkName : $"{version}({forkName})";
			Path = path;
			ReadOnly = readOnly;
		}

		public string Platform { get; set; }

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

		public bool IsDownloaded() => File.Exists(System.IO.Path.Combine(Path, ".gitlab-ci.yml"));

		public static Repository CreateRegular(string platform, string version, string path) => new Repository(platform, version, version, path, false);
		public static Repository CreateFork(string platform, string version, string forkName, string minioPath, string path) => new Repository(platform, version, forkName, path, true) { MinioPath = minioPath };
	}
}