using DevExpress.Data;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Behaviors {
    public class GridControlCustomSummaryBehavior : Behavior<GridControl> {
        Dictionary<int, SummaryCalculationBuffer> Cache = new Dictionary<int, SummaryCalculationBuffer>();

        class SummaryCalculationBuffer {
            int count = 0;
            int checkedCount = 0;

            public bool? GetResult() {
                if(checkedCount == 0)
                    return false;
                if(checkedCount == count)
                    return true;
                return null;
            }

            public void Add(bool value) {
                count++;
                if(value)
                    checkedCount++;
            }
        }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.CustomSummary += AssociatedObject_CustomSummary;
        }

        void AssociatedObject_CustomSummary(object sender, CustomSummaryEventArgs e) {
            if(!e.IsGroupSummary)
                return;
            switch(e.SummaryProcess) {
                case CustomSummaryProcess.Start:
                    Cache[e.GroupRowHandle] = new SummaryCalculationBuffer();
                    e.TotalValue = null;
                    break;
                case CustomSummaryProcess.Calculate:
                    Cache[e.GroupRowHandle].Add((bool)e.FieldValue);
                    break;
                case CustomSummaryProcess.Finalize:
                    e.TotalValue = Cache[e.GroupRowHandle].GetResult();
                    break;
            }
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.CustomSummary -= AssociatedObject_CustomSummary;
        }
    }
}
