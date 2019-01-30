using System;
using System.Collections.Generic;
using System.IO;

namespace DXVisualTestFixer.Common {
    public class Repository {
        public static readonly string[] Versions = new RepositoryLoader().Result.ToArray();

        public string Version { get; set; }
        public string Path { get; set; }
        public static bool IsNewVersion(string version) {
            return Convert.ToInt32(version.Split('.')[0]) >= 18;
        }

        public bool IsValid() {
            return File.Exists(System.IO.Path.Combine(Path, "VisualTestsConfig.xml"));
        }

        public string GetTaskName() {
            return String.Format("Test.v{0} WPF.Functional VisualTests UnoptimizedMode", Version);
        }
        public string GetTaskName_Optimized() {
            return String.Format("Test.v{0} WPF.Functional VisualTests", Version);
        }
        public string GetTaskName_New() {
            return String.Format("Test.v{0} WPF VisualTests", Version);
        }
    }

    class RepositoryLoader : FileStringLoaderBase {
        public RepositoryLoader() : base(@"\\corp\internal\common\visualTests_squirrel\versions.xml") {
        }

        protected override List<string> LoadIfFileNotFound() {
            return new List<string>() { "16.1", "16.2", "17.1", "17.2", "18.1", "18.2", "19.1" };
        }
    }
}
