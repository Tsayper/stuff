using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UdpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 50001;
            var serv = new UdpServer(port, 
                //handler function takes byte[] returns byte[]
                x => Encoding.Default.GetBytes("hello ").Concat(x).ToArray());

            //emulate requests
            do
            {
                var data = Encoding.Default.GetBytes("mike");
                var answ = UdpServer.Call(data, IPAddress.Loopback, port);
                Console.WriteLine(Encoding.Default.GetString(answ));    //hello mike
            }
            while (Console.ReadKey().Key != ConsoleKey.Escape);
        }
    }

    class UdpServer : IDisposable
    {
        readonly Socket serv;
        public event Action<byte[]> OnReceive;
        public event Action<Exception> OnError;
        public UdpServer(int port, Func<byte[], byte[]> handle, int bufferSize = 1024)
        {
            serv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serv.Bind(new IPEndPoint(IPAddress.Loopback, port));
            void Receive()
            {
                var endp = (EndPoint)(new IPEndPoint(IPAddress.Any, port));
                var buff = new byte[bufferSize];
                               
                serv.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref endp,
                    ar =>
                    {
                        try
                        {
                            int rcvd = serv.EndReceiveFrom(ar, ref endp);
                            var reqv = buff.Take(rcvd).ToArray();
                            var resp = handle(reqv);
                            serv.BeginSendTo(resp, 0, resp.Length, SocketFlags.None, endp, null, null);
                            OnReceive?.Invoke(reqv);
                        }
                        catch (Exception e)
                        {
                            OnError?.Invoke(e);
                            if (e is ObjectDisposedException) return;
                        }
                        Receive();
                    }, null);
            }
            Receive();
        }
        public static byte[] Call(byte[] data, IPAddress addr, int port)
        {
            var clnt = new UdpClient();
            var endp = new IPEndPoint(addr, port);
            clnt.Connect(endp);
            clnt.Send(data, data.Length);
            var answ = clnt.Receive(ref endp);
            clnt.Close();
            return answ;
        }
        public void Dispose()
        {
            serv.Dispose();
        }
    }
}
