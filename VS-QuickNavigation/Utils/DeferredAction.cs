using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace VS_QuickNavigation.Utils
{
	/// <summary>
	/// Represents a timer which performs an action on the UI thread when time elapses.  Rescheduling is supported.
	/// </summary>
	public class DeferredAction : IDisposable
	{
		private Timer timer;

		/// <summary>
		/// Creates a new DeferredAction.
		/// </summary>
		/// <param name="action">
		/// The action that will be deferred.  It is not performed until after <see cref="Defer"/> is called.
		/// </param>
		public static DeferredAction Create(Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			return new DeferredAction(action);
		}

		private DeferredAction(Action action)
		{
			this.timer = new Timer(new TimerCallback(delegate
			{
				Application.Current.Dispatcher.Invoke(action);
			}));
		}

		/// <summary>
		/// Defers performing the action until after time elapses.  Repeated calls will reschedule the action
		/// if it has not already been performed.
		/// </summary>
		/// <param name="milliseconds">
		/// The amount of time to wait before performing the action.
		/// </param>
		public void Defer(int milliseconds)
		{
			// Fire action when time elapses (with no subsequent calls).
			this.timer.Change(TimeSpan.FromMilliseconds(milliseconds), TimeSpan.FromMilliseconds(-1));
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (this.timer != null)
			{
				this.timer.Dispose();
				this.timer = null;
			}
		}

		#endregion
	}
}
