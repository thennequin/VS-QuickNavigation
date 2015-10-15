using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Reflection;

namespace VS_QuickNavigation
{
	public class Common
	{
		private static Common instance;

		private Common()
		{
			ExtensionFolder = Path.GetDirectoryName(System.Uri.UnescapeDataString(
				new System.UriBuilder(Assembly.GetExecutingAssembly().CodeBase).Path));
		}

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

		/*public EnvDTE80.DTE2 DTE2
		{
			get
			{
				return ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as EnvDTE80.DTE2;
			}
		}*/
		public EnvDTE80.DTE2 DTE2 { get; set; }
		public IVsShell Shell { get; set; }
		public IVsSolution Solution { get; set; }

		public SolutionWatcher SolutionWatcher { get; set; }
		public Settings Settings { get; set; }

		public string ExtensionFolder { get; private set; }
		public string DataFolder
		{
			get
			{
				string dataFolder = ExtensionFolder + "\\Data";
				if (!Directory.Exists(dataFolder))
				{
					Directory.CreateDirectory(dataFolder);
				}
				return dataFolder;
			}
		}

		public void ShowOptionsPage()
		{
			Package.ShowOptionPage(typeof(Options.OptionsDialogPage));
		}
	}
}
