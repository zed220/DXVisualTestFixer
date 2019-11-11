using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using DXVisualTestFixer.Common;
using JetBrains.Annotations;
using Microsoft.Practices.ServiceLocation;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace DXVisualTestFixer.UI.ViewModels {
	[UsedImplicitly]
	public class ShellViewModel : BindableBase {
		readonly IDXNotification _notification;
		readonly IUpdateService _updateService;
		readonly DispatcherTimer _whatsNewTimer;

		UpdateState _updateState = UpdateState.None;

		public ShellViewModel(IUpdateService updateService, IDXNotification notification, INotificationService notificationService) {
			_updateService = updateService;
			_notification = notification;
			_whatsNewTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
			_whatsNewTimer.Tick += (sender, args) => ShowWhatsNewCore();
			_whatsNewTimer.Interval = TimeSpan.FromSeconds(10);
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

		[UsedImplicitly]
		public void ShowWhatsNew() {
			var configVersion = ServiceLocator.Current.GetInstance<IVersionService>().Version;
			var configSerializer = ServiceLocator.Current.GetInstance<IConfigSerializer>();
			var config = configSerializer.GetConfig();
			var whatsNewVersion = new Version(config.WhatsNewSeenForVersion);
			if(whatsNewVersion >= configVersion)
				return;
			_whatsNewTimer.Start();
		}

		void ShowWhatsNewCore() {
			_whatsNewTimer.Stop();
			
			var configVersion = ServiceLocator.Current.GetInstance<IVersionService>().Version;
			var configSerializer = ServiceLocator.Current.GetInstance<IConfigSerializer>();
			var config = configSerializer.GetConfig();
			var whatsNewVersion = new Version(config.WhatsNewSeenForVersion);
			var whatsNewContent = GetWhatsNewContent(configVersion, whatsNewVersion);
			if(!String.IsNullOrEmpty(whatsNewContent)) {
				ViewModelBase.SetupNotification(_notification, "What's New", GetWhatsNewContent(configVersion, whatsNewVersion), MessageBoxImage.Information);
				NotificationRequest.Raise(_notification);
				config.WhatsNewSeenForVersion = configVersion.ToString();
			}
			configSerializer.SaveConfig(config);
		}

		string GetWhatsNewContent(Version configVersion, Version whatsNewVersion) {
			var allInfos = ServiceLocator.Current.GetInstance<IVersionService>().WhatsNewInfo;
			var sb = new StringBuilder();
			foreach(var info in allInfos.Where(x => x.version > whatsNewVersion)) {
				sb.AppendLine(info.content);
			}
			return sb.ToString();
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