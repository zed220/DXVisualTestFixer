using DevExpress.Xpf.Grid;
using DXVisualTestFixer.Common;
using DXVisualTestFixer.UI.ViewModels;
using System;
using System.Globalization;
using System.Linq;

namespace DXVisualTestFixer.UI.Converters {
    public class TimingsConverter : BaseMultiValueConverter {
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if(values == null || values.Length != 3)
                return null;
            if(values[0] is GroupRowData groupRowData && groupRowData.View.DataContext is MainViewModel viewModel) {
                if(groupRowData.Level > 0)
                    return null;
                var version = groupRowData.GroupValues.FirstOrDefault()?.Value?.ToString();
                if(version == null)
                    return null;
                if(viewModel.TimingInfo.TryGetValue(version, out TimingInfo timingInfo))
                    return FormatTimings(timingInfo.Sources, timingInfo.Tests);
            }
            return null;
        }
        public static string FormatTimings(DateTime? source, DateTime? tests) {
            return $"[Sources:{source?.ToString("yyyy-MM-dd H:mm") ?? "Unknown"}][Tests:{tests?.ToString("yyyy-MM-dd H:mm") ?? "Unknown"}]";
        }
    }
}
