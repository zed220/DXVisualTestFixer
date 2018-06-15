using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
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
    public class ViewResourcesViewModel : BindableBase, Prism.Interactivity.InteractionRequest.INotification {
        readonly Dispatcher Dispatcher;
        readonly ITestsService testsService;

        ProgramStatus _Status;
        List<RepositoryFileModel> _UsedFiles = new List<RepositoryFileModel>();
        RepositoryFileModel _CurrentFile;

        public string Title { get; set; } = "Resources Viewer";
        public object Content { get; set; }

        public IEnumerable<UICommand> Commands { get; }

        public ProgramStatus Status {
            get { return _Status; }
            set { SetProperty(ref _Status, value); }
        }

        public List<RepositoryFileModel> UsedFiles {
            get { return _UsedFiles; }
            set { SetProperty(ref _UsedFiles, value); }
        }
        public RepositoryFileModel CurrentFile {
            get { return _CurrentFile; }
            set { SetProperty(ref _CurrentFile, value); }
        }

        public ViewResourcesViewModel(ITestsService testsService) {
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService(), MessageResult.OK);
            Dispatcher = Dispatcher.CurrentDispatcher;
            this.testsService = testsService;
            Status = ProgramStatus.Loading;
            Task.Factory.StartNew(() => UpdateUsedFiles(testsService.ActualState.UsedFiles, testsService.ActualState.Teams)).ConfigureAwait(false);
        }

        void UpdateUsedFiles(Dictionary<Repository, List<string>> usedFilesByRep, Dictionary<Repository, List<Team>> teams) {
            HashSet<string> usedFiles = RepositoryOptimizerViewModel.GetUsedFiles(usedFilesByRep, testsService);
            UsedFiles = GetActualFiles(usedFilesByRep.Keys.Select(rep => rep.Version).Distinct().Where(v => Repository.IsNewVersion(v)).ToList(), usedFiles, teams);
            if(UsedFiles.Count > 0)
                CurrentFile = UsedFiles[0];
            Status = ProgramStatus.Idle;
        }

        List<RepositoryFileModel> GetActualFiles(List<string> usedVersions, HashSet<string> usedFiles, Dictionary<Repository, List<Team>> teams) {
            List<RepositoryFileModel> result = new List<RepositoryFileModel>();
            foreach(var repository in teams.Keys) {
                foreach(Team team in teams[repository]) {
                    if(!usedVersions.Contains(team.Version))
                        continue;
                    foreach(string teamPath in team.TeamInfos.Select(i => testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
                        foreach(string file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories)) {
                            if(usedFiles.Contains(file.ToLower()))
                                result.Add(new RepositoryFileModel(file, team.Version));
                        }
                    }
                }
            }
            return result;
        }
    }
}
