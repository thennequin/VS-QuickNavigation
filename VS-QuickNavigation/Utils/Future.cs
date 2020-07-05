﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VS_QuickNavigation.Utils
{
	public class Future<T>
	{
		T m_oResults;
		bool m_bCompleted;
		ManualResetEvent m_oMre;

		public Action<Future<T>> Callback;

		public Future()
		{
			m_bCompleted = false;
			m_oMre = new ManualResetEvent(false);
		}

		public void SetResults(T oResults)
		{
			m_oResults = oResults;
			m_bCompleted = true;
			m_oMre.Set();
			if (Callback != null)
				Callback(this);
		}

		public void Wait()
		{
			m_oMre.WaitOne();
		}

		public T Result
		{
			get
			{
				return m_oResults;
			}
		}

		public bool IsCompleted
		{
			get
			{
				return m_bCompleted;
			}
		}
	}

	public class FutureList<T> : IEnumerable<Future<T>>
	{
		List<Future<T>> m_lFutures;
		int m_iCompleted;
		Semaphore m_oSemaphore;

		public FutureList()
		{
			m_lFutures = new List<Future<T>>();
			m_iCompleted = 0;
			m_oSemaphore = new Semaphore(0, int.MaxValue);
		}

		public void Add(Future<T> oResult)
		{
			if (oResult != null)
			{
				oResult.Callback = OnFutureCallback;
				m_lFutures.Add(oResult);
			}
		}

		void OnFutureCallback(Future<T> oResult)
		{
			m_iCompleted++;
			m_oSemaphore.Release();
		}

		public List<Future<T>> GetList()
		{
			return m_lFutures;
		}

		public void WaitAny()
		{
			m_oSemaphore.WaitOne();
		}

		public void WaitAll()
		{
			foreach (Future<T> oResult in m_lFutures)
			{
				oResult.Wait();
			}
		}

		public bool IsCompleted
		{
			get
			{
				return m_lFutures.All(f => f.IsCompleted);
			}
		}

		public int Completed
		{
			get
			{
				//return m_lFutures.Count(f => f.IsCompleted);
				return m_iCompleted;
			}
		}

		public IEnumerator<Future<T>> GetEnumerator()
		{
			return m_lFutures.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_lFutures.GetEnumerator();
		}
	}
}
