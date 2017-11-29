using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.ViewModels {
    public interface IApplyChangesViewModel : ISupportParameter { }

    public class ApplyChangesViewModel : ViewModelBase, IApplyChangesViewModel {
        public IEnumerable<UICommand> DialogCommands { get; private set; }

        public ChangedTestsModel ChangedTestsModel {
            get { return GetProperty(() => ChangedTestsModel); }
            set { SetProperty(() => ChangedTestsModel, value); }
        }

        public ApplyChangesViewModel() {
            CreateCommands();
        }

        protected override void OnParameterChanged(object parameter) {
            base.OnParameterChanged(parameter);
            ChangedTestsModel = parameter as ChangedTestsModel;
        }

        void CreateCommands() {
            List<UICommand> dialogCommands = new List<UICommand>();
            dialogCommands.Add(new UICommand() { IsDefault = true, Command = new DelegateCommand(Apply), Caption = DXMessageBoxLocalizer.GetString(DXMessageBoxStringId.Yes) });
            dialogCommands.Add(new UICommand() { IsCancel = true, Caption = DXMessageBoxLocalizer.GetString(DXMessageBoxStringId.Cancel) });
            DialogCommands = dialogCommands;
        }

        void Apply() {
            ChangedTestsModel.Apply = true;
        }
    }
}
