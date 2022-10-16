using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Server.DB;


namespace Server.Srv
{
    public class UserSrv
    {

        #region Constants
        //----------------------------------------------------
        private const string DB_PREFIX              = "cxas_";
        private const string PATH_SQL_CREATE_DB     = "C:\\SSRL_LOCAL_OM\\CxasAcqServer\\Definitions\\CreateNewSqlDatabase_v1.1.0.txt";
        private const string PATH_DEF               = "C:\\SSRL_LOCAL_OM\\CxasAcqServer\\Definitions\\user.def";
        //----------------------------------------------------
        private const Int32 OFFSET_COMMAND          = 0;
        private const Int32 OFFSET_USER_NAME        = 1;
        //----------------------------------------------------
        private const Int32 REQUEST_LIST_LENGTH     = 1;
        private const Int32 REQUEST_CREATE_LENGTH   = 2;
        private const Int32 REQUEST_SET_LENGTH      = 2;
        private const Int32 REQUEST_EXIST_LENGTH    = 2;
        private const Int32 REQUEST_DELETE_LENGTH   = 2;
        //----------------------------------------------------
        private const Int32 NAME_MIN_LENGTH         = 5;
        private const Int32 NAME_MAX_LENGTH         = 40;
        //----------------------------------------------------
        #endregion


        #region Variables
        //----------------------------------------------------
        private OmDB __DB;
        //----------------------------------------------------
        #endregion



        //====================================================
        public UserSrv(ref OmDB _db)
        {
            __DB = _db;
        }
        //====================================================



        #region Private Methods - Support Methods
        //----------------------------------------------------
        private bool ValidateDbPrefixs(string _name)
        {
            if (_name.Length <= DB_PREFIX.Length)
                return false;

            return _name.Substring(0, DB_PREFIX.Length) == DB_PREFIX;
        }
        private bool ValidateName(string _name, ref string _error)
        {
            if (    _name.Length < NAME_MIN_LENGTH ||
                    _name.Length > NAME_MAX_LENGTH)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "OUTSIDE_OF_LIMITS " + "_name.length";
                return false;
            }

            for (Int32 i = 0; i < __DB.INVALID_CHARS.Length; i++)
            {
                if (_name.Contains(__DB.INVALID_CHARS[i]))
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "INVALID_CHARS " + "_name";
                    return false;
                }

            }

            return true;
        }
        private bool ValidateNewDbName(string _userName, string _dbName, ref string _error)
        {
            Int64 count;

            if (!ValidateName(_dbName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }

            count = CountUsers(_userName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            else if (count != 1)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "USER_DOES_NOT_EXIST " + _userName;
                return false;
            }

            count = CountDBs(_dbName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            else if (count != 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "DB_DOES_EXIST " + _dbName;
                return false;
            }

            return true;
        }
        private bool ValidateNewUserName(string _userName, string _dbName, ref string _error)
        {
            Int64 count;

            if (!ValidateName(_userName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }

            count = CountUsers(_userName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            else if (count != 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "USER_DOES_EXIST " + _userName;
                return false;
            }

            count = CountDBs(_dbName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            else if (count != 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "DB_DOES_EXIST " + _dbName;
                return false;
            }

            return true;
        }
        private bool ValidateExistingUserName(string _userName, ref string _error)
        {
            Int32 count;

            #region validate userName
            //..................................
            if (!ValidateName(_userName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            //..................................
            #endregion


            #region verify existence of _userName
            //..................................
            count = CountUsers(_userName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            else if (count != 1)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_USERNAME " + _userName;
                return false;
            }
            //..................................
            #endregion


            return true;
        }
        private bool ValidateExistingDbName(string _dbName, ref string _error)
        {
            Int32 count;


            #region validate userName
            //..................................
            if (!ValidateName(_dbName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            //..................................
            #endregion


            #region verify existence of dbName
            //..................................
            count = CountDBs(_dbName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return false;
            }
            else if (count != 1)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_DBNAME " + _dbName;
                return false;
            }
            //..................................
            #endregion


            return true;
        }
        //----------------------------------------------------
        private Int32 GetUserList(out List<CxasUser> _users, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            MySqlDataReader mySqlDataReader;
            string sqlString;
            CxasUser user;
            _users = new List<CxasUser>();



            try
            {
                sqlString = "SELECT userName, dbName, userCreate FROM cxas_user.user ORDER BY userName;";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                mySqlDataReader = mySqlCommand.ExecuteReader();

                while (mySqlDataReader.Read())
                {
                    user = new CxasUser();
                    user.UserName = (string)mySqlDataReader["userName"];
                    user.Created = (DateTime)mySqlDataReader["userCreate"];

                    if (Convert.IsDBNull(mySqlDataReader["dbName"]))
                        user.DbName = "";
                    else
                        user.DbName = (string)mySqlDataReader["dbName"];

                    _users.Add(user);
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
        private Int32 GetDatabaseList(out List<string> _databases, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            MySqlDataReader mySqlDataReader;
            string sqlString;
            _databases = new List<string>();



            try
            {
                sqlString = "SHOW schemas;";
                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                mySqlDataReader = mySqlCommand.ExecuteReader();

                while (mySqlDataReader.Read())
                {
                    if (!ValidateDbPrefixs((string)mySqlDataReader["Database"]))
                        continue;

                    _databases.Add((string)mySqlDataReader["Database"]);
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
        //----------------------------------------------------
        private Int32 CountUsers(string _userName, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;
            Int64 count;



            try
            {
                sqlString = "SELECT COUNT(*) FROM cxas_user.user WHERE userName = \"" + _userName + "\";";

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
        private Int32 CountDBs(string _dbName, ref string _error)
        {
            Int32 rt;
            Int32 count;
            List<string> databases;

            rt = GetDatabaseList(out databases, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            count = 0;
            for (Int32 i = 0; i < databases.Count; i++)
            {
                if (databases[i] == _dbName)
                    count++;
            }

            return count;
        }
        //----------------------------------------------------
        private Int32 UpdateUserDbName(string _userName, string _dbName, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;



            try
            {
                sqlString = "UPDATE cxas_user.user " +
                            "SET dbName = \"" + _dbName + "\" " +
                            "WHERE userName = \"" + _userName + "\";";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
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
        //----------------------------------------------------
        private Int32 CreateNewUser(string _userName, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;



            if (!ValidateNewUserName(_userName, DB_PREFIX + _userName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            try
            {
                sqlString = "INSERT INTO cxas_user.user " +
                            "(userName) " +
                            "VALUES (@userName);";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();

                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                mySqlCommand.Parameters.Add("@userName", MySqlDbType.String).Value = _userName;

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
        private Int32 CreateNewDB(string _userName, ref string _error)
        {
            Int32 rt;
            string sqlString;



            if (!ValidateNewDbName(_userName, DB_PREFIX + _userName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            #region read SQL
            //..................................
            rt = __DB.ReadSqlFile(PATH_SQL_CREATE_DB, out sqlString, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion


            #region execute SQL
            //..................................
            rt = __DB.ExecuteSql(sqlString.Replace("<NAME>", DB_PREFIX + _userName), ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion


            #region update user entry
            //..................................
            rt = UpdateUserDbName(_userName, DB_PREFIX + _userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion
            

            return 1;
        }
        //----------------------------------------------------
        private Int32 DeleteUser(string _userName, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;



            if (!ValidateExistingUserName(_userName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            try
            {
                sqlString = "DELETE FROM cxas_user.user WHERE userName = \"" + _userName + "\";";

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
        private Int32 DeleteUserDb(string _dbName, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;



            if (!ValidateExistingDbName(_dbName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            try
            {
                sqlString = "DROP DATABASE `" + _dbName + "`;";

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
        private Int32 CreateUser(string _userName, ref string _res, ref string _error)
        {
            Int32 rt;

            _res = "";


            #region create new user
            //..................................
            rt = CreateNewUser(_userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion


            #region create new database
            //..................................
            rt = CreateNewDB(_userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion


            _res = _userName;

            return 1;
        }        
        private Int32 ListUsers(ref string _res, ref string _error)
        {
            Int32 rt;
            List<DB.CxasUser> users;

            rt = GetUserList(out users, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            
            _res = "";
            for (int i = 0; i < users.Count; i++)
            {
                _res += "\r";
                _res += users[i].ToString();
            }

            return 1;
        }
        private Int32 ListDBs(ref string _res, ref string _error)
        {
            Int32 rt;
            List<string> dbNames;

            rt = GetDatabaseList(out dbNames, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            _res = "";
            for (int i = 0; i < dbNames.Count; i++)
            {
                _res += "\r";
                _res += dbNames[i];
            }

            return 1;
        }
        private Int32 Exist(string _userName, ref string _res, ref string _error)
        {
            Int64 count;

            if (!ValidateName(_userName, ref _error))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            count = CountUsers(_userName, ref _error);
            if (count < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            else if (count > 1)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";                
                _error += "MULTIPLE_USERS_FOUND " + _userName;
                return -1;
            }

            if (count == 0)
                _res = "\rNO";
            else
                _res = "\rYES";

                return 1;
        }
        private Int32 Delete(string _userName, ref string _res, ref string _error)
        {
            Int32 rt;
            string dbName;

            string masterUser;
            string currentUser;


            //####################################################
            if (_userName != "aranda")
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_USERNAME";
                return -1;
            }
            //####################################################


            #region verify user permission
            //..................................
            currentUser = "";
            rt = ReadCurrentUser(ref currentUser, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            masterUser = "";
            rt = ReadMasterUser(ref masterUser, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            if (currentUser != masterUser)  // user must be master user
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "USER_NOT_AUTHORIZED";
                return -1;
            }

            if (_userName == masterUser)    // can't delete master user
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_USERNAME";
                return -1;
            }
            //..................................
            #endregion

            #region get database name
            //..................................
            dbName = "";
            rt = __DB.GetDbName(_userName, ref dbName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion


            #region delete user
            //..................................
            rt = DeleteUser(_userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion


            #region delete user's database
            //..................................
            rt = DeleteUserDb(dbName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            //..................................
            #endregion

            _res = _userName;

            return 1;
        }
        //----------------------------------------------------
        #endregion



        #region Public Methods
        //----------------------------------------------------
        public Int32 StoreCurrentUser(string _userName, ref string _error)
        {
            Int32 rt;

            rt = Def.UpdateDefinition(PATH_DEF, "CURRENT_USER", _userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            return 1;
        }
        public Int32 ReadCurrentUser(ref string _userName, ref string _error)
        {
            Int32 rt;

            rt = Def.ReadDefinition(PATH_DEF, "CURRENT_USER", ref _userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            return 1;
        }
        public Int32 ReadMasterUser(ref string _userName, ref string _error)
        {
            Int32 rt;

            rt = Def.ReadDefinition(PATH_DEF, "MASTER_USER", ref _userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            return 1;
        }
        //----------------------------------------------------
        public Int32 SetUser(string _userName, ref string _res, ref string _error)
        {
            Int32 rt;
            Int32 count;

            count = CountUsers(_userName, ref _error);
            if (count < 0)          // error occured
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            else if (count == 0)    // no user found
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "NO_USER_FOUND " + _userName;
                return -1;
            }
            else if (count > 1)     // multiple users found
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "MULTIPLE_USERS_FOUND " + _userName;
                return -1;
            }

            rt = StoreCurrentUser(_userName, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            _res = _userName;

            return 1;
        }
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

                    #region CREATE
                    //..................................
                    case "CREATE_USER":
                        if (_reqArr.Length != REQUEST_CREATE_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = CreateUser(_reqArr[OFFSET_USER_NAME].ToLower(), ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }                        
                        break;
                    //..................................
                    #endregion


                    #region LIST_USERS
                    //..................................                    
                    case "LIST_USERS":
                        if (_reqArr.Length != REQUEST_LIST_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = ListUsers(ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region LIST_DBS
                    //..................................                                                                
                    case "LIST_DBS":
                        if (_reqArr.Length != REQUEST_LIST_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = ListDBs(ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region EXIST
                    //..................................
                    case "EXIST":
                        if (_reqArr.Length != REQUEST_EXIST_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = Exist(_reqArr[OFFSET_USER_NAME].ToLower(), ref _res, ref error);
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
                        rt = Delete(_reqArr[OFFSET_USER_NAME].ToLower(), ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion



                    default:
                        err = true;
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
