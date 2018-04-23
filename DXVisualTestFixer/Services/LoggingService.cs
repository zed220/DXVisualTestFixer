using DXVisualTestFixer.Common;
using DXVisualTestFixer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string Message { get; private set; }
    }
}
