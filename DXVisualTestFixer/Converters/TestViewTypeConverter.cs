using DXVisualTestFixer.Mif;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DXVisualTestFixer.Converters {
    public class TestViewTypeConverter : IValueConverter {
        public TestViewType TrueValue { get; set; }
        public TestViewType FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (TestViewType)value == TrueValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? TrueValue : FalseValue;
        }
    }
}
