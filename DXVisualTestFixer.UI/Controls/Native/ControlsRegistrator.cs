using DevExpress.Mvvm.Native;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace DXVisualTestFixer.UI.Controls.Native {
    abstract class ControlsRegistrator<T> where T : FrameworkElement {
        T before, current, diff;

        public void Register(MergedTestViewType scrollViewerType, T control) {
            switch(scrollViewerType) {
                case MergedTestViewType.Diff:
                    Unregister(diff);
                    diff = control;
                    break;
                case MergedTestViewType.Before:
                    Unregister(before);
                    before = control;
                    break;
                case MergedTestViewType.Current:
                    Unregister(current);
                    current = control;
                    break;
            }
            RegisterCore(control);
        }
        public void Unregister(T control) {
            if(control == null)
                return;
            UnregisterCore(control);
        }

        protected IEnumerable<T> GetActualControls() {
            return new[] { before, current, diff }.Where(x => x != null);
        }

        protected abstract void RegisterCore(T control);
        protected abstract void UnregisterCore(T control);
    }
}
