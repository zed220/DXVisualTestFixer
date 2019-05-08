using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Bars.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DXVisualTestFixer.UI.Controls.Native {
    class ImageScaleSynchronizer : ControlsRegistrator<ScrollViewer> {
        int scale = 100;
        bool _IsPerfectPixel, _ShowGridLines;

        public bool IsPerfectPixel {
            get { return _IsPerfectPixel; }
            set {
                _IsPerfectPixel = value;
                Zoom100();
            }
        }
        public bool ShowGridLines {
            get { return _ShowGridLines; }
            set {
                _ShowGridLines = value;
                SetScale(scale);
            }
        }

        public void ZoomOut() {
            int d = IsPerfectPixel ? 100 : 10;
            int min = IsPerfectPixel ? 100 : 30;
            SetScale(Math.Max(min, scale - d));
        }
        public void ZoomIn() {
            int d = IsPerfectPixel ? 100 : 10;
            SetScale(Math.Min(1000, scale + d));
        }
        public void Zoom100() {
            SetScale(100);
        }

        protected override void RegisterCore(ScrollViewer control) {
            control.PreviewMouseWheel += previewMouseWheel;
            SetScale(scale);
        }
        protected override void UnregisterCore(ScrollViewer control) {
            control.PreviewMouseWheel -= previewMouseWheel;
        }

        void previewMouseWheel(object sender, MouseWheelEventArgs e) {
            e.Handled = true;
            if(e.Delta < 0)
                ZoomOut();
            else
                ZoomIn();
        }

        public void SetScale(int scale) {
            var notInitializedControls = SetScaleCore(scale);
            if(notInitializedControls != null)
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => SetScaleCore(scale)));
        }
        List<ScrollViewer> SetScaleCore(int scale) {
            var notInitializedControls = new List<ScrollViewer>();
            foreach(var trackingControl in GetActualControls()) {
                var scaleImageControl = TreeHelper.GetChild<ScaleImageControl>(trackingControl);
                if(scaleImageControl == null) {
                    notInitializedControls.Add(trackingControl);
                    continue;
                }
                ScaleTransform scaleTransform = scaleImageControl.LayoutTransform as ScaleTransform;
                if(scaleTransform == null)
                    scaleImageControl.LayoutTransform = scaleTransform = new ScaleTransform();
                if(IsPerfectPixel) {
                    scaleTransform.ScaleX = 1;
                    scaleTransform.ScaleY = 1;
                    scaleImageControl.Scale = scale / 100;
                    scaleImageControl.ShowGridLines = ShowGridLines;
                }
                else {
                    scaleImageControl.Scale = 1;
                    scaleTransform.ScaleX = scale / 100d;
                    scaleTransform.ScaleY = scale / 100d;
                }
            }
            this.scale = scale;
            return notInitializedControls;
        }
    }
}
