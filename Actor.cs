using System;
using System.IO;
using System.Collections.Generic;
//using System.Diagnostics;

namespace Nima
{
	public class Actor
	{
		protected ActorNode m_Root;
		protected ActorNode[] m_Nodes;
		protected ActorImage[] m_ImageNodes;
		protected ISolver[] m_SolverNodes;
		protected Nima.Animation.ActorAnimation[] m_Animations;
		protected int m_MaxTextureIndex;
		protected int m_ImageNodeCount;
		protected int m_SolverNodeCount;

		public IEnumerable<ActorNode> Nodes
		{
			get
			{
				return m_Nodes;
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
				switch(nodeBlock.BlockType)
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
						node = ActorImage.Read(this, nodeBlock);
						if((node as ActorImage).TextureIndex > m_MaxTextureIndex)
						{
							m_MaxTextureIndex = (node as ActorImage).TextureIndex;
						}
						break;

					case BlockTypes.ActorIKTarget:
						node = ActorIKTarget.Read(this, nodeBlock);
						break;
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
					case BlockTypes.Animation:
						Nima.Animation.ActorAnimation anim = Nima.Animation.ActorAnimation.Read(animationBlock, m_Nodes);
						m_Animations[animationIndex++] = anim;
						//ReadAnimationBlock(actor, block);
						break;
				}
			};
		}


		public static Actor LoadFrom(Stream stream)
		{
			BlockReader reader = new BlockReader(stream);

			byte N = reader.ReadByte();
			byte I = reader.ReadByte();
			byte M = reader.ReadByte();
			byte A = reader.ReadByte();
			uint version = reader.ReadUInt32();

			if(N != 78 || I != 73 || M != 77 || A != 65)
			{
				return null;
			}
			if(version != 11)
			{
				return null;
			}

			//version == 1.0
			//Debugger.Log("NIMA confirmed. " + reader.BaseStream.Length);
			//byte[] data = new byte[reader.BaseStream.Length];
			
			Actor actor = new Actor();
			actor.m_MaxTextureIndex = 0;
			actor.m_Root = new ActorNode(actor);

			BlockReader block = null;
			while((block=reader.ReadNextBlock()) != null)
			{
				switch(block.BlockType)
				{
					case BlockTypes.Nodes:
						actor.ReadNodesBlock(block);
						break;
					case BlockTypes.Animations:
						actor.ReadAnimationsBlock(block);
						break;
				}
			}

			return actor;
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

		public void Copy(Actor actor)
		{
			m_Animations = actor.m_Animations;
			m_MaxTextureIndex = actor.m_MaxTextureIndex;
			m_ImageNodeCount = actor.m_ImageNodeCount;
			m_SolverNodeCount = actor.m_SolverNodeCount;
		}
	}

	public class ActorInstance : Actor
	{
		private List<Nima.Animation.AnimationInstance> m_PlayingAnimations;

		private class SolverComparer : IComparer<ISolver>
		{
			public int Compare(ISolver x, ISolver y)  
			{
				return x.Order.CompareTo(y.Order);
			}
		}
		static SolverComparer sm_SolverComparer = new SolverComparer();
		public ActorNode[] AllNodes
		{
			get
			{
				return m_Nodes;
			}
		}

		public virtual void Advance(float seconds)
		{
			for(int i = 0; i < m_PlayingAnimations.Count; i++)
			{
				if(m_PlayingAnimations[i].Advance(seconds))
				{
					m_PlayingAnimations.RemoveAt(i);
					i--;
				}
			}
			
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
		}

		public Nima.Animation.AnimationInstance Play(string name, bool loop = false)
		{
			Nima.Animation.ActorAnimation animation = GetAnimation(name);
			return Play(animation, loop);
		}

		public Nima.Animation.AnimationInstance Play(Nima.Animation.ActorAnimation animation, bool loop = false)
		{
			if(animation == null)
			{
				return null;
			}
			Nima.Animation.AnimationInstance animationInstance = new Nima.Animation.AnimationInstance(this, animation, loop);
			m_PlayingAnimations.Add(animationInstance);
			return animationInstance;
		}

		public ActorInstance(Actor actor)
		{
			this.Copy(actor);

			m_PlayingAnimations = new List<Nima.Animation.AnimationInstance>();
			m_Nodes = new ActorNode[actor.NodeCount];
			m_ImageNodes = new ActorImage[m_ImageNodeCount];
			if(m_SolverNodeCount != 0)
			{
				m_SolverNodes = new ISolver[m_SolverNodeCount];
			}
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

			m_Root = m_Nodes[0];
			foreach(ActorNode node in m_Nodes)
			{
				if(m_Root == node || node == null)
				{
					continue;
				}
				node.ResolveNodeIndices(m_Nodes);
			}

			// Sort the solvers.
			if(m_SolverNodes != null)
			{
				Array.Sort<ISolver>(m_SolverNodes, sm_SolverComparer);
			}
		}
	}
}