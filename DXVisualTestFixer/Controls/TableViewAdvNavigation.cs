﻿using DevExpress.Xpf.Grid;
using DXVisualTestFixer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Controls {
    public class TableViewAdvNavigation : TableView {
        public void MoveNextDataRow() {
            RepeatMoveAction(i => ++i);
        }
        public void MovePrevDataRow() {
            RepeatMoveAction(i => --i);
        }
        void RepeatMoveAction(Func<int, int> action) {
            int tryCount = 0;
            int focusedRowHandleCandidate = FocusedRowHandle;
            int visibleIndex = Grid.GetRowVisibleIndexByHandle(FocusedRowHandle);
            do {
                visibleIndex = action(visibleIndex);
                focusedRowHandleCandidate = Grid.GetRowHandleByVisibleIndex(visibleIndex);
            }
            while(focusedRowHandleCandidate < 0 && tryCount++ < 5);
            if(focusedRowHandleCandidate >= 0)
                FocusedRowHandle = focusedRowHandleCandidate;
        }

        public void DoubleClick(RowDoubleClickEventArgs e) {
            if(!e.HitInfo.InRow)
                return;
            if(Grid.IsGroupRowHandle(e.HitInfo.RowHandle))
                return;
            if(e.HitInfo.Column.FieldName != "TestInfo.Theme")
                return;
            e.Handled = true;
            TestInfoWrapper testInfoWrapper = Grid.GetRow(e.HitInfo.RowHandle) as TestInfoWrapper;
            if(testInfoWrapper == null || testInfoWrapper.Valid == Core.TestState.Error)
                return;
            testInfoWrapper.CommitChange = !testInfoWrapper.CommitChange;
        }
    }
}
