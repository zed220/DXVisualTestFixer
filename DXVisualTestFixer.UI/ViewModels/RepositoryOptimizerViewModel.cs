using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
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
    public class RepositoryOptimizerViewModel : BindableBase, IConfirmation {
        readonly Dispatcher Dispatcher;
        readonly ITestsService testsService;
        readonly IMinioWorker minioWorker;

        ObservableCollection<RepositoryFileModel> _UnusedFiles;
        ObservableCollection<RepositoryFileModel> _RemovedFiles;
        ProgramStatus _Status;

        public ObservableCollection<RepositoryFileModel> UnusedFiles {
            get { return _UnusedFiles; }
            set { SetProperty(ref _UnusedFiles, value); }
        }
        public ObservableCollection<RepositoryFileModel> RemovedFiles {
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

        public RepositoryOptimizerViewModel(ITestsService testsService, IMinioWorker minioWorker) {
            Title = "Repository Optimizer";
            Dispatcher = Dispatcher.CurrentDispatcher;
            this.minioWorker = minioWorker;
            this.testsService = testsService;
            RemovedFiles = new ObservableCollection<RepositoryFileModel>();
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => Commit());
            Status = ProgramStatus.Loading;
            Task.Factory.StartNew(() => UpdateUnusedFiles(testsService.ActualState.UsedFilesLinks, testsService.ActualState.Teams)).ConfigureAwait(false);
        }

        List<RepositoryFileModel> Commit() {
            List<RepositoryFileModel> removedFiltes = new List<RepositoryFileModel>();
            foreach(RepositoryFileModel fileToRemove in RemovedFiles.ToArray()) {
                if(File.Exists(fileToRemove.Path) && (File.GetAttributes(fileToRemove.Path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
                    File.Delete(fileToRemove.Path);
                    removedFiltes.Add(fileToRemove);
                }
            }
            Confirmed = true;
            return removedFiltes;
        }

        async Task UpdateUnusedFiles(Dictionary<Repository, List<string>> usedFilesByRepLinks, Dictionary<Repository, List<Team>> teams) {
            HashSet<string> usedFiles = await GetUsedFiles(usedFilesByRepLinks, testsService, minioWorker);
            UnusedFiles = GetActualFiles(usedFilesByRepLinks.Keys.Select(rep => rep.Version).Distinct().ToList(), usedFiles, teams);
            Status = ProgramStatus.Idle;
        }

        ObservableCollection<RepositoryFileModel> GetActualFiles(List<string> usedVersions, HashSet<string> usedFiles, Dictionary<Repository, List<Team>> teams) {
            ObservableCollection<RepositoryFileModel> result = new ObservableCollection<RepositoryFileModel>();
            foreach(var repository in teams.Keys) {
                foreach(Team team in teams[repository]) {
                    if(!usedVersions.Contains(team.Version))
                        continue;
                    foreach(string teamPath in team.TeamInfos.Select(i => testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
                        if(!Directory.Exists(teamPath))
                            continue;
                        foreach(string file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories)) {
                            if(!usedFiles.Contains(file.ToLower()))
                                result.Add(new RepositoryFileModel(file, team.Version));
                        }
                    }
                }
            }
            return result;
        }
        public static async Task<HashSet<string>> GetUsedFiles(Dictionary<Repository, List<string>> usedFilesByRepLinks, ITestsService testsService, IMinioWorker minioWorker) {
            HashSet<string> usedFiles = new HashSet<string>();
            foreach(var usedFileByRepLink in usedFilesByRepLinks) {
                foreach(string fileRelLink in usedFileByRepLink.Value) {
                    var filesStr = await minioWorker.Download(fileRelLink);
                    if(string.IsNullOrEmpty(filesStr))
                        continue;
                    foreach(var fileRelPath in filesStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                        string filePath = testsService.GetResourcePath(usedFileByRepLink.Key, fileRelPath);
                        if(File.Exists(filePath))
                            usedFiles.Add(filePath.ToLower());
                    }
                }
            }
            return usedFiles;
        }
    }
}
