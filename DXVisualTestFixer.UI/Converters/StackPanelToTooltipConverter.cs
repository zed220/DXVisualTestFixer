using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace DXVisualTestFixer.UI.Converters {
    public class StackPanelToTooltipConverter : BaseMultiValueConverter {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values == null || values.Length == 0)
                return null;
            GroupRowData rowData = values[0] as GroupRowData;
            if(rowData == null)
                return null;
            string result = String.Empty;
            foreach(var data in rowData.GroupValues) {
                result += data.DisplayText + " ";
            }
            result.Remove(result.Length - 1, 1);
            return result;
        }
    }
}
