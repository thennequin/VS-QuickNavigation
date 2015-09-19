using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS_QuickNavigation.Data
{
	class SymbolData
	{
		public string Symbol { get; set; }
		public int StartLine { get; set; }

		public SymbolData(string sSymbol, int iStartLine)
		{
			Symbol = sSymbol;
			StartLine = iStartLine;
		}
	}
}
