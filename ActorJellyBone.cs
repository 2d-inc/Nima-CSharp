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
            
			// The Jelly Bone has a specialized read that doesn't go down the typical node path, this is because majority of the transform properties
			// of the Jelly Bone are controlled by the Jelly Controller and are unnecessary for serialization.
			ActorComponent.Read(actor, reader, node);
			node.m_Opacity = reader.ReadSingle();
			node.m_IsCollapsedVisibility = reader.ReadByte() == 1;
			return node;
		}
	}
}