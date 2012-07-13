using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.ViewportRestrictions;

namespace Microsoft.Research.DynamicDataDisplay.Demo
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			Loaded += new RoutedEventHandler(MainWindow_Loaded);
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Prepare data in arrays
			const int N = 100;
			double[] x = new double[N];
			double[] y = new double[N];

			for (int i = 0; i < N; i++)
			{
				x[i] = i * 0.1;
				y[i] = Math.Cos(x[i]);
			}

			// Add data sources:
			var yDataSource = new EnumerableDataSource<double>(y);
			yDataSource.SetYMapping(Y => Y);
			yDataSource.AddMapping(ShapeElementPointMarker.ToolTipTextProperty,
				Y => string.Format("Value is {0}", Y));

			var xDataSource = new EnumerableDataSource<double>(x);
			xDataSource.SetXMapping(X => X);

			CompositeDataSource compositeDataSource = new CompositeDataSource(xDataSource, yDataSource);

			plotter.Viewport.Restrictions.Add(new PhysicalProportionsRestriction { ProportionRatio = 1 });

			// adding graph to plotter
			plotter.AddLineGraph(compositeDataSource,
				new Pen(Brushes.Goldenrod, 3),
				new SampleMarker(),
				new PenDescription("Cosine"));
		}
	}

	public class SampleMarker : ShapeElementPointMarker
	{
		public override UIElement CreateMarker()
		{
			Canvas result = new Canvas()
			{
				Width = 10,
				Height = Size
			};
			result.Width = Size;
			result.Height = Size;
			result.Background = Brush;
			if (ToolTipText != String.Empty)
			{
				ToolTip tt = new ToolTip();
				tt.Content = ToolTipText;
				result.ToolTip = tt;
			}
			return result;
		}

		public override void SetPosition(UIElement marker, Point screenPoint)
		{
			Canvas.SetLeft(marker, screenPoint.X - Size / 2);
			Canvas.SetTop(marker, screenPoint.Y - Size / 2);
		}
	}

}
