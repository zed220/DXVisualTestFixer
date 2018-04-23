using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using DXVisualTestFixer.Services;
using DXVisualTestFixer.ViewModels;
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

namespace DXVisualTestFixer.Views {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class Shell : ThemedWindow {
        public Shell() {
            InitializeComponent();
        }
    }
}
