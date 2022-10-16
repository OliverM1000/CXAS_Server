using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using Server.DB;
using Server.CXAS;

namespace Server.Srv
{
    public class CRegionSrv
    {

        #region Constants                
        //----------------------------------------------------
        private const Int32 CREGION_S_LENGTH        = 11;
        private const Int32 CREGION_EXAFS_LENGTH    = 14;
        private const Int32 REQUEST_LIST_LENGTH     = 1;
        private const Int32 REQUEST_READ_LENGTH     = 2;
        private const Int32 REQUEST_WRITE_LENGTH    = 99;
        private const Int32 REQUEST_DELETE_LENGTH   = 2;
        //----------------------------------------------------
        private const Int32 OFFSET_COMMAND          = 0;
        //----------------------------------------------------
        private const Int32 OFFSET_TYPE             = 1;
        private const Int32 OFFSET_NAME             = 2;
        private const Int32 OFFSET_ELEMENT          = 3;
        private const Int32 OFFSET_EDGE             = 4;
        private const Int32 OFFSET_POINTS           = 5;
        private const Int32 OFFSET_EDGE_ENERGY      = 6;
        private const Int32 OFFSET_E1               = 7;
        private const Int32 OFFSET_E2               = 8;
        //----------------------------------------------------
        private const Int32 OFFSET_EDOT             = 9;
        private const Int32 OFFSET_EDOTDOT          = 10;
        //----------------------------------------------------
        private const Int32 OFFSET_K0               = 9;
        private const Int32 OFFSET_K0DOT            = 10;
        //----------------------------------------------------
        private const Int32 OFFSET_SCALING          = 11;
        private const Int32 OFFSET_TTA              = 12;        
        private const Int32 OFFSET_TTD              = 13;
        //----------------------------------------------------
        #endregion


        #region Variables
        //----------------------------------------------------
        private OmDB __DB;
        //----------------------------------------------------
        #endregion



        //====================================================
        public CRegionSrv(ref OmDB _db)
        {
            __DB = _db;
        }
        //====================================================



        #region Private Methods
        //----------------------------------------------------
        private Int32 LoadCRegionMotion(ref string[] _reqArr, ref string _error, out CRegionMotion _cRegionMotion)
        {
            Int32 rt;
            string res;
            CRegionSMotion cRegionSMotion;

            res = "";
            switch (_reqArr[OFFSET_TYPE].ToUpper())
            {
                case "S":
                    rt = LoadCRegionSMotion(ref _reqArr, ref res, ref _error, out _cRegionMotion);
                    if (rt < 0)
                    {
                        _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                        return -1;
                    }
                    break;

                case "EXAFS":
                    rt = LoadCRegionExafsMotion(ref _reqArr, ref res, ref _error, out _cRegionMotion);
                    if (rt < 0)
                    {
                        _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                        return -1;
                    }
                    break;

                default:
                    _cRegionMotion = new CRegionSMotion();
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "INCOMPATIBLE_TYPE";
                    return -1;
            }
            return 1;
        }
        private Int32 LoadCRegionSMotion(ref string[] _reqArr, ref string _res, ref string _error, out CRegionMotion _cRegionMotion)
        {
            _cRegionMotion = new CRegionSMotion();

            if (_reqArr.Length != CREGION_S_LENGTH)
            {
                _error = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + "INCOMPATIBLE_REQUEST_LENGTH";
                return -1;
            }

            ((CRegionSMotion)_cRegionMotion).Name = _reqArr[OFFSET_NAME];
            ((CRegionSMotion)_cRegionMotion).Element = _reqArr[OFFSET_ELEMENT];
            ((CRegionSMotion)_cRegionMotion).Edge = _reqArr[OFFSET_EDGE];
            ((CRegionSMotion)_cRegionMotion).Points = Convert.ToInt32(_reqArr[OFFSET_POINTS]);
            ((CRegionSMotion)_cRegionMotion).EEdge = Convert.ToDouble(_reqArr[OFFSET_EDGE_ENERGY]);
            ((CRegionSMotion)_cRegionMotion).E1 = Convert.ToDouble(_reqArr[OFFSET_E1]);
            ((CRegionSMotion)_cRegionMotion).E2 = Convert.ToDouble(_reqArr[OFFSET_E2]);

            ((CRegionSMotion)_cRegionMotion).EDot = Convert.ToDouble(_reqArr[OFFSET_EDOT]);
            ((CRegionSMotion)_cRegionMotion).EDotDot = Convert.ToDouble(_reqArr[OFFSET_EDOTDOT]);

            return 1;
        }
        private Int32 LoadCRegionExafsMotion(ref string[] _reqArr, ref string _res, ref string _error, out CRegionMotion _cRegionMotion)
        {
            _cRegionMotion = new CRegionExafsMotion();

            if (_reqArr.Length != CREGION_EXAFS_LENGTH)
            {
                _error = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + "INCOMPATIBLE_REQUEST_LENGTH";
                return -1;
            }

            ((CRegionExafsMotion)_cRegionMotion).Name = _reqArr[OFFSET_NAME];
            ((CRegionExafsMotion)_cRegionMotion).Element = _reqArr[OFFSET_ELEMENT];
            ((CRegionExafsMotion)_cRegionMotion).Edge = _reqArr[OFFSET_EDGE];
            ((CRegionExafsMotion)_cRegionMotion).TargetPoints = Convert.ToInt32(_reqArr[OFFSET_POINTS]);
            ((CRegionExafsMotion)_cRegionMotion).EEdge = Convert.ToDouble(_reqArr[OFFSET_EDGE_ENERGY]);
            ((CRegionExafsMotion)_cRegionMotion).E1 = Convert.ToDouble(_reqArr[OFFSET_E1]);
            ((CRegionExafsMotion)_cRegionMotion).E2 = Convert.ToDouble(_reqArr[OFFSET_E2]);

            ((CRegionExafsMotion)_cRegionMotion).k0 = Convert.ToDouble(_reqArr[OFFSET_K0]);
            ((CRegionExafsMotion)_cRegionMotion).k0Dot = Convert.ToDouble(_reqArr[OFFSET_K0DOT]);
            ((CRegionExafsMotion)_cRegionMotion).Scaling = Convert.ToInt32(_reqArr[OFFSET_SCALING]);
            ((CRegionExafsMotion)_cRegionMotion).TTA = Convert.ToDouble(_reqArr[OFFSET_TTA]);
            ((CRegionExafsMotion)_cRegionMotion).TTD = Convert.ToDouble(_reqArr[OFFSET_TTD]);

            return 1;
        }
        //----------------------------------------------------
        private Int32 CountCRegion(string _name, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;
            Int64 count;

            try
            {
                sqlString = "SELECT COUNT(*) FROM " + __DB.DbName + ".cregion WHERE name = \"" + _name + "\";";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);

                count = (Int64)mySqlCommand.ExecuteScalar();

                mySqlConnection.Close();
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }

            return (Int32)count;
        }
        //----------------------------------------------------
        #endregion



        #region Private Methods - Support Methods
        //----------------------------------------------------
        private Int32 GetCregion(out CRegion _cregion, string _name, ref string _error)
        {
            Int32 rt;
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            MySqlDataReader mySqlDataReader;
            string sqlString;
            
            _cregion = new CRegion();

            try
            {
                sqlString = "SELECT * FROM " + __DB.DbName + ".cregion WHERE name = \"" + _name + "\";";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                mySqlDataReader = mySqlCommand.ExecuteReader();

                while (mySqlDataReader.Read())
                {
                    _cregion.Type       = (string)mySqlDataReader["type"];
                    _cregion.Name       = (string)mySqlDataReader["name"];
                    _cregion.Created    = (DateTime)mySqlDataReader["cregionCreate"];
                    _cregion.Element    = (string)mySqlDataReader["element"];
                    _cregion.Edge       = (string)mySqlDataReader["edge"];
                    _cregion.Points     = (UInt32)mySqlDataReader["points"];

                    _cregion.EEdge      = (double)mySqlDataReader["edgeEnergy"];
                    _cregion.E1         = (double)mySqlDataReader["e1"];
                    _cregion.E2         = (double)mySqlDataReader["e2"];

                    _cregion.EDot       = (double)mySqlDataReader["eDot"];
                    _cregion.EDotDot    = (double)mySqlDataReader["eDotDot"];

                    _cregion.K0         = (double)mySqlDataReader["k0"];
                    _cregion.K0Dot      = (double)mySqlDataReader["k0Dot"];
                    _cregion.Scaling    = (Int32)mySqlDataReader["scaling"];
                    _cregion.TTA        = (double)mySqlDataReader["tta"];
                    _cregion.TTD        = (double)mySqlDataReader["ttd"];
                }

                mySqlDataReader.Close();
                mySqlConnection.Close();

            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }

            return 1;
        }
        private Int32 GetCRegionList(out List<CRegion> _cregions, ref string _error)
        {                        
            Int32 rt;            
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            MySqlDataReader mySqlDataReader;
            string sqlString;
            CRegion cregion;
            _cregions = new List<CRegion>();



            try
            {
                sqlString = "SELECT * FROM " + __DB.DbName + ".cregion ORDER BY element;";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                mySqlDataReader = mySqlCommand.ExecuteReader();

                while (mySqlDataReader.Read())
                {
                    cregion = new CRegion();

                    cregion.Type    = (string)mySqlDataReader["type"];
                    cregion.Name    = (string)mySqlDataReader["name"];
                    cregion.Created = (DateTime)mySqlDataReader["cregionCreate"];
                    cregion.Element = (string)mySqlDataReader["element"];
                    cregion.Edge    = (string)mySqlDataReader["edge"];
                    cregion.Points  = (UInt32)mySqlDataReader["points"];

                    cregion.EEdge = (double)mySqlDataReader["edgeEnergy"];
                    cregion.E1      = (double)mySqlDataReader["e1"];
                    cregion.E2      = (double)mySqlDataReader["e2"];
                    
                    cregion.EDot    = (double)mySqlDataReader["eDot"];
                    cregion.EDotDot = (double)mySqlDataReader["eDotDot"];

                    cregion.K0      = (double)mySqlDataReader["k0Dot"];
                    cregion.K0Dot   = (double)mySqlDataReader["k0Dot"];
                    cregion.Scaling = (Int32)mySqlDataReader["scaling"];
                    cregion.TTA     = (double)mySqlDataReader["tta"];
                    cregion.TTD     = (double)mySqlDataReader["ttd"];

                    _cregions.Add(cregion);
                }

                mySqlDataReader.Close();
                mySqlConnection.Close();
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }

            return 1;
        }
        private Int32 WriteCregion(ref CRegionMotion _cRegionMotion, ref string _error)
        {
            Int32 rt;            
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;
            

            try
            {
                sqlString = "INSERT INTO " + __DB.DbName + ".cregion (" +
                                "name, " +
                                "type, " +
                                "element, " +
                                "edge, " +
                                "points, " +
                                "edgeEnergy, " +
                                "e1, " +
                                "e2, " +
                                "eDot, " +
                                "eDotDot, " +
                                "k0, " +
                                "k0Dot, " +
                                "scaling, " +
                                "tta, " +
                                "ttd " +
                            ") VALUES (" +
                                "@name, " +
                                "@type, " +
                                "@element, " +
                                "@edge, " +
                                "@points, " +
                                "@edgeEnergy, " +
                                "@e1, " +
                                "@e2, " +
                                "@eDot, " +
                                "@eDotDot, " +
                                "@k0, " +
                                "@k0Dot, " +
                                "@scaling, " +
                                "@tta, " +
                                "@ttd " +
                            ");";


                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);

                
                mySqlCommand.Parameters.Add("@name", MySqlDbType.String).Value = _cRegionMotion.Name;
                mySqlCommand.Parameters.Add("@type", MySqlDbType.String).Value = _cRegionMotion.MotionType;
                
                if (_cRegionMotion.MotionType == "EXAFS")
                {
                    mySqlCommand.Parameters.Add("@element", MySqlDbType.String).Value       = ((CRegionExafsMotion)_cRegionMotion).Element;
                    mySqlCommand.Parameters.Add("@edge", MySqlDbType.String).Value          = ((CRegionExafsMotion)_cRegionMotion).Edge;
                    mySqlCommand.Parameters.Add("@points", MySqlDbType.Int32).Value         = ((CRegionExafsMotion)_cRegionMotion).TargetPoints;
                    mySqlCommand.Parameters.Add("@edgeEnergy", MySqlDbType.Double).Value    = ((CRegionExafsMotion)_cRegionMotion).EEdge;
                    mySqlCommand.Parameters.Add("@e1", MySqlDbType.Double).Value            = ((CRegionExafsMotion)_cRegionMotion).E1;
                    mySqlCommand.Parameters.Add("@e2", MySqlDbType.Double).Value            = ((CRegionExafsMotion)_cRegionMotion).E2;

                    mySqlCommand.Parameters.Add("@eDot", MySqlDbType.Double).Value          = 0;
                    mySqlCommand.Parameters.Add("@eDotDot", MySqlDbType.Double).Value       = 0;

                    mySqlCommand.Parameters.Add("@k0", MySqlDbType.Double).Value            = ((CRegionExafsMotion)_cRegionMotion).k0;
                    mySqlCommand.Parameters.Add("@k0Dot", MySqlDbType.Double).Value         = ((CRegionExafsMotion)_cRegionMotion).k0Dot;
                    mySqlCommand.Parameters.Add("@scaling", MySqlDbType.Double).Value       = ((CRegionExafsMotion)_cRegionMotion).Scaling;
                    mySqlCommand.Parameters.Add("@tta", MySqlDbType.Double).Value           = ((CRegionExafsMotion)_cRegionMotion).TTA;
                    mySqlCommand.Parameters.Add("@ttd", MySqlDbType.Double).Value           = ((CRegionExafsMotion)_cRegionMotion).TTD;

                    
                }
                else
                {
                    mySqlCommand.Parameters.Add("@element", MySqlDbType.String).Value       = ((CRegionSMotion)_cRegionMotion).Element;
                    mySqlCommand.Parameters.Add("@edge", MySqlDbType.String).Value          = ((CRegionSMotion)_cRegionMotion).Edge;
                    mySqlCommand.Parameters.Add("@points", MySqlDbType.Int32).Value         = ((CRegionSMotion)_cRegionMotion).Points;
                    mySqlCommand.Parameters.Add("@edgeEnergy", MySqlDbType.Double).Value    = ((CRegionSMotion)_cRegionMotion).EEdge;
                    mySqlCommand.Parameters.Add("@e1", MySqlDbType.Double).Value            = ((CRegionSMotion)_cRegionMotion).E1;
                    mySqlCommand.Parameters.Add("@e2", MySqlDbType.Double).Value            = ((CRegionSMotion)_cRegionMotion).E2;

                    mySqlCommand.Parameters.Add("@eDot", MySqlDbType.Double).Value          = ((CRegionSMotion)_cRegionMotion).EDot;
                    mySqlCommand.Parameters.Add("@eDotDot", MySqlDbType.Double).Value       = ((CRegionSMotion)_cRegionMotion).EDotDot;

                    mySqlCommand.Parameters.Add("@k0", MySqlDbType.Double).Value            = 0;
                    mySqlCommand.Parameters.Add("@k0Dot", MySqlDbType.Double).Value         = 0;
                    mySqlCommand.Parameters.Add("@scaling", MySqlDbType.Double).Value       = 0;
                    mySqlCommand.Parameters.Add("@tta", MySqlDbType.Double).Value           = 0;
                    mySqlCommand.Parameters.Add("@ttd", MySqlDbType.Double).Value           = 0;
                }

                mySqlCommand.ExecuteNonQuery();
                mySqlConnection.Close();
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }

            return 1;
        }
        private Int32 DeleteCregion(string _name, ref string _error)
        {
            Int32 rt;
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;            
            string sqlString;

            try
            {
                sqlString = "DELETE FROM " + __DB.DbName + ".cregion WHERE name = \"" + _name + "\";";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);

                mySqlCommand.ExecuteScalar();

                mySqlConnection.Close();
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }

            return 1;
        }
        //----------------------------------------------------
        #endregion



        #region Private Methods - Evaluate Requests        
        //----------------------------------------------------
        private Int32 Make(ref string[] _reqArr, ref string _res, ref string _error)
        {
            Int32 rt;
            CRegionMotion cRegionMotion;

            _error = "";
            rt = LoadCRegionMotion(ref _reqArr, ref _error, out cRegionMotion);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            rt = cRegionMotion.MakeTrajectory(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            _res = cRegionMotion.ToString();

            return 1;
        }
        private Int32 List(ref string _res, ref string _error)
        {
            Int32 rt;
            List<CRegion> cregions;

            rt = GetCRegionList(out cregions, ref _error);
            if (rt < 0)            
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            _res = "";
            for (Int32 i = 0; i < cregions.Count; i++)
            {
                _res += "\r";
                _res += cregions[i].Name + " ";
                _res += cregions[i].Element + " ";
                _res += cregions[i].Edge + " ";
                _res += cregions[i].Type + " ";
                _res += cregions[i].Created.ToString("yyyy-MM-dd");
            }

            return 1;
        }
        private Int32 Write(ref string[] _reqArr, ref string _res, ref string _error)
        {
            Int32 rt;
            CRegionMotion cRegionMotion;

         
            rt = LoadCRegionMotion(ref _reqArr, ref _error, out cRegionMotion);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            rt = CountCRegion(cRegionMotion.Name, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            else if (rt > 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_NAME";
                return -1;
            }

            rt = WriteCregion(ref cRegionMotion, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            _res = cRegionMotion.Name;

            return 1;
        }
        private Int32 Read(ref string[] _reqArr, ref string _res, ref string _error)
        {
            Int32 rt;
            CRegion cRegion;
            string name = _reqArr[1];

            rt = CountCRegion(name, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            else if (rt != 1)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_NAME";
                return -1;
            }

            rt = GetCregion(out cRegion, name, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            if (name != cRegion.Name)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_NAME";
                return -1;
            }

            _res = "\r" + cRegion.ToString();            

            return 1;
        }
        private Int32 Delete(ref string[] _reqArr, ref string _res, ref string _error)
        {
            Int32 rt;
            string name = _reqArr[1];

            rt = CountCRegion(name, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            else if (rt != 1)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_NAME";
                return -1;
            }

            rt = DeleteCregion(name, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            _res = name;

            return 1;
        }
        //----------------------------------------------------
        #endregion



        #region Public Methods
        //----------------------------------------------------
        public Int32 EvaluateRequest(ref string[] _reqArr, ref string _res)
        {
            Int32 rt;
            string error;
            bool err = false;



            if (__DB.InitError)
            {
                _res = "DB_INIT_ERROR";
                err = true;
            }

            if (!err)
            {
                _res = "";
                error = "";
                switch (_reqArr[OFFSET_COMMAND].ToUpper())
                {

                    #region MAKE
                    //..................................
                    case "MAKE":
                        if (    _reqArr.Length != CREGION_S_LENGTH &&
                                _reqArr.Length != CREGION_EXAFS_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = Make(ref _reqArr, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion

                    #region LIST                
                    //..................................                    
                    case "LIST":
                        if (_reqArr.Length != REQUEST_LIST_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = List(ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                        }
                        break;
                    //..................................
                    #endregion

                    #region READ                
                    //..................................
                    case "READ":
                        if (_reqArr.Length != REQUEST_READ_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = Read(ref _reqArr, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                        }
                        break;
                    //..................................
                    #endregion

                    #region WRITE                
                    //..................................
                    case "WRITE":
                        if (    _reqArr.Length != CREGION_S_LENGTH &&
                                _reqArr.Length != CREGION_EXAFS_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = Write(ref _reqArr, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion

                    #region DELETE
                    //..................................
                    case "DELETE":
                        if (_reqArr.Length != REQUEST_DELETE_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = Delete(ref _reqArr, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                        }
                        break;
                    //..................................
                    #endregion

                    default:
                        _res = "UNKNOWN_COMMAND";
                        break;                        
                }

            }

            if (err)
            {
                _res = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _res;
                return -1;
            }

            _res = _reqArr[OFFSET_COMMAND].ToUpper() + " " + _res;

            return 1;
        }
        //----------------------------------------------------
        #endregion

    }
}
