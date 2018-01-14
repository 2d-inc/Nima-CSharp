using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorTranslationConstraint : ActorAxisConstraint
	{
        public static ActorTranslationConstraint Read(Actor actor, BinaryReader reader, ActorTranslationConstraint component = null)
		{
            if(component == null)
			{
				component = new ActorTranslationConstraint();
			}
			ActorAxisConstraint.Read(actor, reader, component);

			return component;
		}

        public override void Constrain(ActorNode node)
        {
			ActorNode target = m_Target as ActorNode;
            ActorNode grandParent = m_Parent.Parent;

            Mat2D transformA = m_Parent.WorldTransform;
            Vec2D translationA = new Vec2D(transformA[4], transformA[5]);
            Vec2D translationB = new Vec2D();
            if(target == null)
            {
                Vec2D.Copy(translationB, translationA);
            }
            else
            {
                Mat2D transformB = new Mat2D(target.WorldTransform);
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
                translationB[0] = transformB[4];
                translationB[1] = transformB[5];

                if(!m_CopyX)
                {
                    translationB[0] = m_DestSpace == TransformSpace.Local ? 0.0f : translationA[0];
                }
                else
                {
                    translationB[0] *= m_ScaleX;	
                    if(m_Offset)
                    {
                        translationB[0] += m_Parent.X;
                    }
                }

                if(!m_CopyY)
                {
                    translationB[1] = m_DestSpace == TransformSpace.Local ? 0.0f : translationA[1];
                }
                else
                {
                    translationB[1] *= m_ScaleY;

                    if(m_Offset)
                    {
                        translationB[1] += m_Parent.Y;
                    }
                }

                if(m_DestSpace == TransformSpace.Local)
                {
                    // Destination space is in parent transform coordinates.
                    if(grandParent != null)
                    {
                        Vec2D.TransformMat2D(translationB, translationB, grandParent.WorldTransform);
                    }
                }
            }
            
            bool clampLocal = m_MinMaxSpace == TransformSpace.Local && grandParent != null;
            if(clampLocal)
            {
                // Apply min max in local space, so transform to local coordinates first.
                Mat2D invert = new Mat2D();
                if(!Mat2D.Invert(invert, grandParent.WorldTransform))
                {
                    return;
                }
                // Get our target world coordinates in parent local.
                Vec2D.TransformMat2D(translationB, translationB, invert);
            }
            if(m_EnableMaxX && translationB[0] > m_MaxX)
            {
                translationB[0] = m_MaxX;	
            }
            if(m_EnableMinX && translationB[0] < m_MinX)
            {
                translationB[0] = m_MinX;	
            }
            if(m_EnableMaxY && translationB[1] > m_MaxY)
            {
                translationB[1] = m_MaxY;	
            }
            if(m_EnableMinY && translationB[1] < m_MinY)
            {
                translationB[1] = m_MinY;	
            }
            if(clampLocal)
            {
                // Transform back to world.
                Vec2D.TransformMat2D(translationB, translationB, grandParent.WorldTransform);
            }

            float ti = 1.0f-m_Strength;

            // Just interpolate world translation
            transformA[4] = translationA[0] * ti + translationB[0] * m_Strength;
            transformA[5] = translationA[1] * ti + translationB[1] * m_Strength;
        }

        public override ActorComponent MakeInstance(Actor resetActor)
        {
			ActorTranslationConstraint instance = new ActorTranslationConstraint();
			instance.Copy(this, resetActor);
			return instance;
        }
    }
}