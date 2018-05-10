using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using Microsoft.Practices.Unity;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BindableBase = Prism.Mvvm.BindableBase;

namespace DXVisualTestFixer.UI.ViewModels {
    public class ApplyChangesViewModel : BindableBase, IApplyChangesViewModel {
        List<ITestInfoWrapper> _ChangedTests;

        public IEnumerable<UICommand> DialogCommands { get; private set; }

        public List<ITestInfoWrapper> ChangedTests {
            get { return _ChangedTests; }
            private set { SetProperty(ref _ChangedTests, value); }
        }

        public bool Confirmed { get; set; }
        public string Title { get;  set; }
        public object Content { get; set; }

        public IEnumerable<UICommand> Commands { get; }

        public ApplyChangesViewModel(IMainViewModel mainViewModel) {
            Title = "Settings";
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => Confirmed = true);
            ChangedTests = mainViewModel.GetChangedTests();
        }
    }
}
