using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	[Serializable]
	[DebuggerDisplay("({X}, {Y}) @ {Level}")]
	public struct TileIndex : IEquatable<TileIndex>
	{
		public TileIndex(int x, int y, int level)
		{
			this.x = x;
			this.y = y;
			this.level = level;
		}

		private readonly int x;
		public int X { get { return x; } }

		private readonly int y;
		public int Y { get { return y; } }

		private readonly int level;
		public int Level { get { return level; } }

		public TileIndex GetLowerTile()
		{
			return new TileIndex(x / 2, y / 2, level - 1);
		}

		public TileIndex GetLowerTile(int levelUp)
		{
			TileIndex result = this;
			for (int i = 0; i < levelUp; i++)
			{
				result = result.GetLowerTile();
			}
			return result;
		}

		public bool HasLowerTile
		{
			get { return level > 0; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is TileIndex))
				return false;

			TileIndex other = (TileIndex)obj;

			return
				level == other.level &&
				x == other.x &&
				y == other.y;
		}

		public override int GetHashCode()
		{
			return x ^ y ^ level;
		}

		public override string ToString()
		{
			return String.Format("({0}, {1}) @ {2}", x, y, level);
		}

		#region IEquatable<TileIndex> Members

		public bool Equals(TileIndex other)
		{
			return
				level == other.level &&
				x == other.x &&
				y == other.y;
		}

		#endregion

		[Serializable]
		public class TileIndexEqualityComparer : IEqualityComparer<TileIndex>
		{
			#region IEqualityComparer<TileIndex> Members

			public bool Equals(TileIndex x, TileIndex y)
			{
				// todo what is the best order of comparings here?
				return
					x.level == y.level &&
					x.x == y.x &&
					x.y == y.y;
			}

			public int GetHashCode(TileIndex obj)
			{
				return obj.x ^ obj.y ^ obj.level;
			}

			#endregion
		}
	}
}
