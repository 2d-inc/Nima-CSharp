using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorDistanceConstraint : ActorTargetedConstraint
	{
		enum Mode
        {
            Closer = 0,
            Further = 1,
            Exact = 2
        }
        private Mode m_Mode;
        private float m_Distance;
        
        public ActorDistanceConstraint()
        {
            m_Distance = 100.0f;
            m_Mode = Mode.Closer;
        }

        public static ActorDistanceConstraint Read(Actor actor, BinaryReader reader, ActorDistanceConstraint component = null)
		{
            if(component == null)
			{
				component = new ActorDistanceConstraint();
			}
			ActorTargetedConstraint.Read(actor, reader, component);

			component.m_Distance = reader.ReadSingle();
            component.m_Mode = (Mode)reader.ReadByte();

			return component;
		}

        public override void Constrain(ActorNode node)
        {
			ActorNode target = m_Target as ActorNode;
			if(target == null)
			{
				return;
			}
			
            Vec2D targetTranslation = target.GetWorldTranslation(new Vec2D());
            Vec2D ourTranslation = m_Parent.GetWorldTranslation(new Vec2D());

            Vec2D toTarget = Vec2D.Subtract(new Vec2D(), ourTranslation, targetTranslation);
            float currentDistance = Vec2D.Length(toTarget);
            switch(m_Mode)
            {
                case Mode.Closer:
                    if(currentDistance < m_Distance)
                    {
                        return;
                    }
                    break;
                case Mode.Further:
                    if(currentDistance > m_Distance)
                    {
                        return;
                    }
                    break;
            }
            if(currentDistance < 0.001)
            {
                return;
            }

            Vec2D.Scale(toTarget, toTarget, 1.0f/currentDistance);
            Vec2D.Scale(toTarget, toTarget, m_Distance);

            Mat2D world = m_Parent.WorldTransform;
            Vec2D position = Vec2D.Lerp(new Vec2D(), ourTranslation, Vec2D.Add(new Vec2D(), targetTranslation, toTarget), m_Strength);
            world[4] = position[0];
            world[5] = position[1];
        }

        public override ActorComponent MakeInstance(Actor resetActor)
        {
			ActorDistanceConstraint instance = new ActorDistanceConstraint();
			instance.Copy(this, resetActor);
			return instance;
        }

        public void Copy(ActorDistanceConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_Mode = node.m_Mode;
            m_Distance = node.m_Distance;
		}
    }
}