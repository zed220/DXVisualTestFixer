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

        bool _HasUpdate;
        bool _IsInUpdate;
        bool _IsActive = true;

        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        public ShellViewModel(IUpdateService updateService, IDXNotification notification, INotificationService notificationService) {
            this.updateService = updateService;
            this.notification = notification;
            this.notificationService = notificationService;
            notificationService.Notification += NotificationService_Notification;
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

        void NotificationService_Notification(object sender, INotificationServiceArgs e) {
            ViewModelBase.SetupNotification(notification, e.Title, e.Content, e.Image);
            NotificationRequest.Raise(notification);
        }

        void UpdateService_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch(e.PropertyName) {
                case nameof(IUpdateService.HasUpdate):
                    HasUpdate = updateService.HasUpdate;
                    break;
                case nameof(IUpdateService.IsInUpdate):
                    IsInUpdate = updateService.IsInUpdate;
                    break;
            }
        }

        void Update() => updateService.Update();

        public IEnumerable<ICommand> Commands { get; }

        public bool HasUpdate {
            get => _HasUpdate;
            set => SetProperty(ref _HasUpdate, value);
        }
        public bool IsInUpdate {
            get => _IsInUpdate;
            set => SetProperty(ref _IsInUpdate, value);
        }
    }
}
