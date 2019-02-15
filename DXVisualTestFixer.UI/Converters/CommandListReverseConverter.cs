using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DXVisualTestFixer.UI.Converters {
    public class CommandListReverseConverter : BaseValueConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value is IEnumerable<UICommand> commands)
                return commands.Reverse();
            return value;
        }
    }
}