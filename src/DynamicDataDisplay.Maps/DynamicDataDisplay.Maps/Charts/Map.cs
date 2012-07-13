using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Microsoft.Research.DynamicDataDisplay.Maps.Servers.Network;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;
using Microsoft.Research.DynamicDataDisplay.Maps;
using System.Collections.Generic;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	[ContentProperty("NetworkTileServer")]
	public class Map : Canvas, IPlotterElement
	{
		public Map()
		{
			server.ImageLoaded += OnTileLoaded;
			server.NetworkServerChanged += OnNetworkServerChanged;

#if DEBUG
			drawDebugBounds = false;
#endif
		}

		private readonly MapTileProvider tileProvider = new MapTileProvider();
		protected MapTileProvider TileProvider
		{
			get { return tileProvider; }
		}

		private readonly TileServerSystem server = new TileServerSystem();
		public TileServerSystem TileSystem
		{
			get { return server; }
		}

		[NotNull]
		public NetworkTileServerBase NetworkTileServer
		{
			get { return server.NetworkServer; }
			set { server.NetworkServer = value; }
		}

		public IWriteableTileServer FileTileServer
		{
			get { return server.FileServer; }
			set { server.FileServer = value; }
		}

		protected virtual void OnTileLoaded(object sender, TileLoadResultEventArgs e)
		{
			if (e.Result == TileLoadResult.Success)
			{
				Rect tileBounds = tileProvider.GetBounds(e.ID);

				bool intersectsWithVisible = visibleBounds.IntersectsWith(tileBounds);
				if (intersectsWithVisible && !invalidatePending && e.ID.Level <= tileProvider.Level)
				{
					BeginInvalidateVisual();
				}
			}
		}

		private int tileWidth = 256;
		private int tileHeight = 256;
		protected virtual void OnNetworkServerChanged(object sender, EventArgs e)
		{
			NetworkTileServerBase networkServer = server.NetworkServer;
			if (networkServer != null)
			{
				tileProvider.MinLevel = networkServer.MinLevel;
				tileProvider.MaxLevel = networkServer.MaxLevel;
				tileProvider.MaxLatitude = networkServer.MaxLatitude;
				tileWidth = networkServer.TileWidth;
				tileHeight = networkServer.TileHeight;
			}
			BeginInvalidateVisual();
		}

		private bool drawDebugBounds = false;
		public bool DrawDebugBounds
		{
			get { return drawDebugBounds; }
			set { drawDebugBounds = value; }
		}

		private readonly Pen debugBoundsPen = new Pen(Brushes.Red.MakeTransparent(0.5), 1);

		Rect visibleBounds;
		bool invalidatePending = false;
		bool rendering = false;
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			rendering = true;
			invalidatePending = false;

			base.OnRender(drawingContext);

			var transform = plotter.Viewport.Transform;
			Rect output = plotter.Viewport.Output;
			Rect visible = plotter.Viewport.Visible;
			visibleBounds = visible;

			var tileInfos = GetVisibleTiles();

			var dc = drawingContext;
			var lowerTilesList = GetLoadedLowerTiles(tileInfos);
			// displaying lower tiles
			foreach (var tile in lowerTilesList)
			{
				if (server.IsReady(tile))
				{
					BitmapImage bmp = server[tile];
					Rect visibleRect = tileProvider.GetBounds(tile);
					Rect screenRect = visibleRect.ViewportToScreen(transform);
					Rect enlargedRect = EnlargeRect(screenRect);
					dc.DrawImage(bmp, enlargedRect);
				}
				else
				{
					server.BeginLoadImage(tile);
				}
			}

			foreach (var tileInfo in tileInfos)
			{
				if (server.IsReady(tileInfo.Tile))
				{
					BitmapImage bmp = server[tileInfo.Tile];

					Rect enlargedRect = EnlargeRect(tileInfo.ScreenBounds);
					drawingContext.DrawImage(bmp, enlargedRect);

					if (drawDebugBounds)
					{
						drawingContext.DrawRectangle(null, debugBoundsPen, tileInfo.ScreenBounds);
						var text = CreateText(tileInfo.Tile);
						if (tileInfo.ScreenBounds.Width > text.Width && tileInfo.ScreenBounds.Height > text.Height)
						{
							Point position = tileInfo.ScreenBounds.Location;
							position.Offset(3, 3);
							drawingContext.DrawText(CreateText(tileInfo.Tile), position);
						}
					}
				}
				else
				{
					server.BeginLoadImage(tileInfo.Tile);
				}
			}

			rendering = false;
		}

		protected virtual Rect Transform(Rect visibleRect)
		{
			return visibleRect;
		}

		protected virtual Rect TransformRegion(Rect visibleRect)
		{
			return visibleRect;
		}

		protected List<VisibleTileInfo> GetVisibleTiles()
		{
			var transform = plotter.Viewport.Transform;
			Rect output = plotter.Viewport.Output;
			Rect visible = plotter.Viewport.Visible;

			var tileInfos = (from tile in tileProvider.GetTiles(TransformRegion(visible))
							 let visibleRect = Transform(tileProvider.GetBounds(tile))
							 let screenRect = visibleRect.ViewportToScreen(transform)
							 where output.IntersectsWith(screenRect)
							 select new VisibleTileInfo { Tile = tile, ScreenBounds = screenRect, VisibleBounds = visibleRect }).ToList();

			return tileInfos;
		}

		protected IEnumerable<TileIndex> GetLoadedLowerTiles(IEnumerable<VisibleTileInfo> visibleTiles)
		{
			Set<TileIndex> result = new Set<TileIndex>();

			foreach (var tileInfo in visibleTiles)
			{
				if (!server.IsReady(tileInfo.Tile))
				{
					bool found = false;
					var tile = tileInfo.Tile;
					do
					{
						if (tile.HasLowerTile)
						{
							tile = tile.GetLowerTile();
							if (server.IsReady(tile) || tile.Level == 1)
							{
								found = true;
								result.TryAdd(tile);
							}
						}
						else
						{
							found = true;
						}
					}
					while (!found);
				}
			}

			return result.OrderBy(id => id.Level);
		}

		private const double rectZoomCoeff = 1.0025; // this is PI/E^i // was 1.0025
		protected Rect EnlargeRect(Rect rect)
		{
			return EnlargeRect(rect, rectZoomCoeff);
		}

		protected Rect EnlargeRect(Rect rect, double rectZoomCoeff)
		{
			Rect res = rect;
			res = res.Zoom(res.GetCenter(), rectZoomCoeff);
			return res;
		}

		private FormattedText CreateText(TileIndex tileIndex)
		{
			FormattedText text = new FormattedText(tileIndex.ToString(),
				CultureInfo.CurrentCulture,
				FlowDirection.LeftToRight, new Typeface("Arial"), 8, Brushes.Red);
			return text;
		}

		#region ChangesTextFormat property

		public bool ChangesTextFormat
		{
			get { return (bool)GetValue(ChangesTextFormatProperty); }
			set { SetValue(ChangesTextFormatProperty, value); }
		}

		public static readonly DependencyProperty ChangesTextFormatProperty = DependencyProperty.Register(
		  "ChangesTextFormat",
		  typeof(bool),
		  typeof(Map),
		  new FrameworkPropertyMetadata(true, OnChangesTextFormatChanged));

		private static void OnChangesTextFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Map map = (Map)d;
			map.OnChangesTextFormatChanged();
		}

		private void OnChangesTextFormatChanged()
		{
			// do nothing if disconnected
			if (plotter == null)
				return;

			if (ChangesTextFormat)
			{
				plotter.Children.CollectionChanged += PlotterChildren_CollectionChanged;
				ChangeTextFormat();
			}
			else
			{
				plotter.Children.CollectionChanged -= PlotterChildren_CollectionChanged;
				RevertTextFormat();
			}
		}

		#endregion

		protected virtual void BeginInvalidateVisual()
		{
			if (!rendering)
			{
				invalidatePending = true;
				InvalidateVisual();
			}
			else
			{
				Dispatcher.BeginInvoke(((Action)(() => { InvalidateVisual(); })));
			}
		}

		#region IPlotterElement Members

		Func<double, string> prevXMapping;
		Func<double, string> prevYMapping;
		PhysicalProportionsRestriction proportionsRestriction = new PhysicalProportionsRestriction(2);
		MaxSizeRestriction maxSizeRestriction = new MaxSizeRestriction();
		void IPlotterElement.OnPlotterAttached(Plotter plotter)
		{
			this.plotter = (Plotter2D)plotter;
			this.plotter.Viewport.PropertyChanged += Viewport_PropertyChanged;
			this.plotter.Viewport.Restrictions.Add(proportionsRestriction);
			this.plotter.Viewport.Restrictions.Add(maxSizeRestriction);

			this.plotter.KeyDown += new KeyEventHandler(plotter_KeyDown);

			if (ChangesTextFormat)
			{
				plotter.Children.CollectionChanged += PlotterChildren_CollectionChanged;
				// changing text mappings of CursorCoordinateGraph, if it exists,
				// to display text labels with degrees.
				ChangeTextFormat();
			}

			plotter.CentralGrid.Children.Add(this);
		}

		void plotter_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Q)
			{
				tileProvider.IncreaseLevel();
				BeginInvalidateVisual();
			}
			else if (e.Key == Key.W)
			{
				tileProvider.DecreaseLevel();
				BeginInvalidateVisual();
			}
		}

		void PlotterChildren_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (ChangesTextFormat)
			{
				ChangeTextFormat();
			}
		}

		bool changedTextFormat = false;
		private void ChangeTextFormat()
		{
			if (changedTextFormat)
				return;


			// todo discover why sometimes we arrive here from PlotterChildren_CollectionChanged when we have removed this handler from
			// plotter.Children.CollectionChanged invocation list.
			if (plotter == null)
				return;

			var cursorGraph = plotter.Children.OfType<CursorCoordinateGraph>().FirstOrDefault<CursorCoordinateGraph>();
			if (cursorGraph != null)
			{
				changedTextFormat = true;

				// saving previous text mappings
				prevXMapping = cursorGraph.XTextMapping;
				prevYMapping = cursorGraph.YTextMapping;


				// setting new text mappings
				cursorGraph.XTextMapping = value =>
				{
					if (Double.IsNaN(value))
						return "";

					if (-180 <= value && value <= 180)
					{
						Degree degree = Degree.CreateLongitude(value);
						return degree.ToString();
					}
					else return null;
				};

				cursorGraph.YTextMapping = value =>
				{
					if (Double.IsNaN(value))
						return "";

					if (tileProvider.MinLatitude <= value && value <= tileProvider.MaxLatitude)
					{
						Degree degree = Degree.CreateLatitude(value);
						return degree.ToString();
					}
					else return null;
				};
			}
		}


		private void Viewport_PropertyChanged(object sender, ExtendedPropertyChangedEventArgs e)
		{
			var transform = plotter.Viewport.Transform;

			bool ok = false;
			do
			{
				double width = tileProvider.GetTileWidth(tileProvider.Level);
				double height = tileProvider.GetTileHeight(tileProvider.Level);

				Rect size = new Rect(new Size(width, height));
				Rect onScreen = size.ViewportToScreen(transform);

				// todo написать нормально
				if (onScreen.Width > tileWidth * 1.45)
				{
					if (tileProvider.IncreaseLevel())
					{
						continue;
					}
				}
				else if (onScreen.Width < tileWidth / 1.45)
				{
					if (tileProvider.DecreaseLevel())
					{
						continue;
					}
				}
				ok = true;
			} while (!ok);

			BeginInvalidateVisual();
		}

		void IPlotterElement.OnPlotterDetaching(Plotter plotter)
		{
			visibleBounds = new Rect();

			this.plotter.CentralGrid.Children.Remove(this);
			this.plotter.Viewport.PropertyChanged -= Viewport_PropertyChanged;

			this.plotter.Viewport.Restrictions.Remove(proportionsRestriction);
			this.plotter.Viewport.Restrictions.Remove(maxSizeRestriction);

			this.plotter.Children.CollectionChanged -= PlotterChildren_CollectionChanged;

			RevertTextFormat();

			this.plotter = null;
		}

		private void RevertTextFormat()
		{
			if (changedTextFormat)
			{
				// revert test mappings of CursorCoordinateGraph, if it exists.
				var cursorGraph = plotter.Children.OfType<CursorCoordinateGraph>().FirstOrDefault<CursorCoordinateGraph>();
				if (cursorGraph != null)
				{
					cursorGraph.XTextMapping = prevXMapping;
					cursorGraph.YTextMapping = prevYMapping;
				}
				changedTextFormat = false;
			}
		}

		private Plotter2D plotter;
		public Plotter2D Plotter
		{
			get { return plotter; }
		}

		Plotter IPlotterElement.Plotter
		{
			get { return plotter; }
		}

		#endregion
	}
}
