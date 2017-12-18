using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer {
    public interface ILoggingService {
        void SendMessage(string text);

        event EventHandler<IMessageEventArgs> MessageReserved;
    }

    public class LoggingService : ILoggingService {
        public void SendMessage(string text) {
            MessageReserved?.Invoke(this, new MessageEventArgs(text));
        }

        public event EventHandler<IMessageEventArgs> MessageReserved;
    }

    public interface IMessageEventArgs {
        string Message { get; }
    }

    public class MessageEventArgs : EventArgs, IMessageEventArgs {
        public MessageEventArgs(string text) {
            Message = text;
        }

        public string Message { get; private set; }
    }
}
