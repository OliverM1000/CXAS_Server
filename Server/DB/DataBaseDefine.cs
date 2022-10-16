using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.DB
{
    public class DataBaseConfig
    {

        #region Variables
        //----------------------------------------------------
        private string __server = "";
        private string __port = "";
        private string __user = "";
        private string __pwd = "";
		//----------------------------------------------------
		#endregion


		#region Properties
		//----------------------------------------------------
		//public string database { get; set; }
		//----------------------------------------------------
		public string Server { get { return __server; } }
        public string Port { get { return __port; } }
        public string User { get { return __user; } }
        public string PWD { get { return __pwd; } }        
        public string MySqlConnectionString { get { return ConnectionString(); } }
        //----------------------------------------------------
        #endregion



        //====================================================
        public DataBaseConfig()
        {
        
        }
        //====================================================


        #region Private Methods
        //----------------------------------------------------
        private string ConnectionString()
        {
            string conStr = "";

            if (this.Server.Length < 1)
                return "";

            if (this.Port.Length < 1)
                return "";

            if (this.User.Length < 1)
                return "";

            if (this.PWD.Length < 1)
                return "";            

            conStr = "server="		+ this.Server + ";";
            conStr += "port="		+ this.Port + ";";
            conStr += "user="		+ this.User + ";";
            conStr += "password="	+ this.PWD + ";";
            conStr += "SslMode=None;";

            return conStr;
        }
		//----------------------------------------------------
		#endregion


		#region Public Methods
		//----------------------------------------------------
		public int LoadDefinition(string _path, ref string _error)
		{
			FileStream stream;
			StreamReader reader;
			string line;
			string[] parts;
			bool err = false;

			bool serverRead = false;
			bool portRead = false;
			bool userRead = false;
			bool pwdRead = false;


			if (!File.Exists(_path))
			{
				_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				_error += "FILE_DOES_NOT_EXIST " + _path;
				return -1;
			}
			


			#region Reading File
			//----------------------------------------------------
			try
			{
				stream = new FileStream(_path, FileMode.Open);
				reader = new StreamReader(stream);

				while (reader.Peek() >= 0)
				{
					line = reader.ReadLine();					

					if (line[0] == '#')
						continue;

					parts = line.Split('\t');

					switch (parts[0])
					{
						case "SERVER:":
							if (serverRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "DUBLICATE_KEYWORD " + parts[0];
								break;
							}
							if (parts.Length != 2 || serverRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "INCOMPATIBLE_NUMBER_OF_ARGUMENTS " + parts[0];
								break;
							}
							__server = parts[1];
							serverRead = true;
							break;

						case "PORT:":
							if (portRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "DUBLICATE_KEYWORD " + parts[0];
								break;
							}
							if (parts.Length != 2 || portRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "INCOMPATIBLE_NUMBER_OF_ARGUMENTS " + parts[0];
								break;
							}
							portRead = true;
							__port = parts[1];
							break;

						case "USER:":
							if (userRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "DUBLICATE_KEYWORD " + parts[0];
								break;
							}
							if (parts.Length != 2 || userRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "INCOMPATIBLE_NUMBER_OF_ARGUMENTS " + parts[0];
								break;
							}
							userRead = true;
							__user = parts[1];
							break;

						case "PWD:":
							if (pwdRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "DUBLICATE_KEYWORD " + parts[0];
								break;
							}
							if (parts.Length != 2 || pwdRead)
							{
								err = true;
								_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
								_error += "INCOMPATIBLE_NUMBER_OF_ARGUMENTS " + parts[0];
								break;
							}
							pwdRead = true;
							__pwd = parts[1];
							break;

						default:
							err = true;
							_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
							_error += "UNKNOWN_KEYWORD " + parts[0];
							break;
					}
									
				}

				reader.Close();
				stream.Close();
			}
			catch (Exception ex)
			{
				_error = _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				_error += "EXCEPTION_DUMP\r" + ex.ToString();
				return -1;
			}
			//----------------------------------------------------
			#endregion

			if (err)
			{
				return -1;
			}

			return 1;
		}
		//----------------------------------------------------
		#endregion
	}



}
