using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorRotationConstraint : ActorTargetedConstraint
	{
        const float PI2 = (float)(Math.PI*2.0);

        protected bool m_Copy;
        protected float m_Scale;
        protected bool m_EnableMin;
        protected bool m_EnableMax;
        protected float m_Max;
        protected float m_Min;
        protected bool m_Offset;
		protected TransformSpace m_SourceSpace;
        protected TransformSpace m_DestSpace;
        protected TransformSpace m_MinMaxSpace;
        TransformComponents m_ComponentsA;
        TransformComponents m_ComponentsB;
        
        public ActorRotationConstraint()
        {
            m_Copy = false;
            m_Scale = 1.0f;
            m_EnableMin = false;
            m_EnableMax = false;
            m_Min = 0.0f;
            m_Max = 0.0f;
            m_Offset = false;
            m_SourceSpace = TransformSpace.World;
            m_DestSpace = TransformSpace.World;
            m_MinMaxSpace = TransformSpace.World;
            m_ComponentsA = new TransformComponents();
            m_ComponentsB = new TransformComponents();
        }

        public override void OnDirty(byte dirt)
        {
            MarkDirty();
        }

        public static ActorRotationConstraint Read(Actor actor, BinaryReader reader, ActorRotationConstraint component = null)
		{
            if(component == null)
            {
                component = new ActorRotationConstraint();
            }
			ActorTargetedConstraint.Read(actor, reader, component);

			if((component.m_Copy = reader.ReadByte() == 1))
            {
                component.m_Scale = reader.ReadSingle();
            }
            if((component.m_EnableMin = reader.ReadByte() == 1))
            {
                component.m_Min = reader.ReadSingle();
            }
            if((component.m_EnableMax = reader.ReadByte() == 1))
            {
                component.m_Max = reader.ReadSingle();
            }

            component.m_Offset = reader.ReadByte() == 1;
            component.m_SourceSpace = (TransformSpace)reader.ReadByte();
            component.m_DestSpace = (TransformSpace)reader.ReadByte();
            component.m_MinMaxSpace = (TransformSpace)reader.ReadByte();

			return component;
		}

        public void Copy(ActorRotationConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_Copy = node.m_Copy;
            m_Scale = node.m_Scale;
            m_EnableMin = node.m_EnableMin;
            m_EnableMax = node.m_EnableMax;
            m_Min = node.m_Min;
            m_Max = node.m_Max;

            m_Offset = node.m_Offset;
            m_SourceSpace = node.m_SourceSpace;
            m_DestSpace = node.m_DestSpace;
            m_MinMaxSpace = node.m_MinMaxSpace;
		}

        public override void Constrain(ActorNode node)
        {
			ActorNode target = m_Target as ActorNode;
            ActorNode grandParent = m_Parent.Parent;

            Mat2D transformA = m_Parent.WorldTransform;
            Mat2D transformB = new Mat2D();
            Mat2D.Decompose(transformA, m_ComponentsA);
            if(target == null)
            {
                Mat2D.Copy(transformB, transformA);
                m_ComponentsB[0] = m_ComponentsA[0];
                m_ComponentsB[1] = m_ComponentsA[1];
                m_ComponentsB[2] = m_ComponentsA[2];
                m_ComponentsB[3] = m_ComponentsA[3];
                m_ComponentsB[4] = m_ComponentsA[4];
                m_ComponentsB[5] = m_ComponentsA[5];
            }
            else
            {
                Mat2D.Copy(transformB, target.WorldTransform);
                if(m_SourceSpace == TransformSpace.Local)
                {
                    ActorNode sourceGrandParent = target.Parent;
                    if(sourceGrandParent != null)
                    {
                        Mat2D inverse = new Mat2D();
                        if(!Mat2D.Invert(inverse, sourceGrandParent.WorldTransform))
                        {
                            return;
                        }
                        Mat2D.Multiply(transformB, inverse, transformB);
                    }
                }
                Mat2D.Decompose(transformB, m_ComponentsB);

                if(!m_Copy)
                {
                    m_ComponentsB.Rotation = m_DestSpace == TransformSpace.Local ? 1.0f : m_ComponentsA.Rotation;
                }
                else
                {
                    m_ComponentsB.Rotation *= m_Scale;	
                    if(m_Offset)
                    {
                        m_ComponentsB.Rotation += m_Parent.Rotation;
                    }
                }

                if(m_DestSpace == TransformSpace.Local)
                {
                    // Destination space is in parent transform coordinates.
                    // Recompose the parent local transform and get it in world, then decompose the world for interpolation.
                    if(grandParent != null)
                    {
                        Mat2D.Compose(transformB, m_ComponentsB);
                        Mat2D.Multiply(transformB, grandParent.WorldTransform, transformB);
                        Mat2D.Decompose(transformB, m_ComponentsB);
                    }
                }
            }
            
            bool clampLocal = m_MinMaxSpace == TransformSpace.Local && grandParent != null;
            if(clampLocal)
            {
                // Apply min max in local space, so transform to local coordinates first.
                Mat2D.Compose(transformB, m_ComponentsB);
                Mat2D inverse = new Mat2D();
                if(!Mat2D.Invert(inverse, grandParent.WorldTransform))
                {
                    return;
                }
                Mat2D.Multiply(transformB, inverse, transformB);
                Mat2D.Decompose(transformB, m_ComponentsB);
            }
            if(m_EnableMax && m_ComponentsB.Rotation > m_Max)
            {
                m_ComponentsB.Rotation = m_Max;	
            }
            if(m_EnableMin && m_ComponentsB.Rotation < m_Min)
            {
                m_ComponentsB.Rotation = m_Min;	
            }
            if(clampLocal)
            {
                // Transform back to world.
                Mat2D.Compose(transformB, m_ComponentsB);
                Mat2D.Multiply(transformB, grandParent.WorldTransform, transformB);
                Mat2D.Decompose(transformB, m_ComponentsB);
            }

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

            m_ComponentsB.Rotation = m_ComponentsA.Rotation + diff * m_Strength;
            m_ComponentsB.X = m_ComponentsA.X;
            m_ComponentsB.Y = m_ComponentsA.Y;
            m_ComponentsB.ScaleX = m_ComponentsA.ScaleX;
            m_ComponentsB.ScaleY = m_ComponentsA.ScaleY;
            m_ComponentsB.Skew = m_ComponentsA.Skew;

            Mat2D.Compose(m_Parent.WorldTransform, m_ComponentsB);
        }

        public override ActorComponent MakeInstance(Actor resetActor)
        {
			ActorRotationConstraint instance = new ActorRotationConstraint();
			instance.Copy(this, resetActor);
			return instance;
        }
    }
}