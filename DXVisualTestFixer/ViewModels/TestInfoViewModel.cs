using DevExpress.Mvvm;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface ITestInfoViewModel : ISupportParameter { }

    public class TestInfoModel {
        public TestInfoWrapper TestInfo { get; set; }
        public Action MoveNextRow { get; set; }
        public Action MovePrevRow { get; set; }
    }

    public class TestInfoViewModel : ViewModelBase, ITestInfoViewModel {
        public TestInfoModel TestInfoModel {
            get { return GetProperty(() => TestInfoModel); }
            private set { SetProperty(() => TestInfoModel, value); }
        }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            TestInfoModel = parameter as TestInfoModel;
        }

        public void Valid() {
            TestInfoModel.TestInfo.CommitChange = true;
            TestInfoModel.MoveNextRow();
        }
        public void Invalid() {
            TestInfoModel.TestInfo.CommitChange = false;
            TestInfoModel.MoveNextRow();
        }
    }
}
