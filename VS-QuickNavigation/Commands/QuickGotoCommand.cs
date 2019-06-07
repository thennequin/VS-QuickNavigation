using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Controls;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	internal sealed class QuickGotoCommand
	{
		public const int CommandId = 0x0104;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;

		ContextMenu m_oContextMenu;

		private QuickGotoCommand(Package package)
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

			m_oContextMenu = new ContextMenu();
		}

		public static QuickGotoCommand Instance
		{
			get;
			private set;
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickGotoCommand(package);
		}

		private void GotoCurrentWord(object sender, EventArgs e)
		{
			string sCurrentWord = CommonUtils.GetCurrentWord();

			if (string.IsNullOrWhiteSpace(sCurrentWord))
				return;

			int iCurrentLine = CommonUtils.GetCurrentLine();
			string sCurrentFile = Common.Instance.DTE2.ActiveDocument.FullName.ToLower();

			Data.SymbolData originSymbol = null;
			IEnumerable<Data.SymbolData> symbols = null;
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

			if (originSymbol != null)
			{
				if (originSymbol.Type == Data.ESymbolType.Method)
				{
					//Search prototype
					symbols = solutionSymbols.Where(s => s.Type == Data.ESymbolType.MethodPrototype && s.ScopePretty == originSymbol.ScopePretty && s.Symbol == originSymbol.Symbol);
				}
				else if (originSymbol.Type == Data.ESymbolType.MethodPrototype)
				{
					//Search method
					symbols = solutionSymbols.Where(s => s.Type == Data.ESymbolType.Method && s.ScopePretty == originSymbol.ScopePretty && s.Symbol == originSymbol.Symbol);
				}
			}

			// Search in symbols of current document
			if ((symbols == null || symbols.Any() == false) && documentSymbols != null)
			{
				IEnumerable<Data.SymbolData> filtered = documentSymbols.Where(s => s.StartLine < iCurrentLine && CommonUtils.IsLastWord(s.Symbol, sCurrentWord));
				symbols = filtered.OrderBy(s => s.Type);
			}

			// Search in solution
			if (symbols == null || symbols.Any() == false)
			{
				IEnumerable<Data.SymbolData> filtered = solutionSymbols
					.Where(s => CommonUtils.ContainsWord(s.Symbol, sCurrentWord) && (s.StartLine != iCurrentLine || s.AssociatedFile.Path.ToLower() != sCurrentFile));
				symbols = filtered.OrderBy(s => s.Type);
			}

			if (symbols != null && symbols.Any())
			{
				Data.SymbolData oClassSymbol = (originSymbol != null && originSymbol.Type == Data.ESymbolType.Class) ? originSymbol : symbols.FirstOrDefault(s => s.Type == Data.ESymbolType.Class);
				List<Data.SymbolData> lBaseClasses = new List<Data.SymbolData>();
				List<Data.SymbolData> lInheritedClasses = new List<Data.SymbolData>();

				if (oClassSymbol != null)
				{
					//Base
					if (oClassSymbol.Inherits != null && oClassSymbol.Inherits.Length > 0)
					{
						foreach (string sBase in oClassSymbol.Inherits)
						{
							Data.SymbolData oBaseSymbol = solutionSymbols.FirstOrDefault(s => s.Type == Data.ESymbolType.Class && s.Symbol == sBase);
							if (oBaseSymbol != null)
							{
								lBaseClasses.Add(oBaseSymbol);
							}
						}
					}

					//Inherited
					IEnumerable<Data.SymbolData> inheritsClass = solutionSymbols.Where(s => s.Type == Data.ESymbolType.Class && s.Inherits != null && s.Inherits.Any(i => CommonUtils.IsLastWord(i, oClassSymbol.Symbol)));
					lInheritedClasses.AddRange(inheritsClass);
				}

				if (symbols.Count() == 1 && lBaseClasses.Count == 0 && lInheritedClasses.Count == 0)
				{
					CommonUtils.GotoSymbol(symbols.First());
				}
				else
				{
					System.Windows.Point oPoint;
					if (CommonUtils.GetActiveDocumentCursorScreenPos(out oPoint, true))
					{
						m_oContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
						m_oContextMenu.PlacementRectangle = new System.Windows.Rect(oPoint, oPoint);

						m_oContextMenu.Items.Clear();

						int iCurrent = 0;
						foreach (Data.SymbolData symbol in symbols)
						{
							MenuItem oItem = new MenuItem();
							oItem.Tag = symbol;
							oItem.Header = $"{symbol.AssociatedFilename}({symbol.StartLine})  {symbol.Type}: {symbol.ScopePretty}{symbol.Symbol}{symbol.Parameters}";
							oItem.Click += GotoMenuItem_Click;
							m_oContextMenu.Items.Add(oItem);
							++iCurrent;
							if (iCurrent > 25)
							{
								m_oContextMenu.Items.Add("...");
								break;
							}
						}

						if (lBaseClasses.Count > 0 || lInheritedClasses.Count > 0)
							m_oContextMenu.Items.Add(new Separator());

						if (lBaseClasses.Count != 0)
						{
							MenuItem oBaseItem = new MenuItem();
							oBaseItem.Header = "Base";

							foreach (Data.SymbolData oBaseClass in lBaseClasses)
							{
								MenuItem oSubBaseItem = new MenuItem();
								oSubBaseItem.Tag = oBaseClass;
								oSubBaseItem.Header = $"{oBaseClass.AssociatedFilename}({oBaseClass.StartLine})   {oBaseClass.ScopePretty}{oBaseClass.Symbol}{oBaseClass.Parameters}";
								oSubBaseItem.Click += GotoMenuItem_Click;
								oBaseItem.Items.Add(oSubBaseItem);
							}
							m_oContextMenu.Items.Add(oBaseItem);
						}

						if (lInheritedClasses.Count != 0)
						{
							MenuItem oInheritedItem = new MenuItem();
							oInheritedItem.Header = "Inherited";

							foreach (Data.SymbolData oInheritedClass in lInheritedClasses)
							{
								MenuItem oSubInheritedItem = new MenuItem();
								oSubInheritedItem.Tag = oInheritedClass;
								oSubInheritedItem.Header = $"{oInheritedClass.AssociatedFilename}({oInheritedClass.StartLine})   {oInheritedClass.ScopePretty}{oInheritedClass.Symbol}{oInheritedClass.Parameters}";
								oSubInheritedItem.Click += GotoMenuItem_Click;
								oInheritedItem.Items.Add(oSubInheritedItem);
							}
							m_oContextMenu.Items.Add(oInheritedItem);
						}

						m_oContextMenu.IsOpen = true;
					}
				}
			}
		}

		static void GotoMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				MenuItem oItem = sender as MenuItem;
				if (oItem.Tag is Data.SymbolData)
				{
					CommonUtils.GotoSymbol(oItem.Tag as Data.SymbolData);
				}
			}
		}
	}
}