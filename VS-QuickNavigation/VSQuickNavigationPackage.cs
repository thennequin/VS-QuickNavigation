//------------------------------------------------------------------------------
// <copyright file="VSQuickNavigationPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace VS_QuickNavigation
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#1110", "#1112", "0.4.3", IconResourceID = 1400)] // Info on this package for Help/About
	[Guid(VSQuickNavigationPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	//[ProvideToolWindow(typeof(QuickFileToolWindow))]
	//[ProvideToolWindow(typeof(QuickMethodToolWindow))]
	[ProvideOptionPage(typeof(Options.OptionsDialogPage), "QuickNavigation", "Settings", 0, 0, supportsAutomation: true)]
	public sealed class VSQuickNavigationPackage : Package
	{
		/// <summary>
		/// VSQuickNavigationPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "62b5614a-f04f-40af-9fe6-67d3e05ff7ad";

		/// <summary>
		/// Initializes a new instance of the <see cref="VSQuickNavigationPackage"/> class.
		/// </summary>
		public VSQuickNavigationPackage()
		{
			Common.Instance.Package = this;
			Common.Instance.DTE2 = ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as EnvDTE80.DTE2;
			Common.Instance.Shell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
			Common.Instance.Solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution2;
			Common.Instance.Settings = new Settings();

			Common.Instance.SolutionWatcher = new SolutionWatcher();
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Common.Instance.Settings.Refresh();
			Common.Instance.Settings.LoadSettingsFromStorage();
			//solution;

			QuickFileCommand.Initialize(this);
			QuickHistoryCommand.Initialize(this);
			QuickMethodCommand.Initialize(this);
			QuickSymbolCommand.Initialize(this);

			//ShowOptionPage(typeof(Options.OptionsDialogPage));
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			Common.Instance.SolutionWatcher.Dispose();
		}

		#endregion Package Members
	}
}