using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DevExpress.Xpf.Bars.Native;

namespace DXVisualTestFixer.UI.Controls.Native {
	class ImageScaleSynchronizer : ControlsRegister<ScrollViewer> {
		bool _ShowGridLines;
		Point mouseRelPosition = new Point();

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
			control.MouseMove += ControlOnMouseMove;
		}

		void ControlOnMouseMove(object sender, MouseEventArgs e) {
			var control = GetActualControls().SingleOrDefault(x => x.IsMouseOver);
			if(control == null) {
				mouseRelPosition = new Point();
				return;
			}
			mouseRelPosition = e.GetPosition(control);
		}

		protected override void UnregisterCore(ScrollViewer control) {
			control.MouseMove -= ControlOnMouseMove;
		}

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
					scaleImageControl.UpdateScaleAndOffset(scale / 100, mouseRelPosition);
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