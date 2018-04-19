using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace DXVisualTestFixer.PrismCommon {
    public interface IDXCommands {
        IEnumerable<UICommand> Commands { get; }
    }

    public interface IDXNotification : IDXCommands {
        ImageSource ImageSource { get; }
    }

    public class DXNotification : Notification, IDXNotification {
        public DXNotification(MessageBoxImage imageType) {
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService());
            ImageSource = DXMessageBoxHelper.GetImage(imageType);
        }

        public IEnumerable<UICommand> Commands { get; }
        public ImageSource ImageSource { get; }
    }

    public class DXConfirmation : Confirmation, IDXNotification {
        public DXConfirmation(MessageBoxImage imageType) {
            Commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            Commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => Confirmed = true);
            ImageSource = DXMessageBoxHelper.GetImage(imageType);
        }

        public IEnumerable<UICommand> Commands { get; }
        public ImageSource ImageSource { get; }
    }

    public static class DXMessageBoxHelper {
        public static ImageSource GetImage(MessageBoxImage icon) {
            String uriPrefix = "pack://application:,,,/" + AssemblyInfo.SRAssemblyXpfCore + ";component/Core/Window/Icons/";
            String iconName = String.Empty;
            switch(icon) {
                case MessageBoxImage.Asterisk: iconName = "Information_48x48.svg"; break;
                case MessageBoxImage.Error: iconName = "Error_48x48.svg"; break;
                case MessageBoxImage.Exclamation: iconName = "Warning_48x48.svg"; break;
                case MessageBoxImage.None: return null;
                case MessageBoxImage.Question: iconName = "Question_48x48.svg"; break;
            }
            String uri = uriPrefix + iconName;
            SvgImageSourceExtension extension = new SvgImageSourceExtension();
            extension.Uri = new Uri(uri);
            return (ImageSource)extension.ProvideValue(null);
        }
    }

    public class NotificationContentConverter : MarkupExtension, IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value is Prism.Interactivity.InteractionRequest.INotification)
                return value;
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider) {
            return this;
        }
    }
}
