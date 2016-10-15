using System;
using System.IO;
using Nima.Math2D;

namespace Nima
{
	public class ActorBone : ActorNode
	{
		private float m_Length;
		private bool m_IsConnectedToImage;

		public float Length
		{
			get
			{
				return m_Length;
			}
			set
			{
				if(m_Length == value)
				{
					return;
				}
				m_Length = value;
				if(m_Children == null)
				{
					return;
				}
				foreach(ActorNode node in m_Children)
				{
					ActorBone bone = node as ActorBone;
					if(bone == null)
					{
						continue;
					}
					bone.X = value;
				}
			}
		}

		public bool IsConnectedToImage
		{
			get
			{
				return m_IsConnectedToImage;
			}	
			set
			{
				m_IsConnectedToImage = value;
			}
		}

		public static ActorBone Read(Actor actor, BinaryReader reader, ActorBone node = null)
		{
			if(node == null)
			{
				node = new ActorBone();
			}
			
			ActorNode.Read(actor, reader, node);

			node.m_Length = reader.ReadSingle();


			return node;
		}

		public override ActorNode MakeInstance(Actor resetActor)
		{
			ActorBone instanceNode = new ActorBone();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

		public void Copy(ActorBone node, Actor resetActor)
		{
			base.Copy(node, resetActor);
			m_Length = node.m_Length;
			m_IsConnectedToImage = node.m_IsConnectedToImage;
		}
	}
}