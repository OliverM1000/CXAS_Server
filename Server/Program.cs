using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;



namespace Server
{
    class Program
    {

        #region Constants
        //----------------------------------------------------
        private const Int32 SERVER_PORT = 3333;
        //----------------------------------------------------
        #endregion



        //====================================================
        static void Main(string[] args)
        {
            CxasAcqServer cxasAcqServer = new CxasAcqServer(SERVER_PORT);
        }
        //====================================================
    }
}




