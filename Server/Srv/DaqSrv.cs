using System.Text;

using MySql.Data.MySqlClient;

using Server.CXAS;
using Server.DB;
using Server.Numerics;
using Server.OmEpics;

namespace Server.Srv
{
    public class DaqSrv
    {
        #region Constants
        //----------------------------------------------------
        private const string PATH_DEF = "C:\\SSRL_LOCAL_OM\\CxasAcqServer\\Definitions\\fpga.def";        
        //----------------------------------------------------
        private const Int32 OFFSET_COMMAND  = 0;
        private const Int32 OFFSET_ARG_1    = 1;
        private const Int32 OFFSET_ARG_2    = 2;
        //----------------------------------------------------
        private const Int32 REQUEST_DAQ_LENGTH              = 1;
        private const Int32 REQUEST_GET_LENGTH              = 1;
        private const Int32 REQUEST_SET_LENGTH              = 2;
        private const Int32 REQUEST_OFFSETS_COUNT_LENGTH    = 3;
        private const Int32 REQUEST_OFFSET_ABORT_LENGTH     = 1;
        private const Int32 REQUEST_OFFSET_STATUS_LENGTH    = 1;
        private const Int32 REQUEST_OFFSET_GET_LENGTH       = 1;
        //----------------------------------------------------
        private const Int32 NUM_SAMPLES_MIN_LIMIT       = 0;
        private const Int32 NUM_SAMPLES_MAX_LIMIT       = 100;
        private const Int32 TRIGGER_WIDTH_MIN_LIMIT     = 0;
        private const Int32 TRIGGER_WIDTH_MAX_LIMIT     = 10000000;
        //----------------------------------------------------
        private const Int32 CACHE_FRAME_LIMIT           = 100000;
        private const Int32 CACHE_FRAME_OVERHEAD        = 500;
        private const Int32 FRAME_MAX_CHAR_LENGTH       = 516;
        //----------------------------------------------------
        private const Int32 MIN_PRESCALER   = 0;
        private const Int32 MAX_PRESCALER   = 9999;
        private const Int32 MIN_BASERATE    = 0;
        private const Int32 MAX_BASERATE    = 1;
        //----------------------------------------------------
        private const Int32 NUM_ADC         = 8;
        private const Int32 NUM_CNT         = 32;
        private const Int32 NUM_ENC         = 4;
        //----------------------------------------------------
        private const UInt32 REG_ADC        = 22;
        private const UInt32 REG_CNT        = 23;
        private const UInt32 REG_ENC        = 22;
        //----------------------------------------------------
        private const UInt32 BIT_MASK_ADC   = 0x3FC00;
        private const UInt32 BIT_MASK_CNT   = 0xFFFFFFFF;
        private const UInt32 BIT_MASK_ENC   = 0x3C0000;
        //----------------------------------------------------
        private const UInt32 BIT_SHIFT_ADC  = 10;
        private const UInt32 BIT_SHIFT_CNT  = 0;
        private const UInt32 BIT_SHIFT_ENC  = 18;
        //----------------------------------------------------
        #endregion
        


        #region Variables
        //----------------------------------------------------
        private OmDB                        __DB;
        private bool                        __initError = true;
        private string                      __fpgaName;
        private UInt32                      __triggerOutChannel;
        //----------------------------------------------------        
        private static List<FpgaDataFrame>  __cacheA;
        private static List<FpgaDataFrame>  __cacheB;
        private static bool                 __useCacheA = true;
        private static Mutex                __cacheMu;
        //----------------------------------------------------
        private static Camonitor            __camData;
        private static List<string>         __camDataErrorList;
        private static bool                 __camDataFirstEvent;
        private static bool                 __camDataError;
        //----------------------------------------------------
        private static Camonitor            __camTrig;
        private static List<string>         __camTrigErrorList;
        private static bool                 __camTrigError;
        private static bool                 __trigIsDisabled;
        //----------------------------------------------------
        private Thread                      __CollectSamplesThread;
        private static bool                 __collectOffsetsAbort;
        private static string               __collectOffsetsErrorString;        
        //----------------------------------------------------
        private static UInt32               __configAdc = 0xFF;
        private static UInt32               __configCnt = 0xFFFFFFFF;
        private static UInt32               __configEnc = 0x0F;
        //----------------------------------------------------
        #endregion



        #region Properties
        //----------------------------------------------------
        public bool InitError { get { return __initError; } }
        public string FpgaName { get { return __fpgaName; } }
        public UInt32 TriggerOutChannel { get { return __triggerOutChannel; } }
        //----------------------------------------------------
        #endregion



        //====================================================
        public DaqSrv(ref OmDB _db)
        {
            Int32 rt;
            string error = "";

            __DB = _db;
            __cacheMu = new Mutex();
            __cacheA = new List<FpgaDataFrame>(CACHE_FRAME_LIMIT + CACHE_FRAME_OVERHEAD);
            __cacheB = new List<FpgaDataFrame>(CACHE_FRAME_LIMIT + CACHE_FRAME_OVERHEAD);
            

            rt = Init(PATH_DEF, ref error);
            if (rt < 0)
            {
                error += System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                Console.WriteLine(error);
                __initError = true;
                return;
            }

            rt = InitCamonitor(ref error);
            if (rt < 0)
            {
                error += System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                Console.WriteLine(error);
                __initError = true;
                return;
            }
           
            __initError = false;
        }
        //====================================================



        #region Events - camonitor
        //----------------------------------------------------
        private void __camTrig_MonitorEvent(object sender, OmEpicsMonitorEventArg arg)
        {            
            Int32[] iVal;



            try
            {
                __camTrig.DbrToInt32(ref arg, out iVal);

                if (iVal[0] == 0)
                    __trigIsDisabled = true;
                else
                    __trigIsDisabled = false;
            }
            catch (Exception ex)
            {
                __camTrigError = true;
                __camTrigErrorList.Add(ex.ToString());
            }
        }
        private void __camTrig_ErrorMessageReceived(object sender, OmEpicsMessageEventArg arg)
        {
            __camTrigErrorList.Add(arg.Message);
        }
        //----------------------------------------------------
        private void __camData_MonitorEvent(object sender, OmEpicsMonitorEventArg arg)
        {
            Int32 rt;
            string error;
            UInt32[] fpgaBuffer;
            FpgaDataFrame[] fpgaDataFrames;



            // skip the first event which may contain obsolete data
            if (__camDataFirstEvent)
            {
                __camDataFirstEvent = false;
                return;
            }


            try
            {
                __camData.DbrToRawUInt32(ref arg, out fpgaBuffer);
                rt = FpgaDataFrame.EvalFpgaData(ref fpgaBuffer, out fpgaDataFrames);
                if (rt < 0)
                {
                    error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    error += FpgaDataFrame.ErrorCodeToString(rt);
                    __camDataError = true;
                    __camDataErrorList.Add(error);

                    return;
                }
            }
            catch (Exception ex)
            {
                fpgaDataFrames = new FpgaDataFrame[1];
                fpgaDataFrames[0] = new FpgaDataFrame();

                error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                error += ex.ToString();

                __camDataError = true;
                __camDataErrorList.Add(error);
            }


            // add data to cache
            //............................................
            __cacheMu.WaitOne();

            if (__useCacheA)
            {
                __cacheA.AddRange(fpgaDataFrames);
                if (__cacheA.Count > CACHE_FRAME_LIMIT)
                    __cacheA.RemoveRange(0, __cacheA.Count - CACHE_FRAME_LIMIT);
            }
            else
            {
                __cacheB.AddRange(fpgaDataFrames);
                if (__cacheB.Count > CACHE_FRAME_LIMIT)
                    __cacheB.RemoveRange(0, __cacheB.Count - CACHE_FRAME_LIMIT);
            }

            __cacheMu.ReleaseMutex();
            //............................................
        }
        private void __camData_ErrorMessageReceived(object sender, OmEpicsMessageEventArg arg)
        {
            __camDataErrorList.Add(arg.Message);
        }
        //----------------------------------------------------
        #endregion



        #region Private Methods
        //----------------------------------------------------
        private Int32 Init(string _pathDef, ref string _error)
        {
            Int32 rt;
            string value = "";


            #region read FPGA_NAME
            //....................................................................
            rt = Def.ReadDefinition(_pathDef, "FPGA_NAME", ref value, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            try
            {
                __fpgaName = value;
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }
            //....................................................................
            #endregion


            #region read TRIGGER_OUT_CHANNEL
            //....................................................................
            rt = Def.ReadDefinition(_pathDef, "TRIGGER_OUT_CHANNEL", ref value, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            try
            {
                __triggerOutChannel = Convert.ToUInt32(value);
            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }
            //....................................................................
            #endregion


            #region set .TRIG disable
            //....................................................................
            rt = CA.put(__fpgaName, ".TRIG", 0);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error = "CAPUT_FAILED";
                return -1;
            }
            //....................................................................
            #endregion


            #region set .TSRC to "FREE RUN"
            //....................................................................
            rt = CA.put(__fpgaName, ".TSRC", 0);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                _error = "CAPUT_FAILED";
                return -1;
            }
            //....................................................................
            #endregion


            return 1;
        }
        private Int32 InitCamonitor(ref string _error)
        {
            __camTrigErrorList = new List<string>();
            __camTrig = new Camonitor(__fpgaName, ".TRIG");
            __camTrig.ErrorMessageReceived += __camTrig_ErrorMessageReceived;
            __camTrig.MonitorEvent += __camTrig_MonitorEvent;


            __camDataErrorList = new List<string>();
            __camData = new Camonitor(__fpgaName, ".DATA");            
            __camData.ErrorMessageReceived += __camData_ErrorMessageReceived;
            __camData.MonitorEvent += __camData_MonitorEvent;

            return 1;
        }
        //----------------------------------------------------
        private string CamonitorErrorHistory(ref List<string> _error)
        {
            StringBuilder sb = new StringBuilder();
            

            for (Int32 i = 0; i < _error.Count; i++)
            {
                sb.Append("\r");
                sb.Append(_error[i]);
            }

            _error.Clear();
            return sb.ToString();
        }
        //----------------------------------------------------        
        private Int32 AverageFpgaDataFrames(ref List<FpgaDataFrame> _fpgaDataFrames, out FpgaDataFrame _average, ref string _error)
        {

            _average = new FpgaDataFrame();

            UInt64 gate = 0;
            UInt64[] ai = new UInt64[FpgaDataFrame.numMaxAi];
            UInt64[] counter = new UInt64[FpgaDataFrame.numMaxCounter];
            Int64[] encoder = new Int64[FpgaDataFrame.numMaxEncoder];
            UInt64[] motor = new UInt64[FpgaDataFrame.numMaxMotor];



            try
            {
                for (Int32 n = 0; n < _fpgaDataFrames.Count; n++)
                {
                    for (Int32 i = 0; i < FpgaDataFrame.numMaxAi; i++)
                    {
                        ai[i] += _fpgaDataFrames[n].ai[i];
                    }

                    for (Int32 i = 0; i < FpgaDataFrame.numMaxCounter; i++)
                    {
                        counter[i] += _fpgaDataFrames[n].counter[i];
                    }

                    for (Int32 i = 0; i < FpgaDataFrame.numMaxEncoder; i++)
                    {
                        encoder[i] += _fpgaDataFrames[n].encoder[i];
                    }

                    for (Int32 i = 0; i < FpgaDataFrame.numMaxMotor; i++)
                    {
                        motor[i] += _fpgaDataFrames[n].motor[i];
                    }

                    gate += _fpgaDataFrames[n].gate;

                }


                for (Int32 i = 0; i < FpgaDataFrame.numMaxAi; i++)
                {
                    _average.ai[i] = (UInt32)(ai[i] / (UInt64)_fpgaDataFrames.Count);
                }

                for (Int32 i = 0; i < FpgaDataFrame.numMaxCounter; i++)
                {
                    _average.counter[i] = (UInt32)(counter[i] / (UInt64)_fpgaDataFrames.Count);
                }

                for (Int32 i = 0; i < FpgaDataFrame.numMaxEncoder; i++)
                {
                    _average.encoder[i] = (Int32)(encoder[i] / (Int64)_fpgaDataFrames.Count);
                }

                for (Int32 i = 0; i < FpgaDataFrame.numMaxMotor; i++)
                {
                    _average.motor[i] = (UInt32)(motor[i] / (UInt64)_fpgaDataFrames.Count);
                }

                _average.gate = (UInt32)(gate / (UInt64)_fpgaDataFrames.Count);

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



        #region Private Methods - Support Methods
        //----------------------------------------------------
        private Int32 StartCamonitorData(ref string _error)
        {
            Int32 rt;

            __cacheA.Clear();
            __cacheB.Clear();            
            __camDataFirstEvent = true;

            rt = __camData.StartMonitor();
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "START_CAMONITOR_FAILED ";
                _error += CamonitorErrorHistory(ref __camDataErrorList);
                return -1;
            }

            return 1;
        }
        private Int32 StopCamonitorData(ref string _error)
        {
            Int32 rt;

            rt = __camData.StopMonitor();
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "STOP_CAMONITOR_FAILED ";
                _error += CamonitorErrorHistory(ref __camDataErrorList);
                return -1;
            }

            return 1;
        }
        //----------------------------------------------------
        private Int32 GetDaqInputConfig(ref string _error)
        {
            Int32 rt;
            string res = "";



            rt = GetInputConfig(REG_ADC, BIT_MASK_ADC, BIT_SHIFT_ADC, ref res, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            __configAdc = Convert.ToUInt32(res);


            rt = GetInputConfig(REG_CNT, BIT_MASK_CNT, BIT_SHIFT_CNT, ref res, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            __configCnt = Convert.ToUInt32(res);


            rt = GetInputConfig(REG_ENC, BIT_MASK_ENC, BIT_SHIFT_ENC, ref res, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }
            __configEnc = Convert.ToUInt32(res);


            return 1;
        }
        //----------------------------------------------------
        private Int32 WriteOffsets(ref FpgaOffsets _offsets, ref string _error)
        {
            Int32 rt;
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            string sqlString;
            StringBuilder sqlStringBuilder = new StringBuilder();


            try
            {
                #region build the SQL string
                //....................................................
                sqlStringBuilder.Append("INSERT INTO " + __DB.DbName + ".offsets (");
                sqlStringBuilder.Append("gate");
                for (Int32 i = 0; i < FpgaOffsets.numMaxAdc; i++)
                {
                    sqlStringBuilder.Append(", adc_" + i.ToString());
                }
                for (Int32 i = 0; i < FpgaOffsets.numMaxCnt; i++)
                {
                    sqlStringBuilder.Append(", cnt_" + i.ToString());
                }
                sqlStringBuilder.Append(") VALUES (");
                sqlStringBuilder.Append("@gate");
                for (Int32 i = 0; i < FpgaOffsets.numMaxAdc; i++)
                {
                    sqlStringBuilder.Append(", @adc_" + i.ToString());
                }
                for (Int32 i = 0; i < FpgaOffsets.numMaxCnt; i++)
                {
                    sqlStringBuilder.Append(", @cnt_" + i.ToString());
                }
                sqlStringBuilder.Append(");");
                //....................................................
                #endregion

                sqlString = sqlStringBuilder.ToString();

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);

                mySqlCommand.Parameters.Add("@gate", MySqlDbType.UInt32).Value = _offsets.gate;

                for (Int32 i = 0; i < _offsets.adc.Length; i++)
                {
                    mySqlCommand.Parameters.Add("@adc_" + i.ToString(), MySqlDbType.UInt32).Value = _offsets.adc[i];
                }

                for (Int32 i = 0; i < _offsets.cnt.Length; i++)
                {
                    mySqlCommand.Parameters.Add("@cnt_" + i.ToString(), MySqlDbType.UInt32).Value = _offsets.cnt[i];
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
        private Int32 ReadOffsets(out FpgaOffsets _offsets, ref string _error)
        {
            Int32 rt;
            MySqlConnection mySqlConnection;
            MySqlCommand mySqlCommand;
            MySqlDataReader mySqlDataReader;
            string sqlString;

            _offsets = new FpgaOffsets();

            try
            {
                sqlString = "SELECT * FROM " + __DB.DbName + ".offsets ORDER BY offsetCreate DESC LIMIT 1;";

                mySqlConnection = new MySqlConnection(__DB.Config.MySqlConnectionString);
                mySqlConnection.Open();
                mySqlCommand = new MySqlCommand(sqlString, mySqlConnection);
                mySqlDataReader = mySqlCommand.ExecuteReader();

                if (mySqlDataReader.Read())
                {
                    _offsets.gate = (UInt32)mySqlDataReader["gate"];

                    for (Int32 i = 0; i < FpgaOffsets.numMaxAdc; i++)
                    {
                        _offsets.adc[i] = (UInt32)mySqlDataReader["adc_" + i.ToString()];
                    }

                    for (Int32 i = 0; i < FpgaOffsets.numMaxCnt; i++)
                    {
                        _offsets.cnt[i] = (UInt32)mySqlDataReader["cnt_" + i.ToString()];
                    }

                    _offsets.dateTime = (DateTime)mySqlDataReader["offsetCreate"];
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
        private Int32 EnableDigitalOut(ref string _error)
        {
            Int32 rt;


            rt = CA.put(__fpgaName, ".DO1", 1);     // DISABLE
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CAPUT_FAILED";
                return -1;
            }

            rt = CA.put(__fpgaName, ".DO1", 4);     // ENABLE
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CAPUT_FAILED";
                return -1;
            }

            rt = CA.put(__fpgaName, ".OTP1", 1);    // GATE
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CAPUT_FAILED";
                return -1;
            }

            return 1;
        }
        //----------------------------------------------------
        private Int32 CollectSamples(UInt32 _numSamples, out List<FpgaDataFrame> _fpgaDataFrames, ref bool _abort, ref string _error)
        {
            Int32 rt;
            bool err = false;
            bool abort = false;
            UInt32[] data;
            FpgaDataFrame[] tmpFpgaDataFrames;

            _abort = false;
            _fpgaDataFrames = new List<FpgaDataFrame>();


            rt = __camTrig.StartMonitor();
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "START_CAMONITOR_FAILED ";
                _error += CamonitorErrorHistory(ref __camTrigErrorList);
                return -1;
            }


            for (Int32 i = 0; i < _numSamples; i++)
            {

                #region acquire a sample
                //....................................................................
                rt = CA.put(__fpgaName, ".TRIG", 1);
                if (rt < 0)
                {
                    err = true;
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "CAPUT_FAILED";
                    break;
                }


                // if the last CA.put was successful .TRIG must be ONCE, although the monitored variable __trigIsDisabled might still be TRUE
                // to ensure the code enters the while loop below, we set __trigIsDisabled to FALSE manually
                __trigIsDisabled = false;

                while (!__trigIsDisabled)
                {
                    Thread.Sleep(10);
                    // need a WATCHDOG here
                }
                //....................................................................
                #endregion


                #region read the sample
                //....................................................................
                rt = CA.get_arr(__fpgaName, ".DATA", out data);
                if (rt < 0)
                {
                    err = true;
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "CAGETARR_FAILED ";
                    break;
                }
                rt = FpgaDataFrame.EvalFpgaData(ref data, out tmpFpgaDataFrames);
                if (rt < 0)
                {
                    err = true;
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += FpgaDataFrame.ErrorCodeToString(rt);
                    break;
                }
                //....................................................................
                #endregion


                _fpgaDataFrames.Add(tmpFpgaDataFrames[0]);


                if (_abort)
                {
                    abort = true;
                    break;
                }
            }


            if (err || abort)
            {
                _fpgaDataFrames.Clear();
            }


            rt = __camTrig.StopMonitor();
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "STOP_CAMONITOR_FAILED ";
                _error += CamonitorErrorHistory(ref __camTrigErrorList);
                return -1;
            }

            return 1;
        }
        private Int32 CollectOffsets(UInt32 _numSamples, ref bool _abort, ref string _error)
        {
            Int32 rt;
            FpgaOffsets fpgaOffsets;
            FpgaDataFrame fpgaDataFrame;
            List<FpgaDataFrame> fpgaDataFrames;



            rt = CollectSamples(_numSamples, out fpgaDataFrames, ref _abort, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            rt = AverageFpgaDataFrames(ref fpgaDataFrames, out fpgaDataFrame, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            fpgaOffsets = new FpgaOffsets(fpgaDataFrame);

            rt = WriteOffsets(ref fpgaOffsets, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            return 1;
        }
        //----------------------------------------------------
        #endregion



        #region Private Methods - Evaluate Requests
        //----------------------------------------------------
        private Int32 GetInputConfig(UInt32 _regAdr, UInt32 _bitMask, UInt32 _bitShift, ref string _res, ref string _error)
        {
            Int32 rt;
            string hexString = "";
            UInt32 word;
            UInt32 config;


            rt = CA.put(__fpgaName, ".RIDX", _regAdr);
            if (rt < 0)
            {
                _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _res += "CAPUT_FAILED";
                return -1;
            }

            System.Threading.Thread.Sleep(10);

            rt = CA.get(__fpgaName, ".RVAL", out hexString);
            if (rt < 0)
            {
                _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _res += "CAGET_FAILED";
                return -1;
            }

            word = Convert.ToUInt32(hexString, 16);
            config = (UInt32)((Int32)(word & _bitMask) >> (Int32)_bitShift);
            _res = "\r" + config.ToString("F0");
            return 1;
        }
        private Int32 SetInputConfig(UInt32 _config, string _pvBase, Int32 _num, ref string _res, ref string _error)
        {
            Int32 rt;
            bool[] bitConfig;
            string pv;
            Int32 state;
            string format;


            if (_config < 0 || _config > Math.Pow(2, _num))
            {
                _error = "INVALID_CONFIG";
                return -1;
            }

            format = "D" + Convert.ToInt32(Math.Log10(_num)).ToString();
            BIT.get_high_bit_config(_config, out bitConfig);
            
            for (Int32 i = 0; i < _num; i++)
            {                
                pv = _pvBase + (i + 1).ToString(format);
                state = bitConfig[i] ? 1 : 0;
                rt = CA.put(__fpgaName, pv, state);
                if (rt < 0)
                {
                    _error = "CAPUT_FAILED";
                    return -1;
                }
            }

            return 1;
        }
        //----------------------------------------------------
        private Int32 StartDaq(ref string _error)
        {
            Int32 rt;            


            // get daq config
            rt = GetDaqInputConfig(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            // make sure DO is enabled
            rt = EnableDigitalOut(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            // start the camonitor
            __camDataError = false;
            rt = StartCamonitorData(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            // enable the trigger
            rt = CA.put(__fpgaName, ".TRIG", 2);
            if (rt < 0)
            {                
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CAPUT_FAILED";
                return -1;
            }


            return 1;
        }
        private Int32 StopDaq(ref string _error)
        {
            Int32 rt;
            bool err = false;
                        
            _error = "";


            // disable the trigger
            rt = CA.put(__fpgaName, ".TRIG", 0);
            if (rt < 0)
            {
                _error += System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CAPUT_FAILED";
                err = true;
                // don't return yet, try to stop the camonitor first
            }

            // stop the camonitor
            rt = StopCamonitorData(ref _error);
            if (rt < 0)
            {
                _error += System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }

            return err ? -1 : 1;
        }
        //----------------------------------------------------
        private Int32 Status(ref string _res, ref string _error)
        {
            Int32 rt;            
            Int32 iVal;
            double dVal;
            StringBuilder sb;
            DateTime dateTime;


            sb = new StringBuilder();

            
            // trigger
            //.............................
            rt = CA.get(__fpgaName, ".TRIG", out iVal);
            if (rt < 0)
            {                
                _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _res += "CAGET_FAILED";
                return -1;
            }
            sb.Append(" ");
            sb.Append(iVal.ToString());
            //.............................


            // trigger time
            //.............................
            rt = CA.get(__fpgaName, ".CTME", out dVal);
            if (rt < 0)
            {
                _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _res += "CAGET_FAILED";
                return -1;
            }
            sb.Append(" ");
            sb.Append(dVal.ToString());
            //.............................


            // cache length
            //.............................
            if (__useCacheA)
                iVal = __cacheA.Count;
            else
                iVal = __cacheB.Count;

            sb.Append(" ");
            sb.Append(iVal.ToString());
            //.............................


            // date time
            //.............................
            dateTime = DateTime.Now;
            sb.Append(" ");
            sb.Append(dateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            //.............................


            _res = sb.ToString();

            return 1;
        }
        //----------------------------------------------------
        private Int32 GetDaqData(ref string _res, ref string _error)
        {
            StringBuilder sb;
            

            // switch the cache
            //............................................
            __cacheMu.WaitOne();
            __useCacheA = __useCacheA ? false : true;
            __cacheMu.ReleaseMutex();
            //............................................            


            // get data from the idle cache
            if (!__useCacheA)
            {
                sb = new StringBuilder(__cacheA.Count * (1 + FRAME_MAX_CHAR_LENGTH));
                for (Int32 i = 0; i < __cacheA.Count; i++)
                {
                    sb.Append("\r");
                    sb.Append(__cacheA[i].ToString(__configAdc, __configCnt, __configEnc));
                }
                _res = sb.ToString();
                __cacheA.Clear();
            }
            else
            {
                sb = new System.Text.StringBuilder(__cacheB.Count * (1 + FRAME_MAX_CHAR_LENGTH));
                for (Int32 i = 0; i < __cacheB.Count; i++)
                {
                    sb.Append("\r");
                    sb.Append(__cacheB[i].ToString(__configAdc, __configCnt, __configEnc));
                }
                _res = sb.ToString();
                __cacheB.Clear();
            }
            
            return 1;
        }
        //----------------------------------------------------
        #endregion


        


        #region Private Methods - Evaluate Requests - Offsets
        //----------------------------------------------------
        private Int32 OffsetsCount(ref string[] _reqArr, ref string _res, ref string _error)
        {
            Int32 rt;
            Int32 numSamples;
            Int32 triggerWidth;


            if (__CollectSamplesThread != null && __CollectSamplesThread.IsAlive)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "THREAD_COLLISION";
                return -1;
            }


            try
            {
                numSamples      = Convert.ToInt32(_reqArr[OFFSET_ARG_1]);
                triggerWidth    = Convert.ToInt32(_reqArr[OFFSET_ARG_2]) - 1;

                if (numSamples < NUM_SAMPLES_MIN_LIMIT || numSamples > NUM_SAMPLES_MAX_LIMIT)
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "OUTSIDE_OF_LIMITS";
                    return -1;
                }

                if (triggerWidth < TRIGGER_WIDTH_MIN_LIMIT || triggerWidth > TRIGGER_WIDTH_MAX_LIMIT)
                {
                    _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                    _error += "OUTSIDE_OF_LIMITS";
                    return -1;
                }

            }
            catch (Exception ex)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "EXCEPTION_DUMP\r" + ex.ToString();
                return -1;
            }


            // make sure DO is enabled
            rt = EnableDigitalOut(ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                return -1;
            }


            rt = CA.put(__fpgaName, ".TWID", triggerWidth);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                _error += "CAPUT_FAILED";
                return -1;
            }
            
            
            __CollectSamplesThread = new Thread(() => CollectOffsets((UInt32)numSamples, ref __collectOffsetsAbort, ref __collectOffsetsErrorString));
            __CollectSamplesThread.IsBackground = true;
            __CollectSamplesThread.Start();


            return 1;
        }
        private Int32 OffsetsGet(ref string _res, ref string _error)
        {
            Int32 rt;
            FpgaOffsets fpgaOffsets;


            rt = ReadOffsets(out fpgaOffsets, ref _error);
            if (rt < 0)
            {
                _error = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;                
                return -1;
            }

            _res = fpgaOffsets.ToString();

            return 1;
        }
        private Int32 OffsetsStatus(ref string _res, ref string _error)
        {

            if (__CollectSamplesThread != null && __CollectSamplesThread.IsAlive)
            {
                _res = "1"; // busy
            }
            else
            {
                if (__collectOffsetsErrorString != null && __collectOffsetsErrorString.Length > 0)
                {
                    _error = __collectOffsetsErrorString;
                    return -1;
                }

                _res = "0"; // idle
            }

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
            double val = 0;
            UInt32 config = 0;
            string[] subReqArr;





            if (__DB.InitError)
            {
                _res = "DB_INIT_ERROR";
                err = true;
            }

            if (this.InitError)
            {
                _res = "DAQ_INIT_ERROR";
                err = true;
            }

            if (!err)
            {
                _res = "";
                error = "";
                switch (_reqArr[OFFSET_COMMAND].ToUpper())
                {
                    #region GET-SET_PRESCALER
                    //..................................  
                    case "GET_PRESCALER":
                        if (_reqArr.Length != REQUEST_GET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = CA.get(__fpgaName, ".TPSK", out val);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                            _res += "CAGET_FAILED";
                            break;
                        }
                        _res = "\r" + val.ToString("F0");
                        break;
                    //..................................


                    //..................................
                    case "SET_PRESCALER":
                        if (_reqArr.Length != REQUEST_SET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        if (    Convert.ToInt32(_reqArr[OFFSET_ARG_1]) < MIN_PRESCALER ||
                                Convert.ToInt32(_reqArr[OFFSET_ARG_1]) > MAX_PRESCALER)
                        {
                            err = true;
                            _res = "OUTSIDE_OF_LIMITS";
                            break;
                        }
                        rt = CA.put(FpgaName, ".TPSK", _reqArr[OFFSET_ARG_1]);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                            _res +="CAPUT_FAILED";
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region GET-SET_BASERATE
                    //..................................
                    case "GET_BASERATE":
                        if (_reqArr.Length != REQUEST_GET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = CA.get(__fpgaName, ".TBRT", out val);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                            _res += "CAGET_FAILED";
                            break;
                        }
                        _res = "\r" + val.ToString("F0");
                        break;
                    //..................................


                    //..................................
                    case "SET_BASERATE":
                        if (_reqArr.Length != REQUEST_SET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        if (    Convert.ToInt32(_reqArr[OFFSET_ARG_1]) < MIN_BASERATE ||
                                Convert.ToInt32(_reqArr[OFFSET_ARG_1]) > MAX_BASERATE)
                        {
                            err = true;
                            _res = "OUTSIDE_OF_LIMITS";
                            break;
                        }
                        rt = CA.put(FpgaName, ".TBRT", _reqArr[OFFSET_ARG_1]);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                            _res += "CAPUT_FAILED";
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region GET-SET_CONFIG_ADC
                    //..................................
                    case "GET_CONFIG_ADC":
                        if (_reqArr.Length != REQUEST_GET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = GetInputConfig(REG_ADC, BIT_MASK_ADC, BIT_SHIFT_ADC, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;                            
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }                        
                        break;
                    //..................................


                    //..................................                    
                    case "SET_CONFIG_ADC":
                        if (_reqArr.Length != REQUEST_SET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        if (_reqArr[OFFSET_ARG_1].ToUpper().Contains('X'))
                            config = Convert.ToUInt32(_reqArr[OFFSET_ARG_1], 16);
                        else
                            config = Convert.ToUInt32(_reqArr[OFFSET_ARG_1]);
                        rt = SetInputConfig(config, ".IAD", NUM_ADC, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region GET-SET_CONFIG_CNT
                    //..................................
                    case "GET_CONFIG_CNT":
                        if (_reqArr.Length != REQUEST_GET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = GetInputConfig(REG_CNT, BIT_MASK_CNT, BIT_SHIFT_CNT, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................


                    //..................................                    
                    case "SET_CONFIG_CNT":
                        if (_reqArr.Length != REQUEST_SET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        if (_reqArr[OFFSET_ARG_1].ToUpper().Contains('X'))
                            config = Convert.ToUInt32(_reqArr[OFFSET_ARG_1], 16);
                        else
                            config = Convert.ToUInt32(_reqArr[OFFSET_ARG_1]);
                        rt = SetInputConfig(config, ".IC", NUM_CNT, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region GET-SET_CONFIG_ENC
                    //..................................
                    case "GET_CONFIG_ENC":
                        if (_reqArr.Length != REQUEST_GET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = GetInputConfig(REG_ENC, BIT_MASK_ENC, BIT_SHIFT_ENC, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................


                    //..................................
                    case "SET_CONFIG_ENC":
                        if (_reqArr.Length != REQUEST_SET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        if (_reqArr[OFFSET_ARG_1].ToUpper().Contains('X'))
                            config = Convert.ToUInt32(_reqArr[OFFSET_ARG_1], 16);
                        else
                            config = Convert.ToUInt32(_reqArr[OFFSET_ARG_1]);
                        rt = SetInputConfig(config, ".IMT", NUM_ENC, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region START
                    //..................................
                    case "START":
                        if (_reqArr.Length != REQUEST_DAQ_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = StartDaq(ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region STOP
                    //..................................
                    case "STOP":
                        if (_reqArr.Length != REQUEST_DAQ_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = StopDaq(ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region STATUS
                    //..................................
                    case "STATUS":
                        if (_reqArr.Length != REQUEST_DAQ_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        rt = Status(ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    #region GET
                    //..................................
                    case "GET":
                        if (_reqArr.Length != REQUEST_GET_LENGTH)
                        {
                            err = true;
                            _res = "INCOMPATIBLE_REQUEST_LENGTH";
                            break;
                        }
                        if (__camDataError)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
                            _res += CamonitorErrorHistory(ref __camDataErrorList);
                            break;
                        }
                        rt = GetDaqData(ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                        //..................................
                        #endregion


                        

                    #region GET_OFFSETS
                    case "OFFSETS":
                        subReqArr = _reqArr.Skip(1).Take(_reqArr.Length - 1).ToArray();
                        rt = EvaluateRequestsOffsets(ref subReqArr, ref _res, ref error);
                        if (rt < 0)
                        {
                            err = true;
                            _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + error;
                            break;
                        }
                        break;
                    //..................................
                    #endregion


                    //..................................
                    default:
                        err = true;
                        _res = "UNKNOWN_COMMAND";
                        break;
                    //..................................
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
        public Int32 EvaluateRequestsOffsets(ref string[] _reqArr, ref string _res, ref string _error)
        {
            Int32 rt;
            bool err = false;            

            switch (_reqArr[OFFSET_COMMAND])
            {

                #region COUNT
                //..................................
                case "COUNT":
                    if (_reqArr.Length != REQUEST_OFFSETS_COUNT_LENGTH)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    rt = OffsetsCount(ref _reqArr, ref _res, ref _error);
                    if (rt < 0)
                    {
                        err = true;
                        _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                        break;
                    }
                    break;
                //..................................
                #endregion


                #region ABORT
                //..................................
                case "ABORT":
                    if (_reqArr.Length != REQUEST_OFFSET_ABORT_LENGTH)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    __collectOffsetsAbort = true;
                    __CollectSamplesThread.Join();
                    break;
                //..................................
                #endregion


                #region STATUS
                //..................................
                case "STATUS":
                    if (_reqArr.Length != REQUEST_OFFSET_STATUS_LENGTH)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    rt = OffsetsStatus(ref _res, ref _error);
                    if (rt < 0)
                    {
                        err = true;
                        _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                        break;
                    }
                    break;
                //..................................
                #endregion


                #region GET
                //..................................
                case "GET":
                    if (_reqArr.Length != REQUEST_OFFSET_GET_LENGTH)
                    {
                        err = true;
                        _res = "INCOMPATIBLE_REQUEST_LENGTH";
                        break;
                    }
                    rt = OffsetsGet(ref _res, ref _error);
                    if (rt < 0)
                    {
                        err = true;
                        _res = System.Reflection.MethodBase.GetCurrentMethod().Name + " " + _error;
                        break;
                    }
                    break;
                //..................................
                #endregion


                //..................................
                default:
                    err = true;
                    _error = "UNKNOWN_COMMAND";
                    break;
                    //..................................
            }


            return err ? -1 : 1;
        }
        //----------------------------------------------------
        #endregion

    }
}
