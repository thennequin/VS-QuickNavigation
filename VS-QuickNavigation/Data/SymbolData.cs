using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace VS_QuickNavigation.Data
{
	public class SymbolData
	{
		[Flags]
		public enum ESymbolType
		{
			Namespace			= 1 << 0,
            Struct              = 1 << 1,
            Class				= 1 << 2,
			Interface			= 1 << 3,
			Macro				= 1 << 4,
			Enumerator			= 1 << 5,
			Enumeration			= 1 << 6,
			Method				= 1 << 7,
			MethodPrototype		= 1 << 8,

			Field				= 1 << 9,
			Property			= 1 << 10,

			//StructureName		= 1024,
			//TypeDef				= 2048,
			//UnionName			= 4096,
		}

		public string Symbol { get; set; }
		public int StartLine { get; set; }
		public string Class { get; set; }
		public string Parameters { get; set; }
		public ESymbolType Type { get; set; }

		public FileData AssociatedFile { get; set; }
		public string AssociatedFilePath
		{
			get
			{
				if( AssociatedFile != null)
				{
					string file = AssociatedFile.File;
					char[] slashChars = { '\\', '/' };
					int pos = file.LastIndexOfAny(slashChars);
					if (pos != -1)
						return file.Substring(pos + 1);
					return file;
				}
				return "";
			}
		}

		public SymbolData(string sSymbol, int iStartLine, ESymbolType eType)
		{
			Symbol = sSymbol;
			StartLine = iStartLine;
			Type = eType;
		}

		public string ImagePath
		{
			get
			{
				return Common.Instance.ExtensionFolder + "/Resources/Symbols/" + Common.Instance.Settings.SymbolsTheme + "/" + Type.ToString() + ".png";
			}
		}
	}
}
