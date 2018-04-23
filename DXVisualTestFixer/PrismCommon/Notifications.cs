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
    public class DXNotification : Notification, IDXNotification {
        public DXNotification() {
            Commands = CreateCommands();
        }

        protected virtual IEnumerable<UICommand> CreateCommands() {
            return UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService());
        }

        public MessageBoxImage ImageType { get; set; }
        public IEnumerable<UICommand> Commands { get; }
        public ImageSource ImageSource { get { return DXMessageBoxHelper.GetImage(ImageType); } }
    }

    public class DXConfirmation : DXNotification, IDXConfirmation {
        public DXConfirmation() {
        }

        public bool Confirmed { get; set; }

        protected override IEnumerable<UICommand> CreateCommands() {
            var commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
            commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => Confirmed = true);
            return commands;
        }
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
