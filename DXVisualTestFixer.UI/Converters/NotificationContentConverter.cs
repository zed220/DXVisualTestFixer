using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DXVisualTestFixer.UI.Converters {
    public class NotificationContentConverter : BaseValueConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value is Prism.Interactivity.InteractionRequest.INotification)
                return value;
            return null;
        }
    }
}
