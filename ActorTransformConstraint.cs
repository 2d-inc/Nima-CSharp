using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorTransformConstraint : ActorTargetedConstraint
	{
        const float PI2 = (float)(Math.PI*2.0);

		protected TransformSpace m_SourceSpace;
        protected TransformSpace m_DestSpace;
        TransformComponents m_ComponentsA;
        TransformComponents m_ComponentsB;
        
        public ActorTransformConstraint()
        {
            m_SourceSpace = TransformSpace.World;
            m_DestSpace = TransformSpace.World;
            m_ComponentsA = new TransformComponents();
            m_ComponentsB = new TransformComponents();
        }

        public override void OnDirty(byte dirt)
        {
            MarkDirty();
        }

        public static ActorTransformConstraint Read(Actor actor, BinaryReader reader, ActorTransformConstraint component = null)
		{
            if(component == null)
            {
                component = new ActorTransformConstraint();
            }
			ActorTargetedConstraint.Read(actor, reader, component);

            component.m_SourceSpace = (TransformSpace)reader.ReadByte();
            component.m_DestSpace = (TransformSpace)reader.ReadByte();

			return component;
		}

        public void Copy(ActorTransformConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

            m_SourceSpace = node.m_SourceSpace;
            m_DestSpace = node.m_DestSpace;
		}

        public override void Constrain(ActorNode node)
        {
			ActorNode target = m_Target as ActorNode;
            if(target == null)
            {
                return;
            }

            Mat2D transformA = m_Parent.WorldTransform;
            Mat2D transformB = new Mat2D(target.WorldTransform);
            if(m_SourceSpace == TransformSpace.Local)
            {
                ActorNode grandParent = target.Parent;
                if(grandParent != null)
                {
                    Mat2D inverse = new Mat2D();
                    if(!Mat2D.Invert(inverse, grandParent.WorldTransform))
                    {
                        return;
                    }
                    Mat2D.Multiply(transformB, inverse, transformB);
                }
            }
            if(m_DestSpace == TransformSpace.Local)
            {
                ActorNode grandParent = m_Parent.Parent;
                if(grandParent != null)
                {
                    Mat2D.Multiply(transformB, grandParent.WorldTransform, transformB);
                }
            }
            Mat2D.Decompose(transformA, m_ComponentsA);
            Mat2D.Decompose(transformB, m_ComponentsB);

            float angleA = m_ComponentsA.Rotation%PI2;
            float angleB = m_ComponentsB.Rotation%PI2;
            float diff = angleB - angleA;
            if(diff > Math.PI)
            {
                diff -= PI2;
            }
            else if(diff < -Math.PI)
            {
                diff += PI2;
            }

            float ti = 1.0f-m_Strength;

            m_ComponentsB.Rotation = angleA + diff * m_Strength;
            m_ComponentsB.X = m_ComponentsA.X * ti + m_ComponentsB.X * m_Strength;
            m_ComponentsB.Y = m_ComponentsA.Y * ti + m_ComponentsB.Y * m_Strength;
            m_ComponentsB.ScaleX = m_ComponentsA.ScaleX * ti + m_ComponentsB.ScaleX * m_Strength;
            m_ComponentsB.ScaleY = m_ComponentsA.ScaleY * ti + m_ComponentsB.ScaleY * m_Strength;
            m_ComponentsB.Skew = m_ComponentsA.Skew * ti + m_ComponentsB.Skew * m_Strength;

            Mat2D.Compose(m_Parent.WorldTransform, m_ComponentsB);
        }

        public override ActorComponent MakeInstance(Actor resetActor)
        {
			ActorTransformConstraint instance = new ActorTransformConstraint();
			instance.Copy(this, resetActor);
			return instance;
        }
    }
}