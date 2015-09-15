//------------------------------------------------------------------------------
// <copyright file="QuickFileToolWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.PlatformUI;
	using System;
	using System.Runtime.InteropServices;

	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid("fd82e816-228c-4fbb-991d-4f8e9536b386")]
	//public class QuickFileToolWindow : ToolWindowPane
	public class QuickFileToolWindow : DialogWindow/*, IVsWindowSearch*/
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuickFileToolWindow"/> class.
		/// </summary>
		public QuickFileToolWindow() : base()
		{
			this.Title = "QuickFile";

			this.Content = new QuickFileToolWindowControl();

			this.Width = 750;
			this.Height = 300;
		}
	}
}