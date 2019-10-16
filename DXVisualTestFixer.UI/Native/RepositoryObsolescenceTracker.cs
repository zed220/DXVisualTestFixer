using System;
using System.Windows.Threading;

namespace DXVisualTestFixer.UI.Native {
	internal class RepositoryObsolescenceTracker {
		readonly DispatcherTimer timer;

		public RepositoryObsolescenceTracker(Action onObsolescence) {
			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromHours(1);
			timer.Tick += (s, a) => {
				timer.Stop();
				onObsolescence();
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