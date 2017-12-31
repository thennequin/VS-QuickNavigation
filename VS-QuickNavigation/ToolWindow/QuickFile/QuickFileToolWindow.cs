
namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.PlatformUI;
	using System;
	using System.Runtime.InteropServices;
	
	[Guid("fd82e816-228c-4fbb-991d-4f8e9536b386")]
	public class QuickFileToolWindow : DialogWindow
	{
		public string mTitle;
		public QuickFileToolWindow(bool bHistoryOnly) : base()
		{
			if (bHistoryOnly)
			{
				mTitle = "Quick History";
			}
			else
			{
				mTitle = "Quick File";
			}

			this.Title = mTitle;

			this.Content = new QuickFileToolWindowControl(this, bHistoryOnly);

			this.Width = 1000;
			this.Height = 400;
		}

		public void OpenDialog()
		{
			WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
			QuickFileToolWindowControl oQuickFileControl = ((QuickFileToolWindowControl)this.Content);
			oQuickFileControl.RefreshContent();
			oQuickFileControl.textBox.SelectAll();
			oQuickFileControl.textBox.Focus();
			ShowModal();
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			QuickFileToolWindowControl oQuickFileControl = ((QuickFileToolWindowControl)this.Content);
			oQuickFileControl.textBox.Focus();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs oArgs)
		{
			oArgs.Cancel = true;
			Hide();
		}
	}
}