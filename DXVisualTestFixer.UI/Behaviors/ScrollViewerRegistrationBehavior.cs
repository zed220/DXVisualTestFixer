using DevExpress.Mvvm.UI.Interactivity;
using DXVisualTestFixer.UI.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DXVisualTestFixer.UI.Behaviors {
    public class ScrollViewerRegistrationBehavior : Behavior<DraggableScrollViewer> {
        public static readonly DependencyProperty ImageScalingControlProperty;
        public static readonly DependencyProperty ScrollViewerTypeProperty;

        static ScrollViewerRegistrationBehavior() {
            Type ownerType = typeof(ScrollViewerRegistrationBehavior);
            ImageScalingControlProperty = DependencyProperty.Register("ImageScalingControl", typeof(ImageScalingControl), ownerType, new PropertyMetadata(null, Register));
            ScrollViewerTypeProperty = DependencyProperty.Register("ScrollViewerType", typeof(MergedTestViewType?), ownerType, new PropertyMetadata(null, Register));
        }

        static void Register(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((ScrollViewerRegistrationBehavior)d).Register();
        }

        public ImageScalingControl ImageScalingControl {
            get { return (ImageScalingControl)GetValue(ImageScalingControlProperty); }
            set { SetValue(ImageScalingControlProperty, value); }
        }
        public MergedTestViewType? ScrollViewerType {
            get { return (MergedTestViewType?)GetValue(ScrollViewerTypeProperty); }
            set { SetValue(ScrollViewerTypeProperty, value); }
        }

        protected override void OnAttached() {
            base.OnAttached();
            Register();
        }

        protected override void OnDetaching() {
            base.OnDetaching();
        }

        bool isRegistered;

        void Register() {
            if(isRegistered)
                return;
            if(ImageScalingControl == null || !ScrollViewerType.HasValue)
                return;
            ImageScalingControl.StartTrackingScrollViewer(AssociatedObject, ScrollViewerType.Value);
            isRegistered = true;
        }
    }
}
