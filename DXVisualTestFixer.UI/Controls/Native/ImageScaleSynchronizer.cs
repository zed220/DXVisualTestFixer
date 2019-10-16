using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Bars.Native;

namespace DXVisualTestFixer.UI.Controls.Native {
	internal class ImageScaleSynchronizer : ControlsRegistrator<ScrollViewer> {
		bool _ShowGridLines;

		public bool ShowGridLines {
			get => _ShowGridLines;
			set {
				_ShowGridLines = value;
				SetScale(Scale);
			}
		}

		public int Scale { get; set; } = 100;

		public void ZoomOut() {
			var dScale = Scale <= 100 ? 10 : 100;
			SetScale(Math.Max(30, Scale - dScale));
		}

		public void ZoomIn() {
			var dScale = Scale >= 100 ? 100 : 10;
			SetScale(Math.Min(1000, Scale + dScale));
		}

		public void Zoom100() {
			SetScale(100);
		}

		protected override void RegisterCore(ScrollViewer control) {
			SetScale(Scale);
		}

		protected override void UnregisterCore(ScrollViewer control) { }

		public void SetScale(int scale) {
			var notInitializedControls = SetScaleCore(scale);
			if(notInitializedControls != null)
				Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => SetScaleCore(scale)));
		}

		List<ScrollViewer> SetScaleCore(int scale) {
			var notInitializedControls = new List<ScrollViewer>();
			foreach(var trackingControl in GetActualControls()) {
				var scaleImageControl = TreeHelper.GetChild<ScaleImageControl>(trackingControl);
				if(scaleImageControl == null) {
					notInitializedControls.Add(trackingControl);
					continue;
				}

				var scaleTransform = scaleImageControl.LayoutTransform as ScaleTransform;
				if(scaleTransform == null)
					scaleImageControl.LayoutTransform = scaleTransform = new ScaleTransform();
				if(scale > 100) {
					scaleTransform.ScaleX = 1;
					scaleTransform.ScaleY = 1;
					scaleImageControl.Scale = scale / 100;
					scaleImageControl.ShowGridLines = ShowGridLines;
				}
				else {
					scaleImageControl.Scale = 1;
					scaleTransform.ScaleX = scale / 100d;
					scaleTransform.ScaleY = scale / 100d;
				}
			}

			Scale = scale;
			return notInitializedControls;
		}
	}
}