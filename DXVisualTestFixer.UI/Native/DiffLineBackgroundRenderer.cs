using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;

namespace DXVisualTestFixer.UI.Native {
	public class DiffLineBackgroundRenderer : IBackgroundRenderer {
		static readonly Pen pen;

		static readonly SolidColorBrush removedBackground;
		static readonly SolidColorBrush addedBackground;
		static readonly SolidColorBrush headerBackground;

		readonly TextEditor host;

		static DiffLineBackgroundRenderer() {
			removedBackground = new SolidColorBrush(Color.FromRgb(0xff, 0xdd, 0xdd));
			removedBackground.Freeze();
			addedBackground = new SolidColorBrush(Color.FromRgb(0xdd, 0xff, 0xdd));
			addedBackground.Freeze();
			headerBackground = new SolidColorBrush(Color.FromRgb(0xf8, 0xf8, 0xff));
			headerBackground.Freeze();

			var blackBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
			blackBrush.Freeze();
			pen = new Pen(blackBrush, 0.0);
		}

		public DiffLineBackgroundRenderer(TextEditor host) {
			this.host = host;
		}

		public KnownLayer Layer => KnownLayer.Background;

		public void Draw(TextView textView, DrawingContext drawingContext) {
			foreach(var v in textView.VisualLines) {
				var rc = BackgroundGeometryBuilder.GetRectsFromVisualSegment(textView, v, 0, 1000).First();
				var offset = v.FirstDocumentLine.Offset;
				var length = v.FirstDocumentLine.Length;
				var text = host.Text.Substring(offset, length);

				Brush brush = null;
				if(text.StartsWith("+"))
					brush = addedBackground;
				else if(text.StartsWith("-"))
					brush = removedBackground;
				else if(text.StartsWith("@"))
					brush = headerBackground;

				if(brush == null)
					continue;

				drawingContext.DrawRectangle(brush, pen,
					new Rect(0, rc.Top, textView.ActualWidth, rc.Height));
			}
		}
	}
}