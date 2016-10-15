using System;
using System.IO;
using System.Collections.Generic;
//using System.Diagnostics;

namespace Nima
{
	public enum NodeTypes
	{
		ActorNode = 0,
		ActorBone = 1,
		ActorRootBone = 2,
		ActorImage = 3
	};

	public class Actor
	{
		protected ActorNode m_Root;
		protected ActorNode[] m_Nodes;
		protected ActorImage[] m_ImageNodes;
		protected Nima.Animation.ActorAnimation[] m_Animations;
		protected int m_MaxTextureIndex;
		protected int m_ImageNodeCount;

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

		public static Actor LoadFrom(BinaryReader reader)
		{
			byte N = reader.ReadByte();
			byte I = reader.ReadByte();
			byte M = reader.ReadByte();
			byte A = reader.ReadByte();
			float version = reader.ReadSingle();

			if(N != 78 || I != 73 || M != 77 || A != 65)
			{
				return null;
			}
			if(version != 1.0)
			{
				return null;
			}

			//version == 1.0
			//Debugger.Log("NIMA confirmed. " + reader.BaseStream.Length);
			//byte[] data = new byte[reader.BaseStream.Length];
			
			Actor actor = new Actor();
			actor.m_MaxTextureIndex = 0;
			actor.m_Root = new ActorNode(actor);

			int nodeCount = reader.ReadUInt16();
			actor.m_Nodes = new ActorNode[nodeCount+1];
			actor.m_Nodes[0] = actor.m_Root;
			
			for(int i = 0; i < nodeCount; i++)
			{
				int nodeType = reader.ReadByte();
				NodeTypes type = (NodeTypes)nodeType;
				ActorNode node = null;

				switch(type)
				{
					case NodeTypes.ActorNode:
						node = ActorNode.Read(actor, reader);
						break;

					case NodeTypes.ActorBone:
						node = ActorBone.Read(actor, reader);
						break;

					case NodeTypes.ActorRootBone:
						node = ActorRootBone.Read(actor, reader);
						break;

					case NodeTypes.ActorImage:
						actor.m_ImageNodeCount++;
						node = ActorImage.Read(actor, reader);
						if((node as ActorImage).TextureIndex > actor.m_MaxTextureIndex)
						{
							actor.m_MaxTextureIndex = (node as ActorImage).TextureIndex;
						}
						break;
				}

				actor.m_Nodes[i+1] = node;
				node.Idx = (ushort)(i+1);
			}

			actor.m_ImageNodes = new ActorImage[actor.m_ImageNodeCount];

			// Resolve nodes.
			int imgIdx = 0;
			ActorNode[] nodes = actor.m_Nodes;
			for(int i = 1; i <= nodeCount; i++)
			{
				ActorNode n = nodes[i];
				n.ResolveNodeIndices(nodes);

				ActorImage ain = n as ActorImage;
				if(ain != null)
				{
					actor.m_ImageNodes[imgIdx++] = ain;
				}
			}

			// Read animations.
			int animationCount = reader.ReadUInt16();
			actor.m_Animations = new Nima.Animation.ActorAnimation[animationCount];
			for(int i = 0; i < animationCount; i++)
			{
				Nima.Animation.ActorAnimation anim = Nima.Animation.ActorAnimation.Read(reader, nodes);
				actor.m_Animations[i] = anim;
			}

			return actor;
		}

		public static string ReadString(BinaryReader reader)
		{
			uint stringLength = reader.ReadUInt32();
			byte[] buffer = new byte[stringLength];
			reader.Read(buffer, 0, (int)stringLength);

			string encoded = System.Text.Encoding.UTF8.GetString(buffer);
			byte[] data = Convert.FromBase64String(encoded);
			string decodedString = System.Text.Encoding.UTF8.GetString(data);
			return decodedString;
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
		}
	}

	public class ActorInstance : Actor
	{
		private List<Nima.Animation.AnimationInstance> m_PlayingAnimations;

		public ActorNode[] AllNodes
		{
			get
			{
				return m_Nodes;
			}
		}

		public void Advance(float seconds)
		{
			for(int i = 0; i < m_PlayingAnimations.Count; i++)
			{
				if(m_PlayingAnimations[i].Advance(seconds))
				{
					m_PlayingAnimations.RemoveAt(i);
					i--;
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

			m_Nodes = new ActorNode[actor.NodeCount];
			m_ImageNodes = new ActorImage[m_ImageNodeCount];
			int idx = 0;
			int imgIdx = 0;

			foreach(ActorNode node in actor.Nodes)
			{
				ActorNode instanceNode = node.MakeInstance(this);
				m_Nodes[idx++] = instanceNode;
				ActorImage imageInstance = instanceNode as ActorImage;
				if(imageInstance != null)
				{
					m_ImageNodes[imgIdx++] = imageInstance;
				}
			}

			m_Root = m_Nodes[0];
			foreach(ActorNode node in m_Nodes)
			{
				if(m_Root == node)
				{
					continue;
				}
				node.ResolveNodeIndices(m_Nodes);
			}
		}
	}
}