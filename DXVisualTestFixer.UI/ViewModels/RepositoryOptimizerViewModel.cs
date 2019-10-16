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
using Prism.Interactivity.InteractionRequest;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.UI.ViewModels {
	public class RepositoryOptimizerViewModel : BindableBase, IConfirmation {
		readonly Dispatcher Dispatcher;
		readonly IMinioWorker minioWorker;
		readonly ITestsService testsService;
		ObservableCollection<RepositoryFileModel> _RemovedFiles;
		ProgramStatus _Status;

		ObservableCollection<RepositoryFileModel> _UnusedFiles;

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

		public ObservableCollection<RepositoryFileModel> UnusedFiles {
			get => _UnusedFiles;
			set => SetProperty(ref _UnusedFiles, value);
		}

		public ObservableCollection<RepositoryFileModel> RemovedFiles {
			get => _RemovedFiles;
			set => SetProperty(ref _RemovedFiles, value);
		}

		public ProgramStatus Status {
			get => _Status;
			set => SetProperty(ref _Status, value);
		}

		public IEnumerable<UICommand> Commands { get; }
		public InteractionRequest<IConfirmation> ConfirmationRequest { get; } = new InteractionRequest<IConfirmation>();

		public bool Confirmed { get; set; }
		public string Title { get; set; }
		public object Content { get; set; }

		List<RepositoryFileModel> Commit() {
			var removedFiltes = new List<RepositoryFileModel>();
			foreach(var fileToRemove in RemovedFiles.ToArray())
				if(File.Exists(fileToRemove.Path) && (File.GetAttributes(fileToRemove.Path) & FileAttributes.ReadOnly) != FileAttributes.ReadOnly) {
					File.Delete(fileToRemove.Path);
					removedFiltes.Add(fileToRemove);
				}

			Confirmed = true;
			return removedFiltes;
		}

		async Task UpdateUnusedFiles(Dictionary<Repository, List<string>> usedFilesByRepLinks, Dictionary<Repository, List<Team>> teams) {
			var result = await Task.WhenAll(usedFilesByRepLinks.ToList().Select(x => GetUnusedFilesByRepository(x.Key, x.Value, teams[x.Key], testsService, minioWorker)));
			await Dispatcher.InvokeAsync(() => {
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
			foreach(var team in teams)
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