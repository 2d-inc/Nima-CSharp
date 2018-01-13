using System;
using System.IO;
using Nima.Math2D;

namespace Nima
{
	public class ActorBone : ActorBoneBase
	{
		private ActorBone m_FirstBone;

		public ActorBone FirstBone
		{
			get
			{
				return m_FirstBone;
			}
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorBone instanceNode = new ActorBone();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

		public override void CompleteResolve()
		{
			base.CompleteResolve();
			foreach(ActorNode node in m_Children)
			{
				if(node is ActorBone)
				{
					m_FirstBone = node as ActorBone;
					return;
				}
			}
		}

		public static ActorBone Read(Actor actor, BinaryReader reader, ActorBone node = null)
		{
            if(node == null)
            {
                node = new ActorBone();
            }
            
			ActorBoneBase.Read(actor, reader, node);
			return node;
		}
	}
}