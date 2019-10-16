using System;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Services {
	public class LoggingService : ILoggingService {
		public void SendMessage(string text) {
			MessageReserved?.Invoke(this, new MessageEventArgs(text));
		}

		public event EventHandler<IMessageEventArgs> MessageReserved;
	}

	public class MessageEventArgs : EventArgs, IMessageEventArgs {
		public MessageEventArgs(string text) {
			Message = text;
		}

		public string Message { get; }
	}
}