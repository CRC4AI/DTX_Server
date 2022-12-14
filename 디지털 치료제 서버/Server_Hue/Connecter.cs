using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace Server_Hue
{
    public class Connecter
    {

        Func<Session> _sessionFactory;

         public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory)
        {
            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _sessionFactory = sessionFactory;
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += OnConnectedCompleted; 
            args.RemoteEndPoint = endPoint;
            args.UserToken = socket;
            Console.WriteLine($"내가 원하는 건 : {args.UserToken.ToString()}");
            RegisterConnect(args);
        }

        void RegisterConnect(SocketAsyncEventArgs args)
        {
            Socket socket = args.UserToken as Socket;
            if(socket==null)
                return;

            bool pending = socket.ConnectAsync(args);
            if(!pending)
            {
                OnConnectedCompleted(null,args);
            }

        }
        void OnConnectedCompleted(object sender, SocketAsyncEventArgs args)
        {
            if(args.SocketError == SocketError.Success)
            {
                Session session = _sessionFactory.Invoke();
                session.Start(args.ConnectSocket);
                session.OnConnected(args.RemoteEndPoint);
            }
            else
            {
                Console.WriteLine($"OnConnect Completed Failed:{args.SocketError}");
            }
        }
    }
}
