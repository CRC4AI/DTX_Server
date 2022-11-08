using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;


namespace Client_Hue
{
    class Packet
    {
        public ushort size;
        public ushort packetId;
        public string packetData;
    }

    class LoginOkPacket : Packet
    {

    }

    class Program
    {
        static void Main(string[] args)
        {
            Socket socket;
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            //  IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint ipEndPont = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 4545);
            socket = new Socket(ipEndPont.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                socket.Connect(ipEndPont);
                Console.WriteLine($"connected");
                List<string> data = new List<string>();
                string line;
                Console.WriteLine(System.Environment.CurrentDirectory + @"\Manual.txt");
                using (StreamReader sr = new StreamReader(System.Environment.CurrentDirectory + @"\Manual.txt", Encoding.Default))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        data.Add(line);
                  
                        //Console.WriteLine($"SendSuccessfully");
                        //var manudata=line.Split(',');
                        //manu.Add(manudata[1], data);
                    }

                    for(int i = 0; i <data.Count; i++)
                    {
                        Packet packet = new Packet() { size = 20, packetId = 4, packetData = data[i] };
                        bool success = true;
                        ArraySegment<byte> s = SendBufferHelper.Open(4096);
                        byte[] buffer2 = BitConverter.GetBytes(packet.packetId);
                        byte[] buffer3 = Encoding.UTF8.GetBytes(packet.packetData);
                        var dataSize = 2 + buffer2.Length + buffer3.Length;
                        packet.size = (ushort)dataSize;
                        byte[] buffer = BitConverter.GetBytes(packet.size);

                        ushort size = 0;

                        size += (ushort)buffer.Length;
                        success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + size, s.Count - size), (ushort)buffer2.Length);
                        size += (ushort)buffer2.Length;
                        success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + size, s.Count - size), (ushort)buffer3.Length);
                        size += (ushort)buffer3.Length;
                        success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), size);

                        ArraySegment<byte> sendBuff = SendBufferHelper.Close(size);

                        //size += 2;
                        //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + size, s.Count - size), packet.packetId);
                        //size += 2;
                        //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + size, s.Count - size), packet.packetId);
                        //size += (ushort)buffer3.Length;
                        //success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset, s.Count), size);

                        //ArraySegment<byte> sendBuff = SendBufferHelper.Close(size);

                        if (success)
                            socket.Send(sendBuff);

                        //Array.Copy(buffer, 0, openSegment.Array, openSegment.Offset, buffer.Length);
                        //Array.Copy(buffer2, 0, openSegment.Array, openSegment.Offset + buffer.Length, buffer2.Length);
                        //Array.Copy(buffer3, 0, openSegment.Array, openSegment.Offset + 4, buffer3.Length);
                        // ArraySegment<byte> sendBuffer = SendBufferHelper.Close(packet.size);
                        // byte[] sendBuff = Encoding.IUBitConverter.GetBytes(1658730835597);
                        //int sendBytes = socket.Send(sendBuff);
                       // socket.Send(sendBuff);
                    }
                }

                byte[] recvBuff = new byte[1024];
                int recvbytes = socket.Receive(recvBuff);
                string recvData = Encoding.UTF8.GetString(recvBuff, 0, recvbytes);
                int recvDatas = BitConverter.ToInt32(recvBuff, recvbytes);
                Console.WriteLine($"[FromServer]{recvBuff}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
    }
}
