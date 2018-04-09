using DevExpress.Mvvm;
using DevExpress.Mvvm.ModuleInjection;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Configuration;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DXVisualTestFixer.ViewModels {
    public interface IRepositoryOptimizerViewModel : ISupportParameter { }

    public class UnusedFileModel : BindableBase {
        public UnusedFileModel(string path, string version) {
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

    public class RepositoryOptimizerViewModel : ViewModelBase, IRepositoryOptimizerViewModel {
        Dictionary<Repository, List<string>> usedFilesByRep;
        readonly Dispatcher Dispatcher;

        public ObservableCollection<UnusedFileModel> UnusedFiles {
            get { return GetProperty(() => UnusedFiles); }
            set { SetProperty(() => UnusedFiles, value); }
        }
        public ObservableCollection<UnusedFileModel> RemovedFiles {
            get { return GetProperty(() => RemovedFiles); }
            set { SetProperty(() => RemovedFiles, value); }
        }
        public ProgramStatus Status {
            get { return GetProperty(() => Status); }
            set { SetProperty(() => Status, value); }
        }

        public IEnumerable<UICommand> DialogCommands { get; private set; }

        public RepositoryOptimizerViewModel() {
            Dispatcher = Dispatcher.CurrentDispatcher;
            RemovedFiles = new ObservableCollection<UnusedFileModel>();
            ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving += RepositoryOptimizerViewModel_ViewModelRemoving;
            CreateCommands();
        }

        void CreateCommands() {
            List<UICommand> dialogCommands = new List<UICommand>();
            dialogCommands.Add(new UICommand() { IsDefault = false, Command = new DelegateCommand(SaveButNotClose, () => Status == ProgramStatus.Idle), Caption = "Apply" });
            dialogCommands.Add(new UICommand() { IsDefault = true, Command = new DelegateCommand(() => Commit(), () => Status == ProgramStatus.Idle), Caption = DXMessageBoxLocalizer.GetString(DXMessageBoxStringId.Ok) });
            dialogCommands.Add(new UICommand() { IsCancel = true, Caption = DXMessageBoxLocalizer.GetString(DXMessageBoxStringId.Cancel) });
            DialogCommands = dialogCommands;
        }

        void RepositoryOptimizerViewModel_ViewModelRemoving(object sender, ViewModelRemovingEventArgs e) {
            if(Status == ProgramStatus.Loading) {
                e.Cancel = true;
                return;
            }
            ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving -= RepositoryOptimizerViewModel_ViewModelRemoving;
            if(RemovedFiles.Count == 0)
                return;
            MessageResult? result = GetService<IMessageBoxService>()?.ShowMessage("Save changes?", "Save changes?", MessageButton.YesNoCancel);
            if(!result.HasValue || result.Value == MessageResult.Cancel || result.Value == MessageResult.None) {
                e.Cancel = true;
                ModuleManager.DefaultManager.GetEvents(this).ViewModelRemoving += RepositoryOptimizerViewModel_ViewModelRemoving;
                return;
            }
            if(result.Value == MessageResult.Yes)
                Commit();
        }

        void SaveButNotClose() {
            Status = ProgramStatus.Loading;
            Task.Factory.StartNew(SaveButNotCloseCore);

        }
        void SaveButNotCloseCore() {
            var removedFiles = Commit();
            Dispatcher.BeginInvoke(new Action(() => {
                foreach(var removedFile in removedFiles) {
                    RemovedFiles.Remove(removedFile);
                    UnusedFiles.Remove(removedFile);
                }
                Status = ProgramStatus.Idle;
            }));
        }

        List<UnusedFileModel> Commit() {
            List<UnusedFileModel> removedFiltes = new List<UnusedFileModel>();
            foreach(UnusedFileModel fileToRemove in RemovedFiles.ToArray()) {
                if(File.Exists(fileToRemove.Path) && (File.GetAttributes(fileToRemove.Path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
                    File.Delete(fileToRemove.Path);
                    removedFiltes.Add(fileToRemove);
                }
            }
            return removedFiltes;
        }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            Status = ProgramStatus.Loading;
            usedFilesByRep = (Dictionary<Repository, List<string>>)parameter;
            Task.Factory.StartNew(() => UpdateUnusedFiles(usedFilesByRep)).ConfigureAwait(false);
        }

        void UpdateUnusedFiles(Dictionary<Repository, List<string>> usedFilesByRep) {
            List<string> usedFiles = GetUsedFiles(usedFilesByRep);
            UnusedFiles = GetActualFiles(usedFilesByRep.Keys.Select(rep => rep.Version).Distinct().Where(v => Repository.InNewVersion(v)).ToList(), usedFiles);
            Status = ProgramStatus.Idle;
        }

        ObservableCollection<UnusedFileModel> GetActualFiles(List<string> usedVersions, List<string> usedFiles) {
            ObservableCollection<UnusedFileModel> result = new ObservableCollection<UnusedFileModel>();
            foreach(Team team in TeamConfigsReader.GetAllTeams()) {
                if(!usedVersions.Contains(team.Version))
                    continue;
                Repository repository = TestsService.GetRepository(team.Version);
                if(repository == null) {
                    //not included in config
                    continue;
                }
                foreach(TeamInfo info in team.TeamInfos) {
                    string teamPath = TestsService.GetResourcePath(repository, info.TestResourcesPath);
                    List<string> unUsedFiles = new List<string>();
                    foreach(string file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories)) {
                        if(!usedFiles.Contains(file.ToLower()))
                            result.Add(new UnusedFileModel(file, team.Version));
                    }
                }
            }
            return result;
        }
        List<string> GetUsedFiles(Dictionary<Repository, List<string>> usedFilesByRep) {
            List<string> usedFiles = new List<string>();
            foreach(var usedFileByRep in usedFilesByRep) {
                foreach(string fileRelPath in usedFileByRep.Value) {
                    string filePath = TestsService.GetResourcePath(usedFileByRep.Key, fileRelPath);
                    if(File.Exists(filePath))
                        usedFiles.Add(filePath.ToLower());
                }
            }
            return usedFiles;
        }
    }
}
