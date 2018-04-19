using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using DXVisualTestFixer.Services;
using DXVisualTestFixer.Views;
using Microsoft.Practices.Unity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DXVisualTestFixer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class Shell : ThemedWindow, IShell {
        public Shell(IUnityContainer container, IRegionManager regionManager) {
            InitializeComponent();
            regionManager.RegisterViewWithRegion(Regions.Regions.Main, typeof(MainView));
        }

        IButton headerItem;
        public IButton HeaderItem {
            get { return headerItem; }
            set {
                headerItem = value;
                OnHeaderItemChanged();
            }
        }

        void OnHeaderItemChanged() {
            HeaderItemsSource = new List<IButton>() { HeaderItem };
        }
    }
    public interface IShell {
        IButton HeaderItem { get; set; }
    }
}
