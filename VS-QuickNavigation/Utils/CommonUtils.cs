using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Documents;

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

	public struct WordRef
	{
		public int Line { get; set; }
		public int Offset { get; set; }
        public int Length { get; set; }
        public String LineString { get; set; }

        TextBlock oLineFormatted;
		public TextBlock LineFormatted
		{
			get
			{
				if (oLineFormatted == null)
				{
					oLineFormatted = new TextBlock();

					string sTrimLineString = LineString.TrimStart(' ', '\t');
					int iOffset = LineString.Length - sTrimLineString.Length;

					if (Offset > 0)
					{
						Run oRun = new Run(sTrimLineString.Substring(0, Offset - iOffset));
						oRun.Tag = "N";
						oLineFormatted.Inlines.Add(oRun);
					}

					{
						Run oRun = new Run(sTrimLineString.Substring(Offset - iOffset, Length));
						oRun.Tag = "NHL";
						oRun.FontWeight = System.Windows.FontWeights.Bold;
						oLineFormatted.Inlines.Add(oRun);
					}

					if ((Offset + Length) < LineString.Length)
					{
						Run oRun = new Run(sTrimLineString.Substring(Offset - iOffset + Length));
						oRun.Tag = "N";
						oLineFormatted.Inlines.Add(oRun);
					}
				}

				return oLineFormatted;
			}
		}
	}

	static class CommonUtils
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
		public static bool IsFinalCharacter(char c)
		{
			char[] c_FinalChars = { ' ', '\t', '(', '*', '&', '=', ';', '<', '>', '-', '+', '/', '*', '~', '[', '^', '.' };
			return c_FinalChars.Contains(c);
		}
		public static bool IsWordCharacter(char c)
		{
			return (c == '_') || char.IsLetterOrDigit(c) || (c == '_');
		}

		public static string GetWord(string sLine, int iPos)
		{
			int iMaxEnd = sLine.Length - 1;
			int iStart = iPos;
			int iEnd = iPos;

			while (iStart > 0 && IsWordCharacter(sLine[iStart - 1]))
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

		public static IEnumerable<WordRef> FindWord(string[] sLines, string sWord)
		{
			for (int iLine = 0; iLine < sLines.Length; ++iLine)
			{
				int iStartOffset = 0;
				string sLine = sLines[iLine];
				//sLine.Replace("\r", "");
				string sFullLine = sLine;

				int iPos;
				while ((iPos = sLine.IndexOf(sWord)) != -1)
				{
					bool bOk = true;
					if (iPos > 0 && IsWordCharacter(sLine[iPos - 1]))
						bOk = false;

					if (iPos > 0 && IsWordCharacter(sLine[iPos - 1]))
						bOk = false;

					if (bOk)
					{
						yield return new WordRef { Line = iLine + 1, Offset = iStartOffset + iPos, Length = sWord.Length, LineString = sFullLine };
					}

					int iOffset = iPos + sWord.Length;
					iStartOffset += iOffset;
					sLine = sLine.Substring(iOffset);
				}
			}
		}

		public static IEnumerable<WordRef> FindLastWord(string[] sLines, string sWord)
		{
			for (int iLine = 0; iLine < sLines.Length; ++iLine)
			{
				int iStartOffset = 0;
				string sLine = sLines[iLine];
				//sLine.Replace("\r", "");
				string sFullLine = sLine;
				int iWordLen = sWord.Length;

				int iPos;
				while ((iPos = sLine.IndexOf(sWord)) != -1)
				{
					bool bOk = true;
					if (iPos > 0 && IsWordCharacter(sLine[iPos - 1]))
						bOk = false;

					if ((iPos + iWordLen) < sLine.Length && 
						(IsWordCharacter(sLine[iPos + iWordLen]) || !IsFinalCharacter(sLine[iPos + iWordLen])))
						bOk = false;

					if (bOk)
					{
						yield return new WordRef { Line = iLine + 1, Offset = iStartOffset + iPos, Length = sWord.Length, LineString = sFullLine };
					}

					int iOffset = iPos + sWord.Length;
					iStartOffset += iOffset;
					sLine = sLine.Substring(iOffset);
				}
			}
		}

		public static int GetCurrentLine()
		{
			EnvDTE.Document activeDoc = Common.Instance.DTE2.ActiveDocument;
			if (activeDoc != null)
			{
				EnvDTE.TextDocument textDoc = (EnvDTE.TextDocument)Common.Instance.DTE2.ActiveDocument.Object("TextDocument");
				if (textDoc != null && textDoc.Selection != null && textDoc.Selection.ActivePoint != null)
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
				if (textDoc != null && textDoc.Selection != null && textDoc.Selection.ActivePoint != null)
				{
					if (textDoc.Selection.ActivePoint.Line > 0 )
					{
						string sLine = textDoc.StartPoint.CreateEditPoint().GetLines(textDoc.Selection.ActivePoint.Line, textDoc.Selection.ActivePoint.Line + 1);
						return GetWord(sLine, textDoc.Selection.ActivePoint.LineCharOffset - 1);
					}
				}
			}
			return null;
		}

		public static bool GotoLine(string sFilePath, int iLine)
		{
			if (sFilePath != null)
			{
				EnvDTE.Window window = Common.Instance.DTE2.ItemOperations.OpenFile(sFilePath, EnvDTE.Constants.vsViewKindTextView);
				if (null != window)
				{
					window.Activate();
					if (window.Document != null)
					{
						((EnvDTE.TextSelection)window.Document.Selection).GotoLine(iLine);
						return true;
					}
				}
			}
			return false;
		}

		public static bool GotoSymbol(Data.SymbolData symbol)
		{
			if (symbol != null)
			{
				GotoLine(symbol.AssociatedFile.Path, symbol.StartLine);
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
			IVsTextManager oTextMgr = Common.Instance.GetServiceAs<SVsTextManager, IVsTextManager>();
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
