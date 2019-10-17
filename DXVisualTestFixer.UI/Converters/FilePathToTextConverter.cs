using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using DXVisualTestFixer.UI.Models;

namespace DXVisualTestFixer.UI.Converters {
	public class FilePathToTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => !(value is RepositoryFileModel repositoryFileModel) ? null : !File.Exists(repositoryFileModel.Path) ? null : File.ReadAllText(repositoryFileModel.Path);

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}