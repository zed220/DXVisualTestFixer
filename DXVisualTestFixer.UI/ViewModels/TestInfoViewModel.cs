using DXVisualTestFixer.Common;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.ViewModels {
    public class TestInfoViewModel : BindableBase, ITestInfoViewModel {
        readonly IMainViewModel mainViewModel;

        MergerdTestViewType _MergerdTestViewType;

        public MergerdTestViewType MergerdTestViewType {
            get { return _MergerdTestViewType; }
            set { SetProperty(ref _MergerdTestViewType, value, OnMergerdTestViewTypeChanged); }
        }
        public Action MoveNextRow { get { return mainViewModel.RaiseMoveNext; } }
        public Action MovePrevRow { get { return mainViewModel.RaiseMovePrev; } }
        public ITestInfoWrapper TestInfo { get { return mainViewModel.CurrentTest; } }

        public TestInfoViewModel(IMainViewModel mainViewModel) {
            this.mainViewModel = mainViewModel;
            MergerdTestViewType = mainViewModel.MergerdTestViewType;
        }

        void OnMergerdTestViewTypeChanged() {
            mainViewModel.MergerdTestViewType = MergerdTestViewType;
        }

        public void Valid() {
            TestInfo.CommitChange = true;
            MoveNextRow();
        }
        public void Invalid() {
            TestInfo.CommitChange = false;
            MoveNextRow();
        }

        public void OnNavigatedTo(NavigationContext navigationContext) {
            MergerdTestViewType = mainViewModel.MergerdTestViewType;
            RaisePropertyChanged(nameof(TestInfo));
        }

        public bool IsNavigationTarget(NavigationContext navigationContext) {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext) {
            
        }
    }
}
