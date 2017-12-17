using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

	class CommonUtils
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
	}
}
