using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace ProducerConsumerTest
{
    class Connection
    {
        private BlockingCollection<AsyncResponse> queue = new BlockingCollection<AsyncResponse>();
        private CancellationToken cancelToken = new CancellationToken();
        private Func<Command, Response> _func = null;

        public bool Running { get; private set; }
      
        public Connection()
        {
            Task.Factory.StartNew(() => DoWork(cancelToken), TaskCreationOptions.LongRunning);
        }
        public void Dispose()
        {
            Running = false;

            while (queue.Count > 0) //loop until queue is empty
            {
                var data = queue.Take();
                data.Dispose();
            }
        }

        public void SetFunc(Func<Command, Response> func)
        {
            _func = func;
        }

        public Response SendCommand(Command cmd)
        {
            var Reply = new AsyncResponse(cmd);
            queue.Add(Reply);

            return Reply.GetReturn();
        }
        private void DoWork(CancellationToken token)
        {
           try
           {
                Running = true;

                while(Running == true)
                {
                    var cmd = queue.Take(token);

                    //----- do work here ----
                    //-----------------------

                    Response reply = _func(cmd.cmd);
                    cmd.SetResponse(reply);
                }
           }
            catch(Exception e)
           {
               this.Dispose();
           }        
        }


        private class AsyncResponse
        {
            private Response _returnData = null;
            private ManualResetEvent _waithandle = new ManualResetEvent(false);

            public Command cmd { get; private set; }

            public AsyncResponse(Command cmd)
            {
                _waithandle.Reset();
                this.cmd = cmd;
            }
            public void SetResponse(Response Reply)
            {
                _returnData = Reply;
                _waithandle.Set();
            }

            public void Dispose()
            {
                _waithandle.Set();
            }

            public Response GetReturn()
            {
                _waithandle.WaitOne();
                return _returnData;
            }
        }


    }

    class Command
    {
        private void test()
        {
            Connection con = new Connection();
            con.SetFunc(DoWork);

        }

        private Response DoWork(Command cmd)
        {
            return new Response();
        }
    }

    class Response
    {

    }




}
