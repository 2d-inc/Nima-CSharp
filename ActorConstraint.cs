using System;
using System.IO;
using System.Collections.Generic;

namespace Nima
{
	public abstract class ActorConstraint : ActorComponent
	{
        protected bool m_IsEnabled;
        protected float m_Strength;

        public bool IsEnabled
		{
			get
			{
				return m_IsEnabled;
			}
			set
			{
				if(value == m_IsEnabled)
				{
					return;
				}
                m_IsEnabled = value;
                MarkDirty();
			}
		}

        public override void OnDirty(byte dirt)
		{
            MarkDirty();
		}

        public float Strength
		{
			get
			{
				return m_Strength;
			}
			set
			{
				if(value == m_Strength)
				{
					return;
				}
                m_Strength = value;
                MarkDirty();
			}
		}

        protected void MarkDirty()
        {
            m_Parent.MarkTransformDirty();
        }

        public abstract void Constrain(ActorNode node);

        public override void ResolveComponentIndices(ActorComponent[] components)
		{
			base.ResolveComponentIndices(components);
            if(m_Parent != null)
            {
                // This works because nodes are exported in hierarchy order, so we are assured constraints get added in order as we resolve indices.
                m_Parent.AddConstraint(this);
            }
		}

        public static ActorConstraint Read(Actor actor, BinaryReader reader, ActorConstraint component)
		{
			ActorComponent.Read(actor, reader, component);
			component.m_Strength = reader.ReadSingle();
			component.m_IsEnabled = reader.ReadByte() == 1;

			return component;
		}

        public void Copy(ActorConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_IsEnabled = node.m_IsEnabled;
			m_Strength = node.m_Strength;
		}
    }
}