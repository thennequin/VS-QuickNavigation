using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VS_QuickNavigation.Utils
{
	static public class DispatcherExtensions
	{
		public static void InvokeIfRequired(this Dispatcher disp, Action dotIt, DispatcherPriority priority)
		{
			if (disp.Thread != Thread.CurrentThread)
			{
				disp.Invoke(priority, dotIt);
			}
			else
			{
				dotIt();
			}
		}
	}

	/// <summary>
	/// Provides a threadsafe ObservableCollection of T
	/// </summary>
	public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
	{
		#region Data
		private Dispatcher _dispatcher;
		private ReaderWriterLockSlim _lock;
		#endregion

		#region Ctor
		public ThreadSafeObservableCollection()
		{
			_dispatcher = Dispatcher.CurrentDispatcher;
			_lock = new ReaderWriterLockSlim();
		}
		#endregion


		#region Overrides

		/// <summary>
		/// Clear all items
		/// </summary>
		protected override void ClearItems()
		{
			_dispatcher.InvokeIfRequired(() =>
			{
				_lock.EnterWriteLock();
				try
				{
					base.ClearItems();
				}
				finally
				{
					_lock.ExitWriteLock();
				}
			}, DispatcherPriority.DataBind);
		}

		/// <summary>
		/// Inserts an item
		/// </summary>
		protected override void InsertItem(int index, T item)
		{
			_dispatcher.InvokeIfRequired(() =>
			{
				if (index > this.Count)
					return;

				_lock.EnterWriteLock();
				try
				{
					base.InsertItem(index, item);
				}
				finally
				{
					_lock.ExitWriteLock();
				}
			}, DispatcherPriority.DataBind);
		}

		/// <summary>
		/// Moves an item
		/// </summary>
		protected override void MoveItem(int oldIndex, int newIndex)
		{
			_dispatcher.InvokeIfRequired(() =>
			{
				_lock.EnterReadLock();
				Int32 itemCount = this.Count;
				_lock.ExitReadLock();

				if (oldIndex >= itemCount |
					newIndex >= itemCount |
					oldIndex == newIndex)
					return;

				_lock.EnterWriteLock();
				try
				{
					base.MoveItem(oldIndex, newIndex);
				}
				finally
				{
					_lock.ExitWriteLock();
				}
			}, DispatcherPriority.DataBind);



		}

		/// <summary>
		/// Removes an item
		/// </summary>
		protected override void RemoveItem(int index)
		{

			_dispatcher.InvokeIfRequired(() =>
			{
				if (index >= this.Count)
					return;

				_lock.EnterWriteLock();
				try
				{
					base.RemoveItem(index);
				}
				finally
				{
					_lock.ExitWriteLock();
				}
			}, DispatcherPriority.DataBind);
		}

		/// <summary>
		/// Sets an item
		/// </summary>
		protected override void SetItem(int index, T item)
		{
			_dispatcher.InvokeIfRequired(() =>
			{
				_lock.EnterWriteLock();
				try
				{
					base.SetItem(index, item);
				}
				finally
				{
					_lock.ExitWriteLock();
				}
			}, DispatcherPriority.DataBind);
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Return as a cloned copy of this Collection
		/// </summary>
		public T[] ToSyncArray()
		{
			_lock.EnterReadLock();
			try
			{
				T[] _sync = new T[this.Count];
				this.CopyTo(_sync, 0);
				return _sync;
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}
		#endregion
	}
}
