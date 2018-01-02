using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	internal sealed class QuickReferencesCommand
	{
		public const int CommandId = 0x0105;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;

		private QuickReferencesCommand(Package package)
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
				var menuItem = new MenuCommand(this.GotoCurrentWord, menuCommandID);
				commandService.AddCommand(menuItem);
			}
		}

		public static QuickReferencesCommand Instance
		{
			get;
			private set;
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickReferencesCommand(package);
		}

		private void GotoCurrentWord(object sender, EventArgs e)
		{
			string sCurrentWord = CommonUtils.GetCurrentWord();
			int iCurrentLine = CommonUtils.GetCurrentLine();
			string sCurrentFile = Common.Instance.DTE2.ActiveDocument.FullName.ToLower();

			EnvDTE.Window window = Common.Instance.DTE2.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
			EnvDTE.OutputWindow outputWindow = (EnvDTE.OutputWindow)window.Object;
			outputWindow.ActivePane.Activate();

			Data.SymbolData originSymbol = null;
			//Search in local file
			IEnumerable<Data.SymbolData> documentSymbols = CTagsGenerator.GeneratorFromDocument(Common.Instance.DTE2.ActiveDocument);

			IEnumerable<Data.SymbolData> solutionSymbols = Common.Instance.SolutionWatcher.Files
									.AsParallel()
									.Where(file => file != null && file.Symbols != null)
									.SelectMany(file => file.Symbols);

			if (documentSymbols != null)
			{
				originSymbol = documentSymbols.Where(s => s.StartLine == iCurrentLine && CommonUtils.IsLastWord(s.Symbol,sCurrentWord)).OrderBy(s => s.Type).FirstOrDefault();
			}

			if (originSymbol == null)
			{
				originSymbol = solutionSymbols.Where(s => CommonUtils.IsLastWord(s.Symbol, sCurrentWord)).OrderBy(s => s.Type).FirstOrDefault();
			}

			QuickReferencesToolWindow oWindow = QuickReferencesToolWindow.GetInstance();
			if (originSymbol != null)
			{
				oWindow.SearchReferences(originSymbol);
			}
			else
			{
				oWindow.SearchReferences(sCurrentWord);
			}
		}
	}
}