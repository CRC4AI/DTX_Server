using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Collections.Concurrent;

namespace Server_Hue
{
    enum packTypes
    {
        AI_report = 0, applewatch = 1, airpot = 2, device = 3, ptp = 5,ptpComp = 6,showDevice = 7, joinRoom = 8,labelTimeRequest = 9, labelTimeSetting = 10,labelingData =11, FileSave = 12, disconnectPacket= 100 ,connectPacket = 101
    }
    class PTP_Packet
    {
        public ushort size;
        public ushort packetId;
        public ushort packetType;
        public ushort counts;
        public double time00;
        public double time01;
        public double time02;
        public double time03;
    }
    class Login_Packet
    {
        public ushort size;
        public ushort packetId;
        public ushort packetType;
    }

    enum SessionType
    {
        NotChoiced,
        Session_DataProvider,
        Session_Labeler,
        Session_Displayer,

    }

    class Connection_Packet
    {
        public ushort size;
        public ushort packetId;
        public ushort packetType;
        public ushort deviceId;
    }

    class LabelTimeRecv_Packet
    {
        public ushort size;
        public ushort packetId;
        public ushort packetType;
        public double labelTime;
    }


    class FeedBack_Packet
    {
        public ushort size;
        public ushort packetId;
        public ushort packetType;
        public ushort feedBack;
    }

    class GameSession : PacketSession
    {
        public PTPSession ptp_session = new PTPSession();
        DataRecorder dataRecorder = new DataRecorder();
        public double sessionDelay = 0;
        public bool isFinish = false;
        public ConcurrentQueue<double[]> ptpData = new ConcurrentQueue<double[]>();
        ConcurrentDictionary<KeyValuePair<ushort, ushort>, ConcurrentQueue<string>> datas = new ConcurrentDictionary<KeyValuePair<ushort, ushort>, ConcurrentQueue<string>>();
        ConcurrentQueue<string> appleWatchData = new ConcurrentQueue<string>();
        ConcurrentQueue<string> airpodData = new ConcurrentQueue<string>();
        ConcurrentQueue<string> deviceData = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> bodyTrackingData = new ConcurrentQueue<string>();
        ConcurrentQueue<string> labelData = new ConcurrentQueue<string>();
        public ushort SessionID { get; set; }
        public SessionType SessionType { get; set; }
        public GameRoom Room { get; set; }

        int[] fileIndex = new int[3] { 0, 0, 0 };
        public override void OnConnected(EndPoint endPoint)
        {
            Console.WriteLine($"OnConnected : {endPoint}");
            Program.Room.Enter(this);
        }
        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            int len = 0;
            bool success = true;
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
            len += 2;
            ushort packetID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
            len += 2;
            ushort packetType = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
            KeyValuePair<ushort, ushort> checker = new KeyValuePair<ushort, ushort>(packetID, packetType);
            switch (packetType)
            {
                case ((int)packTypes.applewatch):
                    len += 2;
                    double time = BitConverter.ToDouble(buffer.Array, buffer.Offset + len) - sessionDelay;
                    len += 8;
                    float gyrox = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float gyroy = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float gyroz = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accx = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accy = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accz = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    Int32 heartRate = BitConverter.ToInt16(buffer.Array, buffer.Offset + len);
                    if (isFinish)
                    {
                        appleWatchData.Enqueue($"{time.ToString("F0")},{gyrox.ToString("N3")},{gyroy.ToString("N3")},{gyroz.ToString("N3")},{accx.ToString("N3")},{accy.ToString("N3")},{accz.ToString("N3")},{heartRate}");
                        //if (appleWatchData.Count > 300)
                        //{
                        //    if (datas.TryAdd(checker, appleWatchData))
                        //    {
                        //        if (datas.TryGetValue(checker, out var value))
                        //        {
                        //            fileIndex[0]++;
                        //            dataRecorder.PrintData(value, fileIndex[0], packetID, packetType);
                        //            appleWatchData.Clear();
                        //            datas.TryRemove(checker, out value);
                        //        }
                        //    }
                        //}
                    }
                    break;
                case ((int)packTypes.airpot):
                    len += 2;
                    double time_a = BitConverter.ToDouble(buffer.Array, buffer.Offset + len) - sessionDelay;
                    len += 8;
                    float gyrox_a = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float gyroy_a = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float gyroz_a = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accx_a = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accy_a = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accz_a = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    if (isFinish)
                    {
                        airpodData.Enqueue($"{time_a.ToString("F0")},{gyrox_a.ToString("N3")},{gyroy_a.ToString("N3")},{gyroz_a.ToString("N3")},{accx_a.ToString("N3")},{accy_a.ToString("N3")},{accz_a.ToString("N3")}");
                        //if (airpodData.Count > 50)
                        //{
                        //    if (datas.TryAdd(checker, airpodData))
                        //    {
                        //        if (datas.TryGetValue(checker, out var value))
                        //        {
                        //            fileIndex[1]++;
                        //            dataRecorder.PrintData(value, fileIndex[1], packetID, packetType);
                        //            airpodData.Clear();
                        //            datas.TryRemove(checker, out value);
                        //            Room.FeedBack(0, 101);
                        //        }
                        //    }
                        //}
                    }
                    break;
                case ((int)packTypes.device):
                    len += 2;
                    double time_d = BitConverter.ToDouble(buffer.Array, buffer.Offset + len) - sessionDelay;
                    len += 8;
                    float gyrox_d = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float gyroy_d = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float gyroz_d = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accx_d = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accy_d = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    len += 4;
                    float accz_d = BitConverter.ToSingle(buffer.Array, buffer.Offset + len);
                    if (isFinish)
                    {
                        deviceData.Enqueue($"{time_d.ToString("F0")},{gyrox_d.ToString("N3")},{gyroy_d.ToString("N3")},{gyroz_d.ToString("N3")},{accx_d.ToString("N3")},{accy_d.ToString("N3")},{accz_d.ToString("N3")}");
                        //if (deviceData.Count > 300)
                        //{
                        //    if (datas.TryAdd(checker, deviceData))
                        //    {
                        //        if (datas.TryGetValue(checker, out var value))
                        //        {
                        //            fileIndex[2]++;
                        //            dataRecorder.PrintData(value, fileIndex[2], packetID, packetType);
                        //            deviceData.Clear();
                        //            datas.TryRemove(checker, out value);
                        //            //Random i = new Random();
                        //            Program.Room.FeedBack(0,101);
                        //        }
                        //    }
                        //}
                    }
                    break;
                case ((int)packTypes.connectPacket):
                    len += 2;
                    ushort sessionType = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    SessionType = (SessionType)sessionType;
                    Console.WriteLine($"Device:{SessionType} is Login");
                    
                    break;
                case ((int)packTypes.ptp):
                    //만약에 PTP를 진행한다면..
                    // SIZE, ID, PACKET_PTP,PTPCOUNT,DOUBLE,DOUBLE,DOUBLE,DOUBLE
                    double[] time_ptp = new double[4];
                    len += 2;
                    ushort count = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    len += 2;
                    time_ptp[0] = BitConverter.ToDouble(buffer.Array, buffer.Offset + len);
                    len += 8;
                    time_ptp[1] = BitConverter.ToDouble(buffer.Array, buffer.Offset + len);
                    len += 8;
                    time_ptp[2] = BitConverter.ToDouble(buffer.Array, buffer.Offset + len);
                    len += 8;
                    time_ptp[3] = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    ptpData.Enqueue(time_ptp);
                    break;
                case ((int)packTypes.ptpComp):
                    //만약에 PTP를 진행한다면..
                    // SIZE, ID, PACKET_PTP,PTPCOUNT,DOUBLE,DOUBLE,DOUBLE,DOUBLE
                    len += 2;
                    //SessionID = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    Console.WriteLine($"Device is Connect Comp{SessionID}");
                    if (SessionType == SessionType.Session_DataProvider)
                    {
                        Program.Room.Connect(this);
                        Room = new GameRoom();
                        Room.Connect(this);
                    }
                    else if(SessionType == SessionType.Session_Labeler)
                    {
                        Program.Room.Connect(this);
                    }
                    break;
                case ((int)packTypes.showDevice):
                    len += 2;
                    if (SessionType == SessionType.Session_Labeler)
                    {
                        Console.WriteLine($"return All Provider Data for {SessionID}");
                        Program.Room.ConnectionCheck(this);
                    }
                    break;
                case ((int)packTypes.joinRoom):
                    len += 2;
                    var provider = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    len += 2;
                    var labeler = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    Console.WriteLine($"Labeler join the Provider room {SessionID}");
                    Program.Room.Bind(provider, labeler);
                    break;
                case ((int)packTypes.labelTimeRequest):
                    len += 2;
                    var provider_x = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    len += 2;
                    var labeler_x = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    Console.WriteLine($"Labeler Want Provider time {SessionID}");
                    var session=Program.Room.Find(provider_x,SessionType.Session_DataProvider);
                    Program.Room.LabelTimeReqPacket(session, session.SessionID);
                    session.bodyTrackingData.Enqueue("{label}");
                    break;
                case ((int)packTypes.labelTimeSetting):
                    len += 2;
                    len += 2;
                    var provider_y = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    len += 2;
                    var labeler_y = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);

                    double labelTime_y = BitConverter.ToDouble(buffer.Array, buffer.Offset + len);
                    Console.WriteLine($"Labeler Get Provider time {SessionID},{ labelTime_y}");
                    var session_y = Program.Room.Find(2, SessionType.Session_Labeler);
                    Program.Room.LabelTimeSendPacket(session_y, session_y.SessionID, labelTime_y);
                    session_y.bodyTrackingData.Enqueue("{label}");
                    break;
                case ((int)packTypes.labelingData):
                    len += 2;
                    var chatLen = BitConverter.ToUInt16(buffer.Array, buffer.Offset + len);
                    len += 2;
                    len += 2;
                    string labelingData = Encoding.Unicode.GetString(buffer.Array,buffer.Offset+len,chatLen-2);
                    Console.WriteLine(labelingData);
                    break;
                case ((int)packTypes.FileSave):
                    
                    break;

            }
        }
        public override void OnSend(int numOfBytes)
        {
        }
        public override void OnDisconnected(EndPoint endPoint)
        {
            SessionManager.instance.Remove(this);
            if (Room != null)
            {
                Room.Leave(this);
                Room = null;
                
                Console.WriteLine($"프로바이더 방 탈출 {SessionID}");
            }
            Program.Room.Leave(this);
            {
                Console.WriteLine($"대기방 탈출 {SessionID}");
            }
            Request_Save();
            Console.WriteLine($" OnDisconnected {endPoint}");
        }
        private void Request_Save()
        {
            airpodData.Clear();
            bodyTrackingData.Clear();
            appleWatchData.Clear();
            deviceData.Clear();
        }
    }

    class Program
    {
        static Listener listener = new Listener();
        public static GameRoom Room = new GameRoom();
        public static EndPoint ipPoint;
        static void Main(string[] args)
        {

            Socket socket;
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            // IPAddress ipAddr = ipHost.AddressList[0];
            IPAddress iPAddr = IPAddress.Any;
            IPEndPoint ipEnd = new IPEndPoint(iPAddr, 4545);
            Console.WriteLine(ipEnd);
            ipPoint = ipEnd;

            listener.Init(ipEnd, 10);

            try
            {
                Console.WriteLine("Listening...");
                while (true)
                {
                    ;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }

        }


    }
}
