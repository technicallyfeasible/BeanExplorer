using System;

namespace BeanExplorer.Connector
{
	public class BeanConstants
	{
		public static readonly Guid BeanSerialServiceUuid = new Guid("a495ff10-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanSerialCharUuid = new Guid("a495ff11-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanScratchServiceUuid = new Guid("a495ff20-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanScratch1Uuid = new Guid("a495ff21-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanScratch2Uuid = new Guid("a495ff22-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanScratch3Uuid = new Guid("a495ff23-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanScratch4Uuid = new Guid("a495ff24-c5b1-4b44-b512-1370f02d74de");
		public static readonly Guid BeanScratch5Uuid = new Guid("a495ff25-c5b1-4b44-b512-1370f02d74de");
	}

	public enum BeanMsgId : ushort
	{
		Invalid = 0xFFFF,
		SerialData = 0x0000,
		BtSetAdv = 0x0005,
		BtSetConn = 0x0205,
		BtSetLocalName = 0x0405,
		BtSetPin = 0x0605,
		BtSetTxPwr = 0x0805,
		BtGetConfig = 0x1005,
		BtAdvOnoff = 0x1205,
		BtSetScratch = 0x1405,
		BtGetScratch = 0x1505,
		BtRestart = 0x2005,
		BlCmd = 0x0010,
		BlFwBlock = 0x0110,
		BlStatus = 0x0210,
		CcLedWrite = 0x0020,
		CcLedWriteAll = 0x0120,
		CcLedReadAll = 0x0220,
		CcAccelRead = 0x1020,
		CcTempRead = 0x1120,
		ArSetPower = 0x0030,
		ArGetConfig = 0x0630,
		DbLoopback = 0x00FE,
		DbCounter = 0x01FE
	}
}