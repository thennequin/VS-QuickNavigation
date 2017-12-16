
namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.PlatformUI;
	using System;
	using System.Runtime.InteropServices;

	[Guid("9e2af9f7-c261-4530-bf48-1857826b0cab")]
	public class QuickMethodToolWindow : DialogWindow//ToolWindowPane
	{
		public QuickMethodToolWindow(bool searchInSolution, Data.SymbolData.ESymbolType types) : base()
		{
			if (searchInSolution)
			{
				this.Title = "Quick Symbol";
			}
			else
			{
				this.Title = "Quick Method";
			}

			//Methods of current documents
			this.Content = new QuickMethodToolWindowControl(this, searchInSolution, types);

			this.Width = 1000;
			this.Height = 400;
		}

		public void OpenDialog()
		{
			WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
			ShowModal();
			QuickMethodToolWindowControl oQuickMethodControl = ((QuickMethodToolWindowControl)this.Content);
			oQuickMethodControl.RefreshContent();
			oQuickMethodControl.textBox.SelectAll();
			oQuickMethodControl.textBox.Focus();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs oArgs)
		{
			oArgs.Cancel = true;
			Hide();
		}
	}
}