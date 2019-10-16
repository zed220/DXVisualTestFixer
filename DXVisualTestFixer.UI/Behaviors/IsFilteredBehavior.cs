using System.Windows;
using System.Windows.Data;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.Grid;

namespace DXVisualTestFixer.UI.Behaviors {
	public class IsFilteredBehavior : Behavior<FrameworkElement> {
		public static readonly DependencyProperty FieldNameProperty;
		public static readonly DependencyProperty ViewProperty;
		public static readonly DependencyProperty IsFilteredProperty;

		static IsFilteredBehavior() {
			var ownerType = typeof(IsFilteredBehavior);
			FieldNameProperty = DependencyProperty.Register(nameof(FieldName), typeof(string), ownerType, new PropertyMetadata(null, (d, e) => (d as IsFilteredBehavior)?.UpdateIsFiltered()));
			ViewProperty = DependencyProperty.Register(nameof(View), typeof(DataViewBase), ownerType, new PropertyMetadata(null, (d, e) => ((IsFilteredBehavior) d).UpdateIsFiltered()));
			IsFilteredProperty = DependencyProperty.RegisterAttached("IsFiltered", typeof(bool), ownerType, new PropertyMetadata(false));
		}

		public string FieldName {
			get => (string) GetValue(FieldNameProperty);
			set => SetValue(FieldNameProperty, value);
		}

		public DataViewBase View {
			get => (DataViewBase) GetValue(ViewProperty);
			set => SetValue(ViewProperty, value);
		}

		public static bool GetIsFiltered(DependencyObject obj) {
			return (bool) obj.GetValue(IsFilteredProperty);
		}

		public static void SetIsFiltered(DependencyObject obj, bool value) {
			obj.SetValue(IsFilteredProperty, value);
		}

		protected override void OnAttached() {
			base.OnAttached();
			UpdateIsFiltered();
		}

		void UpdateIsFiltered() {
			var tableView = View as TableView;
			if(AssociatedObject == null || string.IsNullOrEmpty(FieldName) || tableView == null)
				return;
			var column = tableView.Grid.Columns[FieldName];
			if(column == null)
				return;
			BindingOperations.SetBinding(AssociatedObject, IsFilteredProperty, new Binding("IsFiltered") {Source = column});
		}
	}
}