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
    public class ShellViewModel : BindableBase {
        readonly IUpdateService updateService;
        readonly IDXNotification notification;
        readonly INotificationService notificationService;

        bool _IsActive = true;
        UpdateState _UpdateState = UpdateState.None;

        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        public ShellViewModel(IUpdateService updateService, IDXNotification notification, INotificationService notificationService) {
            this.updateService = updateService;
            this.notification = notification;
            this.notificationService = notificationService;
            notificationService.Notification += NotificationService_Notification;
            Commands = new List<ICommand>() { new DelegateCommand(Update).ObservesCanExecute(() => HasUpdate) };
            if(updateService.HasUpdate) {
                UpdateState = UpdateState.Ready;
                if(updateService.IsNetworkDeployment)
                    Update();
                return;
            }
            updateService.PropertyChanged += UpdateService_PropertyChanged;
            updateService.Start();
        }

        void NotificationService_Notification(object sender, INotificationServiceArgs e) {
            ViewModelBase.SetupNotification(notification, e.Title, e.Content, e.Image);
            NotificationRequest.Raise(notification);
        }

        void UpdateService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if(updateService.HasUpdate) {
                UpdateState = UpdateState.Ready;
                return;
            }
            if(updateService.IsInUpdate) {
                UpdateState = UpdateState.Downloading;
                return;
            }
            UpdateState = UpdateState.None;
        }

        void Update() => updateService.Update();

        public IEnumerable<ICommand> Commands { get; }

        public UpdateState UpdateState {
            get => _UpdateState;
            set {
                SetProperty(ref _UpdateState, value);
                OnPropertyChanged(nameof(HasUpdate));
            }
        }

        public bool HasUpdate => UpdateState == UpdateState.Ready;
    }

    public enum UpdateState {
        None,
        Downloading,
        Ready
    }
}
