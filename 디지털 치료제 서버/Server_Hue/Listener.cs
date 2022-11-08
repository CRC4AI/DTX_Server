using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server_Hue
{
    public class Listener
    {
        Socket listenerSocket;
        Func<Session> _sessionFactory;
        PTPSession _session;
        public EndPoint ipPoint;
        public void Init(IPEndPoint endPoint, int register = 10, int backlog = 100)
        {
            listenerSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //_sessionFactory += sessionFactory;

            // 문지기 교육
            listenerSocket.Bind(endPoint);

            // 영업 시작
            // backlog : 최대 대기수
            listenerSocket.Listen(backlog);

            for (int i = 0; i < register; i++)
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                RegisterAccept(args);
            }
        }
        public void Init(IPEndPoint ipEndPoint, Func<Session> sessionFactory, int backLog)
        {
            listenerSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory += sessionFactory;
            listenerSocket.Bind(ipEndPoint);
            listenerSocket.Listen(backLog);
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
            RegisterAccept(args);
        }
        void RegisterAccept(SocketAsyncEventArgs args)
        {
            args.AcceptSocket = null;
            bool pending = listenerSocket.AcceptAsync(args);
            if(!pending)
            {
                OnAcceptCompleted(null,args);
            }
        }
        void OnAcceptCompleted(object sender ,SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                Session session = SessionManager.instance.Generate((IPEndPoint)args.AcceptSocket.RemoteEndPoint);
                args.UserToken = sender;
                ipPoint = args.AcceptSocket.RemoteEndPoint;
                session.Start(args.AcceptSocket);
                session.OnConnected(args.AcceptSocket.RemoteEndPoint);
                //PTP Start()     
                // 접속
            }
            else
            {
                //접속 실패
            }
            RegisterAccept(args);
        }

        public Socket Accept()
        {
            return listenerSocket.Accept();
        }
    }

}
