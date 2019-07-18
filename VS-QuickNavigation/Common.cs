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

		public EnvDTE80.DTE2 DTE2 { get; set; }
		public IVsShell Shell { get; set; }
		public IVsSolution Solution { get; set; }

        public async System.Threading.Tasks.Task<T> GetServiceAsync<T>()
        {
            object result = await Microsoft.VisualStudio.Shell.AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(T));
            T service = (T) result;
            if (service == null)
            {
                result = await Package.GetServiceAsync(typeof(T));
                service = (T) result;
            }
            return service;
        }

        public T GetService<T>()
		{
            T service = (T) GetServiceAsync<T>().Result;
			return service;
		}

		public R GetServiceAs<T, R>()
		{
            object serviceT = GetServiceAsync<T>().Result;
            R service = (R)serviceT;
			return service;
		}

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
