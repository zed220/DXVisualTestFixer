using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Models;
using JetBrains.Annotations;
using Prism.Interactivity.InteractionRequest;
using BindableBase = Prism.Mvvm.BindableBase;
using INotification = Prism.Interactivity.InteractionRequest.INotification;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class RepositoryOptimizerViewModel : BindableBase, IConfirmation {
		readonly Dispatcher _dispatcher;
		readonly IMinioWorker _minioWorker;
		readonly ITestsService _testsService;

		ObservableCollection<RepositoryFileModel> _removedFiles;
		ProgramStatus _status;
		ObservableCollection<RepositoryFileModel> _unusedFiles;

		public RepositoryOptimizerViewModel(ITestsService testsService, IMinioWorker minioWorker) {
			_dispatcher = Dispatcher.CurrentDispatcher;
			_minioWorker = minioWorker;
			_testsService = testsService;
			RemovedFiles = new ObservableCollection<RepositoryFileModel>();
			Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
			Commands.Single(c => c.IsDefault).Command = new DelegateCommand(Commit);
			Status = ProgramStatus.Loading;
			Task.Factory.StartNew(() => UpdateUnusedFiles(testsService.SelectedState.UsedFilesLinks, testsService.SelectedState.Teams)).ConfigureAwait(false);
		}

		[UsedImplicitly]
		public ObservableCollection<RepositoryFileModel> UnusedFiles {
			get => _unusedFiles;
			set => SetProperty(ref _unusedFiles, value);
		}

		[UsedImplicitly]
		public ObservableCollection<RepositoryFileModel> RemovedFiles {
			get => _removedFiles;
			set => SetProperty(ref _removedFiles, value);
		}

		[UsedImplicitly]
		public ProgramStatus Status {
			get => _status;
			set => SetProperty(ref _status, value);
		}

		[UsedImplicitly] public IEnumerable<UICommand> Commands { get; }

		[UsedImplicitly] public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

		public bool Confirmed { get; set; }
		string INotification.Title { get; set; } = "Repository Optimizer";
		object INotification.Content { get; set; }

		void Commit() {
			foreach(var fileToRemove in RemovedFiles)
				if(File.Exists(fileToRemove.Path) && (File.GetAttributes(fileToRemove.Path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly)
					File.Delete(fileToRemove.Path);
			Confirmed = true;
		}

		async Task UpdateUnusedFiles(Dictionary<Repository, List<string>> usedFilesByRepLinks, Dictionary<Repository, List<Team>> teams) {
			var result = await Task.WhenAll(usedFilesByRepLinks.ToList().Select(x => GetUnusedFilesByRepository(x.Key, x.Value, teams[x.Key], _testsService, _minioWorker)));
			await _dispatcher.InvokeAsync(() => {
					UnusedFiles = new ObservableCollection<RepositoryFileModel>(result.SelectMany(x => x));
					Status = ProgramStatus.Idle;
				}
			);
		}

		public static async Task<HashSet<string>> GetUsedFiles(Dictionary<Repository, List<string>> usedFilesByRepLinks, ITestsService testsService, IMinioWorker minioWorker) {
			var result = await Task.WhenAll(usedFilesByRepLinks.ToList().Select(x => GetUsedFilesByRepository(x.Key, x.Value, testsService, minioWorker)));
			return new HashSet<string>(result.SelectMany(x => x));
		}

		static async Task<HashSet<string>> GetUsedFilesByRepository(Repository repository, List<string> filesLinks, ITestsService testsService, IMinioWorker minioWorker) {
			var usedFiles = new List<string>();
			var lockObject = new object();

			await Task.WhenAll(filesLinks.Select(async fileRelLink => {
				var localUsedFiles = new List<string>();
				var filesStr = await minioWorker.Download(fileRelLink);
				if(string.IsNullOrEmpty(filesStr))
					return;
				foreach(var fileRelPath in filesStr.Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)) {
					var localFilePath = testsService.GetResourcePath(repository, fileRelPath.ToLower().Replace(@"c:\builds\", ""));
					if(File.Exists(localFilePath))
						localUsedFiles.Add(localFilePath.ToLower());
				}

				lock(lockObject) {
					usedFiles.AddRange(localUsedFiles);
				}
			})).ConfigureAwait(false);
			return new HashSet<string>(usedFiles);
		}

		static async Task<List<RepositoryFileModel>> GetUnusedFilesByRepository(Repository repository, List<string> usedFilesByRepLinks, List<Team> teams, ITestsService testsService, IMinioWorker minioWorker) {
			var usedFiles = await GetUsedFilesByRepository(repository, usedFilesByRepLinks, testsService, minioWorker).ConfigureAwait(false);
			var result = new List<RepositoryFileModel>();
			foreach(var team in teams ?? Enumerable.Empty<Team>())
			foreach(var teamPath in team.TeamInfos.Select(i => testsService.GetResourcePath(repository, i.TestResourcesPath)).Distinct()) {
				if(!Directory.Exists(teamPath))
					continue;
				foreach(var file in Directory.EnumerateFiles(teamPath, "*", SearchOption.AllDirectories))
					if(!usedFiles.Contains(file.ToLower()))
						result.Add(new RepositoryFileModel(file, team.Version));
			}

			return result;
		}
	}
}