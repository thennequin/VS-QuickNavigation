using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS_QuickNavigation.Utils
{
	public static class DteHelper
	{
		public static bool GetPropertyString(EnvDTE.Properties properties, string propertyName, out string sValueOut)
		{
			if (properties != null)
			{
				foreach (EnvDTE.Property item in properties)
				{
					if (item != null && item.Name == propertyName)
					{
						sValueOut = (string)item.Value;
						return true;
					}
				}
			}
			sValueOut = null;
			return false;
		}

		public static EnvDTE.Property GetProperty(EnvDTE.Properties properties, string propertyName)
		{
			if (properties != null)
			{
				foreach (EnvDTE.Property item in properties)
				{
					if (item != null && item.Name == propertyName)
					{
						return item;
					}
				}
			}
			return null;
		}
	}
}
