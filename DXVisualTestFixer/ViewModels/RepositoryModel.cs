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
                if(!Directory.Exists(Path)) {
                    info.ErrorText = String.Format("Directory \"{0}\" does not exists", Path);
                    info.ErrorType = ErrorType.Default;
                    return;
                }
            }
        }
    }
}
