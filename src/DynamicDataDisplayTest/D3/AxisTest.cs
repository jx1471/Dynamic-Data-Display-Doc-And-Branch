using Microsoft.Research.DynamicDataDisplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.DynamicDataDisplay.Charts;
using System.Windows;
using System.Threading;
using System.Security.Permissions;
using System;

namespace DynamicDataDisplay.Test
{
	[TestClass]
	public class AxisTest
	{
		public TestContext TestContext { get; set; }

		[TestMethod]
		public void HorizontalAxisTest()
		{
			ChartPlotter target = new ChartPlotter();
			IAxis expected = new HorizontalAxis();
			IAxis actual;
			target.HorizontalAxis = expected;
			actual = target.HorizontalAxis;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void VerticalAxisTest()
		{
			ChartPlotter plotter = new ChartPlotter();
			IAxis expected = new VerticalAxis();
			IAxis actual;
			plotter.VerticalAxis = expected;
			actual = plotter.VerticalAxis;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void HorizontalAxisIsDefaultTest()
		{
			ChartPlotter plotter = new ChartPlotter();

			HorizontalAxis axis = (HorizontalAxis)plotter.HorizontalAxis;
			HorizontalAxis axis2 = new HorizontalAxis();
			plotter.Children.Add(axis2);

			Assert.AreEqual(plotter.HorizontalAxis, axis);
			Assert.IsTrue(axis.IsDefaultAxis);

			axis2.IsDefaultAxis = true;
			Assert.AreEqual(plotter.HorizontalAxis, axis2);
			Assert.IsFalse(axis.IsDefaultAxis);

			axis.IsDefaultAxis = true;
			Assert.AreEqual(plotter.HorizontalAxis, axis);
			Assert.IsFalse(axis2.IsDefaultAxis);
		}

		[TestMethod]
		public void VerticalAxisIsDefaultTest()
		{
			ChartPlotter plotter = new ChartPlotter();

			VerticalAxis axis = (VerticalAxis)plotter.VerticalAxis;
			VerticalAxis axis2 = new VerticalAxis();
			plotter.Children.Add(axis2);

			Assert.AreEqual(plotter.VerticalAxis, axis);
			Assert.IsTrue(axis.IsDefaultAxis);

			axis2.IsDefaultAxis = true;
			Assert.AreEqual(plotter.VerticalAxis, axis2);
			Assert.IsFalse(axis.IsDefaultAxis);

			axis.IsDefaultAxis = true;
			Assert.AreEqual(plotter.VerticalAxis, axis);
			Assert.IsFalse(axis2.IsDefaultAxis);
		}
	}
}
