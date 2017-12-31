
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

			IntPtr nextClipboardViewer;

			List<string> m_oCopyHistory;

			public ClipboardForm(List<string> oCopyHistory)
			{
				m_oCopyHistory = oCopyHistory;
				nextClipboardViewer = (IntPtr)SetClipboardViewer((int)this.Handle);
			}

			protected override void WndProc(ref System.Windows.Forms.Message m)
			{
				// defined in winuser.h
				const int WM_DRAWCLIPBOARD = 0x308;
				const int WM_CHANGECBCHAIN = 0x030D;

				switch (m.Msg)
				{
					case WM_DRAWCLIPBOARD:
						//DisplayClipboardData();
						bool bActive = false;

						IntPtr activeWindow = GetActiveWindow();
						uint activeProcess;
						GetWindowThreadProcessId(activeWindow, out activeProcess);

						bActive = System.Diagnostics.Process.GetCurrentProcess().Id == (int)activeProcess;

						if (bActive && System.Windows.Clipboard.ContainsText())
						{
							string sText = System.Windows.Clipboard.GetText();

							while (m_oCopyHistory.Contains(sText))
								m_oCopyHistory.Remove(sText);

							m_oCopyHistory.Insert(0, sText);
							while (m_oCopyHistory.Count > 10)
							{
								m_oCopyHistory.RemoveAt(m_oCopyHistory.Count - 1);
							}
						}
						SendMessage(nextClipboardViewer, m.Msg, m.WParam,
									m.LParam);
						break;

					case WM_CHANGECBCHAIN:
						if (m.WParam == nextClipboardViewer)
							nextClipboardViewer = m.LParam;
						else
							SendMessage(nextClipboardViewer, m.Msg, m.WParam,
										m.LParam);
						break;

					default:
						base.WndProc(ref m);
						break;
				}
			}

			protected override void Dispose(bool disposing)
			{
				ChangeClipboardChain(this.Handle, nextClipboardViewer);
			}
		}


		private QuickPasteCommand(Package package)
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
				var menuItem = new MenuCommand(this.OpenPasteMenu, menuCommandID);
				commandService.AddCommand(menuItem);
			}

			m_oContextMenu = new ContextMenu();

			m_oClipboardForm = new ClipboardForm(m_oCopyHistory);
		}

		public static QuickPasteCommand Instance
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
			Instance = new QuickPasteCommand(package);
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
					if (sTrimText.Length > 60)
						sTrimText = sTrimText.Substring(0, 60) + "...";
					TextBlock oTextBlock = new TextBlock();
					oTextBlock.Inlines.Add(new Run(sTrimText));
					oItem.Header = oTextBlock;
					oItem.Click += PasteMenuItem_Click;
					m_oContextMenu.Items.Add(oItem);
					++iCurrent;
				}

				m_oContextMenu.IsOpen = true;
			}
		}

		private void PasteMenuItem_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			if (sender is MenuItem)
			{
				MenuItem oItem = sender as MenuItem;
				if(oItem.Tag is string)
				{
					string copy = oItem.Tag as string;
					m_oCopyHistory.Remove(copy);
					m_oCopyHistory.Insert(0, copy);
					System.Windows.Clipboard.SetText(copy);

					if (Common.Instance.DTE2.ActiveDocument != null)
					{
						if (Common.Instance.DTE2.ActiveDocument.Selection is EnvDTE.TextSelection)
						{
							((EnvDTE.TextSelection)Common.Instance.DTE2.ActiveDocument.Selection).Paste();
						}
					}
				}
			}
		}
	}
}