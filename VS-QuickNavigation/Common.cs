using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS_QuickNavigation
{
	public class Common
	{
		private static Common instance;

		private Common() { }

		public static Common Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new Common();
				}
				return instance;
			}
		}

		public VSQuickNavigationPackage Package { get; set; }

		public IVsShell Shell { get; set; }

		public IVsSolution Solution { get; set; }

		public SolutionWatcher SolutionWatcher { get; set; }

		//public ObservableCollection<string> Messages { get; set; } = new ObservableCollection<string>();
		//public uint SolutionCookie { get; set; }
	}
}
