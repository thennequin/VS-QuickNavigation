
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
					Data.ESymbolType.Namespace |
					Data.ESymbolType.Struct |
					Data.ESymbolType.Union |
					Data.ESymbolType.Class |
					Data.ESymbolType.Interface |
					Data.ESymbolType.TypeDef |
					Data.ESymbolType.Macro |
					Data.ESymbolType.Enumerator |
					Data.ESymbolType.Enumeration |
					Data.ESymbolType.Method |
					Data.ESymbolType.MethodPrototype |
					Data.ESymbolType.Field |
					Data.ESymbolType.Property |
					Data.ESymbolType.Variable);
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