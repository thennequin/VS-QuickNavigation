
namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.PlatformUI;
	using System;
	using System.Runtime.InteropServices;

	[Guid("9e2af9f7-c261-4530-bf48-1857826b0cab")]
	public class QuickMethodToolWindow : DialogWindow//ToolWindowPane
	{
		public QuickMethodToolWindow() : base()
		{
			this.Title = "Quick Method";

			this.Content = new QuickMethodToolWindowControl(this);

			this.Width = 1000;
			this.Height = 400;
		}
	}
}