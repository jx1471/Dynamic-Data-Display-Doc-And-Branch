using System;
using System.Windows.Media.Imaging;
using Microsoft.Research.DynamicDataDisplay.Common.Auxiliary;
using Microsoft.Research.DynamicDataDisplay.Maps.Servers;
using Microsoft.Research.DynamicDataDisplay.Maps.Servers.Network;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public sealed class TileServerSystem : ITileSystem
	{
		public TileServerSystem() { NetworkServer = new EmptyTileServer(); }
		public TileServerSystem(NetworkTileServerBase networkServer)
		{
			NetworkServer = networkServer;
		}

		public string Name
		{
			get { return networkServer != null ? networkServer.Name : "not set"; }
		}

		private void memoryServer_ImageLoaded(object sender, TileLoadResultEventArgs e)
		{
			pendingImages.Remove(e.ID);
		}

		private void fileServer_ImageLoaded(object sender, TileLoadResultEventArgs e)
		{
			pendingImages.Remove(e.ID);

			if (e.Result == TileLoadResult.Success)
			{
				memoryServer.BeginSaveImage(e.ID, e.Image);
			}

			ImageLoaded.Raise(this, e);
		}

		private void networkServer_ImageLoaded(object sender, TileLoadResultEventArgs e)
		{
			pendingImages.Remove(e.ID);

			bool saveToFileCache = !networkServer.CanLoadFast(e.ID) && saveToCache;
			if (saveToFileCache && e.Result == TileLoadResult.Success)
			{
				BeginSaveImage(e.ID, e.Image);
			}
			if (e.Result == TileLoadResult.Success)
			{
				networkFailures = 0;
				memoryServer.BeginSaveImage(e.ID, e.Image);
			}
			else
			{
				networkFailures++;
				if (autoSwitchToOffline && (networkFailures > maxConsequentNetworkFailuresToSwitchToOffline))
				{
					Mode = TileSystemMode.CacheOnly;
				}
			}
			ImageLoaded.Raise(this, e);
		}

		private readonly Set<TileIndex> pendingImages = new Set<TileIndex>();

		private NetworkTileServerBase networkServer = new EmptyTileServer();
		public NetworkTileServerBase NetworkServer
		{
			get { return networkServer; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (networkServer != value)
				{
					if (networkServer != null)
					{
						networkServer.ImageLoaded -= networkServer_ImageLoaded;
					}

					networkServer = value;
					networkServer.ImageLoaded += networkServer_ImageLoaded;
					CreateServers();

					networkFailures = 0;

					NetworkServerChanged.Raise(this);
				}
			}
		}

		public event EventHandler NetworkServerChanged;

		private void CreateServers()
		{
			DetachPreviousServers();
			pendingImages.Clear();

			if (networkServer != null)
			{
				var fileServer = new AsyncFileSystemServer(Name);
				fileServer.FileExtension = networkServer.FileExtension;

				this.fileServer = fileServer;
				AttachFileServer();

				memoryServer = new LRUMemoryCache(Name);
				memoryServer.ImageLoaded += memoryServer_ImageLoaded;
			}
		}

		private void AttachFileServer()
		{
			if (fileServer != null)
			{
				fileServer.ImageLoaded += fileServer_ImageLoaded;
			}
		}

		private void DetachPreviousServers()
		{
			DetachFileServer();

			if (memoryServer != null)
			{
				memoryServer.ImageLoaded -= memoryServer_ImageLoaded;

				if (memoryServer is IDisposable)
				{
					((IDisposable)memoryServer).Dispose();
				}
			}
		}

		private void DetachFileServer()
		{
			if (fileServer != null)
			{
				fileServer.ImageLoaded -= fileServer_ImageLoaded;

				if (fileServer is IDisposable)
				{
					((IDisposable)fileServer).Dispose();
				}
			}
		}

		private IWriteableTileServer fileServer = null;
		public IWriteableTileServer FileServer
		{
			get { return fileServer; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (fileServer != value)
				{
					DetachFileServer();
					fileServer = value;
					AttachFileServer();
				}
			}
		}

		private ITileSystem memoryServer = null;
		public ITileSystem MemoryServer
		{
			get { return memoryServer; }
		}

		private bool autoSwitchToOffline = false;
		public bool AutoSwitchToOffline
		{
			get { return autoSwitchToOffline; }
			set { autoSwitchToOffline = value; }
		}

		private int networkFailures = 0;

		private int maxConsequentNetworkFailuresToSwitchToOffline = 30;
		public int MaxConsequentNetworkFailuresToSwitchToOffline
		{
			get { return maxConsequentNetworkFailuresToSwitchToOffline; }
			set { maxConsequentNetworkFailuresToSwitchToOffline = value; }
		}

		private TileSystemMode mode = TileSystemMode.OnlineAndCache;
		public TileSystemMode Mode
		{
			get { return mode; }
			set { mode = value; }
		}

		private bool saveToCache = true;
		public bool SaveToCache
		{
			get { return saveToCache; }
			set { saveToCache = value; }
		}

		#region ITileServer Members

		public bool IsReady(TileIndex id)
		{
			return memoryServer.Contains(id);
		}

		public BitmapImage this[TileIndex id]
		{
			get { return memoryServer[id]; }
		}

		public bool Contains(TileIndex id)
		{
			switch (mode)
			{
				case TileSystemMode.OnlineOnly:
					return true;
				case TileSystemMode.OnlineAndCache:
					return true;
				case TileSystemMode.CacheOnly:
					return memoryServer.Contains(id) ||
						fileServer.Contains(id) ||
						networkServer.CanLoadFast(id) && networkServer.Contains(id);
				default:
					throw new InvalidOperationException();
			}
		}

		private bool allowFastNetworkLoad = true;
		public bool AllowFastNetworkLoad
		{
			get { return allowFastNetworkLoad; }
			set { allowFastNetworkLoad = value; }
		}

		public void BeginLoadImage(TileIndex id)
		{
			if (pendingImages.Contains(id))
				return;

			bool beganLoading = false;
			if (memoryServer.Contains(id))
			{
				// do nothing
			}
			else
			{
				if (allowFastNetworkLoad && networkServer.CanLoadFast(id))
				{
					networkServer.BeginLoadImage(id);
					beganLoading = true;
				}
				else
				{
					switch (mode)
					{
						case TileSystemMode.OnlineOnly:
							networkServer.BeginLoadImage(id);
							beganLoading = true;
							break;
						case TileSystemMode.OnlineAndCache:
							if (fileServer.Contains(id))
							{
								fileServer.BeginLoadImage(id);
							}
							else
							{
								networkServer.BeginLoadImage(id);
							}
							beganLoading = true;
							break;
						case TileSystemMode.CacheOnly:
							if (fileServer.Contains(id))
							{
								fileServer.BeginLoadImage(id);
								beganLoading = true;
							}
							break;
						default:
							throw new InvalidOperationException();
					}
				}
				if (beganLoading)
				{
					if (!memoryServer.Contains(id))
					{
						pendingImages.Add(id);
					}
				}
			}
		}

		public event EventHandler<TileLoadResultEventArgs> ImageLoaded;

		#endregion

		#region ITileStore Members

		public void BeginSaveImage(TileIndex id, BitmapImage image)
		{
			fileServer.BeginSaveImage(id, image);
		}

		#endregion
	}

	public enum TileSystemMode
	{
		OnlineOnly,
		OnlineAndCache,
		CacheOnly
	}
}
