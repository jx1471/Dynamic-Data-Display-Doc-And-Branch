using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;

namespace Microsoft.Research.DynamicDataDisplay.Common
{
	internal sealed class NotifyingCanvas : Canvas, INotifyingPanel
	{
		#region INotifyingPanel Members

		private NotifyingUIElementCollection notifyingChildren;
		//Accessor Method for the Collection notifyingChildren
		public NotifyingUIElementCollection NotifyingChildren
		{
			get { return notifyingChildren; }
		}

		protected override UIElementCollection CreateUIElementCollection(FrameworkElement logicalParent)
		{
		//<function summary>
		// Override the CreateUIElementCollection method
		// Assign notifyingChildren to the Collection associated with the logicalParent
		// Raise the event handler ChildrenCreated
		//</function summary>
			notifyingChildren = new NotifyingUIElementCollection(this, logicalParent);
			ChildrenCreated.Raise(this);

			return notifyingChildren;
		}
		//Event handler triggered when children are created.
		public event EventHandler ChildrenCreated;

		#endregion
	}
}
