using DevExpress.Xpf.Grid;
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
    }
}
