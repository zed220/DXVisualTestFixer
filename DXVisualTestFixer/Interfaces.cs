using DevExpress.Mvvm;
using Prism.Interactivity.InteractionRequest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace DXVisualTestFixer {
    public interface IDXNotification : Prism.Interactivity.InteractionRequest.INotification {
        MessageBoxImage ImageType { get; set; }
        IEnumerable<UICommand> Commands { get; }
    }
    public interface IDXConfirmation : IDXNotification, IConfirmation {
    }
}
