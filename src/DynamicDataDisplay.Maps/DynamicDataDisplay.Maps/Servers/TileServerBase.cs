using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using System.Windows;
using System.Diagnostics;
using System.Windows.Threading;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public abstract class TileServerBase : DependencyObject, ITileServer
	{
		protected TileServerBase()
		{
#if DEBUG
			Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;
#endif
		}

		private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
		{
			Debug.WriteLine(String.Format("Server \"{0}\" - Statistics:", GetCustomName()));
			Debug.Indent();
			string[] toString = statistics.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
			foreach (var item in toString)
			{
				Debug.WriteLine(item);
			}
			Debug.Unindent();
			Debug.WriteLine("");
		}

		private readonly TileServerStatistics statistics = new TileServerStatistics();
		public TileServerStatistics Statistics
		{
			get { return statistics; }
		}

		private string name = String.Empty;
		public string Name
		{
			get { return name; }
			protected set { name = value; }
		}

		protected virtual string GetCustomName()
		{
			return name;
		}

		protected void BeginLoadBitmapImpl(Stream stream, TileIndex id)
		{
			Dispatcher.BeginInvoke((Action)(() =>
			{
				BitmapImage bmp = new BitmapImage();
				SubscribeBitmapEvents(bmp);
				pendingBitmaps.Add(bmp, id);

				bmp.BeginInit();
				bmp.StreamSource = stream;
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();

				if (!bmp.IsDownloading)
				{
					UnsubscribeBitmapEvents(bmp);
					ReportSuccess(bmp, id);
					pendingBitmaps.Remove(bmp);
					stream.Dispose();
				}
			}));
		}

		protected void UpdateStatistics(Action action)
		{
			Dispatcher.BeginInvoke(action);
		}

		private readonly PendingBitmapSet pendingBitmaps = new PendingBitmapSet();

		private void SubscribeBitmapEvents(BitmapImage bmp)
		{
			bmp.DownloadCompleted += OnBmpDownloadCompleted;
			bmp.DownloadFailed += OnBmpDownloadFailed;
		}

		private void UnsubscribeBitmapEvents(BitmapImage bmp)
		{
			bmp.DownloadFailed -= OnBmpDownloadFailed;
			bmp.DownloadCompleted -= OnBmpDownloadCompleted;
		}

		protected virtual void OnBmpDownloadFailed(object sender, ExceptionEventArgs e)
		{
			BitmapImage bmp = (BitmapImage)sender;
			bmp.StreamSource.Dispose();

			UnsubscribeBitmapEvents(bmp);

			TileIndex id = pendingBitmaps[bmp];
			pendingBitmaps.Remove(bmp);

			ReportFailure(id);
		}

		protected virtual void OnBmpDownloadCompleted(object sender, EventArgs e)
		{
			BitmapImage bmp = (BitmapImage)sender;
			bmp.StreamSource.Dispose();

			UnsubscribeBitmapEvents(bmp);

			TileIndex id = pendingBitmaps[bmp];
			pendingBitmaps.Remove(bmp);

			ReportSuccess(bmp, id);
		}

		protected virtual void ReportSuccess(BitmapImage bmp, TileIndex id)
		{
			Debug.WriteLine(String.Format("{0}: loaded id = {1}", GetCustomName(), id));
			RaiseDataLoaded(new TileLoadResultEventArgs
			{
				Image = bmp,
				Result = TileLoadResult.Success,
				ID = id
			});
		}

		protected virtual void ReportFailure(TileIndex id)
		{
			Debug.WriteLine(String.Format("{0}: failed id = {1}", GetCustomName(), id));
			RaiseDataLoaded(new TileLoadResultEventArgs
			{
				Image = null,
				ID = id,
				Result = TileLoadResult.Failure
			});
		}

		#region ITileServer Members

		public abstract bool Contains(TileIndex id);

		public abstract void BeginLoadImage(TileIndex id);

		private void RaiseDataLoaded(TileLoadResultEventArgs args)
		{
			ImageLoaded.Raise(this, args);
		}
		public event EventHandler<TileLoadResultEventArgs> ImageLoaded;

		#endregion
	}
}
