using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Srv
{
    public class AcqSrv
    {


        //==================================
        public AcqSrv()
        {

        }
        //==================================


        public Int32 EvaluateRequest(ref string _req, ref string _res)
        {
            string[] reqs = _req.Split();

            switch (reqs[1].ToUpper())
            {
                default:
                    _res = "ERR: DAT";
                    break;
            }

            return 1;
        }

    }
}
