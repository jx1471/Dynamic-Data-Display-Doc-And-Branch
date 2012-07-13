using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Research.DynamicDataDisplay.Common.Auxiliary;
using System.IO;
using System.Diagnostics;

namespace Microsoft.Research.DynamicDataDisplay.Charts.Maps
{
	public class WriteableFileSystemTileServer : FileSystemTileServer, ITileStore, IWriteableTileServer
	{
		public WriteableFileSystemTileServer(string serverName) : base(serverName) { }

		#region IWritableTileSource Members

		private SaveOption saveOption = SaveOption.ForceUpdate;
		public SaveOption SaveOption
		{
			get { return saveOption; }
			set { saveOption = value; }
		}

		public void BeginSaveImage(TileIndex id, BitmapImage image)
		{
			string imagePath = GetImagePath(id);

			bool errorWhileDeleting = false;

			bool containsOld = Contains(id);
			if (containsOld && saveOption == SaveOption.ForceUpdate)
				try
				{
					File.Delete(imagePath);
				}
				catch (IOException exc)
				{
					// todo возможно, тут добавить файл в очередь на удаление или перезапись новым содержимым
					// когда он перестанет быть блокированным
					Debug.WriteLine(String.Format("{0} - error while deleting tile {1}: {2}", Name, id, exc.Message));
					errorWhileDeleting = true;
				}

			bool shouldSave = saveOption == SaveOption.ForceUpdate && !errorWhileDeleting ||
				saveOption == SaveOption.PreserveOld && !containsOld;
			if (shouldSave)
			{
				Debug.WriteLine("Began to save id = " + id);

				Statistics.IntValues["ImagesSaved"]++;

				ImageSaver saver = ScreenshotHelper.SaveBitmapToFile;
				saver.BeginInvoke((BitmapImage)image.GetAsFrozen(), imagePath, null, null);
			}
		}

		private delegate void ImageSaver(BitmapSource bmp, string path);

		#endregion
	}

	public enum SaveOption
	{
		ForceUpdate,
		PreserveOld
	}
}
