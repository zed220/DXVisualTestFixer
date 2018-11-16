using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Controls {
    public class FilterPanelControl : Control {
        public static readonly DependencyProperty HasFixedTestsProperty;

        static FilterPanelControl() {
            Type ownerType = typeof(FilterPanelControl);
            HasFixedTestsProperty = DependencyProperty.Register("HasFixedTests", typeof(bool), ownerType, new PropertyMetadata(false));

            DefaultStyleKeyProperty.OverrideMetadata(ownerType, new FrameworkPropertyMetadata(ownerType));
        }

        public bool HasFixedTests {
            get { return (bool)GetValue(HasFixedTestsProperty); }
            set { SetValue(HasFixedTestsProperty, value); }
        }
    }
}
