using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.DynamicDataDisplay.Charts.Maps;
using System.Windows.Media.Imaging;

namespace Microsoft.Research.DynamicDataDisplay.Maps.Servers
{
	public class EmptyWriteableTileServer : EmptyTileServer, ITileStore, IWriteableTileServer
	{
		#region ITileStore Members

		protected override string GetCustomName()
		{
			return "Empty writeable";
		}

		public void BeginSaveImage(TileIndex id, BitmapImage image)
		{
			// do nothing
		}

		#endregion
	}
}
