using System;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps.Network
{
	/// <summary>
	/// Sample network tile server, which downloads tile images from OpenStreetMap server.
	/// <remarks>
	/// OpenStreetMap - http://www.openstreetmap.org/
	/// Used here by permission of OpenStreetMap.
	/// To use this sample server in your applications, you should read, agree and follow to
	/// OpenStreetMap license.
	/// Do not use this server too much - do not create high load on OpenStreetMap servers.
	/// OpenStreetMap tile isage policy - http://wiki.openstreetmap.org/wiki/Tile_usage_policy
	/// </remarks>
	/// </summary>
	public sealed class OpenStreetMapServer : NetworkTileServer
	{
		public OpenStreetMapServer()
		{
			UriFormat = "http://a.tile.openstreetmap.org/{0}/{1}/{2}.png";
			Name = "Open Street Maps";

			MinLevel = 0;
			MaxLevel = 17;
			MaxConcurrentDownloads = 1;
		}

		protected override string CreateRequestUri(TileIndex index)
		{
			int z = index.Level;
			int x = index.X;
			int y = MapTileProvider.GetSideTilesNum(z) - 1 - index.Y;

			return String.Format(UriFormat, z, x, y);
		}
	}
}
