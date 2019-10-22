using System;
using System.Globalization;
using System.Windows.Data;
using DXVisualTestFixer.UI.Models;

namespace DXVisualTestFixer.UI.Converters {
	public class FilePathToImageSourceConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return !(value is RepositoryFileModel repositoryFileModel) ? null : new Uri(repositoryFileModel.Path, UriKind.Absolute);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}