using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace BeanExplorer.Connector
{
	public static class Crc16
	{
		private const ushort Polynomial = 0x1021;
		public const ushort Initial = 0xffff;
		private static readonly ushort[] Table = new ushort[256];

		public static ushort ComputeChecksum(byte[] bytes)
		{
			return ComputeChecksum(bytes, 0, bytes.Length);
		}

		public static ushort ComputeChecksum(byte[] bytes, Int32 start, Int32 length)
		{
			ushort crc = Initial;
			for (int i = start; i < length; ++i)
			{
				Byte index = (byte)((crc >> 8) ^ (0xff & bytes[i]));
				crc = (ushort)((crc << 8) ^ Table[index]);
			}
			return crc;
		}

		public static ushort ComputeChecksum(IBuffer bytes, Int32 start, Int32 length)
		{
			ushort crc = Initial;
			for (int i = start; i < length; ++i)
			{
				Byte index = (byte) ((crc >> 8) ^ (0xff & bytes.GetByte((uint) i)));
				crc = (ushort)((crc << 8) ^ Table[index]);
			}
			return crc;
		}

		static Crc16()
		{
			ushort temp, a;
			for (int i = 0; i < Table.Length; ++i)
			{
				temp = 0;
				a = (ushort)(i << 8);
				for (int j = 0; j < 8; ++j)
				{
					if (((temp ^ a) & 0x8000) != 0)
					{
						temp = (ushort)((temp << 1) ^ Polynomial);
					}
					else
					{
						temp <<= 1;
					}
					a <<= 1;
				}
				Table[i] = temp;
			}
		}
	}
}
