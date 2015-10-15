using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS_QuickNavigation.Data
{
	class HistoryList<T> : IEnumerable<T>
	{
		List<T> mHistory;
		int mMaxHistory;

		public int MaxHistory
		{
			get
			{
				return mMaxHistory;
			}
			set
			{
				if (value <= 0)
				{
					throw new ArgumentException("MaxHistory need to be superior to 1");
				}
				mMaxHistory = value;
			}
		}

		public HistoryList( int iMaxHistory = 10 )
		{
			mHistory = new List<T>();
			mMaxHistory = iMaxHistory;
		}

		public void Push(T obj)
		{
			if (mHistory.Contains(obj))
			{
				mHistory.Remove(obj);
			}
			mHistory.Add(obj);

			Trim();
		}

		public List<T> GetList()
		{
			return mHistory;
		}

		public void Trim()
		{
			while (mHistory.Count > mMaxHistory)
			{
				mHistory.RemoveAt(mHistory.Count - 1);
			}
		}

		public int IndexOf(T obj)
		{
			return mHistory.IndexOf(obj);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return mHistory.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return mHistory.GetEnumerator();
		}
	}
}
