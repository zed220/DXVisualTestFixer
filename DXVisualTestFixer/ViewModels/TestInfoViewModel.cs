using DevExpress.Mvvm;
using DXVisualTestFixer.Core;
using DXVisualTestFixer.Mif;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface ITestInfoViewModel : ISupportParameter { }

    public class TestInfoModel {
        public TestInfoModel(MergerdTestViewType mergerdTestViewType) {
            MergerdTestViewType = mergerdTestViewType;
        }

        public TestInfoWrapper TestInfo { get; set; }
        public Action MoveNextRow { get; set; }
        public Action MovePrevRow { get; set; }
        public MergerdTestViewType MergerdTestViewType { get; }
        public Action<MergerdTestViewType> SetMergerdTestViewType;
    }

    public class TestInfoViewModel : ViewModelBase, ITestInfoViewModel {
        public TestInfoModel TestInfoModel {
            get { return GetProperty(() => TestInfoModel); }
            private set { SetProperty(() => TestInfoModel, value); }
        }
        public MergerdTestViewType MergerdTestViewType {
            get { return GetProperty(() => MergerdTestViewType); }
            set { SetProperty(() => MergerdTestViewType, value, OnMergerdTestViewTypeChanged); }
        }

        void OnMergerdTestViewTypeChanged() {
            TestInfoModel?.SetMergerdTestViewType(MergerdTestViewType);
        }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            TestInfoModel = (TestInfoModel)parameter;
            MergerdTestViewType = TestInfoModel.MergerdTestViewType;
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
