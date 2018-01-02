
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace VS_QuickNavigation
{
	internal sealed class QuickMethodCommand
	{
		public const int CommandId = 0x0102;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;
        private QuickMethodToolWindow window;

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
				var menuItem = new MenuCommand(this.ShowWindow, menuCommandID);
				commandService.AddCommand(menuItem);
			}

            window = new QuickMethodToolWindow(false,
                    Data.SymbolData.ESymbolType.Method |
                    Data.SymbolData.ESymbolType.MethodPrototype |
					Data.SymbolData.ESymbolType.Property);
        }

		public static QuickMethodCommand Instance
		{
			get;
			private set;
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickMethodCommand(package);
		}

		private void ShowWindow(object sender, EventArgs e)
		{
			window.OpenDialog();
		}
	}
}