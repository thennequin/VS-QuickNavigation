﻿//------------------------------------------------------------------------------
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
using System.Threading;
using Task = System.Threading.Tasks.Task;

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
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	
	[InstalledProductRegistration("#1110", "#1112", Vs_QuickNavigationVersion.Version, IconResourceID = 1400)] // Info on this package for Help/About
	[Guid(VSQuickNavigationPackage.PackageGuidString)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	//[ProvideToolWindow(typeof(QuickFileToolWindow))]
	//[ProvideToolWindow(typeof(QuickMethodToolWindow))]
	[ProvideToolWindow(typeof(QuickReferencesToolWindow))]
	[ProvideOptionPage(typeof(Options.OptionsDialogPage), "QuickNavigation", "Settings", 0, 0, supportsAutomation: true)]
	public sealed class VSQuickNavigationPackage : AsyncPackage
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
		}

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
			await base.InitializeAsync(cancellationToken, progress);
            
            // Switches to the UI thread in order to consume some services used in command initialization
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Common.Instance.DTE2 = Common.Instance.GetService<SDTE>() as EnvDTE80.DTE2;
            Common.Instance.Shell = Common.Instance.GetService<SVsShell>() as IVsShell;
            Common.Instance.Solution = Common.Instance.GetService<SVsSolution>() as IVsSolution2;
            Common.Instance.Settings = new Settings();

            Common.Instance.Settings.Refresh();
			Common.Instance.Settings.LoadSettingsFromStorage();

            Utils.CTagsGenerator.CTagsTask.CreateInstance();
            Common.Instance.SolutionWatcher = new SolutionWatcher();


            QuickFileCommand.Initialize(this);
			QuickHistoryCommand.Initialize(this);
			QuickMethodCommand.Initialize(this);
			QuickSymbolCommand.Initialize(this);
			QuickGotoCommand.Initialize(this);
			QuickReferencesCommand.Initialize(this);
			QuickPasteCommand.Initialize(this);
			QuickCleanCommand.Initialize(this);
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			QuickPasteCommand.Dispose(this);

			Common.Instance.SolutionWatcher.Dispose();
			Utils.CTagsGenerator.CTagsTask.GetInstance().Dispose();

		}

		#endregion Package Members
	}
}
