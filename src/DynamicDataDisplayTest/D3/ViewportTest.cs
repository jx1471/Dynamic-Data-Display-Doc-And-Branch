using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.DynamicDataDisplay;
using System.Windows;

namespace DynamicDataDisplay.Test.D3
{
	[TestClass]
	public class ViewportTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestScreenRect()
		{
			ChartPlotter plotter = new ChartPlotter { Width = 200, Height = 100 };
			var img = plotter.CreateScreenshot();

			Rect screenRect = plotter.Viewport.Output;
			Assert.IsTrue(screenRect.Width > 0 && screenRect.Height > 0);
		}
	}
}
