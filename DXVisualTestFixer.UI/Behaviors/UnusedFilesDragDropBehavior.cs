using System.Windows;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Grid;

namespace DXVisualTestFixer.UI.Behaviors {
	public class UnusedFilesDragDropBehavior : Behavior<TableView> {
		protected override void OnAttached() {
			base.OnAttached();
			AssociatedObject.DragRecordOver += AssociatedObject_DragRecordOver;
		}

		void AssociatedObject_DragRecordOver(object sender, DragRecordOverEventArgs e) {
			e.Handled = true;
			e.Effects = DragDropEffects.None;
			if(e.IsFromOutside)
				e.Effects = DragDropEffects.Move;
		}

		protected override void OnDetaching() {
			base.OnDetaching();
			AssociatedObject.DragRecordOver -= AssociatedObject_DragRecordOver;
		}
	}
}