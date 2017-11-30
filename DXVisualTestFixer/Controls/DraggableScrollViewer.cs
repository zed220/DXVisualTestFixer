using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DXVisualTestFixer.Controls {
    public class DraggableScrollViewer : ScrollViewer {
        Point scrollMousePoint = new Point();
        Point offset;
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonDown(e);
            scrollMousePoint = e.GetPosition(this);
            offset = new Point(HorizontalOffset, VerticalOffset);
            CaptureMouse();
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonUp(e);
            ReleaseMouseCapture();
        }
        protected override void OnPreviewMouseMove(MouseEventArgs e) {
            base.OnPreviewMouseMove(e);
            if(IsMouseCaptured) {
                ScrollToHorizontalOffset(offset.X + (scrollMousePoint.X - e.GetPosition(this).X));
                ScrollToVerticalOffset(offset.Y + (scrollMousePoint.Y - e.GetPosition(this).Y));
            }
        }
    }
}
