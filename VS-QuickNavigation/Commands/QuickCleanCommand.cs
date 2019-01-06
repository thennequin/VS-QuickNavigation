
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;

namespace VS_QuickNavigation
{
	internal sealed class QuickCleanCommand
	{
		public const int CommandId = 0x0107;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;

		private QuickCleanCommand(Package package)
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
				var menuItem = new MenuCommand(this.CleanDatabase, menuCommandID);
				commandService.AddCommand(menuItem);
			}
		}

		public static QuickCleanCommand Instance
		{
			get;
			private set;
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickCleanCommand(package);
		}

		void CleanDatabase(object sender, EventArgs e)
		{
			Common.Instance.SolutionWatcher.ClearSymbolDatabase();
			Common.Instance.SolutionWatcher.TriggeringRefreshFileList();
		}
	}
}
