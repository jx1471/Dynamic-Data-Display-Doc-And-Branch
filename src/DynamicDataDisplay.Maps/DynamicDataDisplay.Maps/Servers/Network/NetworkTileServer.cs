using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Research.DynamicDataDisplay.Maps.Servers.Network;
using Microsoft.Research.DynamicDataDisplay.Common;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public abstract class NetworkTileServer : NetworkTileServerBase
	{
		protected sealed override string GetCustomName()
		{
			return "Network " + Name;
		}

		#region ITileServer Members

		public override bool Contains(TileIndex id)
		{
			return MinLevel <= id.Level && id.Level <= MaxLevel;
		}

		private int runningDownloadsNum = 0;
		protected int RunningDownloadsNum
		{
			get { return runningDownloadsNum; }
		}

		private readonly List<TileIndex> requestQueue = new List<TileIndex>();

		private bool firstCall = true;
		public override void BeginLoadImage(TileIndex id)
		{
			VerifyTileIndex(id);

			string uri = CreateRequestUri(id);

			bool useMultipleServers = ServersNum != 0;
			if (useMultipleServers)
			{
				CurrentServer++;
				if (CurrentServer >= MinServer + ServersNum)
				{
					CurrentServer = MinServer;
				}
			}

			Debug.WriteLine(
				String.Format(
				"\"{0}\" - began to load url=\"{1}\"", GetCustomName(), uri));

			WebRequest request = WebRequest.Create(uri);
			AdjustRequest(request);

			runningDownloadsNum++;

			// this is hack to prevent freezing when request.BeginGetResponse was called
			// at the 1st time
			if (!firstCall)
			{
				request.BeginGetResponse(ResponseReadyCallback,
					new ResponseCallbackInfo { ID = id, Request = request });
			}
			else
			{
				Action action = () => request.BeginGetResponse(ResponseReadyCallback,
					new ResponseCallbackInfo { ID = id, Request = request });
				action.BeginInvoke(null, null);
			}
		}

		protected override void ReportFailure(TileIndex id)
		{
			runningDownloadsNum--;

			BeginLoadImageFromQueue();

			base.ReportFailure(id);
		}

		private void BeginLoadImageFromQueue()
		{
			if (requestQueue.Count > 0 && runningDownloadsNum < MaxConcurrentDownloads)
			{

				var id = requestQueue[requestQueue.Count - 1];
				requestQueue.RemoveAt(requestQueue.Count - 1);

				Debug.WriteLine("ID = " + id + " removed from queue");
				BeginLoadImage(id);
			}
		}

		protected override void ReportSuccess(BitmapImage bmp, TileIndex id)
		{
			runningDownloadsNum--;

			BeginLoadImageFromQueue();

			base.ReportSuccess(bmp, id);
		}

		protected virtual void AdjustRequest(WebRequest request)
		{
			HttpWebRequest r = (HttpWebRequest)request;
			r.UserAgent = UserAgent;
			if (!String.IsNullOrEmpty(Referer))
			{
				r.Referer = Referer;
			}
		}


		private void VerifyTileIndex(TileIndex id)
		{
			if (id.Level < MinLevel || id.Level > MaxLevel)
				throw new ArgumentException(
					String.Format(
						"Tile level {0} is not supported, it should be withing range [{1}—{2}].",
						id.Level, MinLevel, MaxLevel),
					"id");
		}

		private void ResponseReadyCallback(IAsyncResult ar)
		{
			ResponseCallbackInfo info = (ResponseCallbackInfo)ar.AsyncState;
			firstCall = false;
			try
			{
				var response = info.Request.EndGetResponse(ar);

				bool goodTile = IsGoodTileResponse(response);
				if (goodTile)
				{
					UpdateStatistics(() =>
					{
						Statistics.LongValues["DownloadedBytes"] += response.ContentLength;
						Statistics.IntValues["ImagesLoaded"]++;
					});

					BeginLoadBitmapImpl(response.GetResponseStream(), info.ID);
				}
				else
				{
					ReportFailure(info.ID);
				}
			}
			catch (WebException exc)
			{
				string responseUri = exc.Response != null ? exc.Response.ResponseUri.ToString() : "Response=null";

				Debug.WriteLine(
					String.Format(
					"{0} Network \"{1}\" Failure: url=\"{2}\": {3}", DateTime.Now, Name, responseUri, exc.Message));
				ReportFailure(info.ID);
			}
		}

		protected virtual bool IsGoodTileResponse(WebResponse response)
		{
			return true;
		}

		protected abstract string CreateRequestUri(TileIndex index);

		#endregion

		private sealed class ResponseCallbackInfo
		{
			public WebRequest Request { get; set; }
			public TileIndex ID { get; set; }
		}
	}
}
