using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using Microsoft.Practices.Unity;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.UI.ViewModels {
    public class UnusedFileModel {
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

    public class RepositoryOptimizerViewModel : BindableBase, IRepositoryOptimizerViewModel {
        readonly Dispatcher Dispatcher;
        readonly ITestsService testsService;

        ObservableCollection<UnusedFileModel> _UnusedFiles;
        ObservableCollection<UnusedFileModel> _RemovedFiles;
        ProgramStatus _Status;

        public ObservableCollection<UnusedFileModel> UnusedFiles {
            get { return _UnusedFiles; }
            set { SetProperty(ref _UnusedFiles, value); }
        }
        public ObservableCollection<UnusedFileModel> RemovedFiles {
            get { return _RemovedFiles; }
            set { SetProperty(ref _RemovedFiles, value); }
        }
        public ProgramStatus Status {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        public IEnumerable<UICommand> Commands { get; }
        public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

        public bool Confirmed { get; set; }
        public string Title { get; set; }
        public object Content { get; set; }

        public RepositoryOptimizerViewModel(IUnityContainer container, IMainViewModel viewModel, ITestsService testsService) {
            Title = "Repository Optimizer";
            Dispatcher = Dispatcher.CurrentDispatcher;
            this.testsService = testsService;
            RemovedFiles = new ObservableCollection<UnusedFileModel>();
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => Commit());
            Status = ProgramStatus.Loading;
            Task.Factory.StartNew(() => UpdateUnusedFiles(viewModel.UsedFiles, viewModel.Teams)).ConfigureAwait(false);
        }
        
        //void SaveButNotClose() {
        //    Status = ProgramStatus.Loading;
        //    Task.Factory.StartNew(SaveButNotCloseCore);
        //}
        //void SaveButNotCloseCore() {
        //    var removedFiles = Commit();
        //    Dispatcher.BeginInvoke(new Action(() => {
        //        foreach(var removedFile in removedFiles) {
        //            RemovedFiles.Remove(removedFile);
        //            UnusedFiles.Remove(removedFile);
        //        }
        //        Status = ProgramStatus.Idle;
        //    }));
        //}

        List<UnusedFileModel> Commit() {
            List<UnusedFileModel> removedFiltes = new List<UnusedFileModel>();
            foreach(UnusedFileModel fileToRemove in RemovedFiles.ToArray()) {
                if(File.Exists(fileToRemove.Path) && (File.GetAttributes(fileToRemove.Path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
                    File.Delete(fileToRemove.Path);
                    removedFiltes.Add(fileToRemove);
                }
            }
            Confirmed = true;
            return removedFiltes;
        }

        void UpdateUnusedFiles(Dictionary<Repository, List<string>> usedFilesByRep, Dictionary<Repository, List<Team>> teams) {
            HashSet<string> usedFiles = GetUsedFiles(usedFilesByRep);
            UnusedFiles = GetActualFiles(usedFilesByRep.Keys.Select(rep => rep.Version).Distinct().Where(v => Repository.InNewVersion(v)).ToList(), usedFiles, teams);
            Status = ProgramStatus.Idle;
        }

        ObservableCollection<UnusedFileModel> GetActualFiles(List<string> usedVersions, HashSet<string> usedFiles, Dictionary<Repository, List<Team>> teams) {
            ObservableCollection<UnusedFileModel> result = new ObservableCollection<UnusedFileModel>();
            foreach(var repository in teams.Keys) {
                foreach(Team team in teams[repository]) {
                    if(!usedVersions.Contains(team.Version))
                        continue;
                    foreach(string teamPath in team.TeamInfos.Select(i => testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
                        List<string> unUsedFiles = new List<string>();
                        foreach(string file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories)) {
                            if(!usedFiles.Contains(file.ToLower()))
                                result.Add(new UnusedFileModel(file, team.Version));
                        }
                    }
                }
            }
            return result;
        }
        HashSet<string> GetUsedFiles(Dictionary<Repository, List<string>> usedFilesByRep) {
            HashSet<string> usedFiles = new HashSet<string>();
            foreach(var usedFileByRep in usedFilesByRep) {
                foreach(string fileRelPath in usedFileByRep.Value) {
                    string filePath = testsService.GetResourcePath(usedFileByRep.Key, fileRelPath);
                    if(File.Exists(filePath))
                        usedFiles.Add(filePath.ToLower());
                }
            }
            return usedFiles;
        }
    }
}
