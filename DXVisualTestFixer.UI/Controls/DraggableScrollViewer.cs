using DevExpress.Xpf.Core;
using DevExpress.Xpf.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DXVisualTestFixer.UI.Controls {
    public class DraggableScrollViewer : ScrollViewer {
        public static readonly DependencyProperty ScrollModeProperty;

        static DraggableScrollViewer() {
            Type ownerType = typeof(DraggableScrollViewer);
            ScrollModeProperty = DependencyProperty.Register("ScrollMode", typeof(ScrollMode), ownerType, new PropertyMetadata(ScrollMode.Draggable));
        }

        public ScrollMode ScrollMode {
            get { return (ScrollMode)GetValue(ScrollModeProperty); }
            set { SetValue(ScrollModeProperty, value); }
        }

        Point scrollMousePoint = new Point();
        Point offset;
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonDown(e);
            if(ScrollMode == ScrollMode.Legacy)
                return;
            scrollMousePoint = e.GetPosition(this);
            offset = new Point(HorizontalOffset, VerticalOffset);
            CaptureMouse();
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonUp(e);
            if(ScrollMode == ScrollMode.Legacy)
                return;
            ReleaseMouseCapture();
        }
        protected override void OnPreviewMouseMove(MouseEventArgs e) {
            base.OnPreviewMouseMove(e);
            if(IsMouseCaptured) {
                ScrollToHorizontalOffset(offset.X + (scrollMousePoint.X - e.GetPosition(this).X));
                ScrollToVerticalOffset(offset.Y + (scrollMousePoint.Y - e.GetPosition(this).Y));
            }
        }
        protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
            if(KeyboardHelper.IsShiftPressed) {
                e.Handled = true;
                ScrollToHorizontalOffset(HorizontalOffset + (e.Delta > 0 ? -30 : 30));
                return;
            }
            base.OnPreviewMouseWheel(e);
        }
    }
}
