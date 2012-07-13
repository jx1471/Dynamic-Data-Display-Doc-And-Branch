using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public interface ITileServer
	{
		bool Contains(TileIndex id);
		void BeginLoadImage(TileIndex id);
		event EventHandler<TileLoadResultEventArgs> ImageLoaded;

		string Name { get; }
	}
}
