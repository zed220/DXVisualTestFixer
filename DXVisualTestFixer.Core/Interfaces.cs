using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.Core {
    public interface ILoadingProgressController {
        void Enlarge(int delta);
        void Flush();
        void IncreaseProgress(int delta);
        void Start();
        void Stop();

        bool IsEnabled { get; }
        int Maximum { get; }
        int Value { get; }
    }
    public interface ILoggingService {
        void SendMessage(string text);

        event EventHandler<IMessageEventArgs> MessageReserved;
    }
    public interface IMessageEventArgs {
        string Message { get; }
    }
    public interface IVersionService {
        Version Version { get; }
    }
}
