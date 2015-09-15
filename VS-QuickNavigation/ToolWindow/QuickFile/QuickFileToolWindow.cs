
namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.PlatformUI;
	using System;
	using System.Runtime.InteropServices;
	
	[Guid("fd82e816-228c-4fbb-991d-4f8e9536b386")]
	public class QuickFileToolWindow : DialogWindow
	{
		public QuickFileToolWindow() : base()
		{
			this.Title = "QuickFile";

			this.Content = new QuickFileToolWindowControl();

			this.Width = 1000;
			this.Height = 400;
		}
	}
}