
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	internal sealed class QuickGotoCommand
	{
		public const int CommandId = 0x0104;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;

		private QuickGotoCommand(Package package)
		{
			if (package == null)
			{
				throw new ArgumentNullException("package");
			}

			this.package = package;

			OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (commandService != null)
			{
				var menuCommandID = new CommandID(CommandSet, CommandId);
				var menuItem = new MenuCommand(this.GotoCurrentWord, menuCommandID);
				commandService.AddCommand(menuItem);
			}
		}

		public static QuickGotoCommand Instance
		{
			get;
			private set;
		}

		private IServiceProvider ServiceProvider
		{
			get
			{
				return this.package;
			}
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickGotoCommand(package);
		}

		private void GotoCurrentWord(object sender, EventArgs e)
		{
			string sCurrentWord = CommonUtils.GetCurrentWord();
			int iCurrentLine = CommonUtils.GetCurrentLine();
			string sCurrentFile = Common.Instance.DTE2.ActiveDocument.FullName.ToLower();

			Data.SymbolData originSymbol = null;
			Data.SymbolData symbol = null;
			//Search in local file
			IEnumerable<Data.SymbolData> documentSymbols = CTagsGenerator.GeneratorFromDocument(Common.Instance.DTE2.ActiveDocument);

			IEnumerable<Data.SymbolData> solutionSymbols = Common.Instance.SolutionWatcher.Files
									.AsParallel()
									.Where(file => file != null && file.Symbols != null)
									.SelectMany(file => file.Symbols)
									;

			if (documentSymbols != null)
			{
				originSymbol = documentSymbols.Where(s => s.StartLine == iCurrentLine && CommonUtils.IsLastWord(s.Symbol, sCurrentWord)).FirstOrDefault();
			}

			if( originSymbol != null)
			{
				if (originSymbol.Type == Data.SymbolData.ESymbolType.Method)
				{
					//Search prototype 
					symbol = solutionSymbols.Where(s => s.Type == Data.SymbolData.ESymbolType.MethodPrototype && s.ScopePretty == originSymbol.ScopePretty && s.Symbol == originSymbol.Symbol).FirstOrDefault();
				}
				else if (originSymbol.Type == Data.SymbolData.ESymbolType.MethodPrototype)
				{
					//Search method
					symbol = solutionSymbols.Where(s => s.Type == Data.SymbolData.ESymbolType.Method && s.ScopePretty == originSymbol.ScopePretty && s.Symbol == originSymbol.Symbol).FirstOrDefault();
				}
			}

			if (symbol == null && documentSymbols != null)
			{
				IEnumerable<Data.SymbolData> filtered = documentSymbols.Where(s => s.StartLine < iCurrentLine && CommonUtils.IsLastWord(s.Symbol, sCurrentWord));
				int iCount = filtered.Count();
				if (iCount == 1)
				{
					symbol = filtered.First();
				}
				else if( iCount > 1)
				{
					symbol = filtered.OrderBy(s => s.Type).First();
				}
			}

			// Search in solution
			if (symbol == null)
			{
				IEnumerable<Data.SymbolData> filtered = solutionSymbols
					.Where(s => CommonUtils.ContainsWord(s.Symbol, sCurrentWord) && ( s.StartLine != iCurrentLine || s.AssociatedFile.Path.ToLower() != sCurrentFile));
				int iCount = filtered.Count();
				if (iCount == 1)
				{
					symbol = filtered.First();
				}
				else if (iCount > 1)
				{
					symbol = filtered.OrderBy(s => s.Type).First();
				}
			}

			if (symbol != null)
			{
				CommonUtils.GotoSymbol(symbol);
			}
		}
	}
}