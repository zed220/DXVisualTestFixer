using System.Text;

namespace DXVisualTestFixer.Common {
    public class TestInfo {
        StringBuilder _InvalidLogBuilder;
        StringBuilder InvalidLogBuilder { get {
                if(_InvalidLogBuilder == null)
                    _InvalidLogBuilder = new StringBuilder();
                return _InvalidLogBuilder;
            } }

        public string Name { get; set; }
        public string NameWithNamespace { get; set; }
        public string ResourceFolderName { get; set; }
        public Team Team { get; set; }
        public TeamInfo TeamInfo { get; set; }
        public string Theme { get; set; }
        public string Fixture { get; set; }
        public string Version { get; set; }
        public int Dpi { get; set; }
        public byte[] ImageBeforeArr { get; set; }
        public byte[] ImageCurrentArr { get; set; }
        public byte[] ImageDiffArr { get; set; }
        public string TextBefore { get; set; }
        public string TextCurrent { get; set; }
        public bool Optimized { get; set; }
        public string TextDiff { get; set; }
        public string TextDiffFull { get; set; }
        public TestState Valid { get; set; }
        public string InvalidLog { get { return InvalidLogBuilder.ToString(); } }

        public void LogCustomError(string text) {
            InvalidLogBuilder.AppendLine(text);
        }
        public void LogDirectoryNotFound(string dirPath) {
            LogCustomError($"Directory not found: \"{dirPath}\"");
        }
        public void LogFileNotFound(string filePath) {
            LogCustomError($"File not found: \"{filePath}\"");
        }
    }
}
