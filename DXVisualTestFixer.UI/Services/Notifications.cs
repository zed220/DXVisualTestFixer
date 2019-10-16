using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using DevExpress.Mvvm;
using DevExpress.Xpf.Core;
using DXVisualTestFixer.Common;
using Prism.Interactivity.InteractionRequest;

namespace DXVisualTestFixer.PrismCommon {
	public class DXNotification : Notification, IDXNotification {
		public DXNotification() {
			Commands = CreateCommands();
		}

		public IEnumerable<UICommand> Commands { get; }
		public ImageSource ImageSource => DXMessageBoxHelper.GetImage(ImageType);

		public MessageBoxImage ImageType { get; set; }

		protected virtual IEnumerable<UICommand> CreateCommands() {
			return UICommand.GenerateFromMessageButton(MessageButton.OK, new DialogService());
		}
	}

	public class DXConfirmation : DXNotification, IDXConfirmation {
		public bool Confirmed { get; set; }

		protected override IEnumerable<UICommand> CreateCommands() {
			var commands = UICommand.GenerateFromMessageButton(MessageButton.OKCancel, new DialogService(), MessageResult.OK, MessageResult.Cancel);
			commands.Where(c => c.IsDefault).Single().Command = new DelegateCommand(() => Confirmed = true);
			return commands;
		}
	}

	public static class DXMessageBoxHelper {
		public static ImageSource GetImage(MessageBoxImage icon) {
			var uriPrefix = "pack://application:,,,/" + AssemblyInfo.SRAssemblyXpfCore + ";component/Core/Window/Icons/";
			var iconName = string.Empty;
			switch(icon) {
				case MessageBoxImage.Asterisk:
					iconName = "Information_48x48.svg";
					break;
				case MessageBoxImage.Error:
					iconName = "Error_48x48.svg";
					break;
				case MessageBoxImage.Exclamation:
					iconName = "Warning_48x48.svg";
					break;
				case MessageBoxImage.None: return null;
				case MessageBoxImage.Question:
					iconName = "Question_48x48.svg";
					break;
			}

			var uri = uriPrefix + iconName;
			var extension = new SvgImageSourceExtension();
			extension.Uri = new Uri(uri);
			return (ImageSource) extension.ProvideValue(null);
		}
	}
}