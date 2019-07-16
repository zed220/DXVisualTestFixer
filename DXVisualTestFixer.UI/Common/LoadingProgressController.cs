using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DXVisualTestFixer.UI.Common {
    public class LoadingProgressController : BindableBase, ILoadingProgressController {
        readonly Dispatcher dispatcher;
        readonly List<string> scheduledOperations = new List<string>();
        readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        int maximum = 0;
        int value = 0;
        bool isEnabled = false;

        public LoadingProgressController() {
            dispatcher = Dispatcher.CurrentDispatcher;
        }

        public int Maximum {
            get { return maximum; }
            set {
                maximum = value;
                MakeDispatcherOperation();
            }
        }
        public int Value {
            get { return value; }
            set {
                this.value = value;
                MakeDispatcherOperation();
            }
        }
        public bool IsEnabled {
            get { return isEnabled; }
            set {
                isEnabled = value;
                MakeDispatcherOperation();
            }
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

        void MakeDispatcherOperation([CallerMemberName]string name = "") {
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
    }
}
