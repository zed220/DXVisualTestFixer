using System;
using System.Collections.Generic;
using DevExpress.Logify.WPF;
using DXVisualTestFixer.Common;

namespace DXVisualTestFixer.Services {
    sealed class ExceptionService : IExceptionService {
        public void Send(Exception exception) => LogifyAlert.Instance.Send(exception);
        public void Send(Exception exception, IDictionary<string, string> additionalCustomData) => LogifyAlert.Instance.Send(exception, additionalCustomData);
    }
}