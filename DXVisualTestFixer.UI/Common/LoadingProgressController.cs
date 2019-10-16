using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Threading;
using DevExpress.Mvvm;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.Common {
	public class LoadingProgressController : BindableBase, ILoadingProgressController {
		readonly Dispatcher dispatcher;
		readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
		readonly List<string> scheduledOperations = new List<string>();
		bool isEnabled;

		int maximum;
		int value;

		public LoadingProgressController() {
			dispatcher = Dispatcher.CurrentDispatcher;
		}

		public int Maximum {
			get => maximum;
			set {
				maximum = value;
				MakeDispatcherOperation();
			}
		}

		public int Value {
			get => value;
			set {
				this.value = value;
				MakeDispatcherOperation();
			}
		}

		public bool IsEnabled {
			get => isEnabled;
			set {
				isEnabled = value;
				MakeDispatcherOperation();
			}
		}

		public void Start() {
			Value = 0;
			Maximum = 0;
			IsEnabled = true;
		}

		public void Stop() {
			IsEnabled = false;
		}

		public void IncreaseProgress(int delta) {
			Interlocked.Add(ref value, delta);
			MakeDispatcherOperation(nameof(Value));
		}

		public void Enlarge(int delta) {
			Interlocked.Add(ref maximum, delta);
			MakeDispatcherOperation(nameof(Maximum));
		}

		public void Flush() {
			Maximum = 0;
			Value = 0;
		}

		bool MakeReadWriteOperation(Func<bool> read, Action write) {
			try {
				locker.EnterUpgradeableReadLock();
				if(!read())
					return false;
				try {
					locker.EnterWriteLock();
					write();
				}
				finally {
					locker.ExitWriteLock();
				}
			}
			finally {
				locker.ExitUpgradeableReadLock();
			}

			return true;
		}

		void MakeDispatcherOperation([CallerMemberName] string name = "") {
			if(name == "")
				return;
			if(!MakeReadWriteOperation(() => !scheduledOperations.Contains(name), () => scheduledOperations.Add(name)))
				return;
			dispatcher.BeginInvoke(new Action(() => {
				if(!MakeReadWriteOperation(() => scheduledOperations.Contains(name), () => scheduledOperations.Remove(name)))
					return;
				RaisePropertyChanged(name);
			}));
		}
	}
}