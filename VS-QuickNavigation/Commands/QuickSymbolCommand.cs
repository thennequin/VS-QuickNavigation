
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace VS_QuickNavigation
{
	internal sealed class QuickSymbolCommand
	{
		public const int CommandId = 0x0103;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;
		private QuickMethodToolWindow window;

		private QuickSymbolCommand(Package package)
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

			window = new QuickMethodToolWindow(true,
					Data.SymbolData.ESymbolType.Namespace |
					Data.SymbolData.ESymbolType.Struct |
					Data.SymbolData.ESymbolType.Class |
					Data.SymbolData.ESymbolType.Interface |
					Data.SymbolData.ESymbolType.Macro |
					Data.SymbolData.ESymbolType.Enumerator |
					Data.SymbolData.ESymbolType.Enumeration |
					Data.SymbolData.ESymbolType.Method |
					Data.SymbolData.ESymbolType.MethodPrototype |
					Data.SymbolData.ESymbolType.Field |
					Data.SymbolData.ESymbolType.Property);
		}

		public static QuickSymbolCommand Instance
		{
			get;
			private set;
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickSymbolCommand(package);
		}

		private void ShowWindow(object sender, EventArgs e)
		{
			window.OpenDialog();
		}
	}
}