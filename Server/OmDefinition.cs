using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Server
{

    public class Def
    {
        public static Int32 UpdateDefinition(string _path, string _key, string _value, ref string _error)
        {
            FileStream stream;
            StreamReader reader;
            Int32 lineCounter;
            Int32 lineToUpdate;
            Int32 keyCounter;
            string line;
            string[] lines;
            string[] parts;


            if (!File.Exists(_path))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "FILE_DOES_NOT_EXIST";
                return -1;
            }

            if (_key == "" || _key.Any(Char.IsWhiteSpace))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_KEY";
                return -1;
            }


            #region find line to update
            //........................................
            try
            {
                stream = new FileStream(_path, FileMode.Open);
                reader = new StreamReader(stream);

                lineCounter = -1;
                lineToUpdate = -1;
                keyCounter = 0;

                while (reader.Peek() >= 0)                
                {

                    line = reader.ReadLine();
                    lineCounter++;

                    if (line[0] == '#')
                        continue;

                    parts = line.Split();
                    
                    if (parts.Length != 2)
                    {
                        reader.Close();
                        stream.Close();

                        _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                        _error += "INVALID_LINE_FOUND " + lineCounter.ToString();
                        return -1;
                    }

                    if (parts[0] == _key + ":")
                    {
                        lineToUpdate = lineCounter;
                        keyCounter++;
                    }
                }

                reader.Close();
                stream.Close();

                if (keyCounter < 0)
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "KEY_NOT_FOUND";
                    return -1;
                }
                else if (keyCounter > 1)
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "KEY_NOT_UNIQUE";
                    return -1;
                }
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CRTICAL_ERROR";
                return -1;
            }
            //........................................
            #endregion



            #region update line
            //........................................
            try
            {
                lines = File.ReadAllLines(_path);
                lines[lineToUpdate] = _key + ":\t" + _value;
                File.WriteAllLines(_path, lines);
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CRTICAL_ERROR";
                return -1;
            }
            //........................................
            #endregion


            return 1;
        }
        public static Int32 ReadDefinition(string _path, string _key, ref string _value, ref string _error)
        {
            FileStream stream;
            StreamReader reader;
            Int32 lineCounter;
            Int32 keyCounter;
            string line;
            string[] parts;

            if (!File.Exists(_path))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "FILE_DOES_NOT_EXIST";
                return -1;
            }

            if (_key == "" || _key.Any(Char.IsWhiteSpace))
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "INVALID_KEY";
                return -1;
            }

            try
            {
                stream = new FileStream(_path, FileMode.Open);
                reader = new StreamReader(stream);

                lineCounter = -1;
                keyCounter = 0;
                while (reader.Peek() >= 0)
                {
                    line = reader.ReadLine();                    

                    if (line[0] == '#')
                        continue;

                    parts = line.Split();

                    if (parts.Length != 2)
                    {
                        reader.Close();
                        stream.Close();

                        _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                        _error += "INVALID_LINE_FOUND " + lineCounter.ToString();
                        return -1;
                    }

                    if (parts[0] == _key + ":")
                    {
                        _value = parts[1];
                        keyCounter++;
                    }
                }

                reader.Close();
                stream.Close();

                if (keyCounter < 0)
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "KEY_NOT_FOUND";
                    return -1;
                }
                else if (keyCounter > 1)
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "KEY_NOT_UNIQUE";
                    return -1;
                }
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CRTICAL_ERROR";
                return -1;
            }

            return 1;
        }
    }
    
}
