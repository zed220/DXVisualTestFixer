using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.Native;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;
using INotificationService = DXVisualTestFixer.Common.INotificationService;

namespace DXVisualTestFixer.UI.Models {
	public class RepositoryModel : BindableBase {
		readonly Dispatcher _dispatcher;
		readonly IPlatformInfo _platform;
		readonly INotificationService _notificationService;
		
		public readonly Repository Repository;

		public RepositoryModel(Repository source, IPlatformInfo platform, INotificationService notificationService) {
			Repository = source;
			Version = Repository.Version;
			_notificationService = notificationService;
			Path = Repository.Path;
			_dispatcher = Dispatcher.CurrentDispatcher;
			var platformProvider = ServiceLocator.Current.GetInstance<IPlatformProvider>();
			_platform = platform;
			UpdateDownloadState();
		}
		
		public string Version {
			get { return GetProperty(() => Version); }
			set { SetProperty(() => Version, value, OnVersionChanged); }
		}

		public string Path {
			get { return GetProperty(() => Path); }
			set { SetProperty(() => Path, value, OnPathChanged); }
		}

		public DownloadState DownloadState {
			get { return GetProperty(() => DownloadState); }
			set { SetProperty(() => DownloadState, value); }
		}

		void OnVersionChanged() {
			Repository.Version = Version;
		}

		void OnPathChanged() {
			Repository.Path = Path;
		}

		public static void ActualizeRepositories(IPlatformInfo platform, ICollection<RepositoryModel> repositories, INotificationService notificationService, string filePath) {
			var savedVersions = repositories.Select(r => r.Version).ToList();
			foreach(var ver in RepositoryLoader.GetVersions(platform).Where(v => !savedVersions.Contains(v)))
			foreach(var directoryPath in Directory.GetDirectories(filePath)) {
				var dirName = System.IO.Path.GetFileName(directoryPath);
				var localPath = string.Format(platform.LocalPath, ver);
				if(dirName != localPath) continue;
				if(!File.Exists(directoryPath + "\\VisualTestsConfig.xml"))
					continue;
				var repository = new RepositoryModel(Repository.CreateRegular(platform.Name, ver, directoryPath + "\\"), platform, notificationService); 
				repositories.Add(repository);
				InitializeLinks(platform, repository.Path, repository.Version);
			}
		}

		public static void InitializeLinks(IPlatformInfo platform, string repositoryPath, string version) {
			var workPaths = new[] {
				System.IO.Path.Combine(repositoryPath, "..", version, "Bin"),
				System.IO.Path.Combine(repositoryPath, "..", $"20{version}", "Bin"),
				System.IO.Path.Combine("c:\\Work", version, "Bin"),
				System.IO.Path.Combine("c:\\Work", $"20{version}", "Bin"),
				System.IO.Path.Combine("d:\\Work", version, "Bin"),
				System.IO.Path.Combine("d:\\Work", $"20{version}", "Bin")
			};
			
			var workPath = workPaths.Where(Directory.Exists).FirstOrDefault();
			if(workPath == null)
				return;
			
			InitializeBinIfNeed(workPath, repositoryPath);
			InitializeAdditionalLinks(platform, workPath, repositoryPath);
		}

		static void InitializeAdditionalLinks(IPlatformInfo platform, string workPath, string repositoryPath) {
			foreach(var link in platform.Links) {
				var sourcePath = link.sourcePath.Replace("_VisualTests_WorkBinPath_", workPath);
				if(!Directory.Exists(sourcePath))
					continue;
				var targetPath = System.IO.Path.Combine(repositoryPath, link.targetPath);
				if(PrepareToLinkCreationAndCheck(targetPath))
					CreateDirectoryLink(sourcePath, targetPath);
			}
		}

		static void CreateDirectoryLink(string workPath, string targetPath) {
			try {
				FileSystemHelper.Create(workPath, targetPath);
			}
			catch (Exception e) {
				ServiceLocator.Current.GetInstance<IExceptionService>().Send(e, 
					new Dictionary<string, string> {
						{ "WorkPath", workPath },
						{ "TargetPath", targetPath },
					});
			}
		}

		static bool PrepareToLinkCreationAndCheck(string path) {
			if(!Directory.Exists(path)) 
				return true;
			var pathInfo = new FileInfo(path);
			if(pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
				return false;
			var oldBinPath = path + "_old";
			if(Directory.Exists(oldBinPath))
				return false;
			try {
				Directory.Move(path, oldBinPath);
				return true;
			}
			catch {
				return false;
			}
		}

		static void InitializeBinIfNeed(string workPath, string repositoryPath) {
			var binPath = System.IO.Path.Combine(repositoryPath, "Bin");
			if(PrepareToLinkCreationAndCheck(binPath))
				CreateDirectoryLink(workPath, binPath);
		}

		public void UpdateDownloadState() {
			DownloadState = GetDownloadState();
		}

		DownloadState GetDownloadState() {
			if(!Directory.Exists(Path) || !Directory.EnumerateFileSystemEntries(Path).Any())
				return DownloadState.ReadyToDownload;
			return File.Exists(System.IO.Path.Combine(Path, ".gitlab-ci.yml")) ? DownloadState.Downloaded : DownloadState.CanNotDownload;
		}

		[UsedImplicitly]
		public async void Download() {
			if(DownloadState != DownloadState.ReadyToDownload) 
				return;
			try {
				await DownloadAsync();					
			}
			catch(Exception e) {
				_notificationService?.DoNotification("Error", e.Message, MessageBoxImage.Error);
				throw e;
			}
		}

		async Task DownloadAsync() {
			DownloadState = DownloadState.Downloading;
			await Task.Delay(1).ConfigureAwait(false);
			var git = ServiceLocator.Current.GetInstance<IGitWorker>();
			if(!await git.Clone(_platform.GitRepository, Repository)) {
				await _dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.CanNotDownload; }));
				return;
			}
			InitializeLinks(_platform, Repository.Path, Repository.Version);
			await _dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.Downloaded; }));	
			
		}
	}

	public enum DownloadState {
		ReadyToDownload,
		Downloading,
		Downloaded,
		CanNotDownload
	}
}