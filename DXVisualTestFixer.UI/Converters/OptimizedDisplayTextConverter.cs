using DevExpress.Xpf.Core.FilteringUI;
using System;
using System.Globalization;

namespace DXVisualTestFixer.UI.Converters {
    public class OptimizedDisplayTextConverter : BaseValueConverter {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value is FilterValueInfo info && info.Value is bool b)
                return b ? "Optimized" : "Default";
            return value;
        }
    }
}