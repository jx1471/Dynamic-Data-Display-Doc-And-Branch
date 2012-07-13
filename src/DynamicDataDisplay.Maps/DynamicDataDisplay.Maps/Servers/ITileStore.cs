using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public interface ITileStore
	{
		void BeginSaveImage(TileIndex id, BitmapImage image);
	}
}
