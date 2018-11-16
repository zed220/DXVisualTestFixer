using DevExpress.Xpf.Core.FilteringUI;
using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace DXVisualTestFixer.UI.Converters {
    public class SummaryToCheckedStateConverter : BaseMultiValueConverter {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values.Length != 3)
                return null;
            return values[2] as bool?;
        }
    }

    public class HighlightedColorPopupIsOpenConverter : BaseMultiValueConverter {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            return values.Length == 2 && (values[0] != null) && (values[1] is bool) && ((bool)values[1]);
        }
    }
}
