using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using Microsoft.Practices.Unity;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Prism.Common;
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
            var result = await Task.WhenAll(usedFilesByRepLinks.ToList().Select(x => GetUnusedFilesByRepository(x.Key, x.Value, teams[x.Key], testsService, minioWorker))).ConfigureAwait(false);
            Dispatcher.BeginInvoke(new Action(() => {
                UnusedFiles = new ObservableCollection<RepositoryFileModel>(result.SelectMany(x => x));
                Status = ProgramStatus.Idle;    
            }));
        }
        public static async Task<HashSet<string>> GetUsedFiles(Dictionary<Repository, List<string>> usedFilesByRepLinks, ITestsService testsService, IMinioWorker minioWorker) {
            var result = await Task.WhenAll(usedFilesByRepLinks.ToList().Select(x => GetUsedFilesByRepository((x.Key, x.Value), testsService, minioWorker)));
            return new HashSet<string>(result.SelectMany(x => x));
        }
        static async Task<HashSet<string>> GetUsedFilesByRepository((Repository repository, List<string> filesLinks) usedFilesByRepLinks, ITestsService testsService, IMinioWorker minioWorker) {
            HashSet<string> usedFiles = new HashSet<string>();
            foreach(string fileRelLink in usedFilesByRepLinks.filesLinks) {
                var filesStr = await minioWorker.Download(fileRelLink);
                if(string.IsNullOrEmpty(filesStr))
                    continue;
                foreach(var fileRelPath in filesStr.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)) {
                    string filePath = testsService.GetResourcePath(usedFilesByRepLinks.repository, fileRelPath.ToLower().Replace(@"c:\builds\", ""));
                    if(File.Exists(filePath))
                        usedFiles.Add(filePath.ToLower());
                }
            }
            return usedFiles;
        }
        static async Task<List<RepositoryFileModel>> GetUnusedFilesByRepository(Repository repository, List<string> usedFilesByRepLinks, List<Team> teams, ITestsService testsService, IMinioWorker minioWorker) {
            HashSet<string> usedFiles = await GetUsedFilesByRepository((repository, usedFilesByRepLinks), testsService, minioWorker).ConfigureAwait(false);
            var result = new List<RepositoryFileModel>();
            foreach(Team team in teams) {
                foreach(string teamPath in team.TeamInfos.Select(i => testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
                    if(!Directory.Exists(teamPath))
                        continue;
                    foreach(string file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories)) {
                        if(!usedFiles.Contains(file.ToLower()))
                            result.Add(new RepositoryFileModel(file, team.Version));
                    }
                }
            }
            return result;
        }
    }
}
