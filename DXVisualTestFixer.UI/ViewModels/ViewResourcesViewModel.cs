using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using DevExpress.Mvvm.Native;
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
			Task.Factory.StartNew(() => UpdateUsedFiles(testsService.SelectedState.UsedFilesLinks)).ConfigureAwait(false);
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

		async Task UpdateUsedFiles(Dictionary<Repository, List<string>> usedFilesByRep) {
			var allTasks = usedFilesByRep.Select(info => GetUsedFilesSafeAsync(info.Key, info.Value)).ToList();
			await Task.WhenAll(allTasks);
			var usedFiles = new List<RepositoryFileModel>();
			foreach(var info in allTasks.Select(x => x.Result)) {
				usedFiles.AddRange(info.Item2.Select(y => new RepositoryFileModel(y, info.Item1.Version)));
			}
			UsedFiles = usedFiles;
			if(UsedFiles.Count > 0)
			 	CurrentFile = UsedFiles[0];
			Status = ProgramStatus.Idle;
		}

		
		
		async Task<(Repository, List<string>)> GetUsedFilesSafeAsync(Repository repository, List<string> usedFilesLinks) {
			var usedFiles = await RepositoryOptimizerViewModel.GetUsedFilesByRepository(repository, usedFilesLinks, _minioWorker);
			return (repository, usedFiles.Where(File.Exists).Distinct().ToList());
		}
	}
}