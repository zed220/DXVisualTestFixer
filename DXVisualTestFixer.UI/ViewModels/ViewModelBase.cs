using System.Windows;
using DXVisualTestFixer.Common;
using Microsoft.Practices.ServiceLocation;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;

namespace DXVisualTestFixer.UI.ViewModels {
	public class ViewModelBase : BindableBase {
		protected bool CheckConfirmation(InteractionRequest<IConfirmation> service, string title, string content, MessageBoxImage image = MessageBoxImage.Warning) {
			var confirmation = ServiceLocator.Current.TryResolve<IDXConfirmation>();
			SetupNotification(confirmation, title, content, image);
			service.Raise(confirmation);
			return confirmation.Confirmed;
		}

		public static void SetupNotification(IDXNotification notification, string title, string content, MessageBoxImage image) {
			notification.Title = title;
			notification.Content = content;
			notification.ImageType = image;
		}
	}
}