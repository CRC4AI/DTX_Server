using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace Server_Hue
{
    
    class PTPSession
    {
        public double CalculatePTP(GameSession session, ConcurrentQueue<double[]> data)
        {
            ConcurrentBag<double> time = new ConcurrentBag<double>();
            while (data.Count>0)
            {
                data.TryDequeue(out var slot);

                //slot[0] = server send time
                //slot[1] = client recv time
                //slot[2] = client send time
                //slot[3] = server recv time
                
                var a = slot[2] - slot[0];
                var b = slot[3] - slot[1];
                var c = a + b / 2;
                time.Add(c);
            }
            return time.Average();
        }

        public void PTP_Start(GameSession session)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            PTP_Packet packet = new PTP_Packet() { packetId = 0, packetType = (ushort)packTypes.ptp, counts = 1 };

            // 패킷 시작 전송 = 패킷 스타트 카운터 = 1 , 타임 1 에 유닉스 타임 집어넣기

            //PacketSize short 만큼 추가
            size += 2;
            packet.counts = 1;
            size += 2;
            packet.packetId = 0;
            size += 2;
            packet.packetType = (ushort)packTypes.ptp;
            size += 2;
            packet.time00 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            size += 8;
            packet.time01 = 0;
            size += 8;
            packet.time02 = 0;
            size += 8;
            packet.time03 = 0;
            size += 8;
            packet.size = size;


            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset+ sendbyte, s.Count - sendbyte), packet.size);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetId);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetType);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.counts);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.time00);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.time01);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.time02);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.time03);
            //byte[] data;
            //byte[] time;
            //session.OnSend();

            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);


            if (success)
            {
                session.Send(sendBuff);
            }
        }

        public void LoginPacket(GameSession session, ushort _packetId, ushort _packetType)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            Login_Packet packet = new Login_Packet() { packetId = session.SessionID, packetType = _packetType };
            ArraySegment<byte> s = SendBufferHelper.Open(4096);

            size += 2;
            size += 2;
            size += 2;
            size += 2;
            packet.size = size;

            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.size);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetId);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetType);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetType);


            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);

            session.Send(sendBuff);
            Console.WriteLine("Login Packet Send");
        }
    }
}
