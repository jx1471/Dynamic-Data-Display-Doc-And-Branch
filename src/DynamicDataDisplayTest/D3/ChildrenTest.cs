using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Research.DynamicDataDisplay;
using System.Reflection;
using Microsoft.Research.DynamicDataDisplay.Charts.Navigation;
using Microsoft.Research.DynamicDataDisplay.Charts.Maps;
using DynamicDataDisplay.Test.Common;

namespace DynamicDataDisplay.Test
{
	[TestClass]
	public class ChildrenTest
	{

		private List<Assembly> testedAssemblies;

		public ChildrenTest()
		{
			testedAssemblies = (from assemblyName in Assembly.GetExecutingAssembly().GetReferencedAssemblies()
								where assemblyName.Version.Major == 0			// D3 assemblies currently has version 0.*
								select Assembly.Load(assemblyName)).ToList();
		}

		public TestContext TestContext { get; set; }

		[TestMethod]
		public void TestAddingNull()
		{
			ChartPlotter plotter = new ChartPlotter();
			bool thrown = false;
			try
			{
				plotter.Children.Add(null);
			}
			catch (ArgumentNullException)
			{
				thrown = true;
			}

			Assert.IsTrue(thrown);
		}

		[TestMethod]
		public void TestAllElementsAddRemove()
		{
			ChartPlotter plotter = new ChartPlotter();

			var types = GetAllCharts();

			var plotterElements = new List<IPlotterElement>();
			plotter.Children.Clear();
			foreach (var type in types)
			{
				IPlotterElement element = (IPlotterElement)Activator.CreateInstance(type);
				plotterElements.Add(element);
				plotter.Children.Add(element);
			}

			foreach (var item in plotterElements)
			{
				Assert.AreEqual(plotter, item.Plotter);
			}

			plotter.Children.Clear();

			foreach (var item in plotterElements)
			{
				Assert.IsNull(item.Plotter, item.ToString());
			}
		}

		private List<Type> GetAllCharts()
		{
			Type elementType = typeof(IPlotterElement);

			var types = from assembly in testedAssemblies
						from type in assembly.GetExportedTypes()
						where elementType.IsAssignableFrom(type) && !type.IsAbstract && type.IsPublic
						select type;

			var list = types.ToList();

			Assert.IsTrue(list.Count > 0, "List of IPlotterElement, loaded by reflection from referenced assemblies, should not be empty.");

			return list;
		}

		[TestMethod]
		public void TestSettingSameValueAsWasGet()
		{
			var types = GetAllCharts();
			var properties = from type in types
							 let typeProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
							 let ownProperties = from prop in typeProperties
												 where prop.CanWrite && prop.CanRead
												 where testedAssemblies.Contains(prop.DeclaringType.Assembly)
												 select prop
							 select new
							 {
								 Type = type,
								 Properties = ownProperties.ToArray()
							 };

			var propertiesList = properties.ToList();

			var charts = (from item in propertiesList
						  let instance = Activator.CreateInstance(item.Type)
						  select new { Instance = instance, Properties = item.Properties }).ToList();

			// setting the same value to property as was stored in it
			foreach (var chart in charts)
			{
				var instance = chart.Instance;
				foreach (var property in chart.Properties)
				{
					try
					{
						property.SetValue(instance, property.GetValue(instance, null), null);
					}
					catch (ArgumentNullException) { }
					catch (TargetInvocationException) { }
				}
			}
		}

		[TestMethod]
		public void TestSettingPropertiesBeforeAddingToPlotter()
		{
			var types = GetAllCharts();
			var properties = from type in types
							 let typeProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
							 let ownProperties = from prop in typeProperties
												 where prop.CanWrite && prop.CanRead
												 where testedAssemblies.Contains(prop.DeclaringType.Assembly)
												 select prop
							 select new
							 {
								 Type = type,
								 Properties = ownProperties.ToArray()
							 };

			var propertiesList = properties.ToList();

			var charts = (from item in propertiesList
						  let instance = Activator.CreateInstance(item.Type)
						  select new { Instance = instance, Properties = item.Properties }).ToList();

			PropertySetSystem system = new PropertySetSystem();
			// setting custom values
			foreach (var chart in charts)
			{
				var instance = chart.Instance;
				foreach (var property in chart.Properties)
				{
					system.TrySetValue(instance, property);
				}
			}

			ChartPlotter plotter = ChartPlotter.CreateEmpty();

			foreach (var chart in charts)
			{
				plotter.Children.Add(chart.Instance as IPlotterElement);
			}

			foreach (var chart in charts)
			{
				var instance = chart.Instance;
				foreach (var property in chart.Properties)
				{
					system.TrySetValue(instance, property);
				}
			}
		}


		private Type GetExternalBaseType(Type type)
		{
			Type baseType = type.BaseType;
			while (testedAssemblies.Contains(baseType.Assembly))
			{
				baseType = baseType.BaseType;
			}
			return baseType;
		}
	}
}
