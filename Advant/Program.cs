using System;
using System.Threading;
using System.Threading.Tasks;

namespace Advant
{
    class Program
    {
        static void Main(string[] args)
        {
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
