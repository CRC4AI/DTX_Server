using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server_Hue
{
    class GameRoom
    {
        public Dictionary<int, GameSession> _sessions_DataProvider = new Dictionary<int, GameSession>();
        public Dictionary<int, GameSession> _sessions_Labeler = new Dictionary<int, GameSession>();
        Dictionary<int, GameSession> _sessions_Displayer = new Dictionary<int, GameSession>();
        object _lock = new object();
        static ushort playerID;
        public GameSession Find(ushort id, SessionType sessiontype)
        {
            lock (_lock)
            {
                GameSession values;
                switch (sessiontype)
                {
                    case SessionType.Session_DataProvider:
                        _sessions_DataProvider.TryGetValue(id, out values);
                        break;
                    case SessionType.Session_Labeler:
                        _sessions_Labeler.TryGetValue(id, out values);
                        break;
                    case SessionType.Session_Displayer:
                        _sessions_Displayer.TryGetValue(id, out values);
                        break;
                    default:
                        throw new ArgumentException();
                        break;
                }
                return values;
            }
        }

        public void Enter(GameSession session)
        {
            lock (_lock)
            {
                playerID++;
                switch (session.SessionType)
                {
                    case SessionType.Session_DataProvider:
                        session.SessionID = playerID;
                        _sessions_DataProvider.TryAdd(playerID, session);

                        while (session.ptpData.Count < 20)
                        {
                            Thread.Sleep(20);
                            session.ptp_session.PTP_Start(session);
                        }
                        session.sessionDelay = session.ptp_session.CalculatePTP(session, session.ptpData) - 40;
                        session.isFinish = true;
                        Console.WriteLine($"PTP FINISH : DELAY  = {session.sessionDelay}");
                        session.ptp_session.LoginPacket(session, session.SessionID, (ushort)SessionType.Session_DataProvider);
                        break;

                    case SessionType.Session_Labeler:
                        session.SessionID = playerID;
                        _sessions_Labeler.TryAdd(playerID, session);

                        while (session.ptpData.Count < 20)
                        {
                            Thread.Sleep(10);
                            session.ptp_session.PTP_Start(session);
                        }
                        session.sessionDelay = session.ptp_session.CalculatePTP(session, session.ptpData) - 20;
                        session.isFinish = true;
                        Console.WriteLine($"PTP FINISH : DELAY  = {session.sessionDelay}");
                        session.ptp_session.LoginPacket(session, session.SessionID, (ushort)SessionType.Session_Labeler);
                        break;

                    case SessionType.Session_Displayer:
                        session.SessionID = playerID;
                        _sessions_Displayer.TryAdd(playerID, session);
                        session.ptp_session.LoginPacket(session, session.SessionID, (ushort)SessionType.Session_Displayer);
                        break;
                }
            }
        }

        public void Connect(GameSession session)
        {
            lock (_lock)
            {
                switch (session.SessionType)
                {
                    case SessionType.Session_DataProvider:
                        _sessions_DataProvider.TryAdd(session.SessionID, session);
                        Console.WriteLine($"The Session_DataProvider is Enter : {session.SessionID}");
                        break;

                    case SessionType.Session_Labeler:
                        _sessions_Labeler.TryAdd(session.SessionID, session);
                        Console.WriteLine($"The Session_Labeler is Enter : {session.SessionID}");
                        break;

                    case SessionType.Session_Displayer:
                        _sessions_Displayer.TryAdd(session.SessionID, session);
                        Console.WriteLine($"The Session_Displayer is Enter : {session.SessionID}");
                        break;
                }
            }
        }
        public void Bind(int provider_id,int labeler_id)
        {
            lock (_lock)
            {
                Program.Room._sessions_DataProvider.TryGetValue(provider_id, out var value);
                if (value != null)
                {
                    GameSession labeler = Program.Room.Find((ushort)labeler_id, SessionType.Session_Labeler);
                    value.Room._sessions_Labeler.TryAdd(labeler.SessionID, labeler);
                    Console.WriteLine($"Successfully Join the Room {value.SessionID},{value.SessionType},{labeler.SessionID},{labeler.SessionType}");
                }
            }
        }

        public void BroadCast(GameSession session)
        {
            foreach (var a in _sessions_Labeler.Values)
            {
                RemovePacket(a, session.SessionID);
            }
        }

        public void Leave(GameSession session)
        {
            lock (_lock)
            {
       
                switch (session.SessionType)
                {
                    case SessionType.Session_Displayer:
                     
                        _sessions_Displayer.Remove(session.SessionID);
                        break;
                    case SessionType.Session_Labeler:
                        _sessions_Labeler.Remove(session.SessionID);
                        break;
                    case SessionType.Session_DataProvider:
                        BroadCast(session);
                        _sessions_DataProvider.Remove(session.SessionID);
                        break;
                    default:
                        Console.WriteLine("Error: SessionType is default");
                        break;
                }
            }
        }

        public void ConnectionCheck(GameSession session)
        {
            lock (_lock)
            {
                foreach (var a in Program.Room._sessions_DataProvider)
                {
                    ConnectionPacket(session, a.Key);
                }
            }
        }

        public void RemovePacket(GameSession session, int id)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            Connection_Packet packet = new Connection_Packet() { packetType = (ushort)packTypes.disconnectPacket, deviceId = (ushort)id };

            //PacketSize short 만큼 추가
            size += 2;
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
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.deviceId);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.deviceId);
            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);

            if (success)
            {
                session.Send(sendBuff);
                //  Console.WriteLine($"FeedBack Send, The SeesionID :{session.SessionID}");
            }               
        }
        public void LabelTimeReqPacket(GameSession session, int id)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            LabelTimeRecv_Packet packet = new LabelTimeRecv_Packet() { packetType = (ushort)packTypes.labelTimeRequest, packetId = (ushort)id };

            //PacketSize short 만큼 추가
            size += 2;
            size += 2;
            size += 2;
            size += 8;
            size += 8;

            packet.size = size;

            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.size);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetId);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetType);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.labelTime);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.labelTime);
            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);

            if (success)
            {
                session.Send(sendBuff);
                //  Console.WriteLine($"FeedBack Send, The SeesionID :{session.SessionID}");
            }
        }
        public void LabelTimeSendPacket(GameSession session, int id,double time)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            LabelTimeRecv_Packet packet = new LabelTimeRecv_Packet() { packetType = (ushort)packTypes.labelTimeSetting, packetId = (ushort)id ,labelTime = time};

            //PacketSize short 만큼 추가
            size += 2;
            size += 2;
            size += 2;
            size += 8;
            size += 8;

            packet.size = size;

            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.size);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetId);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.packetType);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.labelTime);
            sendbyte += 8;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.labelTime);
            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);

            if (success)
            {
                session.Send(sendBuff);
                //  Console.WriteLine($"FeedBack Send, The SeesionID :{session.SessionID}");
            }
        }

        public void ConnectionPacket(GameSession session, int id)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            Connection_Packet packet = new Connection_Packet() { packetType = (ushort)packTypes.showDevice, deviceId = (ushort)id };

            //PacketSize short 만큼 추가
            size += 2;
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
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.deviceId);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.deviceId);
            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);

            if (success)
            {
                session.Send(sendBuff);
                //  Console.WriteLine($"FeedBack Send, The SeesionID :{session.SessionID}");
            }
        }






        public void FeedBack_PacketGenerate(GameSession session, ushort feedback)
        {
            bool success = true;
            ushort size = 0;
            ushort sendbyte = 0;
            ArraySegment<byte> s = SendBufferHelper.Open(4096);
            FeedBack_Packet packet = new FeedBack_Packet() { packetId = 0, packetType = (ushort)packTypes.AI_report };

            //PacketSize short 만큼 추가
            size += 2;
            packet.packetId = 0;
            size += 2;
            var count = 0;
            size += 2;
            packet.packetType = (ushort)packTypes.AI_report;
            size += 2;
            packet.feedBack = feedback;
            size += 2;
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
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), packet.feedBack);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), (ushort)0);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), (ushort)0);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), (ushort)0);
            sendbyte += 2;
            success &= BitConverter.TryWriteBytes(new Span<byte>(s.Array, s.Offset + sendbyte, s.Count - sendbyte), (ushort)0);
            //byte[] data;
            //byte[] time;
            //session.OnSend();

            ArraySegment<byte> sendBuff = SendBufferHelper.Close(sendbyte);

            if (success)
            {
                session.Send(sendBuff);
                //  Console.WriteLine($"FeedBack Send, The SeesionID :{session.SessionID}");
            }
        }
    }
}
