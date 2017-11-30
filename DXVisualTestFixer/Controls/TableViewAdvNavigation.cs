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
            RepeatMoveAction(MoveNextRow);
        }
        public void MovePrevDataRow() {
            RepeatMoveAction(MovePrevRow);
        }
        void RepeatMoveAction(Action action) {
            int tryCount = 0;
            do {
                action();
                tryCount++;
            } while(FocusedRowHandle < 0 && tryCount < 5);
            if(FocusedRowHandle < 0)
                FocusedRowHandle = 0;
        }
    }
}
