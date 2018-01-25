using DevExpress.Mvvm;
using DevExpress.XtraEditors.DXErrorProvider;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
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
            if(Repository.InNewVersion(Version)) {
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
    }
}
