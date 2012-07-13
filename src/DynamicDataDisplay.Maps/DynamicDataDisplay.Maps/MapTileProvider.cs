using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Research.DynamicDataDisplay.Common;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public class MapTileProvider
	{
		public MapTileProvider()
		{
			rect = DataRect.FromPoints(minX, minY, maxX, maxY);
		}

		private double minX = -180;
		private double maxX = 180;
		private double minY = -87;
		private double maxY = 87;
		private DataRect rect;

		public double MaxLatitude
		{
			get { return maxY; }
			set
			{
				maxY = value;
				minY = -value;

				rect = DataRect.FromPoints(minX, minY, maxX, maxY);
			}
		}

		public double MinLatitude
		{
			get { return minY; }
		}

		public double XSize { get { return maxX - minX; } }
		public double YSize { get { return maxY - minY; } }

		public Rect GetBounds(TileIndex tile)
		{
			double width = GetTileWidth(tile.Level);
			double height = GetTileHeight(tile.Level);
			double x = minX + tile.X * width;
			double y = minY + tile.Y * height;

			Rect bounds = new Rect(x, y, width, height);
			return bounds;
		}

		private int minLevel = 1;
		public int MinLevel
		{
			get { return minLevel; }
			set { minLevel = value; }
		}

		private int maxLevel = 17;
		public int MaxLevel
		{
			get { return maxLevel; }
			set { maxLevel = value; }
		}

		private int level = 1;
		public int Level
		{
			get { return level; }
		}

		public bool DecreaseLevel()
		{
			if (level > minLevel)
			{
				level--;
				return true;
			}
			return false;
		}

		public bool IncreaseLevel()
		{
			if (level < maxLevel)
			{
				level++;
				return true;
			}
			return false;
		}

		public static IEnumerable<TileIndex> GetTilesForLevel(int level)
		{
			int size = GetSideTilesNum(level);
			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					yield return new TileIndex(x, y, level);
				}
			}
		}

		public static long GetAllTilesNum(int level)
		{
			long size = GetSideTilesNum(level);
			return size * size;
		}

		public static int GetSideTilesNum(int level)
		{
			return 1 << level;
		}

		public double TileWidth { get { return GetTileWidth(level); } }
		public double TileHeight { get { return GetTileHeight(level); } }

		public double GetTileWidth(int level)
		{
			return XSize / (1 << level);
		}

		public double GetTileHeight(int level)
		{
			return YSize / (1 << level);
		}

		public IEnumerable<TileIndex> GetTiles(DataRect region)
		{
			region = region.Intersect(rect);
			if (region.IsEmpty)
				return Enumerable.Empty<TileIndex>();

			checked
			{
				double tileWidth = TileWidth;
				double tileHeight = TileHeight;

				int minTileX = (int)Math.Floor((region.XMin - minX) / tileWidth);
				int minTileY = (int)Math.Floor((region.YMin - minY) / tileHeight);

				double realX = minX + minTileX * tileWidth;
				double realY = minY + minTileY * tileHeight;

				int xNum = (int)Math.Ceiling((region.XMax - realX) / tileWidth);
				int yNum = (int)Math.Ceiling((region.YMax - realY) / tileHeight);

				int maxTileX = minTileX + xNum;
				int maxTileY = minTileY + yNum;

				List<TileIndex> res = new List<TileIndex>(xNum * yNum);
				for (int x = minTileX; x < maxTileX; x++)
				{
					for (int y = minTileY; y < maxTileY; y++)
					{
						res.Add(new TileIndex(x, y, level));
					}
				}

				return res;
			}
		}
	}
}
