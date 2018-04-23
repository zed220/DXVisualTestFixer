using DXVisualTestFixer.Services;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DXVisualTestFixer.ViewModels {
    public interface IShellViewModel {
        IEnumerable<ICommand> Commands { get; }
    }

    public class ShellViewModel : BindableBase, IShellViewModel {
        readonly IUpdateService updateService;

        bool _HasUpdate;

        public ShellViewModel(IUpdateService updateService) {
            this.updateService = updateService;
            Commands = new List<ICommand>() { new DelegateCommand(Update).ObservesCanExecute(() => HasUpdate) };
            if(updateService.HasUpdate) {
                HasUpdate = true;
                if(updateService.IsNetworkDeployment)
                    Update();
                return;
            }
            updateService.PropertyChanged += UpdateService_PropertyChanged;
            updateService.Start();
        }

        void UpdateService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(e.PropertyName == nameof(IUpdateService.HasUpdate))
                HasUpdate = updateService.HasUpdate;
        }

        void Update() {
            System.Windows.Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }

        public IEnumerable<ICommand> Commands { get; }

        public bool HasUpdate {
            get { return _HasUpdate; }
            set { SetProperty(ref _HasUpdate, value); }
        }
    }
}
