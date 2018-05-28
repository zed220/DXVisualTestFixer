using System;

namespace DXVisualTestFixer.UI.Models {
    public class RepositoryFileModel {
        public RepositoryFileModel(string path, string version) {
            Path = path;
            Version = version;
        }

        public string Path { get; }
        public string Version { get; }
        public string DirName { get { return System.IO.Path.GetDirectoryName(Path); } }
        public string FileName { get { return System.IO.Path.GetFileName(Path); } }

        public override string ToString() {
            return $"DirName:{DirName}{Environment.NewLine}FileName:{FileName}";
        }
    }
}
