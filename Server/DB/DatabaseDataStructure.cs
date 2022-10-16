using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{
    public class CxasUser
    {
        #region Properties
        //----------------------------------------------------
        public string UserName { get; set; }
        public string DbName { get; set; }
        public DateTime Created { get; set; }
        //----------------------------------------------------
        #endregion


        //====================================================
        public CxasUser()
        {
            DateTime dateTime = new DateTime(1985, 1, 19);
            this.Created = dateTime;
        }
        //====================================================

        #region Public Methods
        //----------------------------------------------------
        public string ToString()
        {
            string str = "";

            if (this.UserName == " ")
                str += "NoUserName ";
            else
                str += this.UserName + " ";


            if (this.DbName == " ")
                str += "NoDbName ";
            else
                str += this.DbName + " ";

            str += this.Created.ToString("yyyy-MM-dd");

            return str;
        }
        //----------------------------------------------------
        #endregion
    }

    public class CRegion
    {

        #region Variables
        //----------------------------------------------------
        private string __type;
        //----------------------------------------------------
        #endregion



        #region Properties
        //----------------------------------------------------

        public string Type
        {
            get { return __type; }
            set { SetType(value); }
        }
        public string Name { get; set; }
        public string Element { get; set; }
        public string Edge { get; set; }
        public UInt32 Points { get; set; }
        public double EEdge { get; set; }
        public double E1 { get; set; }
        public double E2 { get; set; }
        public double EDot { get; set; }
        public double EDotDot { get; set; }
        public double K0 { get; set; }
        public double K0Dot { get; set; }
        public Int32 Scaling { get; set; }
        public double TTA { get; set; }
        public double TTD { get; set; }
        public DateTime Created { get; set; }
        public string Comment1 { get; set; }
        public string Comment2 { get; set; }
        public string Comment3 { get; set; }
        public string Comment4 { get; set; }

        //----------------------------------------------------
        #endregion


        //====================================================
        public CRegion()
        {
            this.Type = "S";
        }
        //====================================================



        #region Private Methods
        //----------------------------------------------------
        private void SetType(string _type)
        {
            // make 'S' the default

            __type = _type.ToUpper();
            if (__type == "EXAFS")
            {                
                this.EDot = 0;
                this.EDotDot = 0;
            }
            else
            {
                this.K0 = 0;
                this.K0Dot = 0;
                this.Scaling = 0;
                this.TTA = 0;
                this.TTD = 0;
            }
        }
        //----------------------------------------------------
        #endregion


        #region Public Methods
        //----------------------------------------------------
        public string ToString()
        {
            string str;

            str = this.Type;
            str += " " + this.Name;            
            str += " " + this.Element;
            str += " " + this.Edge;
            str += " " + this.Points.ToString();
            str += " " + this.EEdge.ToString("F4");
            str += " " + this.E1.ToString("F4");
            str += " " + this.E2.ToString("F4");

            if (this.Type == "EXAFS")
            {
                str += " " + this.K0.ToString("F2");
                str += " " + this.K0Dot.ToString("F2");
                str += " " + this.Scaling.ToString("F0");
                str += " " + this.TTA.ToString("F3");
                str += " " + this.TTD.ToString("F3");
            }
            else
            {
                str += " " + this.EDot.ToString("F2");
                str += " " + this.EDotDot.ToString("F2");
            }

            //str += " " + this.Created.ToString("yyyy-MM-dd");
            
            return str;
        }

        public Int32 ParseString(string _str)
        {
            string[] strArr;
            string[] date;

            try
            {
                strArr = _str.Split();

                if (strArr.Length != 16)
                    return -1;

                date = strArr[15].Split('-');
                if (date.Length != 3)
                    return -1;

                this.Type       = strArr[0];
                this.Name       = strArr[1];                
                this.Element    = strArr[2];
                this.Edge       = strArr[3];

                this.Points     = Convert.ToUInt32(strArr[4]);
                this.EEdge      = Convert.ToDouble(strArr[5]);
                this.E1         = Convert.ToDouble(strArr[6]);
                this.E2         = Convert.ToDouble(strArr[7]);
                
                this.EDot       = Convert.ToDouble(strArr[8]);
                this.EDotDot    = Convert.ToDouble(strArr[9]);

                this.K0         = Convert.ToDouble(strArr[10]);
                this.K0Dot      = Convert.ToDouble(strArr[11]);
                this.Scaling    = Convert.ToInt32(strArr[12]);
                this.TTA        = Convert.ToDouble(strArr[13]);
                this.TTD        = Convert.ToDouble(strArr[14]);

                // yyy-MM-dd
                this.Created = new DateTime(Convert.ToInt32(date[0]), Convert.ToInt32(date[1]), Convert.ToInt32(date[2]));
            }
            catch (Exception ex)
            {
                return -1;            
            }

            return 1;
        }
        //----------------------------------------------------
        #endregion
    }

}

