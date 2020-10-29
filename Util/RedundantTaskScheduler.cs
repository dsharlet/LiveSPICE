using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Util
{
    /// <summary>
    /// Provides a task scheduler that ensures a maximum concurrency level while
    /// running on top of the ThreadPool.
    /// </summary>
    public class RedundantTaskScheduler : TaskScheduler
    {
        /// <summary>The list of tasks to be executed.</summary>
        private readonly List<Task> tasks = new List<Task>(); // protected by lock(_tasks)
        /// <summary>The maximum concurrency level allowed by this scheduler.</summary>
        private readonly int maxThreads;
        /// <summary>Whether the scheduler is currently processing work items.</summary>
        private int running = 0;

        /// <summary>
        /// Initializes an instance of the LimitedConcurrencyLevelTaskScheduler class with the
        /// specified degree of parallelism.
        /// </summary>
        /// <param name="MaxThreads">The maximum degree of parallelism provided by this scheduler.</param>
        public RedundantTaskScheduler(int MaxThreads)
        {
            if (MaxThreads < 1) throw new ArgumentOutOfRangeException("MaxThreads");
            maxThreads = MaxThreads;
        }

        /// <summary>Queues a task to the scheduler.</summary>
        /// <param name="task">The task to be queued.</param>
        protected sealed override void QueueTask(Task task)
        {
            lock (tasks)
            {
                tasks.Add(task);
                if (running < maxThreads)
                {
                    Interlocked.Increment(ref running);
                    ThreadPool.UnsafeQueueUserWorkItem((x) =>
                    {
                        while (true)
                        {
                            Task item;
                            lock (tasks)
                            {
                                item = tasks.LastOrDefault();
                                tasks.Clear();
                            }

                            if (item != null)
                                base.TryExecuteTask(item);
                            else
                                break;
                        }
                        Interlocked.Decrement(ref running);
                    }, null);
                }
            }
        }

        /// <summary>Attempts to execute the specified task on the current thread.</summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns>Whether the task could be executed on the current thread.</returns>
        protected sealed override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <summary>Attempts to remove a previously scheduled task from the scheduler.</summary>
        /// <param name="task">The task to be removed.</param>
        /// <returns>Whether the task could be found and removed.</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (tasks) return tasks.Remove(task);
        }

        /// <summary>Gets the maximum concurrency level supported by this scheduler.</summary>
        public sealed override int MaximumConcurrencyLevel { get { return maxThreads; } }

        /// <summary>Gets an enumerable of the tasks currently scheduled on this scheduler.</summary>
        /// <returns>An enumerable of the tasks currently scheduled.</returns>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(tasks, ref lockTaken);
                if (lockTaken) return tasks.ToArray();
                else throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken) Monitor.Exit(tasks);
            }
        }
    }
}
