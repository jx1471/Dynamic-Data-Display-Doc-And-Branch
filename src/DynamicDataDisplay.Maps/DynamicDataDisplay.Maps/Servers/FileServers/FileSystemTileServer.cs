using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public class FileSystemTileServer : TileServerBase
	{
		protected sealed override string GetCustomName()
		{
			return "File " + Name;
		}

		private static string defaultCachePath;
		static FileSystemTileServer()
		{
			string dllPath = Assembly.GetAssembly(typeof(FileSystemTileServer)).Location;
			string appDir = Path.GetDirectoryName(dllPath);
			defaultCachePath = Path.Combine(appDir, "Cache");
		}

		private string fileExtension = ".png";
		public string FileExtension
		{
			get { return fileExtension; }
			set { fileExtension = value; }
		}

		private string cachePath;
		public FileSystemTileServer(string serverName)
		{
			cachePath = Path.Combine(defaultCachePath, serverName);
			Name = serverName;
		}

		protected string GetNameByIndex(TileIndex index)
		{
			return String.Concat(index.X.ToString(), "x", index.Y.ToString());
		}

		private readonly Dictionary<int, string> zoomDirs = new Dictionary<int, string>();
		protected string GetImagePath(TileIndex index)
		{
			string id = GetNameByIndex(index);

			string zoomDirPath = GetZoomDir(index.Level);
			string imagePath = Path.Combine(zoomDirPath, GetFileName(id));

			return imagePath;
		}

		protected string GetZoomDir(int level)
		{
			if (!zoomDirs.ContainsKey(level))
			{
				string zoomDirPath = Path.Combine(cachePath, GetDirPath(level));
				zoomDirs[level] = zoomDirPath;
			}

			return zoomDirs[level];
		}

		protected string GetDirPath(int level)
		{
			return "z" + (level);
		}

		protected string GetFileName(string id)
		{
			return id + fileExtension;
		}

		#region ITileServer Members

		private int currentLevel;
		/// <summary>
		/// Contains bool value whether there is image with following tile index.
		/// </summary>
		private Dictionary<TileIndex, bool> fileMap = new Dictionary<TileIndex, bool>(new TileIndex.TileIndexEqualityComparer());

		private int maxIndicesToPrecache = 1024;
		public int MaxIndicesToPrecache
		{
			get { return maxIndicesToPrecache; }
			set { maxIndicesToPrecache = value; }
		}

		public override bool Contains(TileIndex id)
		{
			// todo probably preload existing images into fileMap.
			// todo probably save previous fileMaps.
			if (id.Level != currentLevel)
			{
				fileMap = new Dictionary<TileIndex, bool>(new TileIndex.TileIndexEqualityComparer());
				currentLevel = id.Level;

				if (MapTileProvider.GetAllTilesNum(currentLevel) <= maxIndicesToPrecache)
				{
					Stopwatch timer = Stopwatch.StartNew();
					var directory = new DirectoryInfo(GetZoomDir(currentLevel));
					if (directory.Exists)
					{
						var files = directory.GetFiles();
						var fileNames = (from file in files
										 select file.Name).ToList();
						fileNames.Sort();

						var tileInfos = from tile in MapTileProvider.GetTilesForLevel(currentLevel)
										let name = GetFileName(GetNameByIndex(tile))
										orderby name
										select new { Tile = tile, Name = name };

						foreach (var tileInfo in tileInfos)
						{
							fileMap[tileInfo.Tile] = fileNames.Contains(tileInfo.Name);
						}
						Debug.WriteLine("Precached directory for level " + currentLevel + ": " + timer.ElapsedMilliseconds + " ms");
					}
				}
			}

			if (fileMap.ContainsKey(id))
			{
				return fileMap[id];
			}
			else
			{
				bool res = File.Exists(GetImagePath(id));
				fileMap[id] = res;
				return res;
			}
		}

		public override void BeginLoadImage(TileIndex id)
		{
			string imagePath = GetImagePath(id);

			FileStream stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read);
			BeginLoadBitmapImpl(stream, id);
		}

		protected override void OnBmpDownloadCompleted(object sender, EventArgs e)
		{
			base.OnBmpDownloadCompleted(sender, e);

			BitmapImage bmp = (BitmapImage)sender;
			bmp.StreamSource.Dispose();
		}

		protected override void OnBmpDownloadFailed(object sender, ExceptionEventArgs e)
		{
			base.OnBmpDownloadFailed(sender, e);

			BitmapImage bmp = (BitmapImage)sender;
			bmp.StreamSource.Dispose();
		}

		#endregion
	}
}

