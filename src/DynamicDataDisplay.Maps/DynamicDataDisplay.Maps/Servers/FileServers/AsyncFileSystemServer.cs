using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using Microsoft.Research.DynamicDataDisplay.Common.Auxiliary;
using System.Windows.Threading;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public class AsyncFileSystemServer : WriteableFileSystemTileServer
	{
		private delegate BitmapImage ImageLoader(TileIndex id);

		public AsyncFileSystemServer(string name)
			: base(name)
		{
			corruptedFilesDeleteTimer.Tick += corruptedFilesDeleteTimer_Tick;
		}

		// todo is this neccessary?
		private int maxParallelRequests = Int32.MaxValue;
		public int MaxParallelRequests
		{
			get { return maxParallelRequests; }
			set { maxParallelRequests = value; }
		}

		private bool CanRunRequest
		{
			get { return runningRequests <= maxParallelRequests; }
		}

		private int runningRequests = 0;
		private readonly Stack<TileIndex> requests = new Stack<TileIndex>();
		protected Stack<TileIndex> Requests
		{
			get { return requests; }
		}

		public override void BeginLoadImage(TileIndex id)
		{
			string imagePath = GetImagePath(id);

			ImageLoader loader = BeginLoadImageAsync;

			if (CanRunRequest)
			{
				runningRequests++;
				Statistics.IntValues["ImagesLoaded"]++;
				loader.BeginInvoke(id, OnImageLoadedAsync, new AsyncInfo { ID = id, Loader = loader });
			}
			else
			{
				requests.Push(id);
			}
		}

		private readonly DispatcherTimer corruptedFilesDeleteTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(2000)
		};
		private readonly List<Action> fileDeleteActions = new List<Action>();

		private void corruptedFilesDeleteTimer_Tick(object sender, EventArgs e)
		{
			Debug.WriteLine("Deleting files: " + Environment.TickCount);

			// todo is this is necessary?
			// GC is called to collect partly loaded corrupted bitmap
			// otherwise it prevented from image file being deleted.
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();

			foreach (var action in fileDeleteActions)
			{
				action.BeginInvoke(null, null);
			}
			fileDeleteActions.Clear();
			corruptedFilesDeleteTimer.Stop();
		}

		protected BitmapImage BeginLoadImageAsync(TileIndex id)
		{
			//Debug.Assert(Contains(id));

			string imagePath = GetImagePath(id);

			BitmapImage bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.CacheOption = BitmapCacheOption.OnLoad;
			bmp.UriSource = new Uri(imagePath);
			try
			{
				bmp.EndInit();
			}
			catch (NotSupportedException exc)
			{
				Debug.WriteLine(String.Format("{0}: failed id = {1}. Exc = \"{2}\"", GetCustomName(), id, exc.Message));

				Action corruptedFileDeleteAction = () =>
				{
					try
					{
						File.Delete(imagePath);
					}
					catch (Exception e)
					{
						Debug.WriteLine("Exception while deleting corrupted image file \"" + imagePath + "\": " + e.Message);
					}
				};

				lock (fileDeleteActions)
				{
					fileDeleteActions.Add(corruptedFileDeleteAction);
					if (!corruptedFilesDeleteTimer.IsEnabled)
						corruptedFilesDeleteTimer.Start();
				}

				return null;
			}
			catch (FileNotFoundException exc)
			{
				Debug.WriteLine(String.Format("{0}: failed id = {1}. Exc = \"{2}\"", GetCustomName(), id, exc.Message));
				return null;
			}

			return (BitmapImage)bmp.GetAsFrozen();
		}

		private void OnImageLoadedAsync(IAsyncResult ar)
		{
			AsyncInfo info = (AsyncInfo)ar.AsyncState;
			ImageLoader loader = info.Loader;
			BitmapImage bmp = loader.EndInvoke(ar);

			Dispatcher.BeginInvoke((Action)(() =>
			{
				runningRequests--;
				if (bmp != null)
				{
					ReportSuccess(bmp, info.ID);
				}
				else
				{
					ReportFailure(info.ID);
				}
				if (CanRunRequest && requests.Count > 0)
				{
					BeginLoadImage(requests.Pop());
				}
			}));
		}

		private sealed class AsyncInfo
		{
			public TileIndex ID { get; set; }
			public ImageLoader Loader { get; set; }
		}
	}
}
