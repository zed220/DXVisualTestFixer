using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using JetBrains.Annotations;
using BindableBase = Prism.Mvvm.BindableBase;
using INotification = Prism.Interactivity.InteractionRequest.INotification;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class ViewResourcesViewModel : BindableBase, INotification {
		readonly Dispatcher Dispatcher;
		readonly IMinioWorker minioWorker;
		readonly ITestsService testsService;
		RepositoryFileModel _CurrentFile;

		ProgramStatus _Status;
		List<RepositoryFileModel> _UsedFiles = new List<RepositoryFileModel>();

		public ViewResourcesViewModel(ITestsService testsService, IMinioWorker minioWorker) {
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService(), MessageResult.OK);
			Dispatcher = Dispatcher.CurrentDispatcher;
			this.testsService = testsService;
			this.minioWorker = minioWorker;
			Status = ProgramStatus.Loading;
			Task.Factory.StartNew(() => UpdateUsedFiles(testsService.ActualState.UsedFilesLinks, testsService.ActualState.Teams)).ConfigureAwait(false);
		}

		public IEnumerable<UICommand> Commands { get; }

		public ProgramStatus Status {
			get => _Status;
			set => SetProperty(ref _Status, value);
		}

		public List<RepositoryFileModel> UsedFiles {
			get => _UsedFiles;
			set => SetProperty(ref _UsedFiles, value);
		}

		public RepositoryFileModel CurrentFile {
			get => _CurrentFile;
			set => SetProperty(ref _CurrentFile, value);
		}

		public string Title { get; set; } = "Resources Viewer";
		public object Content { get; set; }

		async Task UpdateUsedFiles(Dictionary<Repository, List<string>> usedFilesByRep, Dictionary<Repository, List<Team>> teams) {
			var usedFiles = await RepositoryOptimizerViewModel.GetUsedFiles(usedFilesByRep, testsService, minioWorker);
			UsedFiles = GetActualFiles(usedFilesByRep.Keys.Select(rep => rep.Version).Distinct().ToList(), usedFiles, teams);
			if(UsedFiles.Count > 0)
				CurrentFile = UsedFiles[0];
			Status = ProgramStatus.Idle;
		}

		List<RepositoryFileModel> GetActualFiles(List<string> usedVersions, HashSet<string> usedFiles, Dictionary<Repository, List<Team>> teams) {
			var result = new List<RepositoryFileModel>();
			foreach(var repository in teams.Keys)
			foreach(var team in teams[repository] ?? Enumerable.Empty<Team>()) {
				if(!usedVersions.Contains(team.Version))
					continue;
				foreach(var teamPath in team.TeamInfos.Select(i => testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
					if(!Directory.Exists(teamPath))
						continue;
					foreach(var file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories))
						if(usedFiles.Contains(file.ToLower()))
							result.Add(new RepositoryFileModel(file, team.Version));
				}
			}

			return result;
		}
	}
}