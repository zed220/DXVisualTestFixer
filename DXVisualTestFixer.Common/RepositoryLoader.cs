using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DXVisualTestFixer.Common {
    public class Repository {
        public static readonly string[] Versions = new RepositoryLoader().Result.Where(r => Convert.ToInt32(r.Replace(".", string.Empty)) > 172).ToArray();

        public string Version { get; set; }
        public string Path { get; set; }

        public bool IsDownloaded() {
            return File.Exists(System.IO.Path.Combine(Path, "VisualTestsConfig.xml"));
        }

        public string GetTaskName() {
            return String.Format("Test.v{0} WPF VisualTests", Version);
        }
    }

    class RepositoryLoader : FileStringLoaderBase {
        public RepositoryLoader() : base(@"\\corp\internal\common\visualTests_squirrel\versions.xml") {
        }

        protected override List<string> LoadIfFileNotFound() {
            return new List<string>() { "18.1", "18.2", "19.1" };
        }
    }
}
