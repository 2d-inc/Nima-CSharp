using System;
using System.IO;
using Nima.Math2D;

namespace Nima
{
	public class ActorBoneBase : ActorNode
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
					ActorBoneBase bone = node as ActorBoneBase;
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
		public Vec2D GetTipWorldTranslation(Vec2D vec)
		{
			Mat2D transform = new Mat2D();
			transform[4] = Length;
			Mat2D.Multiply(transform, WorldTransform, transform);
			vec[0] = transform[4];
			vec[1] = transform[5];
			return vec;
		}

		public static ActorBoneBase Read(Actor actor, BinaryReader reader, ActorBoneBase node)
		{
			ActorNode.Read(actor, reader, node);

			node.m_Length = reader.ReadSingle();

			return node;
		}

		public void Copy(ActorBoneBase node, Actor resetActor)
		{
			base.Copy(node, resetActor);
			m_Length = node.m_Length;
			m_IsConnectedToImage = node.m_IsConnectedToImage;
		}
	}
}