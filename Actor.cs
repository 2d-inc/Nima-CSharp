using System;
using System.IO;
using System.Collections.Generic;

namespace Nima
{
	public class Actor
	{
		public enum BlockTypes
		{
			Unknown = 0,
			Components = 1,
			ActorNode = 2,
			ActorBone = 3,
			ActorRootBone = 4,
			ActorImage = 5,
			View = 6,
			Animation = 7,
			Animations = 8,
			Atlases = 9,
			Atlas = 10,
			ActorIKTarget = 11,
			ActorEvent = 12,
			CustomIntProperty = 13,
			CustomFloatProperty = 14,
			CustomStringProperty = 15,
            ActorNodeSolo = 23,

        };

		[Flags]
		public enum Flags
		{
		    IsImageDrawOrderDirty = 1<<0,
		    IsVertexDeformDirty = 1<<1
		}

		protected Flags m_Flags;
		protected ActorNode m_Root;
		protected ActorComponent[] m_Components;
		protected ActorNode[] m_Nodes;
		protected ActorImage[] m_ImageNodes;
		protected ISolver[] m_Solvers;
		protected Nima.Animation.ActorAnimation[] m_Animations;
		protected int m_MaxTextureIndex;
		protected int m_ImageNodeCount;
		protected int m_SolverNodeCount;
		protected int m_NodeCount;

        public event EventHandler<Nima.Animation.AnimationEventArgs> AnimationEvent;

		public Actor()
		{
			m_Flags = Flags.IsImageDrawOrderDirty | Flags.IsVertexDeformDirty;
		}

		public void OnAnimationEvent(Nima.Animation.AnimationEventArgs ev)
		{
			if(AnimationEvent != null)
			{
				AnimationEvent(this, ev);
			}
		}

		public IList<ActorComponent> Components
		{
			get
			{
				return m_Components;
			}
		}

		public ActorComponent[] AllComponents
		{
			get
			{
				return m_Components;
			}
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

		public ActorComponent this[int index]
		{
		    get
		    {
		    	return m_Components[index];
		    }
		}

		public int ComponentCount
		{
			get
			{
				return m_Components.Length;
			}
		}

		public int NodeCount
		{
			get
			{
				return m_NodeCount;
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

		public Nima.Animation.ActorAnimationInstance GetAnimationInstance(string name)
		{
			Nima.Animation.ActorAnimation animation = GetAnimation(name);
			if(animation == null)
			{
				return null;
			}
			return new Nima.Animation.ActorAnimationInstance(this, animation);
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



		private void ReadComponentsBlock(BlockReader block)
		{
			int componentCount = block.ReadUInt16();
			m_Components = new ActorComponent[componentCount+1];
			m_Components[0] = m_Root;

			// Guaranteed from the exporter to be in index order.
			BlockReader nodeBlock = null;

			int componentIndex = 1;
			m_NodeCount = 1;
			while((nodeBlock=block.ReadNextBlock()) != null)
			{
				ActorComponent component = null;
				if(Enum.IsDefined(typeof(BlockTypes), nodeBlock.BlockType))
				{
					BlockTypes type = (BlockTypes)nodeBlock.BlockType;
					switch(type)
					{
						case BlockTypes.ActorNode:
							component = ActorNode.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorBone:
							component = ActorBone.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorRootBone:
							component = ActorRootBone.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorImage:
							m_ImageNodeCount++;
							component = ActorImage.Read(this, nodeBlock, makeImageNode());
							if((component as ActorImage).TextureIndex > m_MaxTextureIndex)
							{
								m_MaxTextureIndex = (component as ActorImage).TextureIndex;
							}
							break;

						case BlockTypes.ActorIKTarget:
							component = ActorIKTarget.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorEvent:
							component = ActorEvent.Read(this, nodeBlock);
							break;

						case BlockTypes.CustomIntProperty:
							component = CustomIntProperty.Read(this, nodeBlock);
							break;

						case BlockTypes.CustomFloatProperty:
							component = CustomFloatProperty.Read(this, nodeBlock);
							break;

						case BlockTypes.CustomStringProperty:
							component = CustomStringProperty.Read(this, nodeBlock);
							break;
						
						case BlockTypes.ActorNodeSolo:
							component = ActorNodeSolo.Read(this, nodeBlock);
							break;
					}
				}
				if(component is ActorNode)
				{
					m_NodeCount++;
				}
				if(component is ISolver)
				{
					m_SolverNodeCount++;
				}

				m_Components[componentIndex] = component;
				if(component != null)
				{
					component.Idx = (ushort)(componentIndex);
				}
				componentIndex++;
			}

			m_ImageNodes = new ActorImage[m_ImageNodeCount];
			m_Solvers = new ISolver[m_SolverNodeCount];
			m_Nodes = new ActorNode[m_NodeCount];
			m_Nodes[0] = m_Root;

			// Resolve nodes.
			int imgIdx = 0;
			int slvIdx = 0;
			int anIdx = 0;

			ActorComponent[] components = m_Components;
			for(int i = 1; i <= componentCount; i++)
			{
				ActorComponent c = components[i];
				// Nodes can be null if we read from a file version that contained nodes that we don't interpret in this runtime.
				if(c != null)
				{
					c.ResolveComponentIndices(components);
				}

				ActorImage ain = c as ActorImage;
				if(ain != null)
				{
					m_ImageNodes[imgIdx++] = ain;
				}

				ISolver slv = c as ISolver;
				if(slv != null)
				{
					m_Solvers[slvIdx++] = slv;
				}

				ActorNode an = c as ActorNode;
				if(an != null)
				{
					m_Nodes[anIdx++] = an;
				}
			}


			// Sort the solvers.
			if(m_Solvers != null)
			{
				Array.Sort<ISolver>(m_Solvers, sm_SolverComparer);
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
						Nima.Animation.ActorAnimation anim = Nima.Animation.ActorAnimation.Read(animationBlock, m_Components);
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
			if(version != 13)
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
					case (int)BlockTypes.Components:
						ReadComponentsBlock(block);
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
			m_NodeCount = actor.m_NodeCount;

			if(actor.ComponentCount != 0)
			{
				m_Components = new ActorComponent[actor.ComponentCount];
			}
			if(m_NodeCount != 0) // This will always be at least 1.
			{
				m_Nodes = new ActorNode[m_NodeCount];
			}
			if(m_ImageNodeCount != 0)
			{
				m_ImageNodes = new ActorImage[m_ImageNodeCount];
			}
			if(m_SolverNodeCount != 0)
			{
				m_Solvers = new ISolver[m_SolverNodeCount];
			}

			if(actor.ComponentCount != 0)
			{
				int idx = 0;
				int imgIdx = 0;
				int slvIdx = 0;
				int ndIdx = 0;

				foreach(ActorComponent component in actor.Components)
				{
					if(component == null)
					{
						m_Components[idx++] = null;
						continue;
					}
					ActorComponent instanceComponent = component.MakeInstance(this);
					m_Components[idx++] = instanceComponent;
					ActorNode nodeInstance = instanceComponent as ActorNode;
					if(nodeInstance != null)
					{
						m_Nodes[ndIdx++] = nodeInstance;
					}

					ActorImage imageInstance = instanceComponent as ActorImage;
					if(imageInstance != null)
					{
						m_ImageNodes[imgIdx++] = imageInstance;
					}

					ISolver solver = instanceComponent as ISolver;
					if(solver != null)
					{
						m_Solvers[slvIdx++] = solver;
					}
				}
			}

			m_Root = m_Components[0] as ActorNode;

			foreach(ActorComponent component in m_Components)
			{
				if(m_Root == component || component == null)
				{
					continue;
				}
				component.ResolveComponentIndices(m_Components);
			}

			// Is this really necessary? We sorted on load...
			// Sort the solvers.
			if(m_Solvers != null)
			{
				Array.Sort<ISolver>(m_Solvers, sm_SolverComparer);
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
			if(m_Solvers != null)
			{
				foreach(ISolver solver in m_Solvers)
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
					ISolver solver = m_Solvers[i];
					solver.SolveStart();
				}	

				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_Solvers[i];
					solver.Solve();
				}

				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_Solvers[i];
					solver.SuppressMarkDirty = true;
				}
					
				foreach(ActorNode n in m_Nodes)
				{
					n.UpdateTransforms();
				}

				for(int i = 0; i < m_SolverNodeCount; i++)
				{
					ISolver solver = m_Solvers[i];
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