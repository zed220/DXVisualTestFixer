﻿using System;
using System.Globalization;
using Prism.Interactivity.InteractionRequest;

namespace DXVisualTestFixer.UI.Converters {
	public class NotificationContentConverter : BaseValueConverter {
		public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return value as INotification;
		}
	}
}