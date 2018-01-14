using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorScaleConstraint : ActorAxisConstraint
	{
        TransformComponents m_ComponentsA;
        TransformComponents m_ComponentsB;

        ActorScaleConstraint()
        {
            m_ComponentsA = new TransformComponents();
            m_ComponentsB = new TransformComponents();
        }

        public static ActorScaleConstraint Read(Actor actor, BinaryReader reader, ActorScaleConstraint component = null)
		{
            if(component == null)
			{
				component = new ActorScaleConstraint();
			}
			ActorAxisConstraint.Read(actor, reader, component);

			return component;
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

                if(!m_CopyX)
                {
                    m_ComponentsB[2] = m_DestSpace == TransformSpace.Local ? 1.0f : m_ComponentsA[2];
                }
                else
                {
                    m_ComponentsB[2] *= m_ScaleX;	
                    if(m_Offset)
                    {
                        m_ComponentsB[2] *= m_Parent.ScaleX;
                    }
                }

                if(!m_CopyY)
                {
                    m_ComponentsB[3] = m_DestSpace == TransformSpace.Local ? 0.0f : m_ComponentsA[3];
                }
                else
                {
                    m_ComponentsB[3] *= m_ScaleY;

                    if(m_Offset)
                    {
                        m_ComponentsB[3] *= m_Parent.ScaleY;
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
            if(m_EnableMaxX && m_ComponentsB.ScaleX > m_MaxX)
            {
                m_ComponentsB.ScaleX = m_MaxX;	
            }
            if(m_EnableMinX && m_ComponentsB.ScaleX < m_MinX)
            {
                m_ComponentsB.ScaleX = m_MinX;	
            }
            if(m_EnableMaxY && m_ComponentsB.ScaleY > m_MaxY)
            {
                m_ComponentsB.ScaleY = m_MaxY;	
            }
            if(m_EnableMinY && m_ComponentsB.ScaleY < m_MinY)
            {
                m_ComponentsB.ScaleY = m_MinY;	
            }
            if(clampLocal)
            {
                // Transform back to world.
                Mat2D.Compose(transformB, m_ComponentsB);
                Mat2D.Multiply(transformB, grandParent.WorldTransform, transformB);
                Mat2D.Decompose(transformB, m_ComponentsB);
            }

            float ti = 1.0f-m_Strength;

            m_ComponentsB.Rotation = m_ComponentsA.Rotation;
            m_ComponentsB.X = m_ComponentsA.X;
            m_ComponentsB.Y = m_ComponentsA.Y;
            m_ComponentsB.ScaleX = m_ComponentsA.ScaleX * ti + m_ComponentsB.ScaleX * m_Strength;
            m_ComponentsB.ScaleY = m_ComponentsA.ScaleY * ti + m_ComponentsB.ScaleY * m_Strength;
            m_ComponentsB.Skew = m_ComponentsA.Skew;

            Mat2D.Compose(m_Parent.WorldTransform, m_ComponentsB);
        }

        public override ActorComponent MakeInstance(Actor resetActor)
        {
			ActorScaleConstraint instance = new ActorScaleConstraint();
			instance.Copy(this, resetActor);
			return instance;
        }
    }
}