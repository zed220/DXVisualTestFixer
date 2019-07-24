using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace DXVisualTestFixer.UI.Behaviors {
    public class IsFilteredBehavior : Behavior<FrameworkElement> {
        public static readonly DependencyProperty FieldNameProperty;
        public static readonly DependencyProperty ViewProperty;
        public static readonly DependencyProperty IsFilteredProperty;

        static IsFilteredBehavior() {
            Type ownerType = typeof(IsFilteredBehavior);
            FieldNameProperty = DependencyProperty.Register(nameof(FieldName), typeof(string), ownerType, new PropertyMetadata(null, (d, e) => (d as IsFilteredBehavior)?.UpdateIsFiltered()));
            ViewProperty = DependencyProperty.Register(nameof(View), typeof(DataViewBase), ownerType, new PropertyMetadata(null, (d, e) => ((IsFilteredBehavior)d).UpdateIsFiltered()));
            IsFilteredProperty = DependencyProperty.RegisterAttached("IsFiltered", typeof(bool), ownerType, new PropertyMetadata(false));
        }

        public static bool GetIsFiltered(DependencyObject obj) => (bool)obj.GetValue(IsFilteredProperty);
        public static void SetIsFiltered(DependencyObject obj, bool value) => obj.SetValue(IsFilteredProperty, value);

        public string FieldName {
            get { return (string)GetValue(FieldNameProperty); }
            set { SetValue(FieldNameProperty, value); }
        }
        public DataViewBase View {
            get { return (DataViewBase)GetValue(ViewProperty); }
            set { SetValue(ViewProperty, value); }
        }

        protected override void OnAttached() {
            base.OnAttached();
            UpdateIsFiltered();
        }

        void UpdateIsFiltered() {
            var tableView = View as TableView;
            if(AssociatedObject == null || String.IsNullOrEmpty(FieldName) || tableView == null)
                return;
            var column = tableView.Grid.Columns[FieldName];
            if(column == null)
                return;
            BindingOperations.SetBinding(AssociatedObject, IsFilteredProperty, new Binding("IsFiltered") { Source = column });
        }
    }
}
