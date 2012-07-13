using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public sealed class WeakRefMemoryTileServer : TileServerBase, ITileSystem
	{

		public WeakRefMemoryTileServer(string name)
		{
			Name = name;
		}

		protected override string GetCustomName()
		{
			return "Memory " + Name;
		}

		// todo change that this cache is beeing cleaned by GC when simply panning map.

		private readonly Dictionary<TileIndex, WeakReference> cache = new Dictionary<TileIndex, WeakReference>(new TileIndex.TileIndexEqualityComparer());

		public override bool Contains(TileIndex id)
		{
			if (cache.ContainsKey(id))
			{
				bool isAlive = cache[id].IsAlive;
				if (isAlive)
				{
					return true;
				}
				else
				{
					//removing dead reference
					cache.Remove(id);
					return false;
				}
			}

			return false;
		}

		public override void BeginLoadImage(TileIndex id)
		{
			if (Contains(id))
			{
				BitmapImage img = (BitmapImage)cache[id].Target;
				ReportSuccess(img, id);
			}
			else
			{
				ReportFailure(id);
			}
		}

		public BitmapImage this[TileIndex id]
		{
			get
			{
				return (BitmapImage)cache[id].Target;
			}
		}

		#region ITileStore Members

		public void BeginSaveImage(TileIndex id, BitmapImage image)
		{
			cache[id] = new WeakReference(image);
			Statistics.IntValues["ImagesSaved"]++;
		}

		#endregion
	}
}
