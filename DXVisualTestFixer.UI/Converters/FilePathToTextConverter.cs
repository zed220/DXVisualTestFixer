using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using DXVisualTestFixer.UI.Models;

namespace DXVisualTestFixer.UI.Converters {
	public class FilePathToTextConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var repositoryFileModel = value as RepositoryFileModel;
			if(repositoryFileModel == null)
				return null;
			if(!File.Exists(repositoryFileModel.Path))
				return null;
			return File.ReadAllText(repositoryFileModel.Path);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}