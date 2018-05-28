using System.Diagnostics;
using System.IO;
using System.Linq;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.Models {
    public class SolutionModel {
        public SolutionModel(string version, string path) {
            Version = version;
            Path = GetRealPath(path);
        }

        public string Version { get; }
        public string Path { get; }
        public void OpenSolution() {
            var solutionFilePath = Directory.EnumerateFiles(Path, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
            if(solutionFilePath == null || !File.Exists(solutionFilePath))
                return;
            string openSolutionPath = @"C:\Program Files (x86)\Common Files\Microsoft Shared\MSEnv\VSLauncher.exe";
            if(!File.Exists(openSolutionPath))
                return;
            ProcessStartInfo info = new ProcessStartInfo(openSolutionPath, solutionFilePath);
            info.Verb = "runas";
            Process.Start(info);
        }
        public void OpenFolder() {
            Process.Start(Path);
        }
        string GetRealPath(string path) {
            string folderName = Repository.IsNewVersion(Version) ? "VisualTests" : "DevExpress.Xpf.VisualTests";
            return System.IO.Path.Combine(path, folderName);
        }
    }
}
