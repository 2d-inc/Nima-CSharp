using System;
using System.IO;
using System.Collections.Generic;

namespace Nima
{
	public abstract class ActorTargetedConstraint : ActorConstraint
	{
        protected ushort m_TargetIdx;
        protected ActorComponent m_Target;

        public ActorComponent Target
        {
            get
            {
                return  m_Target;
            }
        }
        public static ActorTargetedConstraint Read(Actor actor, BinaryReader reader, ActorTargetedConstraint component)
		{
			ActorConstraint.Read(actor, reader, component);
			component.m_TargetIdx = reader.ReadUInt16();

			return component;
		}

        public override void ResolveComponentIndices(ActorComponent[] components)
		{
            base.ResolveComponentIndices(components);
            if(m_TargetIdx != 0)
            {
                m_Target = components[m_TargetIdx];
                if(m_Target != null)
                {
                    m_Actor.AddDependency(m_Parent, m_Target);
                }
            }
        }

        public void Copy(ActorTargetedConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_TargetIdx = node.m_TargetIdx;
		}
    }
}