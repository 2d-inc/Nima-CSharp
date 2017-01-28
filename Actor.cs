using System;
using System.IO;
using System.Collections.Generic;
//using System.Diagnostics;

namespace Nima
{
	public class Actor
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

		[Flags]
		public enum Flags
		{
		    IsImageDrawOrderDirty = 1<<0,
		    IsVertexDeformDirty = 1<<1
		}

		protected Flags m_Flags;
		protected ActorNode m_Root;
		protected ActorNode[] m_Nodes;
		protected ActorImage[] m_ImageNodes;
		protected ISolver[] m_SolverNodes;
		protected Nima.Animation.ActorAnimation[] m_Animations;
		protected int m_MaxTextureIndex;
		protected int m_ImageNodeCount;
		protected int m_SolverNodeCount;

		public Actor()
		{
			m_Flags = Flags.IsImageDrawOrderDirty|Flags.IsVertexDeformDirty;
		}

		public IEnumerable<ActorNode> Nodes
		{
			get
			{
				return m_Nodes;
			}
		}

		public ActorNode[] AllNodes
		{
			get
			{
				return m_Nodes;
			}
		}

		public IEnumerable<Nima.Animation.ActorAnimation> Animations
		{
			get
			{
				return m_Animations;
			}
		}

		public IEnumerable<ActorImage> ImageNodes
		{
			get
			{
				return m_ImageNodes;
			}
		}

		public IEnumerable<Nima.Animation.ActorAnimation> Animations
		{
			get
			{
				return m_Animations;
			}
		}

		public ActorNode this[int index]
		{
		    get
		    {
		    	return m_Nodes[index];
		    }
		}

		public int NodeCount
		{
			get
			{
				return m_Nodes.Length;
			}
		}

		public int ImageNodeCount
		{
			get
			{
				return m_ImageNodeCount;
			}
		}

		public int SolverNodeCount
		{
			get
			{
				return m_SolverNodeCount;
			}
		}

		public int TexturesUsed
		{
			get
			{
				return m_MaxTextureIndex + 1;
			}
		}

		public ActorNode Root
		{
			get
			{
				return m_Root;
			}
		}

		public Nima.Animation.ActorAnimation GetAnimation(string name)
		{
			foreach(Nima.Animation.ActorAnimation a in m_Animations)
			{
				if(a.Name == name)
				{
					return a;
				}
			}
			return null;
		}

		public ActorNode GetNode(string name)
		{
			foreach(ActorNode node in m_Nodes)
			{
				if(node.Name == name)
				{
					return node;
				}
			}
			return null;
		}

		private class SolverComparer : IComparer<ISolver>
		{
			public int Compare(ISolver x, ISolver y)  
			{
				return x.Order.CompareTo(y.Order);
			}
		}
		static SolverComparer sm_SolverComparer = new SolverComparer();

		private class ImageDrawOrderComparer : IComparer<ActorImage>
		{
			public int Compare(ActorImage x, ActorImage y)  
			{
				return x.DrawOrder.CompareTo(y.DrawOrder);
			}
		}
		static ImageDrawOrderComparer sm_ImageDrawOrderComparer = new ImageDrawOrderComparer();



		private void ReadNodesBlock(BlockReader block)
		{
			int nodeCount = block.ReadUInt16();
			m_Nodes = new ActorNode[nodeCount+1];
			m_Nodes[0] = m_Root;

			// Guaranteed from the exporter to be in index order.
			BlockReader nodeBlock = null;

			int nodeIndex = 1;
			while((nodeBlock=block.ReadNextBlock()) != null)
			{
				ActorNode node = null;
				if(Enum.IsDefined(typeof(BlockTypes), nodeBlock.BlockType))
				{
					BlockTypes type = (BlockTypes)nodeBlock.BlockType;
					switch(type)
					{
						case BlockTypes.ActorNode:
							node = ActorNode.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorBone:
							node = ActorBone.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorRootBone:
							node = ActorRootBone.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorImage:
							m_ImageNodeCount++;
							node = ActorImage.Read(this, nodeBlock, makeImageNode());
							if((node as ActorImage).TextureIndex > m_MaxTextureIndex)
							{
								m_MaxTextureIndex = (node as ActorImage).TextureIndex;
							}
							break;

						case BlockTypes.ActorIKTarget:
							node = ActorIKTarget.Read(this, nodeBlock);
							break;
					}
				}
				if(node is ISolver)
				{
					m_SolverNodeCount++;
				}

				m_Nodes[nodeIndex] = node;
				if(node != null)
				{
					node.Idx = (ushort)(nodeIndex);
				}
				nodeIndex++;
			}

			m_ImageNodes = new ActorImage[m_ImageNodeCount];
			m_SolverNodes = new ISolver[m_SolverNodeCount];

			// Resolve nodes.
			int imgIdx = 0;
			int slvIdx = 0;
			ActorNode[] nodes = m_Nodes;
			for(int i = 1; i <= nodeCount; i++)
			{
				ActorNode n = nodes[i];
				// Nodes can be null if we read from a file version that contained nodes that we don't interpret in this runtime.
				if(n != null)
				{
					n.ResolveNodeIndices(nodes);
				}

				ActorImage ain = n as ActorImage;
				if(ain != null)
				{
					m_ImageNodes[imgIdx++] = ain;
				}

				ISolver slv = n as ISolver;
				if(slv != null)
				{
					m_SolverNodes[slvIdx++] = slv;
				}
			}


			// Sort the solvers.
			if(m_SolverNodes != null)
			{
				Array.Sort<ISolver>(m_SolverNodes, sm_SolverComparer);
			}
		}

		private void ReadAnimationsBlock(BlockReader block)
		{
			// Read animations.
			int animationCount = block.ReadUInt16();
			m_Animations = new Nima.Animation.ActorAnimation[animationCount];
			BlockReader animationBlock = null;
			int animationIndex = 0;
			
			while((animationBlock=block.ReadNextBlock()) != null)
			{
				switch(animationBlock.BlockType)
				{
					case (int)BlockTypes.Animation:
						Nima.Animation.ActorAnimation anim = Nima.Animation.ActorAnimation.Read(animationBlock, m_Nodes);
						m_Animations[animationIndex++] = anim;
						//ReadAnimationBlock(actor, block);
						break;
				}
			};
		}


		public bool LoadFrom(Stream stream)
		{
			BlockReader reader = new BlockReader(stream);

			byte N = reader.ReadByte();
			byte I = reader.ReadByte();
			byte M = reader.ReadByte();
			byte A = reader.ReadByte();
			uint version = reader.ReadUInt32();

			if(N != 78 || I != 73 || M != 77 || A != 65)
			{
				return false;
			}
			if(version != 12)
			{
				return false;
			}

			//version == 1.0
			//Debugger.Log("NIMA confirmed. " + reader.BaseStream.Length);
			//byte[] data = new byte[reader.BaseStream.Length];
			
			m_MaxTextureIndex = 0;
			m_Root = new ActorNode(this);

			BlockReader block = null;
			while((block=reader.ReadNextBlock()) != null)
			{
				switch(block.BlockType)
				{
					case (int)BlockTypes.Nodes:
						ReadNodesBlock(block);
						break;
					case (int)BlockTypes.Animations:
						ReadAnimationsBlock(block);
						break;
				}
			}

			return true;
		}

		public static string ReadString(BinaryReader reader)
		{
			uint stringLength = reader.ReadUInt32();
			byte[] buffer = new byte[stringLength];
			reader.Read(buffer, 0, (int)stringLength);

			return System.Text.Encoding.UTF8.GetString(buffer);
		}

		public static void ReadFloat32Array(BinaryReader reader, float[] array)
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = reader.ReadSingle();
			}
		}

		public static void ReadUInt16Array(BinaryReader reader, ushort[] array)
		{
			for(int i = 0; i < array.Length; i++)
			{
				array[i] = reader.ReadUInt16();
			}
		}

		public void MarkImageDrawOrderDirty()
		{
			m_Flags |= Flags.IsImageDrawOrderDirty;
		}

		public bool IsVertexDeformDirty
		{
			get
			{
				return (m_Flags & Flags.IsVertexDeformDirty) != 0x00;
			}
		}

		public void Copy(Actor actor)
		{
			m_Animations = actor.m_Animations;
			m_Flags = actor.m_Flags;
			m_MaxTextureIndex = actor.m_MaxTextureIndex;
			m_ImageNodeCount = actor.m_ImageNodeCount;
			m_SolverNodeCount = actor.m_SolverNodeCount;

			if(actor.NodeCount != 0)
			{
				m_Nodes = new ActorNode[actor.NodeCount];
			}
			if(m_ImageNodeCount != 0)
			{
				m_ImageNodes = new ActorImage[m_ImageNodeCount];
			}
			if(m_SolverNodeCount != 0)
			{
				m_SolverNodes = new ISolver[m_SolverNodeCount];
			}

			if(actor.NodeCount != 0)
			{
				int idx = 0;
				int imgIdx = 0;
				int slvIdx = 0;

				foreach(ActorNode node in actor.Nodes)
				{
					if(node == null)
					{
						m_Nodes[idx++] = null;
						continue;
					}
					ActorNode instanceNode = node.MakeInstance(this);
					m_Nodes[idx++] = instanceNode;
					ActorImage imageInstance = instanceNode as ActorImage;
					if(imageInstance != null)
					{
						m_ImageNodes[imgIdx++] = imageInstance;
					}

					ISolver solver = instanceNode as ISolver;
					if(solver != null)
					{
						m_SolverNodes[slvIdx++] = solver;
					}
				}
			}

			m_Root = m_Nodes[0];
			foreach(ActorNode node in m_Nodes)
			{
				if(m_Root == node || node == null)
				{
					continue;
				}
				node.ResolveNodeIndices(m_Nodes);
			}

			// Is this really necessary? We sorted on load...
			// Sort the solvers.
			if(m_SolverNodes != null)
			{
				Array.Sort<ISolver>(m_SolverNodes, sm_SolverComparer);
			}
		}

		protected virtual void UpdateVertexDeform(ActorImage image) {}
		protected virtual ActorImage makeImageNode()
		{
			return new ActorImage();
		}

		public virtual void Advance(float seconds)
		{
			bool runSolvers = false;
			if(m_SolverNodes != null)
			{
				foreach(ISolver solver in m_SolverNodes)
				{
					if(solver.NeedsSolve)
					{
						runSolvers = true;
						break;
					}
				}
			}

			foreach(ActorNode n in m_Nodes)
			{
				n.UpdateTransforms();
			}

			if(runSolvers)
			{
				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_SolverNodes[i];
					solver.SolveStart();
				}	

				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_SolverNodes[i];
					solver.Solve();
				}

				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_SolverNodes[i];
					solver.SuppressMarkDirty = true;
				}
					
				foreach(ActorNode n in m_Nodes)
				{
					n.UpdateTransforms();
				}

				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_SolverNodes[i];
					solver.SuppressMarkDirty = false;
				}
			}


			if((m_Flags & Flags.IsImageDrawOrderDirty) != 0)
			{
				m_Flags &= ~Flags.IsImageDrawOrderDirty;

				if (m_ImageNodes != null)
				{
					Array.Sort<ActorImage>(m_ImageNodes, sm_ImageDrawOrderComparer);
				}
			}
			if((m_Flags & Flags.IsVertexDeformDirty) != 0)
			{
				m_Flags &= ~Flags.IsVertexDeformDirty;
				for(int i = 0; i < m_ImageNodeCount; i++)
				{
					ActorImage imageNode = m_ImageNodes[i];
					if(imageNode != null && imageNode.IsVertexDeformDirty)
					{
						imageNode.IsVertexDeformDirty = false;
						UpdateVertexDeform(imageNode);
					}
				}
			}
		}
	}
}