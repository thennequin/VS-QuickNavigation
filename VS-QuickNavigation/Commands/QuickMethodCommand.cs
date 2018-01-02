
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace VS_QuickNavigation
{
	/// <summary>
	/// Command handler
	/// </summary>
	internal sealed class QuickMethodCommand
	{
		/// <summary>
		/// Command ID.
		/// </summary>
		public const int CommandId = 0x0102;

		/// <summary>
		/// Command menu group (command set GUID).
		/// </summary>
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		/// <summary>
		/// VS Package that provides this command, not null.
		/// </summary>
		private readonly Package package;

        private QuickMethodToolWindow window;

        /// <summary>
        /// Initializes a new instance of the <see cref="QuickMethodCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private QuickMethodCommand(Package package)
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

            window = new QuickMethodToolWindow(false,
                    Data.SymbolData.ESymbolType.Method |
                    Data.SymbolData.ESymbolType.MethodPrototype |
					Data.SymbolData.ESymbolType.Property);
        }

		/// <summary>
		/// Gets the instance of the command.
		/// </summary>
		public static QuickMethodCommand Instance
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
			Instance = new QuickMethodCommand(package);
		}

		/// <summary>
		/// Shows the tool window when the menu item is clicked.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		private void ShowToolWindow(object sender, EventArgs e)
		{
			window.OpenDialog();
		}
	}
}