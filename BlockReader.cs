using System;
using System.IO;

namespace Nima
{
	public class BlockReader : BinaryReader
	{
		private int m_BlockType;

		public BlockReader(Stream stream) : base(stream)
		{
			m_BlockType = 0;
		}

		public BlockReader(int type, MemoryStream stream) : base(stream)
		{
			m_BlockType = type;
		}

		public int BlockType
		{
			get
			{
				return m_BlockType;
			}
		}

		// A block is defined as a TLV with type of one byte, length of 4 bytes, and then the value following.
		public BlockReader ReadNextBlock()
		{
			if(BaseStream.Position == BaseStream.Length)
			{
				return null;
			}
			byte blockType = ReadByte();
			uint length = ReadUInt32();
			byte[] buffer = ReadBytes((int)length);
			if(buffer.Length != length)
			{
				return null;
			}
			return new BlockReader((int)blockType, new MemoryStream(buffer));
		}
	}
}