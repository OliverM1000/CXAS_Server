using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.CXAS
{
    public class FpgaDataFrame
    {

		#region Variables
		//-----------------------------------------------------------------
		private const Int32 STRING_BUILDER_LEN	= 520;		// 1x 20 (Timer) + 1x 10 (Gate) + 8x 10 (ADC) + 32x 10 (CNT) + 4x (ENC) + 46 spaces = 516
		//-----------------------------------------------------------------
		public static Int32 numMaxCounter	= 32;
		public static Int32 numMaxAi		= 8;
		public static Int32 numMaxEncoder	= 4;
		public static Int32 numMaxMotor		= 4;
		//-----------------------------------------------------------------
		public UInt64 time;
		public UInt32 gate;
		public UInt32[]	ai;
		public UInt32[]	counter;
		public Int32[] encoder;
		public UInt32[]	motor;
		//-----------------------------------------------------------------
		#endregion



		//=================================================================
		public FpgaDataFrame()
		{
			ai = new UInt32[numMaxAi];
			counter = new UInt32[numMaxCounter];
			encoder = new Int32[numMaxEncoder];
			motor = new UInt32[numMaxMotor];
		}
		//=================================================================					



		#region Public Static Methods
		//-----------------------------------------------------------------
		public static int EvalFpgaData(ref UInt32[] _buffer, out FpgaDataFrame[] _DataFrames)
		{
			_DataFrames = new FpgaDataFrame[0];

			#region Variables
			int n;
			int i = 0;
			int j = 0;

			UInt32 word;

			UInt32 data_num_bytes		= 0;
			UInt32 user_data_per_frame	= 0;
			UInt32 num_frames			= 0;

			UInt32 frame_size;                // frame size in words (4 bytes), not the first frame
			UInt32 frame_offset;

			UInt32 config_config;
			UInt32 config_adc;
			UInt32 config_counter;
			UInt32 config_encoder;
			UInt32 config_motor;

			UInt32 num_trigger	= 2;
			UInt32 num_config	= 0;
			UInt32 num_counters	= 0;
			UInt32 num_adcs		= 0;
			UInt32 num_motors	= 0;
			UInt32 num_encoders	= 0;

			UInt32 offset_trigger;
			UInt32 offset_config;
			UInt32 offset_counter;
			UInt32 offset_adc;
			UInt32 offset_encoder;
			UInt32 offset_motor;
			UInt32 offset_ios;

			UInt32 status_motor_1;
			UInt32 status_motor_2;
			UInt32 status_motor_3;
			UInt32 status_motor_4;
			UInt32 status_digital_in;
			UInt32 status_dac;
			#endregion


			#region Definitions
			UInt32 OFFSET_DATA_LEN			= 0;
			UInt32 OFFSET_TRIG_REG_1		= 1;
			UInt32 OFFSET_TRIG_REG_2		= 2;
			UInt32 OFFSET_CONF_REG_1		= 3;
			UInt32 OFFSET_CONF_REG_2		= 4;

			UInt32 MASK_TRIGGER_TIME_MSB	= 0xFC000000;
			UInt32 MASK_TRIGGER_TIME_LSB	= 0x7FFFFFFF;
			UInt32 MASK_TRIGGER_WIDTH		= 0x3FFFFFF;

			UInt32 MASK_CONFIG				= 0x3F;
			UInt32 MASK_ADC					= 0x3FC00;
			UInt32 MASK_COUNTER				= 0xFFFFFFFF;
			UInt32 MASK_MOTOR_ENCODER		= 0x3C0000;

			UInt32 MASK_STATUS_MOTOR_1		= 0x1F;
			UInt32 MASK_STATUS_MOTOR_2		= 0x3E0;
			UInt32 MASK_STATUS_MOTOR_3		= 0x7C00;
			UInt32 MASK_STATUS_MOTOR_4		= 0xF8000;

			UInt32 MASK_MOTOR_CW_LIMIT		= 0x01;
			UInt32 MASK_MOTOR_CCW_LIMIT		= 0x02;
			UInt32 MASK_MOTOR_INDEX_HIT		= 0x04;
			UInt32 MASK_MOTOR_CW_MOVING		= 0x08;
			UInt32 MASK_MOTOR_CCW_MOVING	= 0x10;
			UInt32 MASK_DIGITAL_IN			= 0xFF00000;
			UInt32 MASK_DAC					= 0x80000000;

			UInt32 SHIFT_STATUS_MOTOR_1		= 0;
			UInt32 SHIFT_STATUS_MOTOR_2		= 5;
			UInt32 SHIFT_STATUS_MOTOR_3		= 10;
			UInt32 SHIFT_STATUS_MOTOR_4		= 15;
			UInt32 SHIFT_STATUS_DIGITAL_IN	= 20;
			UInt32 SHIFT_STATUS_DAC			= 32;


			UInt32 SHIFT_CONFIG_CONFIG		= 0;
			UInt32 SHIFT_CONFIG_ADC			= 10;
			UInt32 SHIFT_CONFIG_ENCODER		= 18;
			UInt32 SHIFT_CONFIG_COUNTER		= 0;
			#endregion

			//----------------------------------------------------------------------
			// The ZERO Header
			//----------------------------------------------------------------------
			// the very first 5 words (20 bytes) contain additional information
			// which is not repeated in subsequent headers
			//
			//	0	Total lentgh in Bytes
			//	1	Trigger 1
			//	2	Trigger 2
			//	3	Config 1
			//	4	Config 2
			//----------------------------------------------------------------------
			//--- LENGTH -----------------------------------------------------------
			word = _buffer[0];
			data_num_bytes = word;
			if (data_num_bytes < 20)				// the first frame must have at least 5 words (20 bytes)
			{
				//error_msg = "eval_dma: num_bytes: " + data_num_bytes.ToString();				
				return -1;
			}
			//----------------------------------------------------------------------
			//--- DAQ DATA TYPE ----------------------------------------------------
			word = _buffer[1];
			if ((word & 0x80000000) != 0)			// check if daq data or automatic status report
			{
				//error_msg = "eval_dma: is automatic status report";				
				return -2;
			}
			//----------------------------------------------------------------------
			//--- CONFIG -----------------------------------------------------------
			// Config 1
			word = _buffer[3];
			config_config	= (UInt32)((Int32)(word & MASK_CONFIG)			>> (Int32)SHIFT_CONFIG_CONFIG);
			config_adc		= (UInt32)((Int32)(word & MASK_ADC)				>> (Int32)SHIFT_CONFIG_ADC);
			config_encoder	= (UInt32)((Int32)(word & MASK_MOTOR_ENCODER)	>> (Int32)SHIFT_CONFIG_ENCODER);
			config_motor	= (UInt32)((Int32)(word & MASK_MOTOR_ENCODER)	>> (Int32)SHIFT_CONFIG_ENCODER);

			// Config 1
			word = _buffer[4];
			config_counter	= (UInt32)((Int32)(word & MASK_COUNTER)			>> (Int32)SHIFT_CONFIG_COUNTER);
			//----------------------------------------------------------------------


			//----------------------------------------------------------------------
			num_config		= (UInt32)CountHighBits(config_config);
			num_adcs		= (UInt32)CountHighBits(config_adc);
			num_encoders	= (UInt32)CountHighBits(config_encoder);
			num_motors		= (UInt32)CountHighBits(config_motor);
			num_counters	= (UInt32)CountHighBits(config_counter);
			//----------------------------------------------------------------------


			//----------------------------------------------------------------------
			// verify the validity of data_num_bytes and get num_frames
			frame_size = num_config + num_adcs + num_encoders + num_motors + num_counters + 3;
			user_data_per_frame = 4 * frame_size;
			if ((data_num_bytes - 8) % user_data_per_frame != 0)
			{
				//error_msg = "eval_dma: num_frames is not an integer";
				return -3;
			}
			num_frames = (data_num_bytes - 8) / user_data_per_frame;
			//----------------------------------------------------------------------


			_DataFrames = new FpgaDataFrame[num_frames];
			for (i = 0; i < num_frames; i++)
			{
				_DataFrames[i] = new FpgaDataFrame();
			}


			//----------------------------------------------------------------------
			// Read data frames
			//----------------------------------------------------------------------						
			for (n = 0; n < num_frames; n++)
			{

				// determine correct offsets
				// the 1st frame (n == 0) differs from the others
				if (n == 0)
				{
					frame_offset = 1;

					offset_trigger	= frame_offset;
					offset_config	= offset_trigger + num_trigger + 2;
					offset_counter	= offset_config + num_config;
					offset_adc		= offset_counter + num_counters;
					offset_encoder	= offset_adc + num_adcs;
					offset_motor	= offset_encoder + num_encoders;
					offset_ios		= offset_motor + num_motors;
				}
				else
				{
					frame_offset	= (UInt32)(frame_size * n) + 3;

					offset_trigger	= frame_offset;
					offset_config	= offset_trigger + num_trigger;
					offset_counter	= offset_config + num_config;
					offset_adc		= offset_counter + num_counters;
					offset_encoder	= offset_adc + num_adcs;
					offset_motor	= offset_encoder + num_encoders;
					offset_ios		= offset_motor + num_motors;
				}


				//--- read TRIGGER --- 2 words ---
				word = _buffer[offset_trigger];
				_DataFrames[n].time = word & MASK_TRIGGER_TIME_LSB;

				word = _buffer[offset_trigger + 1];
				_DataFrames[n].time |= (word & MASK_TRIGGER_TIME_MSB) << 5;
				_DataFrames[n].gate = word & MASK_TRIGGER_WIDTH;


				//--- read COUNTERs --- 0 to 32 words ---
				for (i = 0; i < num_counters; i++)
				{
					_DataFrames[n].counter[i] = _buffer[offset_counter + i];
				}


				//--- read ADCs --- 0 to 8 words ---
				for (i = 0; i < num_adcs; i++)
				{
					_DataFrames[n].ai[i] = _buffer[offset_adc + i];
				}


				//--- read ENCODERs --- 0 to 4 words ---
				for (i = 0; i < num_encoders; i++)
				{
					UInt32 encoder = _buffer[offset_encoder + i];
					//_DataFrames[n].encoder[i] = (Int32)(encoder + 0x7FFFFFFF) * (1);	// 2-2
					_DataFrames[n].encoder[i] = (Int32)(encoder + 0x7FFFFFFF) * (-1);   // 9-3																						
				}


				//--- read MOTORs --- 0 to 4 words ---
				for (i = 0; i < num_motors; i++)
				{
					_DataFrames[n].motor[i] = _buffer[offset_motor + i];
				}
			}
			//----------------------------------------------------------------------

			return 1;
		}
		public static string ErrorCodeToString(Int32 _errorCode)
		{
			string error = "";

			if (_errorCode >= 0)
			{
				error = "NO_ERROR";
				return error;
			}
			
			switch (_errorCode)
			{
				case -1:					
					error = "CORRUPT_DATA";
					break;

				case -2:
					error = "NO_DATA";
					break;

				case -3:					
					error = "CORRUPT_DATA";
					break;

				default:
					error = "ERROR_CODE_UNKNOWN";
					break;
			}

			return error;
		}
		//-----------------------------------------------------------------
		#endregion


		
		//-----------------------------------------------------------------
		public override string ToString()
        {
			StringBuilder sb = new StringBuilder(STRING_BUILDER_LEN);


            sb.Append(time.ToString());

			sb.Append(" ");
			sb.Append(gate.ToString());

			for (Int32 i = 0; i < ai.Length; i++)
			{
				sb.Append(" ");
				sb.Append(ai[i].ToString());
			}

			for (Int32 i = 0; i < counter.Length; i++)
			{
				sb.Append(" ");
				sb.Append(counter[i].ToString());
			}

			for (Int32 i = 0; i < encoder.Length; i++)
			{
				sb.Append(" ");
				sb.Append(encoder[i].ToString());
			}

			return sb.ToString();
		}
		public string ToString(UInt32 _adc, UInt32 _cnt, UInt32 _enc)
		{
			StringBuilder sb = new StringBuilder(STRING_BUILDER_LEN);


			
			sb.Append(time.ToString());
			
			
			sb.Append(" ");
			sb.Append(gate.ToString());


			/*
			for (Int32 i = 0; i < ai.Length; i++)
			{
				if (((_adc >> i) & 0x01) == 0x01)
                {					
					sb.Append(" ");
					sb.Append(ai[i].ToString());
					
				}					
			}


			for (Int32 i = 0; i < counter.Length; i++)
			{
				if (((_cnt >> i) & 0x01) == 0x01)
                {					
					sb.Append(" ");
					sb.Append(counter[i].ToString());
				}					
			}


			for (Int32 i = 0; i < encoder.Length; i++)
			{
				if (((_enc >> i) & 0x01) == 0x01)
				{					
					sb.Append(" ");
					sb.Append(encoder[i].ToString());
				}
			}
			*/




			for (Int32 i = 0; i < CountHighBits(_adc); i++)
			{				
				sb.Append(" ");
				sb.Append(ai[i].ToString());
			}


			for (Int32 i = 0; i < CountHighBits(_cnt); i++)
			{
					sb.Append(" ");
					sb.Append(counter[i].ToString());
			}


			for (Int32 i = 0; i < CountHighBits(_enc); i++)
			{
				sb.Append(" ");
				sb.Append(encoder[i].ToString());
			}
			

			return sb.ToString();
		}
		//-----------------------------------------------------------------


		#region Private Static Methods
		//-----------------------------------------------------------------
		private static int CountHighBits(UInt32 _input)
		{
			UInt32 value = _input;
			UInt32 num = 0;

			while (value > 0)
			{
				num += value & 0x01;
				value >>= 1;
			}

			return (Int32)num;
		}
		//-----------------------------------------------------------------
		#endregion
	}


	public class FpgaOffsets
	{

		#region Variables
		//-----------------------------------------------------------------
		private const Int32 STRING_BUILDER_LEN = 450;       //1x 10 (Gate) + 8x 10 (ADC) + 32x 10 (CNT) + 19 (DateTime) + 40 spaces = 469
		//-----------------------------------------------------------------
		public static Int32 numMaxAdc = 8;
		public static Int32 numMaxCnt = 32;
		//-----------------------------------------------------------------
		public UInt32 gate;
		public UInt32[] adc;
		public UInt32[] cnt;
		public DateTime dateTime;
		//-----------------------------------------------------------------
		#endregion


		//=================================================================					
		public FpgaOffsets()
		{
			adc = new UInt32[numMaxAdc];
			cnt = new UInt32[numMaxCnt];
		}
		public FpgaOffsets(FpgaDataFrame _fpgaDataFrame)
		{
			adc = new UInt32[numMaxAdc];
			cnt = new UInt32[numMaxCnt];

			gate = _fpgaDataFrame.gate;

			for (Int32 i = 0; i < numMaxAdc; i++)
			{
                adc[i] = _fpgaDataFrame.ai[i];				
			}
			for (Int32 i = 0; i < numMaxCnt; i++)
			{				
				cnt[i] = _fpgaDataFrame.counter[i];
			}
		}
		//=================================================================					



		

		public override string ToString()
        {
			StringBuilder sb = new StringBuilder(STRING_BUILDER_LEN);

					
			sb.Append(gate.ToString());

			for (Int32 i = 0; i < adc.Length; i++)
			{
				sb.Append(" ");
				sb.Append(adc[i].ToString());
			}

			for (Int32 i = 0; i < cnt.Length; i++)
			{
				sb.Append(" ");
				sb.Append(cnt[i].ToString());
			}

			sb.Append(" ");
			sb.Append(dateTime.ToString("yyyy-MM-dd HH:mm:ss"));

			return sb.ToString();
		}


    }
}
