using System;
using System.Windows;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.Services {
	public class NotificationService : INotificationService {
		public void DoNotification(string title, string content, MessageBoxImage image = MessageBoxImage.Asterisk) {
			Notification?.Invoke(this, new NotificationServiceArgs(title, content, image));
		}

		public event EventHandler<INotificationServiceArgs> Notification;
	}

	public class NotificationServiceArgs : INotificationServiceArgs {
		public NotificationServiceArgs(string title, string content, MessageBoxImage image) {
			Title = title;
			Content = content;
			Image = image;
		}

		public string Title { get; }
		public string Content { get; }
		public MessageBoxImage Image { get; }
	}
}