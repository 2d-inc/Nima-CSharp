using System;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEngine;
#endif

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
			CustomBooleanProperty = 16,
			ActorColliderRectangle = 17,
			ActorColliderTriangle = 18,
			ActorColliderCircle = 19,
			ActorColliderPolygon = 20,
			ActorColliderLine = 21,
            ActorNodeSolo = 23,
			JellyComponent = 28,
			ActorJellyBone = 29,
			ActorIKConstraint = 30,
			ActorDistanceConstraint = 31,
			ActorTranslationConstraint = 32,
			ActorRotationConstraint = 33,
			ActorScaleConstraint = 34,
			ActorTransformConstraint = 35
        };

		[Flags]
		public enum Flags
		{
		    IsImageDrawOrderDirty = 1<<0,
		    IsVertexDeformDirty = 1<<1,
			IsDirty = 1<<2
		}

		protected Flags m_Flags;
		protected ActorNode m_Root;
		protected ActorComponent[] m_Components;
		protected ActorNode[] m_Nodes;
		protected ActorImage[] m_ImageNodes;
		protected Nima.Animation.ActorAnimation[] m_Animations;
		protected int m_MaxTextureIndex;
		protected int m_ImageNodeCount;
		protected int m_NodeCount;
		protected uint m_Version;

        public event EventHandler<Nima.Animation.AnimationEventArgs> AnimationEvent;
		IList<ActorComponent> m_DependencyOrder;
		uint m_DirtDepth;

		public Actor()
		{
			m_Flags = Flags.IsImageDrawOrderDirty | Flags.IsVertexDeformDirty;
			m_DirtDepth = 0;
		}

		public bool AddDependency(ActorComponent a, ActorComponent b)
		{
			IList<ActorComponent> dependents = b.m_Dependents;
			if(dependents == null)
			{
				b.m_Dependents = dependents = new List<ActorComponent>();
			}
			if(dependents.Contains(a))
			{
				return false;
			}
			dependents.Add(a);
			return true;
		}

		void SortDependencies()
		{
			DependencySorter sorter = new DependencySorter();
			m_DependencyOrder = sorter.Sort(m_Root);
			uint graphOrder = 0;
			foreach(ActorComponent component in m_DependencyOrder)
			{
				component.m_GraphOrder = graphOrder++;
				component.m_DirtMask = 255;
			}
			m_Flags |= Flags.IsDirty;
		}

		public bool AddDirt(ActorComponent component, byte value, bool recurse = false)
		{
			if((component.m_DirtMask & value) == value)
			{
				// Already marked.
				return false;
			}

			// Make sure dirt is set before calling anything that can set more dirt.
			byte dirt = (byte)(component.m_DirtMask | value);
			component.m_DirtMask = dirt;

			m_Flags |= Flags.IsDirty;

			component.OnDirty(dirt);

			// If the order of this component is less than the current dirt depth, update the dirt depth
			// so that the update loop can break out early and re-run (something up the tree is dirty).
			if(component.m_GraphOrder < m_DirtDepth)
			{
				m_DirtDepth = component.m_GraphOrder;	
			}
			if(!recurse)
			{
				return true;
			}
			IList<ActorComponent> dependents = component.m_Dependents;
			if(dependents != null)
			{
				foreach(ActorComponent d in dependents)
				{
					AddDirt(d, value, recurse);
				}
			}

			return true;
		}

		public void OnAnimationEvent(Nima.Animation.AnimationEventArgs ev)
		{
			if(AnimationEvent != null)
			{
				AnimationEvent(this, ev);
			}
		}

		public uint Version
		{
			get
			{
				return m_Version;
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

						case BlockTypes.CustomBooleanProperty:
							component = CustomBooleanProperty.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorColliderRectangle:
							component = ActorColliderRectangle.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorColliderTriangle:
							component = ActorColliderTriangle.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorColliderCircle:
							component = ActorColliderCircle.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorColliderPolygon:
							component = ActorColliderPolygon.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorColliderLine:
							component = ActorColliderLine.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorNodeSolo:
							component = ActorNodeSolo.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorJellyBone:
							component = ActorJellyBone.Read(this, nodeBlock);
							break;

						case BlockTypes.JellyComponent:
							component = JellyComponent.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorIKConstraint:
							component = ActorIKConstraint.Read(this, nodeBlock);
							break;

						case BlockTypes.ActorDistanceConstraint:
							component = ActorDistanceConstraint.Read(this, nodeBlock);
							break;
					}
				}
				if(component is ActorNode)
				{
					m_NodeCount++;
				}

				m_Components[componentIndex] = component;
				if(component != null)
				{
					component.Idx = (ushort)(componentIndex);
				}
				componentIndex++;
			}

			m_ImageNodes = new ActorImage[m_ImageNodeCount];
			m_Nodes = new ActorNode[m_NodeCount];
			m_Nodes[0] = m_Root;

			// Resolve nodes.
			int imgIdx = 0;
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

				ActorNode an = c as ActorNode;
				if(an != null)
				{
					m_Nodes[anIdx++] = an;
				}
			}

			for(int i = 1; i <= componentCount; i++)
			{
				ActorComponent c = components[i];
				if(c != null)
				{
					c.CompleteResolve();
				}
			}

			SortDependencies();
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
			if(version < 12)
			{
				return false;
			}

			m_Version = version;

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

			if(actor.ComponentCount != 0)
			{
				int idx = 0;
				int imgIdx = 0;
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

			foreach(ActorComponent component in m_Components)
			{
				if(m_Root == component || component == null)
				{
					continue;
				}
				component.CompleteResolve();
			}

			SortDependencies();
		}

		protected virtual void UpdateVertexDeform(ActorImage image) {}
		protected virtual ActorImage makeImageNode()
		{
			return new ActorImage();
		}

		public virtual void Advance(float seconds)
		{
			if((m_Flags & Flags.IsDirty) != 0)
			{
				const int MaxSteps = 100;
				int step = 0;
				int count = m_DependencyOrder.Count;
				while((m_Flags & Flags.IsDirty) != 0 && step < MaxSteps)
				{
					m_Flags &= ~Flags.IsDirty;
					// Track dirt depth here so that if something else marks dirty, we restart.
					for(int i = 0; i < count; i++)
					{
						ActorComponent component = m_DependencyOrder[i];
						m_DirtDepth = (uint)i;
						byte d = component.m_DirtMask;
						if(d == 0)
						{
							continue;
						}
						component.m_DirtMask = 0;
						component.Update(d);
						if(m_DirtDepth < i)
						{
							break;
						}
					}
					step++;
				}
			}

			if((m_Flags & Flags.IsImageDrawOrderDirty) != 0)
			{
				m_Flags &= ~Flags.IsImageDrawOrderDirty;

				if (m_ImageNodes != null)
				{
					Array.Sort<ActorImage>(m_ImageNodes, sm_ImageDrawOrderComparer);
					// Mark draw index.
					for(int i = 0; i < m_ImageNodes.Length; i++)
					{
						m_ImageNodes[i].DrawIndex = i;
					}
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