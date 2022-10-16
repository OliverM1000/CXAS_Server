using MySql.Data.MySqlClient;




namespace Server
{
    public class CxasAcqServer
    {
        #region Constants
        //----------------------------------------------------
        private const string PATH_DB_DEF = "C:\\SSRL_LOCAL_OM\\CxasAcqServer\\Definitions\\db.def";
        //----------------------------------------------------
        private const string DEFAULT_USER_NAME = "DEFAULT";
        //----------------------------------------------------
        private const Int32 OFFSET_USER_NAME            = 0;        
        private const Int32 OFFSET_KEYWORD              = 1;
        //----------------------------------------------------
        private const Int32 OFFSET_SPECIAL_COMMAND      = 2;
        private const Int32 OFFSET_SPECIAL_SET_USER     = 3;
        //----------------------------------------------------
        #endregion



        #region Variables
        //----------------------------------------------------
        private DB.OmDB         __DB;
        private DB.CxasUser     __user;
        private OmServer        __server;
        //----------------------------------------------------
        private Srv.UserSrv     __userSrv;
        private Srv.AcqSrv      __acqSrv;
        private Srv.DataSrv     __dataSrv;
        private Srv.CRegionSrv  __cRegionSrv;
        private Srv.DaqSrv      __daqSrv;
        //----------------------------------------------------
        #endregion


        
        #region Properties
        //----------------------------------------------------
        private string UserName { get; set; }
        private string MasterUserName { get; set; }
        //----------------------------------------------------
        #endregion


        
        //====================================================
        public CxasAcqServer(int _port)
        {
            Int32 rt;
            string error = "";
            string currentUser = "";
            string masterUser = "";
            this.UserName = DEFAULT_USER_NAME;

            

            __DB = new DB.OmDB();
            rt = __DB.Init(PATH_DB_DEF, ref error);
            if (rt < 0)
            {
                error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                Console.WriteLine(error);
                return;
            }


            
            #region User Server
            //....................................................
            __userSrv = new Srv.UserSrv(ref __DB);
            rt = __userSrv.ReadCurrentUser(ref currentUser, ref error);
            if (rt < 0)
            {
                error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                Console.WriteLine(error);
                return;
            }
            rt = __userSrv.ReadMasterUser(ref masterUser, ref error);
            if (rt < 0)
            {
                error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                Console.WriteLine(error);
                return;
            }
            this.UserName = currentUser;
            this.MasterUserName = masterUser;

            Console.WriteLine("Master User:\t" + this.MasterUserName);
            Console.WriteLine("Current User:\t" + this.UserName);
            //....................................................
            #endregion



            #region C-Region Server
            //....................................................
            __cRegionSrv = new Srv.CRegionSrv(ref __DB);
            //....................................................
            #endregion



            #region DAQ Server
            //....................................................
            __daqSrv = new Srv.DaqSrv(ref __DB);
            //....................................................
            #endregion



            #region Acquisition Server
            //....................................................
            __acqSrv = new Srv.AcqSrv();
            //....................................................
            #endregion



            #region Data Server
            //....................................................
            __dataSrv = new Srv.DataSrv();
            //....................................................
            #endregion            



            // -- START THE SERVER ---
            __server = new OmServer(_port, (OmServer.ServerCallback)EvaluateRequest);
        }
        //====================================================



        #region Private Mehtods
        //----------------------------------------------------
        private Int32 AuthUserName(string[] _reqArr, ref string _res)
        {          
            if (    _reqArr[OFFSET_USER_NAME] == DEFAULT_USER_NAME ||
                    _reqArr[OFFSET_USER_NAME] != this.UserName)
            {                
                _res = "INVALID_USER";
                return -1;
            }

            return 1;
        }
        //----------------------------------------------------
        private Int32 ValidateUserName(string _userName, ref string _error)
        {

            if (_userName.Length < 4 || _userName.Length > 40)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "LENGTH";
                return -1;
            }

            for (Int32 i = 0; i < __DB.INVALID_CHARS.Length; i++)
            {
                if (_userName.Contains(__DB.INVALID_CHARS[i]))
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error = "INVALID_CHARS";
                    return -1;
                }
            }

            return 1;
        }
        private Int32 EvaluateRequest(object _sender, UInt64 _id, ref string _req, ref string _res)
        {
            Int32 rt;
            bool err = false;
            string[] reqArr = _req.Split();

            if (reqArr.Length < 2)                              // every request must have: user_name and at least one keyword
            {
                err = true;
                _res = "INVALID_REQUEST_LENGTH";
            }


            if (!err)
            {
                if (reqArr[OFFSET_KEYWORD] == "SERVER")         // SERVER is a special command
                {
                    rt = EvaluateSpecialRequest(_sender, _id, ref _req, ref _res);
                    if (rt < 0)
                        err = true;
                }
                else
                {
                    rt = EvaluateCommonRequest(_sender, ref _req, ref _res);
                    if (rt < 0)
                        err = true;
                }
            }

            if (err)
            {
                _res = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _res;
                return -1;
            }
            
            return 1;
        }
        //----------------------------------------------------        
        private Int32 EvaluateSpecialRequest(object _sender, UInt64 _id, ref string _req, ref string _res)
        {
            Int32 rt;
            string error = "";
            bool err = false;
            string[] reqArr = _req.Split();

            string userName;
            string dbName;

            switch (reqArr[OFFSET_SPECIAL_COMMAND].ToUpper())
            {
                #region GET_ID
                //....................................................
                case "GET_ID":
                    if (reqArr.Length != OFFSET_SPECIAL_COMMAND + 1)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    _res = reqArr[OFFSET_SPECIAL_COMMAND].ToUpper() + " \r" + _id;
                    break;
                //....................................................
                #endregion


                #region LIST_IDS
                //....................................................
                case "LIST_IDS":
                    if (reqArr.Length != OFFSET_SPECIAL_COMMAND + 1)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    ((OmServer)_sender).GetClientIds(ref _res);
                    _res = reqArr[OFFSET_SPECIAL_COMMAND].ToUpper() + " " + _res;
                    break;
                //....................................................
                #endregion


                #region SET_USER
                //....................................................
                case "SET_USER":
                    if (reqArr.Length != OFFSET_SPECIAL_COMMAND + 2)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    rt = ValidateUserName(reqArr[OFFSET_SPECIAL_SET_USER], ref error);
                    if (rt < 0)
                    {
                        err = true;
                        _res = error;
                        break;
                    }
                    rt = __userSrv.SetUser(reqArr[OFFSET_SPECIAL_SET_USER], ref _res, ref error);
                    if (rt < 0)
                    {
                        err = true;
                        _res = error;
                        break;
                    }
                    userName = _res;
                    dbName = "";
                    rt = __DB.GetDbName(userName, ref dbName, ref error);
                    if (rt < 0)
                    {
                        err = true;
                        _res = error;
                        break;
                    }
                    this.UserName = userName;
                    __DB.DbName = dbName;
                    _res = reqArr[OFFSET_SPECIAL_COMMAND].ToUpper() + " " + _res;
                    break;
                //....................................................
                #endregion


                #region GET_USER
                //....................................................
                case "GET_USER":
                    _res = reqArr[OFFSET_SPECIAL_COMMAND].ToUpper() + " \r" + this.UserName;
                    break;
                //....................................................
                #endregion


                #region DEFAULT
                //....................................................
                default:
                    err = true;
                    _res = "UNKNOWN_COMMAND";
                    break;
                //....................................................
                #endregion
            }

            if (err)
            {
                _res = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _res;
                return -1;
            }

            _res = reqArr[OFFSET_KEYWORD].ToUpper() + " " + _res;

            return 1;
        }
        private Int32 EvaluateCommonRequest(object _sender, ref string _req, ref string _res)
        {
            Int32 rt;
            bool err = false;
            string[] reqSubArr;
            string[] reqArr = _req.Split();

            
            rt = AuthUserName(reqArr, ref _res);
            if (rt < 0)
            {
                Console.WriteLine("unauthorized user rejected: " + reqArr[OFFSET_USER_NAME]);
                err = true;
            }
            
            if (!err)
            {
                reqSubArr = reqArr.Skip(2).Take(reqArr.Length - 2).ToArray();

                switch (reqArr[OFFSET_KEYWORD].ToUpper())
                {
                    #region USER
                    //....................................................
                    case "USER":
                        rt = __userSrv.EvaluateRequest(ref reqSubArr, ref _res);
                        if (rt < 0)
                        {
                            err = true;
                            break;
                        }
                        break;
                    //....................................................
                    #endregion


                    #region CREGION
                    //....................................................
                    case "CREGION":
                        rt = __cRegionSrv.EvaluateRequest(ref reqSubArr, ref _res);
                        if (rt < 0)
                        {
                            err = true;
                            break;
                        }
                        break;
                    //....................................................
                    #endregion


                    #region DAQ
                    //....................................................
                    case "DAQ":
                        rt = __daqSrv.EvaluateRequest(ref reqSubArr, ref _res);
                        if (rt < 0)
                        {
                            err = true;
                            break;
                        }
                        break;
                    //....................................................
                    #endregion

                    #region Acquisition
                    //....................................................
                    case "ACQ":
                        rt = __acqSrv.EvaluateRequest(ref _req, ref _res);
                        if (rt < 0)
                        {
                            err = true;
                            break;
                        }
                        break;
                    //....................................................
                    #endregion


                    #region Data
                    //....................................................
                    case "DATA":
                        rt = __dataSrv.EvaluateRequest(ref _req, ref _res);
                        if (rt < 0)
                        {
                            err = true;
                            break;
                        }
                        break;
                    //....................................................
                    #endregion


                    #region Default
                    //....................................................
                    default:
                        err = true;
                        _res = "UNKNOWN_KEYWORD";
                        break;
                    //....................................................
                    #endregion
                }                
            }

            if (err)
            {
                _res = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _res;
                return -1;
            }

            _res = reqArr[OFFSET_KEYWORD].ToUpper() + " " + _res;

            return 1;
        }
        //----------------------------------------------------
        #endregion
    }


    
}
