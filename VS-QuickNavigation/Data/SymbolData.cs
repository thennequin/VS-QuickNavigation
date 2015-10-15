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
		public enum ESymbolType
		{
			ClassName			= 1,
			Define				= 2,
			Enumerator			= 4,
			Method				= 8,
			EnumerationName		= 16,
			Member				= 32,
			FunctionPrototype	= 64,
			StructureName		= 128,
			TypeDef				= 256,
			UnionName			= 512,
			Variable			= 1024
		}

		public string Symbol { get; set; }
		public int StartLine { get; set; }
		public string Class { get; set; }
		public string Parameters { get; set; }
		public ESymbolType Type { get; set; }

		public SymbolData(string sSymbol, int iStartLine, ESymbolType eType)
		{
			Symbol = sSymbol;
			StartLine = iStartLine;
			Type = eType;
		}

		static Dictionary<ESymbolType, BitmapImage> sIconMap = new Dictionary<ESymbolType, BitmapImage>();
		public BitmapImage Icon
		{
			get
			{
				if (!sIconMap.ContainsKey(Type))
				{
					sIconMap.Add(Type, new BitmapImage(new Uri(Common.Instance.ExtensionFolder + "/Resources/Symbols/" + Type.ToString() + ".png", UriKind.Relative)));
				}

				return sIconMap[Type];
			}
		}

		public string IconString
		{
			get
			{
				return Common.Instance.ExtensionFolder + "/Resources/Symbols/" + Type.ToString() + ".png";
            }
		}
	}
}
