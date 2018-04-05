using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DXVisualTestFixer.Behaviors {
    public class UnusedFilesDragDropBehavior : Behavior<TableView> {
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.DragRecordOver += AssociatedObject_DragRecordOver;
        }

        void AssociatedObject_DragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e) {
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
