using System;
using System.IO;
using Nima.Math2D;

namespace Nima
{//test upstream change
	public class ActorRootBone : ActorNode
	{
		public static ActorRootBone Read(Actor actor, BinaryReader reader, ActorRootBone node = null)
		{
			if(node == null)
			{
				node = new ActorRootBone();
			}
			
			ActorNode.Read(actor, reader, node);


			return node;
		}

		public override ActorNode MakeInstance(Actor resetActor)
		{
			ActorRootBone instanceNode = new ActorRootBone();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

		public void Copy(ActorRootBone node, Actor resetActor)
		{
			base.Copy(node, resetActor);
		}
	}
}