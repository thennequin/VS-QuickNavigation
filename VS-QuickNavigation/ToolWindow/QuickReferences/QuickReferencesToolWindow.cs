using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;

namespace VS_QuickNavigation
{
	[Guid("D6D06A41-D72D-4381-AE07-76FC01DE6190")]
	class QuickReferencesToolWindow : Microsoft.VisualStudio.Shell.ToolWindowPane
	{
		static public QuickReferencesToolWindow GetInstance()
		{
			QuickReferencesToolWindow oWindow = Common.Instance.Package.FindToolWindow(typeof(QuickReferencesToolWindow), 0, true) as QuickReferencesToolWindow;

			IVsWindowFrame windowFrame = (IVsWindowFrame)oWindow.Frame;
			Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());

			return oWindow;
		}

		public QuickReferencesToolWindow()
		{
			this.Caption = "Quick References";
			this.Content = new QuickReferencesToolWindowControl();
		}

		public void SearchReferences(Data.SymbolData symbol)
		{
			SearchReferences(symbol.Symbol);
		}

		public void SearchReferences(string sSymbol)
		{
			QuickReferencesToolWindowControl refControl = (QuickReferencesToolWindowControl)this.Content;
			refControl.SearchReferences(sSymbol);
		}
	}
}
