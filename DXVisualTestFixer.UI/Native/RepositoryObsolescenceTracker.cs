using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;

namespace DXVisualTestFixer.UI.Native {
	class RepositoryObsolescenceTracker {
		readonly DispatcherTimer timer;
		readonly Func<Repository[]> getReposForCheck;
		readonly IPlatformProvider platforms;

		public RepositoryObsolescenceTracker(IGitWorker gitWorker, Func<Repository[]> getReposForCheck, Func<Task> onObsolescence) {
			timer = new DispatcherTimer();
			this.getReposForCheck = getReposForCheck;
			platforms = ServiceLocator.Current.GetInstance<IPlatformProvider>();
			timer.Interval = TimeSpan.FromMinutes(20);
			timer.Tick += async (s, a) => {
				timer.Stop();
				foreach(var repo in this.getReposForCheck()) {
					try {
						if(await gitWorker.IsOutdatedAsync(platforms.PlatformInfos.Single(p => p.Name == repo.Platform).GitRepository, repo)) {
							await onObsolescence();
							return;
						}
					}
					catch {
					}
				}
				timer.Start();
			};
		}

		public void Stop() {
			if(timer.IsEnabled)
				timer.Stop();
		}

		public void Start() {
			timer.Start();
		}
	}
}