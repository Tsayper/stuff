using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace UdpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 50001;
            var serv = new UdpServer(port, 
                x => Encoding.Default.GetBytes("hello ").Concat(x).ToArray());

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                var c = new UdpClient();
                var ep = new IPEndPoint(IPAddress.Loopback, port);
                c.Connect(ep);
                var x = Encoding.Default.GetBytes("mike");
                c.Send(x, x.Length);
                var answer = c.Receive(ref ep);
                Console.WriteLine(Encoding.Default.GetString(answer));
                c.Close();
            }
        }
    }

    public class UdpServer
    {
        public UdpServer(int port, Func<byte[], byte[]> handle, int buffSz = 1024)
        {
            var serv = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serv.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            serv.Bind(new IPEndPoint(IPAddress.Loopback, port));
            serv.SendTimeout = 100;
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
    }
}
