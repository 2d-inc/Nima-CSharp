using System;
using System.IO;

namespace Nima
{
	public enum BlockTypes
	{
		Unknown = 0,
		Nodes = 1,
		ActorNode = 2,
		ActorBone = 3,
		ActorRootBone = 4,
		ActorImage = 5,
		View = 6,
		Animation = 7,
		Animations = 8,
		Atlases = 9,
		Atlas = 10,
		ActorIKTarget = 11
	};


	public class BlockReader : BinaryReader
	{
		private BlockTypes m_BlockType;

		public BlockReader(Stream stream) : base(stream)
		{
			m_BlockType = 0;
		}

		public BlockReader(BlockTypes type, MemoryStream stream) : base(stream)
		{
			m_BlockType = type;
		}

		public BlockTypes BlockType
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
			Console.WriteLine("BLOCK TYPE " + blockType + " LENGTH " + length + " " + ((BlockTypes)blockType));
			byte[] buffer = ReadBytes((int)length);
			if(buffer.Length != length)
			{
				return null;
			}
			return new BlockReader((BlockTypes)blockType, new MemoryStream(buffer));
		}
	}
}