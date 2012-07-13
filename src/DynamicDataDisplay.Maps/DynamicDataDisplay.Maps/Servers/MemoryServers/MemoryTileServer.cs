using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public sealed class MemoryTileServer : TileServerBase, ITileSystem	{
		public MemoryTileServer(string name)
		{
			Name = name;
		}

		private readonly Dictionary<TileIndex, BitmapImage> cache = new Dictionary<TileIndex, BitmapImage>(new TileIndex.TileIndexEqualityComparer());

		public override bool Contains(TileIndex id)
		{
			return cache.ContainsKey(id);
		}

		public override void BeginLoadImage(TileIndex id)
		{
			if (Contains(id))
				ReportSuccess(cache[id], id);
			else
				ReportFailure(id);
		}

		public BitmapImage this[TileIndex id]
		{
			get
			{
				return cache[id];
			}
		}

		#region ITileStore Members

		public void BeginSaveImage(TileIndex id, BitmapImage image)
		{
			cache[id] = image;
		}

		#endregion
	}
}
