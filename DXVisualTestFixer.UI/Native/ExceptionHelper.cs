using System;
using System.Linq;
using DevExpress.Mvvm.Native;

namespace DXVisualTestFixer.UI.Native {
	public static class ExceptionHelper {
		public static Exception[] Unwrap(this Exception exception) {
			if(exception is AggregateException aggregateException)
				return aggregateException.InnerExceptions.ToArray();
			return exception.YieldToArray();
		}

	}
}