using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;
using DXVisualTestFixer.Common;
using JetBrains.Annotations;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class ShellViewModel : BindableBase {
		readonly IDXNotification _notification;
		readonly IUpdateService _updateService;

		UpdateState _updateState = UpdateState.None;

		public ShellViewModel(IUpdateService updateService, IDXNotification notification, INotificationService notificationService) {
			_updateService = updateService;
			_notification = notification;
			notificationService.Notification += NotificationService_Notification;
			Commands = new List<ICommand> {new DelegateCommand(Update).ObservesCanExecute(() => HasUpdate)};
			if(updateService.HasUpdate) {
				UpdateState = UpdateState.Ready;
				if(updateService.IsNetworkDeployment)
					Update();
				return;
			}

			updateService.PropertyChanged += UpdateService_PropertyChanged;
			updateService.Start();
		}

		[UsedImplicitly] public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

		[UsedImplicitly] public IEnumerable<ICommand> Commands { get; }

		[UsedImplicitly]
		public UpdateState UpdateState {
			get => _updateState;
			set {
				SetProperty(ref _updateState, value);
				RaisePropertyChanged(nameof(HasUpdate));
			}
		}

		bool HasUpdate => UpdateState == UpdateState.Ready;

		void NotificationService_Notification(object sender, INotificationServiceArgs e) {
			ViewModelBase.SetupNotification(_notification, e.Title, e.Content, e.Image);
			NotificationRequest.Raise(_notification);
		}

		void UpdateService_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if(_updateService.HasUpdate) {
				UpdateState = UpdateState.Ready;
				return;
			}

			if(_updateService.IsInUpdate) {
				UpdateState = UpdateState.Downloading;
				return;
			}

			UpdateState = UpdateState.None;
		}

		void Update() {
			_updateService.Update();
		}
	}

	public enum UpdateState {
		None,
		Downloading,
		Ready
	}
}