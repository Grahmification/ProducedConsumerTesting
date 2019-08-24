using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ProducerConsumerTest
{
    class ThreadSafeFunction<Arg, Ret>
    {       
        private BlockingCollection<AsyncResponse<Arg, Ret>> queue = new BlockingCollection<AsyncResponse<Arg, Ret>>();
        private Func<Arg, Ret> _func = null;

        private CancellationTokenSource _cancelToken = new CancellationTokenSource();
 
        public bool Running { get; private set; }

        public ThreadSafeFunction()
        {
            
        }

        public void Initialize(Func<Arg, Ret> func)
        {
            _func = func;
            Task.Factory.StartNew(() => DoWork(_cancelToken.Token), TaskCreationOptions.LongRunning);
        }

        public void Dispose()
        {
            Running = false;
            _cancelToken.Cancel();

            while (queue.Count > 0) //loop until queue is empty
            {
                var data = queue.Take();
                data.Dispose();
            }
        }

        public Ret Run(Arg Parameter)
        {
            var data = new AsyncResponse<Arg, Ret>(Parameter);
            queue.Add(data);

            return data.GetReturn();
        }
        private void DoWork(CancellationToken token)
        {
            try
            {
                Running = true;

                while (Running == true)
                {
                    var data = queue.Take(token);

                    Ret reply = _func(data.input);
                    data.SetReturnValue(reply);
                }
            }
            catch (Exception e)
            {
                this.Dispose();
            }
        }


        private class AsyncResponse<Arg, Ret>
        {
            private Ret _returnData;
            private ManualResetEvent _waithandle = new ManualResetEvent(false);

            public Arg input { get; private set; }

            public AsyncResponse(Arg input)
            {
                _waithandle.Reset();
                this.input = input;
            }
            public void Dispose()
            {
                _waithandle.Set();
            }

            public void SetReturnValue(Ret ReturnValue)
            {
                _returnData = ReturnValue;
                _waithandle.Set();
            }
            public Ret GetReturn()
            {
                _waithandle.WaitOne();
                return _returnData;
            }
        }

    }



    public class Tester
    {
        private ThreadSafeFunction<int[], int> TSFunction = new ThreadSafeFunction<int[],int>();

        public Tester()
        {
            
        }
        public int calculate()
        {
            TSFunction.Initialize(Calculate);

            Task.Factory.StartNew(() => doWork(), TaskCreationOptions.LongRunning);

            var val1 = TSFunction.Run(new int[] { 1, 1, 2, 3, 4 });
            var val2 = TSFunction.Run(new int[] { 1, 1, 2, 3, 4 });

            TSFunction.Dispose();

            return val2;
        }
        public void doWork()
        {
            var val1 = TSFunction.Run(new int[] { 1, 1, 2, 3, 4 });
            var val2 = TSFunction.Run(new int[] { 1, 1, 2, 3, 4 });
        }


        private int Calculate(int[] input)
        {
            int sum = 0;

            foreach (int number in input)
            {
                sum += number;
                Thread.Sleep(200);
            }

            return sum;
        }
    }

}
