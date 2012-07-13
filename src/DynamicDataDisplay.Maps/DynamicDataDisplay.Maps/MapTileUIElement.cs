using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	internal sealed class MapTileUIElement : Control
	{
		public MapTileUIElement()
		{
			IsHitTestVisible = false;
			IsEnabled = false;
		}

		public Rect Bounds { get; set; }
		public BitmapImage Tile { get; set; }
		public bool DrawDebugBounds { get; set; }

		public override void EndInit()
		{
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			var dc = drawingContext;
			dc.DrawImage(Tile, Bounds);
			if (DrawDebugBounds)
			{
				dc.DrawRectangle(null, new Pen(Brushes.Red, 1), Bounds);
			}
		}
	}
}
