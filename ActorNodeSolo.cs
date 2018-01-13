using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorNodeSolo : ActorNode
	{
		private uint m_ActiveChildIndex = 0;

        public ActorNodeSolo()
        {

        }

        public ActorNodeSolo(Actor actor) : base(actor)
		{
        }

		public uint ActiveChildIndex
		{
			get
			{
				return m_ActiveChildIndex;
			}
			set
			{
				if(m_ActiveChildIndex != value)
				{
					m_ActiveChildIndex = value;
					for(int i = 0; i < m_Children.Count; i++)
					{
						ActorNode an = m_Children[i];
						bool cv = (i != (m_ActiveChildIndex-1));
						an.CollapsedVisibility = cv;
					}
				}
			}
		}

		public void Copy(ActorNodeSolo node, Actor resetActor)
        {
            base.Copy(node, resetActor);
			m_ActiveChildIndex = node.ActiveChildIndex;
        }
		
		public static ActorNodeSolo Read(Actor actor, BinaryReader reader, ActorNodeSolo node = null)
        {
            if (node == null)
            {
                node = new ActorNodeSolo();
            }

			ActorNode.Read(actor, reader, node);

			node.ActiveChildIndex = (uint)reader.ReadSingle();

			return node;
        }

		public override ActorComponent MakeInstance(Actor resetActor)
        {
            ActorNodeSolo instanceNode = new ActorNodeSolo();
            instanceNode.Copy(this, resetActor);
            return instanceNode;
        }
    }
}