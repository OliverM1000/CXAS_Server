using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Server
{

    public class OmServer
    {

        #region Const
        //----------------------------------------------------
        private const Int32 MAX_MESSAGE_LEN = 600;
        //----------------------------------------------------
        #endregion



        #region Variables
        //----------------------------------------------------
        private TcpListener __server;        
        private List<ClientObj> __clients;
        private ServerCallback __serverCallback;
        //----------------------------------------------------
        #endregion



        #region Delegates
        //----------------------------------------------------
        public delegate Int32 ServerCallback(object _sender, UInt64 _id, ref string _req, ref string _res);
        //----------------------------------------------------
        #endregion        



        //====================================================
        public OmServer(int _port, ServerCallback _severCallback)
        {
            __clients = new List<ClientObj>();
            __serverCallback = _severCallback; ;
            __server = new TcpListener(IPAddress.Any, _port);
            __server.Start();            

            Console.WriteLine("server started");

            AcceptClients();
        }
        //====================================================



        #region Public Methods
        //----------------------------------------------------
        public void GetClientIds( ref string _res)
        {            
            __clients.OrderBy(client => client.id);

            _res = "";
            for (int i = 0; i < __clients.Count; i++)
            {
                _res += "\r" + __clients[i].id; 
            }
        }
        //----------------------------------------------------
        #endregion


        #region Private Methods
        //----------------------------------------------------
        private UInt64 GetNextClientId()
        {
            if (__clients.Count == 0)
                return 1;

            return __clients[__clients.Count - 1].id + 1;
        }
        private Int32 RemoveClientFromList(UInt64 _id)
        {
            for (int i = 0; i < __clients.Count; i++)
            {
                if (__clients[i].id == _id)
                {
                    __clients.RemoveAt(i);
                    return 1;
                }
            }

            return -1;
        }
        //----------------------------------------------------


        //----------------------------------------------------
        private void AcceptClients()
        {
            while (true)
            {
                ClientObj omClient = new ClientObj();
                omClient.id = GetNextClientId();
                omClient.client = __server.AcceptTcpClient();

                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(omClient);

                __clients.Add(omClient);
                Console.WriteLine("new client connection: " + omClient.id + "\\" + __clients.Count);
            }
        }
        private void HandleClient(object _obj)
        {
            int rt;
            string errorLine;
            string req, res;
            ClientObj omClient = (ClientObj)_obj;
            StreamReader reader = new StreamReader(omClient.client.GetStream(), Encoding.ASCII);
            StreamWriter writer = new StreamWriter(omClient.client.GetStream(), Encoding.ASCII);


            while (true)
            {
                try
                {
                    req = "";
                    res = "";
                    req = reader.ReadLine();
                    if (req == null)
                        break;

                    Console.WriteLine(omClient.id + " > " + req);


                    //####################################################
                    rt = __serverCallback(this, omClient.id, ref req, ref res);
                    if (rt < 0)
                    {
                        res = res.Trim();
                        res = "ERROR " + this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + res;
                    }
                    else
                    {
                        res = res.Trim();
                        res = "SUCCESS " + res;
                    }
                    //####################################################


                    writer.WriteLine(res);
                    writer.Flush();


                    string shortRes;
                    if (res.Length > 600)
                        shortRes = res.Substring(0, MAX_MESSAGE_LEN) + " ...";
                    else
                        shortRes = res;

                    Console.WriteLine(omClient.id + " < " + shortRes);

                }
                catch (Exception ex)
                {
                    errorLine = "ERROR: " + this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    errorLine += "ClientId " + omClient.id.ToString();
                    errorLine += "\r\n" + ex.ToString();
                    Console.WriteLine(errorLine) ;
                    break;
                }
            }

            reader.Close();
            writer.Close();
            omClient.client.Close();
            Console.WriteLine("client disconnected: " + omClient.id.ToString());

            rt = RemoveClientFromList(omClient.id);
            if (rt < 0)
            {
                Console.WriteLine("Error: Could not remove clientId: " + omClient.id);
            }
        }
        //----------------------------------------------------
        #endregion     
    }








    public class ClientObj
    {
        public UInt64 id;
        public TcpClient client;
    }
}
