//------------------------------------------------------------------------------
// <copyright file="QuickMethodToolWindowControl.xaml.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace VS_QuickNavigation
{
	using Microsoft.VisualStudio.Shell;
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Threading;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Documents;
	using Data;
	using System.Linq;

	/// <summary>
	/// Interaction logic for QuickMethodToolWindowControl.
	/// </summary>
	public partial class QuickMethodToolWindowControl : UserControl
	{
		

		private QuickMethodToolWindow mQuickMethodToolWindow;
		private List<SymbolData> mSymbols = new List<SymbolData>();
		private CancellationTokenSource mToken;

		public QuickMethodToolWindowControl(QuickMethodToolWindow oParent)
		{
			this.InitializeComponent();

			mQuickMethodToolWindow = oParent;

			EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
			EnvDTE.CodeElements codeElements = dte2.ActiveDocument.ProjectItem.FileCodeModel.CodeElements;

			AnalyseCodeElements(codeElements);

			DataContext = this;

			RefreshResults();

			textBox.Focus();
		}

		private void AnalyseCodeElements(EnvDTE.CodeElements codeElements)
		{
			if (null != codeElements)
			{
				foreach (EnvDTE.CodeElement objCodeElement in codeElements)
				{
					if (objCodeElement is EnvDTE.CodeFunction)
					{
						EnvDTE.CodeFunction objCodeFunction = objCodeElement as EnvDTE.CodeFunction;
						string sSymbol = objCodeElement.FullName;
						sSymbol += "(";

						if (null != objCodeFunction.Parameters && objCodeFunction.Parameters.Count > 0)
						{
							sSymbol += " ";
							bool bFirstMember = true;
							foreach (EnvDTE.CodeParameter objCodeParameter in objCodeFunction.Parameters)
							{
								if (bFirstMember)
								{
									bFirstMember = false;
								}
								else
								{
									sSymbol += ", ";
								}
								sSymbol += objCodeParameter.Type.AsString;
								sSymbol += " ";
								sSymbol += objCodeParameter.Name;
							}
							sSymbol += " ";
						}

						sSymbol += ")";

						mSymbols.Add(new SymbolData(sSymbol, objCodeElement.StartPoint.Line));
					}
					else
					{
						AnalyseCodeElements(GetCodeElementMembers(objCodeElement));
					}
				}
			}
		}

		private EnvDTE.CodeElements GetCodeElementMembers(EnvDTE.CodeElement objCodeElement)
		{
			EnvDTE.CodeElements colCodeElements = null;

			if (objCodeElement is EnvDTE.CodeNamespace)
			{
				colCodeElements = ((EnvDTE.CodeNamespace)objCodeElement).Members;
			}
			else if (objCodeElement is EnvDTE.CodeType)
			{
				colCodeElements = ((EnvDTE.CodeType)objCodeElement).Members;
			}
			else if (objCodeElement is EnvDTE.CodeFunction)
			{
				colCodeElements = ((EnvDTE.CodeFunction)objCodeElement).Parameters;
			}
			else if (objCodeElement is EnvDTE.CodeClass)
			{
				colCodeElements = ((EnvDTE.CodeClass)objCodeElement).Members;
			}
			return colCodeElements;
		}

		private void textBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsUp)
			{
				if (e.Key == System.Windows.Input.Key.Up)
				{
					listView.SelectedIndex--;
					e.Handled = true;
				}
				else if (e.Key == System.Windows.Input.Key.Down)
				{
					listView.SelectedIndex++;
					e.Handled = true;
				}
			}
			else if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
					int selectedIndex = listView.SelectedIndex;
					if (selectedIndex == -1) selectedIndex = 0;
					SearchResult<SymbolData> symbolData = listView.Items[selectedIndex] as SearchResult<SymbolData>;
					((EnvDTE.TextSelection)dte2.ActiveDocument.Selection).GotoLine(symbolData.Data.StartLine);

					mQuickMethodToolWindow.Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					mQuickMethodToolWindow.Close();
				}
			}
		}

		private void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			RefreshResults();
		}

		private void listView_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.IsDown)
			{
				if (e.Key == System.Windows.Input.Key.Return)
				{
					EnvDTE80.DTE2 dte2 = ServiceProvider.GlobalProvider.GetService(typeof(Microsoft.VisualStudio.Shell.Interop.SDTE)) as EnvDTE80.DTE2;
					int selectedIndex = listView.SelectedIndex;
					if (selectedIndex == -1) selectedIndex = 0;
					SearchResult<SymbolData> symbolData = listView.Items[selectedIndex] as SearchResult<SymbolData>;
					((EnvDTE.TextSelection)dte2.ActiveDocument.Selection).GotoLine(symbolData.Data.StartLine);

					(this.Parent as QuickFileToolWindow).Close();
				}
				else if (e.Key == System.Windows.Input.Key.Escape)
				{
					(this.Parent as QuickFileToolWindow).Close();
				}
			}
		}

		private async void RefreshResults()
		{
			if (null != mToken)
			{
				mToken.Cancel();
			}
			mToken = new CancellationTokenSource();

			//try
			{
				string sSearch = textBox.Text;
				IEnumerable<SearchResult<SymbolData>> results = mSymbols
					.AsParallel()
					.WithCancellation(mToken.Token)
					.Select(symbolData => new SearchResult<SymbolData>(symbolData, sSearch, symbolData.Symbol))
					.Where(fileData => fileData.SearchScore >= 0)
					.OrderByDescending(fileData => fileData.SearchScore)
					//.Take(250)
					;


				if (!mToken.Token.IsCancellationRequested)
				{
					listView.ItemsSource = results;
				}
			}
			//catch (Exception)
			//{

			//}
		}
	}
}