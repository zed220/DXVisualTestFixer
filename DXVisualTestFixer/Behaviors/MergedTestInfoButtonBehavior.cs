using DevExpress.Mvvm.UI.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.Behaviors {
    public class MergedTestInfoButtonBehavior : Behavior<Button> {
        public static readonly DependencyProperty Hide1Property = DependencyProperty.Register("Hide1", typeof(FrameworkElement), typeof(MergedTestInfoButtonBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty Hide2Property = DependencyProperty.Register("Hide2", typeof(FrameworkElement), typeof(MergedTestInfoButtonBehavior), new PropertyMetadata(null));
        public static readonly DependencyProperty ShowProperty = DependencyProperty.Register("Show", typeof(FrameworkElement), typeof(MergedTestInfoButtonBehavior), new PropertyMetadata(null));


        public FrameworkElement Hide1 {
            get { return (FrameworkElement)GetValue(Hide1Property); }
            set { SetValue(Hide1Property, value); }
        }
        public FrameworkElement Hide2 {
            get { return (FrameworkElement)GetValue(Hide2Property); }
            set { SetValue(Hide2Property, value); }
        }
        public FrameworkElement Show {
            get { return (FrameworkElement)GetValue(ShowProperty); }
            set { SetValue(ShowProperty, value); }
        }

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Click += AssociatedObject_Click;
        }

        void AssociatedObject_Click(object sender, RoutedEventArgs e) {
            if(Hide1 != null)
                Hide1.Visibility = Visibility.Collapsed;
            if(Hide2 != null)
                Hide2.Visibility = Visibility.Collapsed;
            if(Show != null)
                Show.Visibility = Visibility.Visible;
        }

        protected override void OnDetaching() {
            base.OnDetaching();
            AssociatedObject.Click -= AssociatedObject_Click;
        }

    }
}
