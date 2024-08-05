using System;
using System.Collections.Generic;
using System.Text;
using VirtualMachine.Processor;

namespace VirtualMachine.TypeDefs.Processor
{
    public class NullTracer<T> : ITracer<T>
    {
        public static NullTracer<T> Instance { get; } = new NullTracer<T>();
        public void Trace(IVirtualMachine<T> vm)
        {
        }
    }

    public class NullTimer<T> : ITimer<T>
    {
        public static NullTimer<T> Instance { get; } = new NullTimer<T>();
        public T Resource { get; } = default(T);

        public void Dispose()
        {
        }

        public void Reset()
        {
        }

        public void Restart()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}
