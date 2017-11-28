using DevExpress.Mvvm;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface ITestInfoViewModel : ISupportParameter { }

    public class TestInfoViewModel : ViewModelBase, ITestInfoViewModel {
        public TestInfo TestInfo {
            get { return GetProperty(() => TestInfo); }
            private set { SetProperty(() => TestInfo, value); }
        }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            TestInfo = parameter as TestInfo;
        }
    }
}
