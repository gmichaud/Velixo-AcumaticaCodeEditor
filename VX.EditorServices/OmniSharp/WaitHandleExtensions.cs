using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VX.EditorServices.OmniSharp
{
    //Source: http://bdurrani.github.io/blog/2015/07/07/Async-Await-and-WaitHandles.html
    //Modified to work as extension method
    static class WaitHandleExtensions
    {
        /// <summary>
        /// Create a task based on the wait handle, which is the base class
        /// for quite a few synchronization primitives
        /// </summary>
        /// <param name="handle">The wait handle</param>
        /// <returns>A valid task</returns>
        public static Task WaitHandleAsTask(this WaitHandle handle)
        {
            return WaitHandleAsTask(handle, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Create a task based on the wait handle, which is the base class
        /// for quite a few synchronization primitives
        /// </summary>
        /// <param name="handle">The wait handle</param>
        /// <param name="timeout">The timeout</param>
        /// <returns>A valid task</returns>
        public static Task WaitHandleAsTask(this WaitHandle handle, TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<object>();
            var registration = ThreadPool.RegisterWaitForSingleObject(handle, (state, timedOut) =>
            {
                var localTcs = (TaskCompletionSource<object>)state;
                if (timedOut)
                {
                    localTcs.SetCanceled();
                }
                else
                {
                    localTcs.SetResult(null);
                }
            }, tcs, timeout, true);
            
            // clean up the RegisterWaitHandle
            tcs.Task.ContinueWith((_, state) => ((RegisteredWaitHandle)state).Unregister(null), registration, TaskScheduler.Default);
            return tcs.Task;
        }
    }
}
