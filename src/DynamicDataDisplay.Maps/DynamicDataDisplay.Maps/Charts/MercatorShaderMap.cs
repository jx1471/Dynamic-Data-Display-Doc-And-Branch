using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Research.DynamicDataDisplay.Charts.Maps;
using Microsoft.Research.DynamicDataDisplay.Common;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using System.Windows.Media.Effects;
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Microsoft.Research.DynamicDataDisplay.Maps;
using Microsoft.Research.DynamicDataDisplay.Common.Auxiliary;
using System.ComponentModel;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public class MercatorShaderMap : Map
	{
		public MercatorShaderMap()
		{
			IsHitTestVisible = false;
		}

		private MercatorTransform mercatorTransform = new MercatorTransform();

		private Panel ContentPanel
		{
			get { return this; }
		}

		protected override void OnTileLoaded(object sender, TileLoadResultEventArgs e)
		{
			if (e.Result == TileLoadResult.Success)
			{
				BeginInvalidateVisual();
			}
		}

		protected override void OnNetworkServerChanged(object sender, EventArgs e)
		{
			base.OnNetworkServerChanged(sender, e);

			mercatorTransform = new MercatorTransform(TileProvider.MaxLatitude);
		}

		protected override Rect Transform(Rect viewportRect)
		{
			return viewportRect.ViewportToData(mercatorTransform);
		}

		protected override Rect TransformRegion(Rect dataRect)
		{
			return dataRect.DataToViewport(mercatorTransform);
		}

		private int maxLevelShift = 2;
		private bool rendering = false;
		private void OnRender()
		{
			if (rendering && !renderingPending)
			{
				Dispatcher.BeginInvoke(OnRender);
				return;
			}
			else if (rendering)
				return;

			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			rendering = true;
			renderingPending = false;

			ContentPanel.Children.Clear();

			var transform = Plotter.Viewport.Transform;
			Rect output = Plotter.Viewport.Output;

			Rect visible = Plotter.Viewport.Visible;
			var tileInfos = GetVisibleTiles();

			int minLevel = TileProvider.Level - maxLevelShift;
			var lowerTiles = GetLoadedLowerTiles(tileInfos).Where(id => id.Level >= minLevel);
			foreach (var tile in lowerTiles)
			{
				if (TileSystem.IsReady(tile))
				{
					BitmapImage bmp = TileSystem[tile];
					Rect visibleRect = Transform(TileProvider.GetBounds(tile));
					Rect screenRect = visibleRect.DataToScreen(transform);

					DrawTile(bmp, screenRect, visibleRect);
				}
				else
				{
					TileSystem.BeginLoadImage(tile);
				}
			}

			foreach (var tileInfo in tileInfos)
			{
				if (TileSystem.IsReady(tileInfo.Tile))
				{
					BitmapImage bmp = TileSystem[tileInfo.Tile];
					DrawTile(bmp, tileInfo.ScreenBounds, tileInfo.VisibleBounds);
				}
				else
				{
					TileSystem.BeginLoadImage(tileInfo.Tile);
				}
			}

			rendering = false;
		}

		private void DrawTile(BitmapImage bmp, Rect screenBounds, Rect visibleBounds)
		{
			MapTileUIElement element = new MapTileUIElement
			{
				Bounds = EnlargeRect(screenBounds, 1.01),
				Tile = bmp,
				Effect = CreateEffect(visibleBounds),
				DrawDebugBounds = this.DrawDebugBounds
			};
			element.EndInit();

			ContentPanel.Children.Add(element);
		}

		private Effect CreateEffect(Rect bounds)
		{
			MercatorShader effect = new MercatorShader();

			effect.YMax = Math.Max(bounds.Top, bounds.Bottom);
			effect.YDiff = bounds.Height;

			double latMax = mercatorTransform.DataToViewport(new Point(0, Math.Max(bounds.Top, bounds.Bottom))).Y;
			double latMin = mercatorTransform.DataToViewport(new Point(0, Math.Min(bounds.Top, bounds.Bottom))).Y;

			effect.YLatMax = latMax;
			effect.YLatDiff = Math.Abs(latMax - latMin);
			effect.Scale = mercatorTransform.Scale;

			return effect;
		}

		bool renderingPending = false;
		protected override void BeginInvalidateVisual()
		{
			OnRender();
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
		}
	}
}
