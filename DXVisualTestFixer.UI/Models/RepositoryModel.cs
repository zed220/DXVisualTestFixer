using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.UI.Models {
	public class RepositoryModel : BindableBase {
		readonly Dispatcher dispatcher;
		public readonly Repository Repository;

		public RepositoryModel() : this(new Repository()) { }

		public RepositoryModel(Repository source) {
			Repository = source;
			Version = Repository.Version;
			Path = Repository.Path;
			dispatcher = Dispatcher.CurrentDispatcher;
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

		public static void ActualizeRepositories(ICollection<RepositoryModel> Repositories, string filePath) {
			var savedVersions = Repositories.Select(r => r.Version).ToList();
			foreach(var ver in RepositoryLoader.GetVersions().Where(v => !savedVersions.Contains(v))) {
				foreach(var directoryPath in Directory.GetDirectories(filePath)) {
					var dirName = System.IO.Path.GetFileName(directoryPath);
					if(dirName.Contains($"20{ver}") || dirName.Contains(ver)) {
						if(!File.Exists(directoryPath + "\\VisualTestsConfig.xml"))
							continue;
						Repositories.Add(new RepositoryModel(new Repository {Version = ver, Path = directoryPath + "\\"}));
					}
				}
			}
		}

		public void UpdateDownloadState() => DownloadState = GetDownloadState();

		DownloadState GetDownloadState() {
			if(!Directory.Exists(Path) || !Directory.EnumerateFileSystemEntries(Path).Any())
				return DownloadState.ReadyToDownload;
			return File.Exists(System.IO.Path.Combine(Path, "VisualTestsConfig.xml")) ? DownloadState.Downloaded : DownloadState.CanNotDownload;
		}

		[UsedImplicitly]
		public void Download() {
			if(DownloadState == DownloadState.ReadyToDownload) Task.Factory.StartNew(DownloadAsync);
		}

		async Task DownloadAsync() {
			await dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.Downloading; }));
			var git = ServiceLocator.Current.GetInstance<IGitWorker>();
			if(!await git.Clone(Repository)) {
				await dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.CanNotDownload; }));
				return;
			}

			await dispatcher.BeginInvoke(new Action(() => { DownloadState = DownloadState.Downloaded; }));
		}
	}

	public enum DownloadState {
		ReadyToDownload,
		Downloading,
		Downloaded,
		CanNotDownload
	}
}