using DevExpress.Xpf.Docking;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.PrismCommon {
    public class LayoutPanelRegionAdapter : RegionAdapterBase<LayoutPanel> {
        public LayoutPanelRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory) : base(regionBehaviorFactory) {
        }

        protected override void Adapt(IRegion region, LayoutPanel regionTarget) {
            region.Views.CollectionChanged += (o, e) => Views_CollectionChanged(o, e, regionTarget);
        }

        void Views_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e, LayoutPanel regionTarget) {
            switch(e.Action) {
                case NotifyCollectionChangedAction.Add:
                    regionTarget.Content = e.NewItems.Cast<object>().Last();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    regionTarget.Content = null;
                    break;
                case NotifyCollectionChangedAction.Replace:
                    regionTarget.Content = e.NewItems.Cast<object>().Last();
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        protected override IRegion CreateRegion() {
            return new SingleActiveRegion();
        }
    }
}
