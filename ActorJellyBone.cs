using System;
using System.IO;
using Nima.Math2D;

namespace Nima
{
	public class ActorJellyBone : ActorBoneBase
	{
		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorJellyBone instanceNode = new ActorJellyBone();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

        public static ActorJellyBone Read(Actor actor, BinaryReader reader, ActorJellyBone node = null)
		{
            if(node == null)
            {
                node = new ActorJellyBone();
            }
            
			ActorBoneBase.Read(actor, reader, node);
			return node;
		}
	}
}