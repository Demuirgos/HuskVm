using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualMachine.Processor;

namespace VirtualMachine.iLang.Extras
{
    internal class Timer<T> : ITimer<Stopwatch>
    {
        public List<TimeSpan> Logs { get; set; } = new List<TimeSpan>();
        public Stopwatch Resource { get; } = new Stopwatch();

        public void Clear()
        {
            Logs.Clear();
        }

        public void Dispose()
        {
            Resource.Stop();
        }

        public void Reset()
        {
            Resource.Reset();
        }

        public void Restart()
        {
            Resource.Restart();
        }
        public void Start()
        {
            Resource.Start();
        }

        public void Stop()
        {
            Resource.Stop();
            Logs.Add(Resource.Elapsed);
        }
    }
}
