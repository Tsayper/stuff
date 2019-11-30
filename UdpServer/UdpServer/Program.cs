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

    public class UdpServer
    {
        public UdpServer(int port, Func<byte[], byte[]> handle, int buffSz = 1024)
        {
            var serv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serv.Bind(new IPEndPoint(IPAddress.Loopback, port));
            void Receive()
            {
                var ep = (EndPoint)(new IPEndPoint(IPAddress.Any, port));
                var buff = new byte[buffSz];
                serv.BeginReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref ep,
                    ar =>
                    {
                        int rcvd = serv.EndReceiveFrom(ar, ref ep);
                        var answ = handle(buff.Take(rcvd).ToArray());
                        serv.BeginSendTo(answ, 0, answ.Length, SocketFlags.None, ep, null, null);
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
    }
}
