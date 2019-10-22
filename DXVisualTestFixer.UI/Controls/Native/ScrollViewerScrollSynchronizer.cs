using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls.Native {
	class ScrollViewerScrollSynchronizer : ControlsRegistrator<DraggableScrollViewer> {
		protected override void RegisterCore(DraggableScrollViewer scrollViewer) {
			scrollViewer.ScrollChanged += scrollChanged;
		}

		protected override void UnregisterCore(DraggableScrollViewer scrollViewer) {
			if(scrollViewer == null)
				return;
			scrollViewer.ScrollChanged -= scrollChanged;
		}

		void scrollChanged(object sender, ScrollChangedEventArgs e) {
			foreach(var sv in GetActualControls()) {
				sv.ScrollToHorizontalOffset(e.HorizontalOffset);
				sv.ScrollToVerticalOffset(e.VerticalOffset);
			}
		}
	}
}