using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Threading;

namespace Server.OmEpics
{
    public class CA
    {
		#region Const
		//----------------------------------------------------
		private const Int32 OM_EPICS_SUCCESS = 0;			
		private const string OM_EPICS_DLL_VERSION	= "om_epics.dll";
		private const string OM_EPICS_DLL_PATH		= "C:\\SSRL_LOCAL_OM\\dll\\" + OM_EPICS_DLL_VERSION;
		//----------------------------------------------------
		#endregion


		#region caget
		//----------------------------------------------------
		public static Int32 get(string _name, string _pv_name, out string _valStr)
		{
			Int32 rt;
			bool err = false;
			IntPtr valPtr;

			try
			{
				rt = ext_om_epics_caget(_name, _pv_name, out valPtr);
				if (rt != OM_EPICS_SUCCESS || valPtr == IntPtr.Zero)
				{
					_valStr = "";
					err = true;
				}
				else
				{
					_valStr = Marshal.PtrToStringAnsi(valPtr);
				}				
			}
			catch (Exception ex)
			{
				_valStr = "";
				err = true;
			}

			GC.Collect();
			return err ? -1 : 1 ;
		}
		public static Int32 get(string _name, string _pv_name, out Int32 _val)
		{
			Int32 rt;
			bool err = false;
			string valStr;

			try
			{
				rt = get(_name, _pv_name, out valStr);
				if (rt < 0 || valStr == null)
				{
					_val = 0;
					err = true;
				}
                else
                {
					_val = (Int32)Double.Parse(valStr);
				}
			}
			catch (Exception ex)
			{
				_val = 0;
				err = true;
			}
			
			return err ? -1 : 1;
		}
		public static Int32 get(string _name, string _pv_name, out double _val)
		{
			Int32 rt;
			bool err = false;
			string valStr;

			try
			{
				rt = get(_name, _pv_name, out valStr);
				if (rt < 1 || valStr == null)
				{
					_val = 0;
					err = true;
				}
				else
				{
					_val = Double.Parse(valStr);
				}
			}
			catch(Exception ex)
			{
				_val = 0;
				err = true;
			}
						
			return err ? -1 : 1;
		}
		//----------------------------------------------------
		public static Int32 get_arr(string _name, string _pv_name, out UInt32[] _arr)
		{
			Int32 rt;
			bool err = false;
			IntPtr intPtr;
			Int32 count = 0;

			try
			{
				rt = ext_om_epics_caget_arr(_name, _pv_name, ref count, out intPtr);
				if (rt != OM_EPICS_SUCCESS || intPtr == IntPtr.Zero)
				{
					_arr = new UInt32[1];
					err = true;
				}
				else
				{
					_arr = new UInt32[count];

					//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
					unsafe
					{
						var SourcePtr = (UInt32*)intPtr;
						for (int i = 0; i < count; i++)
						{
							_arr[i] = *SourcePtr;
							SourcePtr++;
						}
					}
					//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

					rt = ext_om_epics_free_arr(intPtr);

				}
			}
			catch (Exception ex)
			{
				_arr = new UInt32[1];
				err = true;
			}


			GC.Collect();

			return err ? -1 : 1;
		}
		//----------------------------------------------------
		#endregion


		#region caput
		//----------------------------------------------------
		public static Int32 put(string _name, string _pv_name, object _val)
		{
			Int32 rt;
			bool err = false;

			try
			{
				rt = ext_om_epics_caput(_name, _pv_name, _val.ToString());
				if (rt != OM_EPICS_SUCCESS)
					err = true;
			}
			catch (Exception ex)
			{
				err = true;
			}

			GC.Collect();

			return err ? -1 : 1;
		}
		//----------------------------------------------------
		#endregion


		#region DLL Import - OM EPICS	
		//----------------------------------------------------
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_caput(string _name, string _pv_name, string _value);
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_caget(string _name, string _pv_name, out IntPtr _value);
		//----------------------------------------------------
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_caget_arr(string _name, string _pv_name, ref Int32 _count, out IntPtr _buf);
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_free_arr(IntPtr _buf);
		//----------------------------------------------------
		#endregion
	}



	public class Camonitor
	{
		#region Const
		//----------------------------------------------------
		private const Int32 OM_EPICS_SUCCESS = 0;
		private const string OM_EPICS_DLL_VERSION	= "om_epics.dll";
		private const string OM_EPICS_DLL_PATH		= "C:\\SSRL_LOCAL_OM\\dll\\" + OM_EPICS_DLL_VERSION;
		//----------------------------------------------------
		private const int EPICS_STRING	= 0;
		private const int EPICS_INT		= 1;
		private const int EPICS_SHORT	= 1;
		private const int EPICS_FLOAT	= 2;
		private const int EPICS_ENUM	= 3;
		private const int EPICS_CHAR	= 4;
		private const int EPICS_LONG	= 5;
		private const int EPICS_DOUBLE	= 6;
		//----------------------------------------------------
		#endregion



		#region Private Variables
		//----------------------------------------------------
		private string __name;
		private string __pvName;
		//----------------------------------------------------
		private cbString __cbDebugMsg;
		private cbString __cbErrorMsg;
		private cbString __cbEventMsg;
		//----------------------------------------------------
		private Thread __monitorThread;
		private OmEpicsMonitorCb __cb;
		private IntPtr __pvPtr;
		//----------------------------------------------------
		#endregion



		#region Properties
		//----------------------------------------------------
		public string Name
		{
			get { return __name; }
		}
		public string PvName
		{
			get { return __pvName; }
		}
		//----------------------------------------------------
		#endregion



		#region Events
		//----------------------------------------------------
		public event OmEpicsMessageEventHandler DebugMessageReceived;
		public event OmEpicsMessageEventHandler ErrorMessageReceived;
		public event OmEpicsMessageEventHandler EventMessageReceived;
		//----------------------------------------------------
		public event OmEpicsMonitorEventHandler MonitorEvent;
		//----------------------------------------------------
		#endregion



		#region Delegates
		//----------------------------------------------------
		private delegate void cbString(string str);
		//----------------------------------------------------
		#endregion



		//====================================================		
		public Camonitor(string _Name, string _pvName)
		{
			__cbDebugMsg = new cbString(IncomingDebugMessage);
			__cbErrorMsg = new cbString(IncomingErrorMessage);
			__cbEventMsg = new cbString(IncomingEventMessage);

			ext_debug_msg(__cbDebugMsg);
			ext_error_msg(__cbErrorMsg);
			ext_event_msg(__cbEventMsg);

			__name = _Name;
			__pvName = _pvName;
		}
		~Camonitor()
		{
			//StopMonitor();
		}
		//====================================================






		#region Private Methods
		//----------------------------------------------------
		private void StartMonitorDllCall(string _name, string _pvName, OmEpicsMonitorCb _cb)
		{
			Int32 rt;
			string debugMsg;
			string errMsg;

			try
			{
				rt = ext_om_epics_camonitor(__pvPtr, _cb);
				if (rt != OM_EPICS_SUCCESS)
				{
					errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
					errMsg += "DLL_ERROR " + this.Name + this.PvName;
					EmitErrorMessage(errMsg);
				}
			}
			catch (Exception ex)
			{
				errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				errMsg += "CRITICAL_ERROR " + this.Name + this.PvName + "\r" + ex.ToString();
				EmitErrorMessage(errMsg);
			}
			finally
			{
				debugMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				debugMsg += "THREAD_TERMINATED " + this.Name + this.PvName;
				EmitDebugMessage(debugMsg);

				ext_om_epics_free_pv(__pvPtr);
			}
		}
		//----------------------------------------------------
		#endregion



		#region Private Methods - Events
		//----------------------------------------------------
		private void EmitDebugMessage(string _msg)
		{
			if (DebugMessageReceived != null)
			{
				OmEpicsMessageEventArg arg = new OmEpicsMessageEventArg();
				arg.Name = this.Name;
				arg.PvName = this.PvName;
				arg.Message = _msg;

				DebugMessageReceived(this, arg);			
			}			
		}
		private void EmitErrorMessage(string _msg)
		{
			if (ErrorMessageReceived != null)
			{
				OmEpicsMessageEventArg arg = new OmEpicsMessageEventArg();
				arg.Name = this.Name;
				arg.PvName = this.PvName;
				arg.Message = _msg;

				ErrorMessageReceived(this, arg);
			}
		}
		private void EmitEventMessage(string _msg)
		{
			if (EventMessageReceived != null)
			{
				OmEpicsMessageEventArg arg = new OmEpicsMessageEventArg();
				arg.Name = this.Name;
				arg.PvName = this.PvName;
				arg.Message = _msg;

				EventMessageReceived(this, arg);
			}
		}
		//----------------------------------------------------		
		private void IncomingDebugMessage(string _msg)
		{
			EmitDebugMessage("DLL " + _msg);
		}
		private void IncomingErrorMessage(string _msg)
		{
			EmitErrorMessage("DLL " + _msg);
		}
		private void IncomingEventMessage(string _msg)
		{
			EmitEventMessage("DLL" + _msg);
		}
		//----------------------------------------------------
		private void InternalCbMonitorEvent(IntPtr _ptrDbr, IntPtr _ptrPv)
		{
			om_epics_pv pv;
			Int32[][] dbr;
			Int32 dbrLen;
			Int32 count;


			pv = (om_epics_pv)Marshal.PtrToStructure((IntPtr)_ptrPv, typeof(om_epics_pv));
			count = (Int32)pv.count;

			switch (pv.type_id)
			{
				case EPICS_STRING:              // char[40]
					dbrLen = 10;
					break;

				case EPICS_INT:                 // short			Int16
					dbrLen = 1;
					break;

				case EPICS_FLOAT:               // float			Float32
					dbrLen = 1;
					break;

				case EPICS_ENUM:                // unsigned short	UInt 16
					dbrLen = 1;
					break;

				case EPICS_CHAR:                // unsigned char	UInt8
					dbrLen = 1;
					break;

				case EPICS_LONG:                // int				Int32
					dbrLen = 1;
					break;

				case EPICS_DOUBLE:              // double			Float64
					dbrLen = 2;
					break;

				default:                        // DEFAULT
					dbrLen = 1;
					break;
			}

			dbr = new Int32[count][];
			for (Int32 i = 0; i < count; i++)
			{
				dbr[i] = new Int32[dbrLen];
			}


			//..........................................
			unsafe
			{
				var SourcePtr = (Int32*)_ptrDbr;
				for (Int32 i = 0; i < count; i++)
				{
					for (Int32 j = 0; j < dbrLen; j++)
					{
						dbr[i][j] = *SourcePtr;
						SourcePtr++;
					}
				}
			}
			//..........................................

			EmitMonitorEvent(dbr, (Int32)pv.type_id);

		}
		private void EmitMonitorEvent(Int32[][] _dbr, Int32 _pvType)
		{
			if (MonitorEvent != null)
			{
				OmEpicsMonitorEventArg arg = new OmEpicsMonitorEventArg();
				arg.Name = this.Name;
				arg.PvName = this.PvName;
				arg.PvType = _pvType;
				arg.dbr = _dbr;

				MonitorEvent(this, arg);
			}
		}
		//----------------------------------------------------
		#endregion



		#region Public Methods - Start & Stop
		//----------------------------------------------------
		public Int32 StartMonitor()
		{
			Int32 rt;
			string debugMsg;
			string errMsg;
			GC.Collect();

			if (__monitorThread != null && __monitorThread.IsAlive)
			{
				errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				errMsg += "THREAD_COLLISION " + this.Name + this.PvName;
				EmitErrorMessage(errMsg);
				return -1;
			}


			rt = ext_om_epics_create_pv(this.Name, this.PvName, out __pvPtr);
			if (rt != OM_EPICS_SUCCESS)
			{
				errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				errMsg += "DLL_ERROR " + this.Name + this.PvName;
				EmitErrorMessage(errMsg);
				return -1;
			}

			__cb = new OmEpicsMonitorCb(InternalCbMonitorEvent);

			__monitorThread = new Thread(() => StartMonitorDllCall(this.Name, this.PvName, __cb));
			__monitorThread.IsBackground = true;
			__monitorThread.Start();

			debugMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
			debugMsg += "THREAD_STARTED " + this.Name + this.PvName;
			EmitDebugMessage(debugMsg);

			return 1;
		}
		public Int32 StopMonitor()
		{
			Int32 rt;
			string debugMsg;
			string errMsg;

			#region Check
			//---------------------------------------------------------------
			if (__pvPtr == null || __pvPtr == IntPtr.Zero)
			{
				errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				errMsg += "POINTER_IS_NULL " + this.Name + this.PvName;
				EmitErrorMessage(errMsg);				
				return -1;
			}

			if (__monitorThread == null)
			{
				errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				errMsg += "THREAD_IS_NULL " + this.Name + this.PvName;
				EmitErrorMessage(errMsg);
				return -1;
			}

			if (!__monitorThread.IsAlive)
			{
				errMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
				errMsg += "THREAD_IS_NOT_ALIVE " + this.Name + this.PvName;
				EmitErrorMessage(errMsg);
				
				return -1;
			}
			//---------------------------------------------------------------
			#endregion


			debugMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
			debugMsg += "CALLING " + "ext_om_epics_clear_camonitor";
			EmitDebugMessage(debugMsg);
			
			ext_om_epics_clear_camonitor(__pvPtr);

			debugMsg = this.GetType().Name + "." + System.Reflection.MethodBase.GetCurrentMethod().Name + " ";
			debugMsg += "TERMINATED " + "ext_om_epics_clear_camonitor";
			EmitDebugMessage(debugMsg);			

			return 1;
		}
		//----------------------------------------------------
		#endregion



		#region Public Methods - CreatePV & FreePV
		//----------------------------------------------------
		public Int32 CreatePv(string _name, string _pvName, out IntPtr _pv)
		{
			Int32 rt;
			rt = ext_om_epics_create_pv(_name, _pvName, out _pv);
			if (rt != OM_EPICS_SUCCESS)
				return -1;

			//om_epics_pv pv;
			//pv = (om_epics_pv)Marshal.PtrToStructure((IntPtr)_pv, typeof(om_epics_pv));

			return 1;
		}
		public Int32 FreePv(IntPtr _pv)
		{
			ext_om_epics_free_pv(_pv);
			return 1;
		}
		//----------------------------------------------------
		#endregion



		#region Public Mehtods - Conversions
		//----------------------------------------------------
		public Int32 DbrToRawUInt32(ref OmEpicsMonitorEventArg _arg, out UInt32[] _val)
		{
			int count_a = _arg.dbr.Length;
			int count_b = _arg.dbr[0][0];



			_val = new UInt32[count_b];

			for (int i = 0; i < _val.Length; i++)
			{
				_val[i] = (UInt32)_arg.dbr[i][0];
			}


			return 0;
		}
		public Int32 DbrToInt32(ref OmEpicsMonitorEventArg _arg, out Int32[] _val)
		{
			int i, j, k;
			byte[] tmp_byte_arr, byte_arr;

			int count = _arg.dbr.Length;

			_val = new Int32[count];

			for (i = 0; i < count; i++)
			{
				byte_arr = new byte[_arg.dbr[i].Length * 4];
				for (j = 0; j < _arg.dbr[i].Length; j++)
				{
					tmp_byte_arr = BitConverter.GetBytes(_arg.dbr[i][j]);
					for (k = 0; k < tmp_byte_arr.Length; k++)
					{
						byte_arr[k + j * 4] = tmp_byte_arr[k];
					}
				}

				switch (_arg.PvType)
				{
					case EPICS_STRING:
						return -1;
						break;

					case EPICS_INT:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (Int32)BitConverter.ToInt32(byte_arr, 0);
						break;

					case EPICS_FLOAT:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (Int32)BitConverter.ToDouble(byte_arr, 0);
						break;

					case EPICS_ENUM:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (Int32)BitConverter.ToUInt16(byte_arr, 0);
						break;

					case EPICS_CHAR:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (Int32)byte_arr[0];
						break;

					case EPICS_LONG:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (Int32)BitConverter.ToUInt32(byte_arr, 0);
						break;

					case EPICS_DOUBLE:
						if (_arg.dbr[i].Length != 2)
							return -1;
						_val[i] = (Int32)BitConverter.ToDouble(byte_arr, 0);
						break;

					default:
						return -1;
				}
			}

			return 0;
		}
		public Int32 DbrToDouble(ref OmEpicsMonitorEventArg _arg, out double[] _val)
		{
			int i, j, k;
			byte[] tmp_byte_arr, byte_arr;

			int count = _arg.dbr.Length;

			_val = new double[count];

			for (i = 0; i < count; i++)
			{
				byte_arr = new byte[_arg.dbr[i].Length * 4];
				for (j = 0; j < _arg.dbr[i].Length; j++)
				{
					tmp_byte_arr = BitConverter.GetBytes(_arg.dbr[i][j]);
					for (k = 0; k < tmp_byte_arr.Length; k++)
					{
						byte_arr[k + j * 4] = tmp_byte_arr[k];
					}
				}

				switch (_arg.PvType)
				{
					case EPICS_STRING:
						return -1;
						break;

					case EPICS_INT:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (double)BitConverter.ToInt32(byte_arr, 0);
						break;

					case EPICS_FLOAT:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (double)BitConverter.ToDouble(byte_arr, 0);
						break;

					case EPICS_ENUM:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (double)BitConverter.ToUInt16(byte_arr, 0);
						break;

					case EPICS_CHAR:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (double)byte_arr[0];
						break;

					case EPICS_LONG:
						if (_arg.dbr[i].Length != 1)
							return -1;
						_val[i] = (double)BitConverter.ToUInt32(byte_arr, 0);
						break;

					case EPICS_DOUBLE:
						if (_arg.dbr[i].Length != 2)
							return -1;
						_val[i] = (double)BitConverter.ToDouble(byte_arr, 0);
						break;

					default:
						return -1;
				}
			}

			return 0;
		}
		//----------------------------------------------------
		#endregion



		#region DLL Import - OM EPICS	
		//----------------------------------------------------
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_debug_msg(cbString CallbackString);
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_event_msg(cbString CallbackString);
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_error_msg(cbString CallbackString);
		//----------------------------------------------------
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_camonitor(IntPtr _pvPtr, OmEpicsMonitorCb _cb);
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_clear_camonitor(IntPtr _pvPtr);
		//----------------------------------------------------
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_create_pv(string _name, string _pvName, out IntPtr _pvPtr);
		[DllImport(OM_EPICS_DLL_PATH)] private static extern int ext_om_epics_free_pv(IntPtr _pvPtr);
		//----------------------------------------------------
		#endregion
	}



	public class OmEpicsMessageEventArg
	{
		public string Name { get; set; }
		public string PvName { get; set; }
		public string Message { get; set; }
	}
	public class OmEpicsMonitorEventArg
	{
		public string Name { get; set; }
		public string PvName { get; set; }
		public Int32 PvType { get; set; }
		public Int32[][] dbr { get; set; }
	}
	


	public delegate void OmEpicsMessageEventHandler(object sender, OmEpicsMessageEventArg arg);
	public delegate void OmEpicsMonitorEventHandler(object sender, OmEpicsMonitorEventArg arg);
	public delegate void OmEpicsMonitorCb(IntPtr _ptr, IntPtr _ptrPv);



	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)] unsafe public struct om_epics_pv
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
		public string Name;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
		public string PvName;

		[MarshalAs(UnmanagedType.U8)]   //chid		// !8		
		public UInt64 channel_id;

		[MarshalAs(UnmanagedType.U8)]   //evid		// !8		
		public UInt64 event_id;

		[MarshalAs(UnmanagedType.U4)]   //chtype	// !4
		public UInt32 type_id;

		[MarshalAs(UnmanagedType.I4)]   // long		// !4
		public Int32 count;

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int event_cb(epics_event_handler_arg arg);

		[UnmanagedFunctionPointer(CallingConvention.StdCall)]
		public delegate int user_cb(IntPtr ptr, IntPtr pv_ptr);

		[MarshalAs(UnmanagedType.I4)]
		public Int32 onceConnected;
	}
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)] unsafe public struct epics_event_handler_arg
	{
		[MarshalAs(UnmanagedType.I8)]   //void
		public Int64 ptr_usr;

		[MarshalAs(UnmanagedType.U8)]   // chanId = chid
		public UInt64 chid;

		[MarshalAs(UnmanagedType.I4)]   // long
		public Int32 type_id;

		[MarshalAs(UnmanagedType.I4)]   // long
		public Int32 count;

		[MarshalAs(UnmanagedType.I8)]   // const void		
		public Int64 ptr_dbr;

		[MarshalAs(UnmanagedType.I4)]   // int
		public int status;
	}
}
