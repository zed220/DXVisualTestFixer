using System;
using System.Windows.Threading;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.Native {
	class RepositoryObsolescenceTracker {
		readonly DispatcherTimer timer;
		readonly IGitWorker gitWorker;
		readonly Func<Repository[]> getReposForCheck;

		public RepositoryObsolescenceTracker(IGitWorker gitWorker, Func<Repository[]> getReposForCheck, Action onObsolescence) {
			timer = new DispatcherTimer();
			this.gitWorker = gitWorker;
			this.getReposForCheck = getReposForCheck;
			timer.Interval = TimeSpan.FromMinutes(20);
			timer.Tick += async (s, a) => {
				timer.Stop();
				foreach(var repo in this.getReposForCheck()) {
					if(await this.gitWorker.IsOutdatedAsync(repo)) {
						onObsolescence();
						return;
					}
				}
				timer.Start();
			};
		}

		public void Stop() {
			timer.Stop();
		}

		public void Start() {
			timer.Start();
		}
	}
}