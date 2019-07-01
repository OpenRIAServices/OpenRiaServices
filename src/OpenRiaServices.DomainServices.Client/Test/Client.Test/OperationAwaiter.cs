using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Threading;

namespace OpenRiaServices.DomainServices.Client
{
    public static class OperationExtensions
    {
        public static OperationAwaiter GetAwaiter(this OperationBase operation)
        {
            return new OperationAwaiter(operation);
        }

        public struct OperationAwaiter : INotifyCompletion
        {
            private readonly OperationBase _operation;
            public OperationAwaiter(OperationBase control)
            {
                _operation = control;
            }
            public bool IsCompleted
            {
                get { return _operation.IsComplete; }
            }

            public void OnCompleted(Action continuation)
            {
                _operation.Completed += (sender, args) => continuation();
            }

            public void GetResult()
            {
                if (!_operation.IsComplete)
                    throw new InvalidOperationException();
            }
        }
    }
}
