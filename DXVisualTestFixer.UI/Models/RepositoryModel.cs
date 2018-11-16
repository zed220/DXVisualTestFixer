using DevExpress.Mvvm;
using DevExpress.XtraEditors.DXErrorProvider;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Models {
    public class RepositoryModel : BindableBase, IDXDataErrorInfo {
        public readonly Repository Repository;

        public string Version {
            get { return GetProperty(() => Version); }
            set { SetProperty(() => Version, value, OnVersionChanged); }
        }
        public string Path {
            get { return GetProperty(() => Path); }
            set { SetProperty(() => Path, value, OnPathChanged); }
        }

        void OnVersionChanged() {
            Repository.Version = Version;
        }
        void OnPathChanged() {
            Repository.Path = Path;
        }

        public RepositoryModel() : this(new Repository()) { }
        public RepositoryModel(Repository source) {
            Repository = source;
            Version = Repository.Version;
            Path = Repository.Path;
        }

        public void GetError(ErrorInfo info) { }

        public void GetPropertyError(string propertyName, ErrorInfo info) {
            info.ErrorText = null;
            info.ErrorType = ErrorType.None;
            if(propertyName == nameof(Path)) {
                IsValid(info);
            }
        }
        public bool IsValid(ErrorInfo info = null) {
            if(info == null)
                info = new ErrorInfo();
            if(!Directory.Exists(Path)) {
                info.ErrorText = $"Directory \"{Path}\" does not exists. Example value: \"c:\\Work\\2017.1\\XPF\". Since the 18.1 version you must use specific repository - XPF\\VisualTests";
                info.ErrorType = ErrorType.Default;
                return false;
            }
            if(String.IsNullOrWhiteSpace(Version)) {
                info.ErrorText = $"Version \"{Version}\" does not valid. Enter valid value";
                return false;
            }
            if(Repository.IsNewVersion(Version)) {
                string configPath = System.IO.Path.Combine(Path, "VisualTestsConfig.xml");
                if(!File.Exists(configPath)) {
                    info.ErrorText = $"File \"VisualTestsConfig.xml\" does not exists in directory \"{Path}\".\nSince the 18.1 version you must use specific git repository:\n" +
                        "git@gitserver:XPF/VisualTests.git\n" +
                        "Don't create fork, use as is.\n" +
                        "Contact Zinovyev for additional info";
                    info.ErrorType = ErrorType.Default;
                    return false;
                }
            }
            else {
                string visualTestsPath = System.IO.Path.Combine(Path, "DevExpress.Xpf.VisualTests");
                if(!Directory.Exists(visualTestsPath)) {
                    info.ErrorText = $"Directory \"{visualTestsPath}\" does not exists. Example value: \"c:\\Work\\2017.2\\XPF\"";
                    info.ErrorType = ErrorType.Default;
                    return false;
                }
            }
            return true;
        }

        public static void ActualizeRepositories(ICollection<RepositoryModel> Repositories, string filePath) {
            List<string> savedVersions = Repositories.Select(r => r.Version).ToList();
            foreach(var ver in Repository.Versions.Where(v => !savedVersions.Contains(v))) {
                string verDir = String.Format("20{0}", ver);
                foreach(var directoryPath in Directory.GetDirectories(filePath)) {
                    string dirName = System.IO.Path.GetFileName(directoryPath);
                    if(dirName.Contains(String.Format("20{0}", ver)) || dirName.Contains(ver)) {
                        if(Repository.IsNewVersion(ver)) {
                            if(!File.Exists(directoryPath + "\\VisualTestsConfig.xml"))
                                continue;
                            Repositories.Add(new RepositoryModel(new Repository() { Version = ver, Path = directoryPath + "\\" }));
                            continue;
                        }
                        string visualTestsPathVar = System.IO.Path.Combine(directoryPath, "XPF\\");
                        if(!Directory.Exists(visualTestsPathVar)) {
                            visualTestsPathVar = System.IO.Path.Combine(directoryPath, "common\\XPF\\");
                            if(!Directory.Exists(visualTestsPathVar))
                                continue;
                        }
                        Repositories.Add(new RepositoryModel(new Repository() { Version = ver, Path = visualTestsPathVar }));
                    }
                }
            }
        }
    }
}
