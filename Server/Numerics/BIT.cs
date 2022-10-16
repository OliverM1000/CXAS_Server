using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Numerics
{
    public class BIT
    {


		//-----------------------------------------------------------------
		public static Int32 count_high_bits(UInt32 _input)
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
		public static Int32 get_high_bit_config(UInt32 _input, out bool[] _bit_config)
		{
			int n = 0;
			int i = 0;
			UInt32 value = _input;
			UInt32 num = 0;

			_bit_config = new bool[32];

			while (value > 0)
			{
				_bit_config[i] = (value & 0x01) == 0 ? false : true;
				num += value & 0x01;
				value >>= 1;
				i++;
			}

			return (Int32)num;
		}
		//-----------------------------------------------------------------

	}
}
