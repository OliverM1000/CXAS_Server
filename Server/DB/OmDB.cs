using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;



namespace Server.DB
{
    public class OmDB
    {
        #region Constants
        //---------------------------------------------------
        public readonly string[] INVALID_CHARS = { " ", "!", "@", "#", "$", "%", "^", "&", "*", "(", ")", "+", "=", "[", "]", "{", "}", "\\", "/", "|", "?", ".", ",", ";", ":", "'", ",", "`", "~" };
        //---------------------------------------------------
        #endregion



        #region Variables
        //---------------------------------------------------
        private DataBaseConfig __dbConfig;        
        private bool __initError;
        //----------------------------------------------------
        #endregion



        #region Properties
        //----------------------------------------------------
        public string DbName { get; set; }
        public bool InitError { get { return __initError; }  }
        public DataBaseConfig Config { get { return __dbConfig; } }
        //----------------------------------------------------
        #endregion



        //====================================================
        public OmDB()
        {
            __initError = true;
        }
        //====================================================



        #region Private Methods
        //----------------------------------------------------
        //----------------------------------------------------
        #endregion



        #region Public Methods
        //----------------------------------------------------
        public Int32 Init(string _pathDbDefine, ref string _error)
        {
            Int32 rt;
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;
            string dbVersion;

            __dbConfig = new DataBaseConfig();
            rt = __dbConfig.LoadDefinition(_pathDbDefine, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                __initError = true;
                return -1;
            }
            
            try
            {
                sqlString = "SELECT VERSION();";
                mySqlConnection = new MySqlConnection(__dbConfig.MySqlConnectionString);
                mySqlConnection.Open();                
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                dbVersion = (string)mySqlCommand.ExecuteScalar();
                mySqlConnection.Close();

                Console.WriteLine("Init DB: " + dbVersion);
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                __initError = true;
                return -1;
            }

            __initError = false;

            return 1;
        }
        //----------------------------------------------------
        public Int32 GetDbName(string _userName, ref string _dbName, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;


            try
            {
                sqlString = "SELECT dbName from cxas_user.user WHERE userName = '" + _userName + "';";

                mySqlConnection = new MySqlConnection(this.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);

                if (Convert.IsDBNull(mySqlCommand.ExecuteScalar()))
                    _dbName = "";
                else
                    _dbName = (string)mySqlCommand.ExecuteScalar();

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
        public Int32 ReadSqlFile(string _path, out string _sqlString, ref string _error)
        {
            FileStream fileStream;
            StreamReader reader;

            _sqlString = "";

            if (!File.Exists(_path))
            {
                _error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "FILE_DOES_NOT_EXIST " + _path;
                return -1;
            }


            try
            {
                fileStream = new FileStream(_path, FileMode.Open);
                reader = new StreamReader(fileStream);

                while (reader.Peek() >= 0)
                {
                    _sqlString += reader.ReadLine();
                }

                reader.Close();
                fileStream.Close();
            }
            catch (Exception ex)
            {
                _error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }

            return 1;
        }
        public Int32 ExecuteSql(string _sqlString, ref string _error)
        {
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;

            try
            {
                mySqlConnection = new MySqlConnection(__dbConfig.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(_sqlString, mySqlConnection);
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
        #endregion
















    }
}
