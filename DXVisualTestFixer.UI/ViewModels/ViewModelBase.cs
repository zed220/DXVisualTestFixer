using Microsoft.Practices.ServiceLocation;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System.Windows;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.UI.ViewModels {
    public class ViewModelBase : BindableBase {
        public InteractionRequest<INotification> NotificationRequest { get; } = new InteractionRequest<INotification>();

        protected bool CheckConfirmation(InteractionRequest<IConfirmation> service, string title, string content, MessageBoxImage image = MessageBoxImage.Warning) {
            IDXConfirmation confirmation = ServiceLocator.Current.TryResolve<IDXConfirmation>();
            SetupNotification(confirmation, title, content, image);
            service.Raise(confirmation);
            return confirmation.Confirmed;
        }
        protected void DoNotification(string title, string content, MessageBoxImage image = MessageBoxImage.Information) {
            IDXNotification confirmation = ServiceLocator.Current.TryResolve<IDXNotification>();
            SetupNotification(confirmation, title, content, image);
            NotificationRequest.Raise(confirmation);
        }
        void SetupNotification(IDXNotification notification, string title, string content, MessageBoxImage image) {
            notification.Title = title;
            notification.Content = content;
            notification.ImageType = image;
        }
    }
}
