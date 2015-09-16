//------------------------------------------------------------------------------
// <copyright file="VSQuickNavigationPackage.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

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
	[InstalledProductRegistration("#1110", "#1112", "0.3", IconResourceID = 1400)] // Info on this package for Help/About
	[Guid(VSQuickNavigationPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideToolWindow(typeof(QuickFileToolWindow))]
	[ProvideToolWindow(typeof(QuickMethodToolWindow))]
	public sealed class VSQuickNavigationPackage : Package
	{
		private IVsShell vsShell = null;
		private IVsSolution2 solution = null;

		/// <summary>
		/// VSQuickNavigationPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "62b5614a-f04f-40af-9fe6-67d3e05ff7ad";

		/// <summary>
		/// Initializes a new instance of the <see cref="VSQuickNavigationPackage"/> class.
		/// </summary>
		public VSQuickNavigationPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			QuickFileToolWindowCommand.Initialize(this);
			QuickMethodToolWindowCommand.Initialize(this);

			vsShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
			if (vsShell != null)
			{
			}

			solution = ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution)) as IVsSolution2;
			if (solution != null)
			{
			}
		}

		#endregion Package Members
	}
}