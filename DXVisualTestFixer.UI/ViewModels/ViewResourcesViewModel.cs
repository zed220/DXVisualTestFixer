using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
		readonly IMinioWorker _minioWorker;
		readonly ITestsService _testsService;

		RepositoryFileModel _currentFile;
		ProgramStatus _status;
		List<RepositoryFileModel> _usedFiles = new List<RepositoryFileModel>();

		public ViewResourcesViewModel(ITestsService testsService, IMinioWorker minioWorker) {
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService(), MessageResult.OK);
			_testsService = testsService;
			_minioWorker = minioWorker;
			Status = ProgramStatus.Loading;
			// Task.Factory.StartNew(() => UpdateUsedFiles(testsService.SelectedState.UsedFilesLinks, testsService.SelectedState.Teams)).ConfigureAwait(false);
		}

		[UsedImplicitly] public IEnumerable<UICommand> Commands { get; }

		[UsedImplicitly]
		public ProgramStatus Status {
			[UsedImplicitly] get => _status;
			set => SetProperty(ref _status, value);
		}

		[UsedImplicitly]
		public List<RepositoryFileModel> UsedFiles {
			get => _usedFiles;
			set => SetProperty(ref _usedFiles, value);
		}

		[UsedImplicitly]
		public RepositoryFileModel CurrentFile {
			[UsedImplicitly] get => _currentFile;
			set => SetProperty(ref _currentFile, value);
		}

		public string Title { get; set; } = "Resources Viewer";
		public object Content { get; set; }

		// async Task UpdateUsedFiles(Dictionary<Repository, List<string>> usedFilesByRep, Dictionary<Repository, List<Team>> teams) {
		// 	var usedFiles = await RepositoryOptimizerViewModel.GetUsedFiles(usedFilesByRep, _testsService, _minioWorker);
		// 	UsedFiles = GetActualFiles(usedFilesByRep.Keys.Select(rep => rep.Version).Distinct().ToList(), usedFiles, teams);
		// 	if(UsedFiles.Count > 0)
		// 		CurrentFile = UsedFiles[0];
		// 	Status = ProgramStatus.Idle;
		// }
		//
		// List<RepositoryFileModel> GetActualFiles(List<string> usedVersions, HashSet<string> usedFiles, Dictionary<Repository, List<Team>> teams) {
		// 	var result = new List<RepositoryFileModel>();
		// 	foreach(var repository in teams.Keys)
		// 	foreach(var team in teams[repository] ?? Enumerable.Empty<Team>()) {
		// 		if(!usedVersions.Contains(team.Version))
		// 			continue;
		// 		foreach(var teamPath in team.TeamInfos.Select(i => _testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
		// 			if(!Directory.Exists(teamPath))
		// 				continue;
		// 			foreach(var file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories))
		// 				if(usedFiles.Contains(file.ToLower()))
		// 					result.Add(new RepositoryFileModel(file, team.Version));
		// 		}
		// 	}
		//
		// 	return result;
		// }
	}
}