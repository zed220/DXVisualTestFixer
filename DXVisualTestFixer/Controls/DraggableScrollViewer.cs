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
        public static readonly DependencyProperty DraggableModeProperty;

        static DraggableScrollViewer() {
            DraggableModeProperty = DependencyProperty.RegisterAttached("DraggableMode", typeof(bool), typeof(DraggableScrollViewer), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
        }

        public static bool GetDraggableMode(DependencyObject obj) {
            return (bool)obj.GetValue(DraggableModeProperty);
        }

        public static void SetDraggableMode(DependencyObject obj, bool value) {
            obj.SetValue(DraggableModeProperty, value);
        }

        Point scrollMousePoint = new Point();
        Point offset;
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonDown(e);
            if(!GetDraggableMode(this))
                return;
            scrollMousePoint = e.GetPosition(this);
            offset = new Point(HorizontalOffset, VerticalOffset);
            CaptureMouse();
        }
        protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {
            base.OnPreviewMouseLeftButtonUp(e);
            if(!GetDraggableMode(this))
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
    }
}
