using DXVisualTestFixer.Common;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DXVisualTestFixer.UI.ViewModels {
    public class ShellViewModel : BindableBase, IShellViewModel {
        readonly IUpdateService updateService;
        readonly IDXNotification notification;

        bool _HasUpdate;

        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        public ShellViewModel(IUpdateService updateService, IDXNotification notification) {
            this.updateService = updateService;
            this.notification = notification;
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
            updateService.Update();
        }

        public IEnumerable<ICommand> Commands { get; }

        public bool HasUpdate {
            get { return _HasUpdate; }
            set { SetProperty(ref _HasUpdate, value); }
        }

        public void DoNotification(string title, string content, MessageBoxImage image = MessageBoxImage.Information) {
            ViewModelBase.SetupNotification(notification, title, content, image);
            NotificationRequest.Raise(notification);
        }
    }
}
