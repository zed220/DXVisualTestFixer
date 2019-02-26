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
    public class StackPanelToTooltipConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
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
            if(rowData.GroupLevel == 0 && rowData.View.DataContext is MainViewModel viewModel) {
                var version = rowData.GroupValues.FirstOrDefault()?.Value?.ToString();
                if(viewModel.TimingInfo.TryGetValue(version, out TimingInfo timingInfo))
                    result += " " + TimingsConverter.FormatTimings(timingInfo.Sources, timingInfo.Tests);
            }
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
