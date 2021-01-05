using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace WebGISSocket
{
    class TCPClient
    {
        public TcpListener myTcpListener;
        public TcpClient tcp;

        [Obsolete]
        public TCPClient()
        {
            //Connect();
            Listen();
        }

        [Obsolete]
        public void Listen()
        {
            myTcpListener = new TcpListener(1000);
            myTcpListener.Start();      //开始侦听传入的连接请求 
            while (true)
            {
                while (!myTcpListener.Pending())
                {
                    Thread.Sleep(1000);
                }

                // 创建线程来处理
                GISThreadHandler myWorker = new GISThreadHandler
                {
                    myTcplistener = this.myTcpListener
                };
                Thread myWorkerthread = new Thread(new ThreadStart(myWorker.HandleThread));
                myWorkerthread.Start();
            }
        }
    }
}
