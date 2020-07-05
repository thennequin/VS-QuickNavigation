using System;

namespace VS_QuickNavigation.Data
{
	[Flags]
	public enum ESymbolType
	{
		Namespace = 1 << 0,
		Struct = 1 << 1,
		Union = 1 << 2,
		Class = 1 << 3,
		Interface = 1 << 4,
		TypeDef = 1 << 5,
		Macro = 1 << 6,
		Enumerator = 1 << 7,
		Enumeration = 1 << 8,
		Method = 1 << 9,
		[Utils.Description("Method proto")]
		MethodPrototype = 1 << 10,

		Field = 1 << 11,
		Property = 1 << 12,
		Event = 1 << 13,

		Variable = 1 << 14,
		Local = 1 << 15
	}

	static public class ESymbolTypeExtension
	{
		public static string GetImagePath(this ESymbolType eType)
		{
			return Common.Instance.ExtensionFolder + "/Resources/Symbols/" + Common.Instance.Settings.SymbolsTheme + "/" + eType.ToString() + ".png";
		}

		public static string GetDescription(this ESymbolType eType)
		{
			return Utils.CommonUtils.GetDescription(eType);
		}

		public static bool IsContainer(this ESymbolType eType)
		{
			return eType == ESymbolType.Struct
				|| eType == ESymbolType.Union
				|| eType == ESymbolType.Class
				|| eType == ESymbolType.Interface;
		}

		public static System.Windows.Controls.Image GetImage(this ESymbolType eType)
		{
			System.Windows.Controls.Image oImage = new System.Windows.Controls.Image();
			System.Windows.Media.Imaging.BitmapImage oBitmap = new System.Windows.Media.Imaging.BitmapImage();
			oBitmap.BeginInit();
			oBitmap.UriSource = new Uri(eType.GetImagePath());
			oBitmap.EndInit();
			oImage.Stretch = System.Windows.Media.Stretch.Fill;
			oImage.Source = oBitmap;

			return oImage;
		}
	}

	public class SymbolData
	{
		public string Symbol { get; set; }
		public int StartLine { get; set; }
		public int EndLine { get; set; }
		public string Scope { get; set; }
		public string ScopePretty
		{
			get
			{
				string sScope = Scope;

				if (string.IsNullOrWhiteSpace(sScope))
					return "";

				int index = sScope.IndexOf(':');

				if (index != -1)
					sScope = sScope.Substring(index+1);

				if (sScope.Length > 0)
					sScope += "::";

				return sScope;
			}
		}

		public string TypeRef { get; set; }
		public string Access { get; set; }
		public string[] Inherits { get; set; }
		public string Parameters { get; set; }
		public ESymbolType Type { get; set; }
		public string TypeDesc
		{
			get
			{
				return Utils.CommonUtils.GetDescription(Type);
			}
		}

		public FileData AssociatedFile { get; set; }
		public string AssociatedFilename
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
			EndLine = -1;
		}

		public string ImagePath
		{
			get
			{
				return Type.GetImagePath();
			}
		}
	}
}
