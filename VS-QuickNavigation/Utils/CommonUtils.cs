using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace VS_QuickNavigation.Utils
{
	class Description : Attribute
	{
		public string Text;

		public Description(string text)
		{
			Text = text;
		}
	}

	class CommonUtils
	{
		public static T[] ToArray<T>(params T[] objs)
		{
			return objs;
		}

		public static string GetDescription(Enum en)
		{
			Type type = en.GetType();
			System.Reflection.MemberInfo[] memInfo = type.GetMember(en.ToString());

			if (memInfo != null && memInfo.Length > 0)
			{
				object[] attrs = memInfo[0].GetCustomAttributes(typeof(Description), false);

				if (attrs != null && attrs.Length > 0)
					return ((Description)attrs[0]).Text;
			}

			return en.ToString();

		}

		public static bool IsWordCharacter(char c)
		{
			return (c == '_') | char.IsLetterOrDigit(c);
		}

		public static string GetWord(string sLine, int iPos)
		{
			int iMaxEnd = sLine.Length - 1;
			int iStart = iPos;
			int iEnd = iPos;

			while (iStart > 1 && IsWordCharacter(sLine[iStart - 1]))
				iStart--;
			while (iEnd <= iMaxEnd && IsWordCharacter(sLine[iEnd]))
				iEnd++;

			return sLine.Substring(iStart, iEnd - iStart);
		}

		public static bool ContainsWord(string sString, string sWord)
		{
			int iPos = sString.IndexOf(sWord);
			if(iPos != -1)
			{
				if (iPos > 0 && IsWordCharacter(sString[iPos - 1]))
					return false;

				int iEndPos = iPos + sWord.Length;
				if (iEndPos < (sString.Length - 1) && IsWordCharacter(sString[iEndPos + 1]))
					return false;

				return true;
			}
			return false;
		}

		public static bool IsLastWord(string sString, string sWord)
		{
			int iPos = sString.IndexOf(sWord);
			if (iPos != -1)
			{
				if (iPos != (sString.Length - sWord.Length))
					return false;

				if (iPos > 0 && IsWordCharacter(sString[iPos - 1]))
					return false;

				return true;
			}
			return false;
		}

		public static int GetCurrentLine()
		{
			EnvDTE.Document activeDoc = Common.Instance.DTE2.ActiveDocument;
			if (activeDoc != null)
			{
				EnvDTE.TextDocument textDoc = (EnvDTE.TextDocument)Common.Instance.DTE2.ActiveDocument.Object("TextDocument");
				if (textDoc != null)
				{
					return textDoc.Selection.ActivePoint.Line;
				}
			}
			return 0;
		}

		public static string GetCurrentWord()
		{
			EnvDTE.Document activeDoc = Common.Instance.DTE2.ActiveDocument;
			if (activeDoc != null)
			{
				EnvDTE.TextDocument textDoc = (EnvDTE.TextDocument)Common.Instance.DTE2.ActiveDocument.Object("TextDocument");
				if (textDoc != null)
				{
					if (textDoc.Selection.ActivePoint.Line > 0)
					{
						string sLine = textDoc.StartPoint.CreateEditPoint().GetLines(textDoc.Selection.ActivePoint.Line, textDoc.Selection.ActivePoint.Line + 1);
						return GetWord(sLine, textDoc.Selection.ActivePoint.LineCharOffset - 1);
					}
				}
			}
			return null;
		}

		public static bool GotoSymbol(Data.SymbolData symbol)
		{
			if (symbol != null)
			{
				EnvDTE.Window window = Common.Instance.DTE2.ItemOperations.OpenFile(symbol.AssociatedFile.Path, EnvDTE.Constants.vsViewKindTextView);
				if (null != window)
				{
					window.Activate();
					if (window.Document != null)
					{
						((EnvDTE.TextSelection)window.Document.Selection).GotoLine(symbol.StartLine);
						return true;
					}
				}
			}
			return false;
		}

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);
		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		public static bool GetActiveDocumentCursorScreenPos(out System.Windows.Point oPoint, bool bNextLine)
		{
			IVsTextManager oTextMgr = (IVsTextManager)((IServiceProvider)Common.Instance.Package).GetService(typeof(SVsTextManager));
			if(oTextMgr != null)
			{
				IVsTextView oTextViewCurrent;
				oTextMgr.GetActiveView(1, null, out oTextViewCurrent);

				int iLine, iCol;
				oTextViewCurrent.GetCaretPos(out iLine, out iCol);
				if (bNextLine)
					++iLine;

				Microsoft.VisualStudio.OLE.Interop.POINT[] pts = new Microsoft.VisualStudio.OLE.Interop.POINT[1];
				oTextViewCurrent.GetPointOfLineColumn(iLine, iCol, pts);
				RECT oViewRect = new RECT();
				GetWindowRect(oTextViewCurrent.GetWindowHandle(), ref oViewRect);
				
				oPoint = new System.Windows.Point(oViewRect.Left + pts[0].x, oViewRect.Top + pts[0].y);
				return true;
			}

			oPoint = new System.Windows.Point();
			return false;
		}
	}
}
