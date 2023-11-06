﻿
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using VS_QuickNavigation.Utils;

namespace VS_QuickNavigation
{
	internal sealed class QuickPasteCommand
	{
		public const int CommandId = 0x0106;
		public static readonly Guid CommandSet = new Guid("ad64a987-3060-494b-94c1-07bab75f9da3");

		private readonly Package package;

		class ShortkeyContextMenu : ContextMenu
		{
			public ShortkeyContextMenu()
			{
			}

			int Modulo(int iValue, int iModulo)
			{
				while (iValue < 0)
					iValue += iModulo;

				return iValue % iModulo;
			}

			protected override void OnKeyDown(KeyEventArgs e)
			{
				if (e.Key >= Key.D0 && e.Key <= Key.D9)
				{
					int iIndex = Modulo((e.Key - Key.D0) - 1, 10);
					if (iIndex < Items.Count && Items[iIndex] != null && Items[iIndex] is MenuItem)
					{
						QuickPasteCommand.PasteMenuItem_Click((Items[iIndex] as MenuItem), null);
						IsOpen = false;
					}
				}

				if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
				{
					int iIndex = Modulo((e.Key - Key.NumPad0) - 1, 10);
					if (iIndex < Items.Count && Items[iIndex] != null && Items[iIndex] is MenuItem)
					{
						QuickPasteCommand.PasteMenuItem_Click((Items[iIndex] as MenuItem), null);
						IsOpen = false;
					}
				}
				base.OnKeyDown(e);
			}
		};

		private ContextMenu m_oContextMenu;

		List<string> m_oCopyHistory = new List<string>();

		ClipboardForm m_oClipboardForm;

		class ClipboardForm : System.Windows.Forms.Form
		{
			[DllImport("User32.dll")]
			protected static extern int SetClipboardViewer(int hWndNewViewer);

			[DllImport("User32.dll", CharSet = CharSet.Auto)]
			public static extern bool ChangeClipboardChain(IntPtr hWndRemove, IntPtr hWndNewNext);

			[DllImport("user32.dll", CharSet = CharSet.Auto)]
			public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll")]
			static extern IntPtr GetActiveWindow();

			[DllImport("user32.dll")]
			public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

			// defined in winuser.h
			const int WM_DRAWCLIPBOARD = 0x308;
			const int WM_CHANGECBCHAIN = 0x030D;

			IntPtr m_hNextClipboardViewer;

			List<string> m_oCopyHistory;

			public ClipboardForm(List<string> oCopyHistory)
			{
				m_oCopyHistory = oCopyHistory;
				m_hNextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
			}

			protected override void WndProc(ref System.Windows.Forms.Message m)
			{
				switch (m.Msg)
				{
					case WM_DRAWCLIPBOARD:
						try
						{
							bool bActive = !Common.Instance.Settings.WatchOnlyVsClipboard;
							if (bActive == false)
							{
								IntPtr activeWindow = GetActiveWindow();
								uint activeProcess;
								GetWindowThreadProcessId(activeWindow, out activeProcess);
								bActive = System.Diagnostics.Process.GetCurrentProcess().Id == (int)activeProcess;
							}

							if (bActive && System.Windows.Clipboard.ContainsText())
							{
								string sText = System.Windows.Clipboard.GetText();
								if (string.IsNullOrEmpty(sText) == false)
								{
									while (m_oCopyHistory.Contains(sText))
										m_oCopyHistory.Remove(sText);

									m_oCopyHistory.Insert(0, sText);
									while (m_oCopyHistory.Count > 10)
									{
										m_oCopyHistory.RemoveAt(m_oCopyHistory.Count - 1);
									}
								}
							}
						}
						catch (Exception) { }

						if (m_hNextClipboardViewer.ToInt64() != 0)
						{
							SendMessage(m_hNextClipboardViewer, m.Msg, m.WParam, m.LParam);
						}
						break;

					case WM_CHANGECBCHAIN:
						if (m.WParam == m_hNextClipboardViewer)
						{
							m_hNextClipboardViewer = m.LParam;
						}
						else if (m_hNextClipboardViewer.ToInt64() != 0)
						{
							SendMessage(m_hNextClipboardViewer, m.Msg, m.WParam, m.LParam);
						}
						break;

					default:
						base.WndProc(ref m);
						break;
				}
			}

			public void PreDispose()
			{
				ChangeClipboardChain(this.Handle, m_hNextClipboardViewer);
			}
		}


		private QuickPasteCommand(Package package)
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
				var menuItem = new MenuCommand(this.OpenPasteMenu, menuCommandID);
				commandService.AddCommand(menuItem);
			}

			m_oContextMenu = new ShortkeyContextMenu();

			m_oClipboardForm = new ClipboardForm(m_oCopyHistory);
		}

		public static QuickPasteCommand Instance
		{
			get;
			private set;
		}

		public static void Initialize(Package package)
		{
			Instance = new QuickPasteCommand(package);
		}

		public static void Dispose(Package package)
		{
			Instance.m_oClipboardForm.PreDispose();
			Instance.m_oClipboardForm.Dispose();
		}

		private void OpenPasteMenu(object sender, EventArgs e)
		{
			System.Windows.Point oPoint;
			if (CommonUtils.GetActiveDocumentCursorScreenPos(out oPoint, true))
			{
				m_oContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Absolute;
				m_oContextMenu.PlacementRectangle = new System.Windows.Rect(oPoint, oPoint);

				m_oContextMenu.Items.Clear();

				int iCurrent = 0;
				foreach (string sText in m_oCopyHistory)
				{
					MenuItem oItem = new MenuItem();
					oItem.Tag = sText;
					string sTrimText = sText.Trim();
					bool bAddDots = false;
					int iNewLinePos = sTrimText.IndexOf('\n');
					if (iNewLinePos != -1)
					{
						sTrimText = sTrimText.Substring(0, iNewLinePos);
						bAddDots = true;
					}
					if (sTrimText.Length > 60)
					{
						sTrimText = sTrimText.Substring(0, 60);
						bAddDots = true;
					}
					if (bAddDots)
					{
						sTrimText += "...";
					}
					TextBlock oTextBlock = new TextBlock();
					oTextBlock.Inlines.Add(new Run(sTrimText));
					oItem.Header = oTextBlock;
					oItem.Click += PasteMenuItem_Click;
					if (iCurrent < 10)
					{
						oItem.InputGestureText = ((iCurrent + 1) % 10).ToString();
					}
					m_oContextMenu.Items.Add(oItem);
					++iCurrent;
				}

				m_oContextMenu.IsOpen = true;
			}
		}

		static void PasteMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				MenuItem oItem = sender as MenuItem;
				if(oItem.Tag is string)
				{
					string copy = oItem.Tag as string;
					Instance.m_oCopyHistory.Remove(copy);
					Instance.m_oCopyHistory.Insert(0, copy);

					try
					{
						System.Windows.Clipboard.SetDataObject(copy);

						if (Common.Instance.DTE2.ActiveDocument != null)
						{
							if (Common.Instance.DTE2.ActiveDocument.Selection is EnvDTE.TextSelection)
							{
								((EnvDTE.TextSelection)Common.Instance.DTE2.ActiveDocument.Selection).Paste();
							}
						}
					}
					catch(Exception)
					{
						System.Threading.Thread.Sleep(50);
					}
				}
			}
		}
	}
}