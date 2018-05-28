using DevExpress.Mvvm;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DXVisualTestFixer.UI.Common {
    public class LoadingProgressController : BindableBase, ILoadingProgressController {
        public int Maximum {
            get { return GetProperty(() => Maximum); }
            private set { SetProperty(() => Maximum, value); }
        }
        public int Value {
            get { return GetProperty(() => Value); }
            private set { SetProperty(() => Value, value); }
        }

        public bool IsEnabled {
            get { return GetProperty(() => IsEnabled); }
            private set { SetProperty(() => IsEnabled, value); }
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
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { Value += delta; }));
        }
        public void Enlarge(int delta) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => { Maximum += delta; }));
        }
        public void Flush() {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                Maximum = 0;
                Value = 0;
            }));
        }
    }
}
