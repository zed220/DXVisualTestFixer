using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVisualTestFixer.Common {
    public class Repository {
        public string Version { get; set; }
        public string Path { get; set; }

        public bool IsDownloaded() {
            return File.Exists(System.IO.Path.Combine(Path, "VisualTestsConfig.xml"));
        }

        public string GetTaskName() {
            return String.Format("Test.v{0} WPF VisualTests", Version);
        }
    }


    public class RepositoryLoader : FileStringLoaderBase {
        RepositoryLoader() : base(@"\\corp\internal\common\visualTests_squirrel\versions.xml") {
        }

        public static string[] GetVersions() => new RepositoryLoader().Result.ToArray();

        protected override List<string> LoadIfFileNotFound() {
            return new List<string>() { "18.2", "19.1", "19.2" };
        }
    }
}
