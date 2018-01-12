using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Controls;
using DXVisualTestFixer.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Behaviors {
    public class CustomFilterPopulatorBehavior : Behavior<GridControlEx> {
        #region inner classes
        class CustomFilterItem : ICustomItem {
            public object EditValue { get; set; }
            public object DisplayValue { get; set; }
        }
        #endregion

        TableView View { get { return (TableView)AssociatedObject.View; } }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.ItemsSourceChanged += AssociatedObject_ItemsSourceChanged;
        }

        void AssociatedObject_ItemsSourceChanged(object sender, ItemsSourceChangedEventArgs e) {
            AssociatedObject.ClearColumnFilter("TeamName");
            View.CompactModeFilterItems.Clear();
            List<CompactModeFilterItem> filterItems = new List<CompactModeFilterItem>() { new CompactModeFilterItem() { DisplayValue = "All" } };
            foreach(var team in AssociatedObject.GetProducts().OrderBy(t => t)) {
                CompactModeFilterItem item = new CompactModeFilterItem();
                item.DisplayValue = team;
                item.EditValue = $"[TeamName] = '{team}'";
                filterItems.Add(item);
            }
            if(filterItems.Count > 2) {
                filterItems.ForEach(View.CompactModeFilterItems.Add);
                View.CompactPanelShowMode = CompactPanelShowMode.Always;
            }
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.ItemsSourceChanged -= AssociatedObject_ItemsSourceChanged;
        }
    }
}
