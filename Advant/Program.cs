using System.Linq;
using System.Threading;
using System.Collections.Generic;

namespace Advant
{
    class Program
    {
        static void Main(string[] args)
        {
            int chunkSize = 3;
            List<int> l = new List<int>();
            l.Add(1);
            l.Add(2);
            l.Add(3);
            l.Add(4);
            l.Add(5);
            l.Add(6);
            l.Add(7);


            var t = l.Select((x, i) => new { Index = i, Value = x })
            .GroupBy(x => x.Index / chunkSize)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();




            var _job = new Job();
            int milissInMinute = 60000;

            var dataInput = _job.ParseInputFile();

            int timeSleep = milissInMinute * dataInput.TimeSleep;

            while (true)
            {
                _job.Run(dataInput).ConfigureAwait(false);
               // task.Start();

                //Task.Factory.StartNew(() => _job.Run(dataInput));

                Thread.Sleep(timeSleep);
            }
        }
    }
}
