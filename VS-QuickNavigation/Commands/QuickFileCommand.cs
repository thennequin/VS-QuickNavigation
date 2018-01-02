
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace VS_QuickNavigation
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class QuickFileCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0100;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly Package package;

		private QuickFileToolWindow window;

		/// <summary>
		/// Initializes a new instance of the <see cref="QuickFileCommand"/> class.
		/// Adds our command handlers for menu (commands must exist in the command table file)
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		private QuickFileCommand(Package package)
		{
			if (package == null)
			{
				throw new ArgumentNullException("package");
			}

			this.package = package;

			OleMenuCommandService commandService = Common.Instance.GetService<IMenuCommandService>() as OleMenuCommandService;
			if (commandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
				commandService.AddCommand(menuItem);
			}

			window = new QuickFileToolWindow(false);
		}
		
		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static QuickFileCommand Instance
		{
			get;
			private set;
		}

		/// <summary>
		/// Initializes the singleton instance of the command.
		/// </summary>
		/// <param name="package">Owner package, not null.</param>
		public static void Initialize(Package package)
		{
			Instance = new QuickFileCommand(package);
		}

		/// <summary>
		/// Shows the tool window when the menu item is clicked.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		private void ShowToolWindow(object sender, EventArgs e)
		{
			// Get the instance number 0 of this tool window. This window is single instance so this instance
			// is actually the only one.
			// The last flag is set to true so that if the tool window does not exists it will be created.
			/*ToolWindowPane window = this.package.FindToolWindow(typeof(QuickFileToolWindow), 0, true);
			if ((null == window) || (null == window.Frame))
			{
				throw new NotSupportedException("Cannot create tool window");
			}

			IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
			Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());*/

			//Common.Instance.SolutionWatcher.TestSpeed();

			
			window.OpenDialog();
		}
	}
}