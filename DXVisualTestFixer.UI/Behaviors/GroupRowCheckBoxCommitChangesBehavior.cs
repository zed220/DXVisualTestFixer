using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DXVisualTestFixer.UI.Behaviors {
    public class GroupRowCheckBoxCommitChangesBehavior : Behavior<CheckEdit> {
        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.EditValueChanging += AssociatedObject_EditValueChanging;
        }

        bool passUpdate = false;

        void AssociatedObject_EditValueChanging(object sender, EditValueChangingEventArgs e) {
            if(!AssociatedObject.IsKeyboardFocusWithin || passUpdate)
                return;
            e.IsCancel = true;
            e.Handled = true;
            GroupRowData data = AssociatedObject.DataContext as GroupRowData;
            TableView view = ((TableView)data.View);
            int rowCount = view.Grid.GetChildRowCount(data.RowHandle.Value);
            view.Grid.BeginDataUpdate();
            for(int i = 0; i < rowCount; i++) {
                int childRowHandle = view.Grid.GetChildRowHandle(data.RowHandle.Value, i);
                if(childRowHandle < 0)
                    continue;
                ITestInfoModel rowElement = ((ITestInfoModel)view.Grid.GetRow(childRowHandle));
                if(rowElement.Valid != TestState.Error)
                    rowElement.CommitChange = (bool)e.NewValue;
            }
            passUpdate = true;
            Dispatcher.BeginInvoke(new Action(() => {
                view.Grid.EndDataUpdate();
                passUpdate = false;
            }), DispatcherPriority.Background);
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.EditValueChanging -= AssociatedObject_EditValueChanging;
        }
    }
}
