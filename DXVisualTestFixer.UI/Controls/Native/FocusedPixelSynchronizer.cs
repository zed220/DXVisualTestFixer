using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Bars.Native;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DXVisualTestFixer.UI.Controls.Native {
    class FocusedPixelSynchronizer : ControlsRegistrator<ScrollViewer> {
        bool _IsEnabled;

        public bool IsEnabled {
            get { return _IsEnabled; }
            set {
                _IsEnabled = value;
                EnumerateTrackingControls(el => {
                    el.ShowHighlightedPoint = value;
                });
            }
        }

        protected override void RegisterCore(ScrollViewer control) {
            control.PreviewMouseMove += Control_PreviewMouseMove;
            control.MouseEnter += Control_MouseEnter;
            control.MouseLeave += Control_MouseLeave;
        }
        protected override void UnregisterCore(ScrollViewer control) {
            control.PreviewMouseMove -= Control_PreviewMouseMove;
            control.MouseEnter -= Control_MouseEnter;
            control.MouseLeave -= Control_MouseLeave;
        }

        void Control_MouseLeave(object sender, MouseEventArgs e) {
            EnumerateTrackingControls(el => {
                el.ShowHighlightedPoint = false;
                el.HighlightedPoint = default(Point);
            });
        }
        void Control_MouseEnter(object sender, MouseEventArgs e) {
            UpdateTrackingPoint(e);
        }
        void Control_PreviewMouseMove(object sender, MouseEventArgs e) {
            UpdateTrackingPoint(e);
        }
        void UpdateTrackingPoint(MouseEventArgs e) {
            var pos = e.GetPosition((FrameworkElement)e.OriginalSource);
            EnumerateTrackingControls(el => {
                el.ShowHighlightedPoint = IsEnabled;
                el.HighlightedPoint = IsEnabled ? pos : default(Point);
            });
        }

        void EnumerateTrackingControls(Action<ScaleImageControl> action) {
            foreach(var trackingControl in GetActualControls().Select(x => TreeHelper.GetChild<ScaleImageControl>(x)).Where(x => x != null)) {
                action(trackingControl);
            }
        }
    }
}
