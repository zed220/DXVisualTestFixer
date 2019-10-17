using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevExpress.Xpf.Utils;

namespace DXVisualTestFixer.UI.Controls {
	public class DraggableScrollViewer : ScrollViewer {
		public static readonly DependencyProperty ScrollModeProperty;
		Point offset;

		Point scrollMousePoint;

		static DraggableScrollViewer() {
			var ownerType = typeof(DraggableScrollViewer);
			ScrollModeProperty = DependencyProperty.Register("ScrollMode", typeof(ScrollMode), ownerType, new PropertyMetadata(ScrollMode.Draggable));
		}

		public ScrollMode ScrollMode {
			get => (ScrollMode) GetValue(ScrollModeProperty);
			set => SetValue(ScrollModeProperty, value);
		}

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
			if(!IsMouseCaptured) return;
			ScrollToHorizontalOffset(offset.X + (scrollMousePoint.X - e.GetPosition(this).X));
			ScrollToVerticalOffset(offset.Y + (scrollMousePoint.Y - e.GetPosition(this).Y));
		}

		protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
			if(KeyboardHelper.IsShiftPressed) {
				e.Handled = true;
				ScrollToHorizontalOffset(HorizontalOffset + (e.Delta > 0 ? -30 : 30));
				return;
			}

			if(ScrollMode == ScrollMode.Legacy)
				if(!KeyboardHelper.IsControlPressed) {
					e.Handled = true;
					ScrollToVerticalOffset(VerticalOffset + -e.Delta);
				}

			base.OnPreviewMouseWheel(e);
		}
	}
}