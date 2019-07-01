using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace OpenRiaServices.DomainServices.Client
{
    public static class TaskExtension
    {
        public static CustomTaskAwaitable ConfigureScheduler(this Task task, TaskScheduler scheduler)
        {
            return new CustomTaskAwaitable(task, scheduler);
        }
    }

    public struct CustomTaskAwaitable
    {
        CustomTaskAwaiter awaitable;

        public CustomTaskAwaitable(Task task, TaskScheduler scheduler)
        {
            awaitable = new CustomTaskAwaiter(task, scheduler);
        }

        public CustomTaskAwaiter GetAwaiter() { return awaitable; }

        public struct CustomTaskAwaiter : INotifyCompletion
        {
            readonly Task _task;
            readonly TaskScheduler _scheduler;

            public CustomTaskAwaiter(Task task, TaskScheduler scheduler)
            {
                this._task = task;
                this._scheduler = scheduler;
            }

            public void OnCompleted(Action continuation)
            {
                // ContinueWith sets the scheduler to use for the continuation action
                _task.ContinueWith((x, state) => ((Action)state)(),
                    (object)continuation,
                    _scheduler);
            }

            public bool IsCompleted { get { return _task.IsCompleted; } }
            public void GetResult() { }
        }
    }
}
