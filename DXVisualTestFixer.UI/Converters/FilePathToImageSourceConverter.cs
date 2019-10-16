using System;
using System.Globalization;
using System.Windows.Data;
using DXVisualTestFixer.UI.Models;

namespace DXVisualTestFixer.UI.Converters {
	public class FilePathToImageSourceConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var repositoryFileModel = value as RepositoryFileModel;
			if(repositoryFileModel == null)
				return null;
			return new Uri(repositoryFileModel.Path, UriKind.Absolute);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}