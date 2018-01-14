using System;
using System.IO;
using Nima.Math2D;
using System.Collections.Generic;

namespace Nima
{
	public class JellyComponent : ActorComponent
	{
        const int JellyMax = 16;
        static float OptimalDistance = 4.0f*(float)(Math.Sqrt(2.0)-1.0)/3.0f;
        static float CurveConstant = OptimalDistance * (float)Math.Sqrt(2.0) * 0.5f;

        private float m_EaseIn;
        private float m_EaseOut;
        private float m_ScaleIn;
        private float m_ScaleOut;
        private ushort m_InTargetIdx;
        private ushort m_OutTargetIdx;
        private ActorNode m_InTarget;
        private ActorNode m_OutTarget;
        private IList<ActorJellyBone> m_Bones;
        private Vec2D m_InPoint;
        private Vec2D m_InDirection;
        private Vec2D m_OutPoint;
        private Vec2D m_OutDirection;

        private Vec2D m_CachedTip;
        private Vec2D m_CachedOut;
        private Vec2D m_CachedIn;
        private float m_CachedScaleIn;
        private float m_CachedScaleOut;

        private Vec2D[] m_JellyPoints;

        JellyComponent()
        {
            m_InPoint = new Vec2D();
            m_InDirection = new Vec2D();
            m_OutPoint = new Vec2D();
            m_OutDirection = new Vec2D();
            m_CachedTip = new Vec2D();
            m_CachedOut = new Vec2D();
            m_CachedIn = new Vec2D();

            m_JellyPoints = new Vec2D[JellyMax+1];
            for(var i = 0; i <= JellyMax; i++)
            {
                m_JellyPoints[i] = new Vec2D();
            }
        }

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			JellyComponent instanceNode = new JellyComponent();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

        public static JellyComponent Read(Actor actor, BinaryReader reader, JellyComponent node = null)
		{
            if(node == null)
            {
                node = new JellyComponent();
            }
            
			ActorComponent.Read(actor, reader, node);
			
            node.m_EaseIn = reader.ReadSingle();
            node.m_EaseOut = reader.ReadSingle();
            node.m_ScaleIn = reader.ReadSingle();
            node.m_ScaleOut = reader.ReadSingle();
            node.m_InTargetIdx = reader.ReadUInt16();
            node.m_OutTargetIdx = reader.ReadUInt16();

			return node;
		}
		public void Copy(JellyComponent node, Actor resetActor)
		{
			base.Copy(node, resetActor);
			m_EaseIn = node.m_EaseIn;
            m_EaseOut = node.m_EaseOut;
            m_ScaleIn = node.m_ScaleIn;
            m_ScaleOut = node.m_ScaleOut;
            m_InTargetIdx = node.m_InTargetIdx;
            m_OutTargetIdx = node.m_OutTargetIdx;
		}

        public override void ResolveComponentIndices(ActorComponent[] components)
        {
            base.ResolveComponentIndices(components);

            if(m_InTargetIdx != 0)
            {
                m_InTarget = components[m_InTargetIdx] as ActorNode;
            }
            if(m_OutTargetIdx != 0)
            {
                m_OutTarget = components[m_OutTargetIdx] as ActorNode;
            }
        }

		public override void CompleteResolve()
		{
			base.CompleteResolve();

            ActorBone bone = m_Parent as ActorBone;
            bone.m_Jelly = this;

            // Get jellies.
            IList<ActorNode> children = bone.Children;
            if(children == null)
            {
                return;
            }

            m_Bones = new List<ActorJellyBone>();
            foreach(ActorNode child in children)
            {
                if(child is ActorJellyBone)
                {
                    m_Bones.Add(child as ActorJellyBone);
                }
            }    
        }

        const float EPSILON = 0.001f; // Intentionally agressive.

        bool FuzzyEquals(Vec2D a, Vec2D b) 
        {
            float a0 = a[0], a1 = a[1];
            float b0 = b[0], b1 = b[1];
            return (Math.Abs(a0 - b0) <= EPSILON*Math.Max(1.0f, Math.Max(Math.Abs(a0), Math.Abs(b0))) &&
                    Math.Abs(a1 - b1) <= EPSILON*Math.Max(1.0f, Math.Max(Math.Abs(a1), Math.Abs(b1))));
        }

        void ForwardDiffBezier(float c0, float c1, float c2, float c3, Vec2D[] points, int count, int offset)
        {
            float f = (float)count;

            float p0 = c0;

            float p1 = 3.0f * (c1 - c0) / f;

            f *= count;
            float p2 = 3.0f * (c0 - 2.0f * c1 + c2) / f;
            
            f *= count;
            float p3 = (c3 - c0 + 3.0f * (c1 - c2)) / f;

            c0 = p0;
            c1 = p1 + p2 + p3;
            c2 = 2 * p2 + 6 * p3;
            c3 = 6 * p3;

            for (int a = 0; a <= count; a++) 
            {
                points[a][offset] = c0;
                c0 += c1;
                c1 += c2;
                c2 += c3;
            }
        }

        IList<Vec2D> NormalizeCurve(Vec2D[] curve, int numSegments)
        {
            IList<Vec2D> points = new List<Vec2D>();
            int curvePointCount = curve.Length;
            float[] distances = new float[curvePointCount];
            distances[0] = 0;
            for(int i = 0; i < curvePointCount-1; i++)
            {
                Vec2D p1 = curve[i];
                Vec2D p2 = curve[i+1];
                distances[i + 1] = distances[i] + Vec2D.Distance(p1, p2);
            }
            float totalDistance = distances[curvePointCount-1];

            float segmentLength = totalDistance/numSegments;
            int pointIndex = 1;
            for(int i = 1; i <= numSegments; i++)
            {
                float distance = segmentLength * i;

                while(pointIndex < curvePointCount-1 && distances[pointIndex] < distance)
                {
                    pointIndex++;
                }

                float d = distances[pointIndex];
                float lastCurveSegmentLength = d - distances[pointIndex-1];
                float remainderOfDesired = d - distance;
                float ratio = remainderOfDesired / lastCurveSegmentLength;
                float iratio = 1.0f-ratio;

                Vec2D p1 = curve[pointIndex-1];
                Vec2D p2 = curve[pointIndex];
                points.Add(new Vec2D(p1[0]*ratio+p2[0]*iratio, p1[1]*ratio+p2[1]*iratio));
            }

            return points;
        }

        private void UpdateJellies()
        {
            if(m_Bones == null)
            {
                return;
            }
            ActorBone bone = m_Parent as ActorBone;
            // We are in local bone space.
            Vec2D tipPosition = new Vec2D(bone.Length, 0.0f);

            if(FuzzyEquals(m_CachedTip, tipPosition) && FuzzyEquals(m_CachedOut, m_OutPoint) && FuzzyEquals(m_CachedIn, m_InPoint) && m_CachedScaleIn == m_ScaleIn && m_CachedScaleOut == m_ScaleOut)
            {
                return;
            }

            Vec2D.Copy(m_CachedTip, tipPosition);
            Vec2D.Copy(m_CachedOut, m_OutPoint);
            Vec2D.Copy(m_CachedIn, m_InPoint);
            m_CachedScaleIn = m_ScaleIn;
            m_CachedScaleOut = m_ScaleOut;

            Vec2D q0 = new Vec2D();
            Vec2D q1 = m_InPoint;
            Vec2D q2 = m_OutPoint;
            Vec2D q3 = tipPosition;

            ForwardDiffBezier(q0[0], q1[0], q2[0], q3[0], m_JellyPoints, JellyMax, 0);
            ForwardDiffBezier(q0[1], q1[1], q2[1], q3[1], m_JellyPoints, JellyMax, 1);

            IList<Vec2D> normalizedPoints = NormalizeCurve(m_JellyPoints, m_Bones.Count);

            Vec2D lastPoint = m_JellyPoints[0];

            float scale = m_ScaleIn;
            float scaleInc = (m_ScaleOut - m_ScaleIn)/(m_Bones.Count-1);
            for(int i = 0; i < normalizedPoints.Count; i++)
            {
                ActorJellyBone jelly = m_Bones[i];
                Vec2D p = normalizedPoints[i];

                jelly.Translation = lastPoint;
                jelly.Length = Vec2D.Distance(p, lastPoint);
                jelly.ScaleY = scale;
                scale += scaleInc;

                Vec2D diff = Vec2D.Subtract(new Vec2D(), p, lastPoint);
                jelly.Rotation = (float)Math.Atan2(diff[1], diff[0]);
                lastPoint = p;
            }
        }

        public override void Update(byte dirt)
		{
            ActorBone bone = m_Parent as ActorBone;
            ActorBone parentBone = bone.Parent as ActorBone;
            JellyComponent parentBoneJelly = parentBone == null ? null : parentBone.m_Jelly;

            Mat2D inverseWorld = new Mat2D();
            if(!Mat2D.Invert(inverseWorld, bone.WorldTransform))
            {
                return;
            }

            if(m_InTarget != null)
            {
                Vec2D translation = m_InTarget.GetWorldTranslation(new Vec2D());
                Vec2D.TransformMat2D(m_InPoint, translation, inverseWorld);
                Vec2D.Normalize(m_InDirection, m_InPoint);
            }
            else if(parentBone != null)
            {
                if(parentBone.FirstBone == bone && parentBoneJelly != null && parentBoneJelly.m_OutTarget != null)
                {
                    Vec2D translation = parentBoneJelly.m_OutTarget.GetWorldTranslation(new Vec2D());
                    Vec2D localParentOut = Vec2D.TransformMat2D(new Vec2D(), translation, inverseWorld);
                    Vec2D.Normalize(localParentOut, localParentOut);
                    Vec2D.Negate(m_InDirection, localParentOut);
                }
                else
                {
                    Vec2D d1 = new Vec2D(1.0f, 0.0f);
                    Vec2D d2 = new Vec2D(1.0f, 0.0f);

                    Vec2D.TransformMat2(d1, d1, parentBone.WorldTransform);
                    Vec2D.TransformMat2(d2, d2, bone.WorldTransform);

                    Vec2D sum = Vec2D.Add(new Vec2D(), d1, d2);
                    Vec2D.TransformMat2(m_InDirection, sum, inverseWorld);
                    Vec2D.Normalize(m_InDirection, m_InDirection);
                }
                m_InPoint[0] = m_InDirection[0] * m_EaseIn * bone.Length * CurveConstant;
                m_InPoint[1] = m_InDirection[1] * m_EaseIn * bone.Length * CurveConstant;
            }
            else
            {
                m_InDirection[0] = 1.0f;
                m_InDirection[1] = 0.0f;
                m_InPoint[0] = m_InDirection[0] * m_EaseIn * bone.Length * CurveConstant;
            }

            if(m_OutTarget != null)
            {
                Vec2D translation = m_OutTarget.GetWorldTranslation(new Vec2D());
                Vec2D.TransformMat2D(m_OutPoint, translation, inverseWorld);
                Vec2D tip = new Vec2D(bone.Length, 0.0f);
                Vec2D.Subtract(m_OutDirection, m_OutPoint, tip);
                Vec2D.Normalize(m_OutDirection, m_OutDirection);
            }
            else if(bone.FirstBone != null)
            {
                ActorBone firstBone = bone.FirstBone;
                JellyComponent firstBoneJelly = firstBone.m_Jelly;
                if(firstBoneJelly != null && firstBoneJelly.m_InTarget != null)
                {
                    Vec2D translation = firstBoneJelly.m_InTarget.GetWorldTranslation(new Vec2D());
                    Vec2D worldChildInDir = Vec2D.Subtract(new Vec2D(), firstBone.GetWorldTranslation(new Vec2D()), translation);
                    Vec2D.TransformMat2(m_OutDirection, worldChildInDir, inverseWorld);
                }
                else
                {
                    Vec2D d1 = new Vec2D(1.0f, 0.0f);
                    Vec2D d2 = new Vec2D(1.0f, 0.0f);

                    Vec2D.TransformMat2(d1, d1, firstBone.WorldTransform);
                    Vec2D.TransformMat2(d2, d2, bone.WorldTransform);

                    Vec2D sum = Vec2D.Add(new Vec2D(), d1, d2);
                    Vec2D.Negate(sum, sum);
                    Vec2D.TransformMat2(m_OutDirection, sum, inverseWorld);
                    Vec2D.Normalize(m_OutDirection, m_OutDirection);
                }
                Vec2D.Normalize(m_OutDirection, m_OutDirection);
                Vec2D scaledOut = Vec2D.Scale(new Vec2D(), m_OutDirection, m_EaseOut*bone.Length*CurveConstant);
                m_OutPoint[0] = bone.Length;
                m_OutPoint[1] = 0.0f;
                Vec2D.Add(m_OutPoint, m_OutPoint, scaledOut);
            }
            else
            {
                m_OutDirection[0] = -1.0f;
                m_OutDirection[1] = 0.0f;

                Vec2D scaledOut = Vec2D.Scale(new Vec2D(), m_OutDirection, m_EaseOut*bone.Length*CurveConstant);
                 m_OutPoint[0] = bone.Length;
                m_OutPoint[1] = 0.0f;
                Vec2D.Add(m_OutPoint, m_OutPoint, scaledOut);
            }

            UpdateJellies();
        }
	}
}