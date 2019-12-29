using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using VS_QuickNavigation.Utils;
using System.ComponentModel;
using Microsoft.Win32;

namespace VS_QuickNavigation
{
	[ComVisible(true)]
	[Guid("821F6D15-EB25-4A8C-A3A8-6672BE82790F")]
	public class Settings : Component, IProfileManager
	{
		public Settings()
		{
			ResetSettings();
		}

		public async void Refresh()
		{
			
			//EnvDTE.Properties props = Common.Instance.DTE.get_Properties(@"VSQuickNavigationPackage", "Settings");
			try
			{
                EnvDTE.DTE dte = await Common.Instance.Package.GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                EnvDTE.Properties props = dte.Properties["QuickNavigation", "Settings"];

				string exts;
				//EnvDTE.Property prop = Utils.DteHelper.GetProperty(props, "ListedExtensions");
				EnvDTE.Property prop = props.Item("");
				if (Utils.DteHelper.GetPropertyString(props, "ListedExtensions", out exts))
				{
					ListedExtensionsString = exts;
				}
				else
				{
					ListedExtensions = DefaultListedExtensions;
				}
			}
			catch(Exception)
			{ }
			
		}

		public void SaveSettingsToXml(IVsSettingsWriter writer)
		{
			writer.WriteSettingString("ListedExtensions", ListedExtensionsString);
		}

		public void LoadSettingsFromXml(IVsSettingsReader reader)
		{
			string outValue;
			if (reader.ReadSettingString("ListedExtensions", out outValue) == VSConstants.S_OK)
			{
				ListedExtensionsString = outValue;
			}
		}

		public void SaveSettingsToStorage()
		{
			RegistryKey reg = null;
			try
			{
				using (reg = Common.Instance.Package.UserRegistryRoot.OpenSubKey("VSQuickNavigation", true))
				{
					
					if (reg == null)
					{
						reg = Common.Instance.Package.UserRegistryRoot.CreateSubKey("VSQuickNavigation");
					}
					reg.SetValue("ListedExtensions", ListedExtensionsString);

				}
			}
			catch (Exception)
			{
				//ExceptionReporting.Report(exception);
			}
			finally
			{
				if (reg != null)
				{
					reg.Close();
				}
			}
		}

		public void LoadSettingsFromStorage()
		{
			RegistryKey reg = null;
			try
			{
				using (reg = Common.Instance.Package.UserRegistryRoot.OpenSubKey("VSQuickNavigation"))
				{
					if (reg != null)
					{
						string value = Convert.ToString(reg.GetValue("ListedExtensions", null));
						if (!string.IsNullOrEmpty(value))
						{
							ListedExtensionsString = value;
						}
						else
						{
							ListedExtensions = DefaultListedExtensions;
						}
					}
				}
			}
			catch (Exception)
			{
				ListedExtensions = DefaultListedExtensions;
				//TrayNotifications = true;
				//ExceptionReporting.Report(exception);
			}
			finally
			{
				if (reg != null)
				{
					reg.Close();
				}
			}
		}

		public void ResetSettings()
		{
			ListedExtensions = DefaultListedExtensions;
			MaxFileHistory = 10;
			ParserThreads = 4;
			WatchOnlyVsClipboard = true;
		}

		static public string[] DefaultListedExtensions
		{
			get
			{
				return new string[] {
					".vb",".cs",".resx",".resw",".xsd",".wsdl",".xaml",".xml",".htm",".html",".css",
					".xsd",".aspx",".ascx",".asmx",".svc",".asax",".config",".asp",".asa",".cshtml",".vbhtml",
					".xsl",".xslt",".dtd",
					".c",".cpp",".cxx",".cc",".tli",".tlh",".h",".hh",".hpp",".hxx",".hh",".inl",".rc",".resx",".idl",".asm",".inc"
				};
			}
		}

		/// 

		public string[] ListedExtensions { get; set; }
		public string ListedExtensionsString
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (null != ListedExtensions)
				{
					foreach (String s in ListedExtensions)
					{
						if (sb.Length > 0)
						{
							sb.Append("\n");
						}
						sb.Append(s);
					}
				}
				return sb.ToString();
			}
			set
			{
				ListedExtensions = value.Replace("\r","").Split('\n').Where(ext => !string.IsNullOrEmpty(ext)).ToArray();
			}
		}

		public int MaxFileHistory { get; set; }

		public int ParserThreads { get; set; }

		public bool WatchOnlyVsClipboard { get; set; }

		public string SymbolsTheme
		{
			get
			{
				//return "Default";
				//return "VS2010";
				return "VS2012";
				//return "Eclipse";
				//return "Netbeans";
			}
		}
	}
}
