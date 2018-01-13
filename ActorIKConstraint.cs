using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
    public class InfluencedBone
    {
        public ushort m_BoneIdx;
        public ActorBone m_Bone;
    }

	public class ActorIKConstraint : ActorTargetedConstraint
	{
		const float PI2 = (float)Math.PI*2;
		const float PI = (float)Math.PI;
        private class BoneChain
		{
			public int m_Index;
			public ActorBone m_Bone;
			public float m_Angle;
			public bool m_Included;
            public TransformComponents m_TransformComponents;
			public Mat2D m_ParentWorldInverse;
		}
        private bool m_InvertDirection;
        private InfluencedBone[] m_InfluencedBones;
        private BoneChain[] m_FKChain;
        private IList<BoneChain> m_BoneData;

        protected ActorIKConstraint()
        {
        }

        public bool InvertDirection
		{
			get
			{
				return m_InvertDirection;
			}
			set
			{
				if(value == m_InvertDirection)
				{
					return;
				}
                m_InvertDirection = value;
                MarkDirty();
			}
		}

        public override void ResolveComponentIndices(ActorComponent[] components)
		{
			base.ResolveComponentIndices(components);
            if(m_Parent != null)
            {
                // This works because nodes are exported in hierarchy order, so we are assured constraints get added in order as we resolve indices.
                m_Parent.AddConstraint(this);
            }

			if(m_InfluencedBones != null)
			{
				foreach(InfluencedBone influenced in m_InfluencedBones)
				{
					influenced.m_Bone = components[influenced.m_BoneIdx] as ActorBone;
				}
			}
		}

		public override void CompleteResolve()
		{
			base.CompleteResolve();
			if(m_InfluencedBones != null || m_InfluencedBones.Length == 0)
			{
				return;
			}

			// Initialize solver.
			ActorBone start = m_InfluencedBones[0].m_Bone;
			ActorBone end = m_InfluencedBones[m_InfluencedBones.Length-1].m_Bone;
			int count = 0;
			while(end != null && end != start.Parent)
			{
				count++;
				end = end.Parent as ActorBone;
			}

			bool allIn = count < 3;
			end = m_InfluencedBones[m_InfluencedBones.Length-1].m_Bone;
			m_FKChain = new BoneChain[count];
			int idx = count-1;
			while(end != null && end != start.Parent)
			{
				BoneChain bc = new BoneChain();
				bc.m_Bone = end;
				bc.m_Angle = 0.0f;
				bc.m_Included = allIn;
				bc.m_TransformComponents = new TransformComponents();
				bc.m_ParentWorldInverse = new Mat2D();
				bc.m_Index = idx;
				m_FKChain[idx--] = bc;
				end = end.Parent as ActorBone;
			}

			// Make sure bones are good.
			m_BoneData = new List<BoneChain>();
			foreach(InfluencedBone bone in m_InfluencedBones)
			{
				BoneChain item = Array.Find(m_FKChain, chainItem => chainItem.m_Bone == bone.m_Bone);
				if(item == null)
				{
					Console.WriteLine("Bone not in chain: " + bone.m_Bone.Name);
					continue;
				}
				m_BoneData.Add(item);
			}
			if(!allIn)
			{
				// Influenced bones are in the IK chain.
				for(int i = 0; i < m_BoneData.Count-1; i++)
				{
					BoneChain item = m_BoneData[i];
					item.m_Included = true;
					m_FKChain[item.m_Index+1].m_Included = true;
				}
				
			}
			
			// Finally mark dependencies.
			foreach(InfluencedBone bone in m_InfluencedBones)
			{
				// Don't mark dependency on parent as ActorComponent already does this.
				if(bone.m_Bone == m_Parent)
				{
					continue;
				}

				m_Actor.AddDependency(this, bone.m_Bone);
			}

			if(m_Target != null)
			{
				m_Actor.AddDependency(this, m_Target);
			}

			// All the first level children of the influenced bones should depend on the final bone.
			BoneChain tip = m_FKChain[m_FKChain.Length-1];
			foreach(BoneChain fk in m_FKChain)
			{
				if(fk == tip)
				{
					continue;
				}

				ActorBone bone = fk.m_Bone;
				foreach(ActorNode node in bone.Children)
				{
					BoneChain item = Array.Find(m_FKChain, chainItem => chainItem.m_Bone == node);
					if(item != null)
					{
						// node is in the FK chain.
						continue;
					}
					m_Actor.AddDependency(node, tip.m_Bone);
				}
			}
		}

        public static ActorIKConstraint Read(Actor actor, BinaryReader reader, ActorIKConstraint component)
		{
            if(component == null)
			{
				component = new ActorIKConstraint();
			}
			ActorConstraint.Read(actor, reader, component);

			component.m_InvertDirection = reader.ReadByte() == 1;
			
			int numInfluencedBones = (int)reader.ReadByte();
			if(numInfluencedBones > 0)
			{
				component.m_InfluencedBones = new InfluencedBone[numInfluencedBones];

				for(int i = 0; i < numInfluencedBones; i++)
				{
					InfluencedBone ib = new InfluencedBone();
					ib.m_BoneIdx = reader.ReadUInt16();
					component.m_InfluencedBones[i] = ib;
				}
			}

			return component;
		}

        public override void Constrain(ActorNode node)
        {
			ActorNode target = m_Target as ActorNode;
			if(target == null)
			{
				return;
			}
			Vec2D worldTargetTranslation = new Vec2D();
			target.GetWorldTranslation(worldTargetTranslation);
			
			if(m_InfluencedBones.Length == 0)
			{
				return;
			}

			// Decompose the chain.
			foreach(BoneChain item in m_FKChain)
			{
				ActorBone bone = item.m_Bone;
				Mat2D parentWorld = bone.Parent.WorldTransform;
				Mat2D.Invert(item.m_ParentWorldInverse, parentWorld);
				Mat2D.Multiply(bone.Transform, item.m_ParentWorldInverse, bone.WorldTransform);
				Mat2D.Decompose(bone.Transform, item.m_TransformComponents);
			}

			int count = m_BoneData.Count;
			if(count == 1)
			{
				Solve1(m_BoneData[0], worldTargetTranslation);
			}
			else if(count == 2)
			{
				Solve2(m_BoneData[1], m_BoneData[2], worldTargetTranslation);
			}
			else
			{
				BoneChain tip = m_BoneData[count-1];
				for(int i = 0; i < count-1; i++)
				{
					BoneChain item = m_BoneData[i];
					Solve2(item, tip, worldTargetTranslation);
					for(int j = item.m_Index+1; j < m_FKChain.Length-1; j++)
					{
						BoneChain fk = m_FKChain[j];
						Mat2D.Invert(fk.m_ParentWorldInverse, fk.m_Bone.Parent.WorldTransform);
					}
				}
			}

			// At the end, mix the FK angle with the IK angle by strength
			if(m_Strength != 1.0)
			{
				foreach(BoneChain fk in m_FKChain)
				{
					if(!fk.m_Included)
					{
						ActorBone bone = fk.m_Bone;
						Mat2D.Multiply(bone.WorldTransform, bone.Parent.WorldTransform, bone.Transform);
						continue;
					}
					float fromAngle = fk.m_TransformComponents.Rotation%PI2;
					float toAngle = fk.m_Angle%PI2;
					float diff = toAngle - fromAngle;
					if(diff > PI)
					{
						diff -= PI2;
					}
					else if(diff < -PI)
					{
						diff += PI2;
					}
					float angle = fromAngle + diff * m_Strength;
					ConstrainRotation(fk, angle);
				}
			}
        }

		void ConstrainRotation(BoneChain fk, float rotation)
		{
			ActorBone bone = fk.m_Bone;
			Mat2D parentWorld = bone.Parent.WorldTransform;
			Mat2D transform = bone.Transform;
			TransformComponents c = fk.m_TransformComponents;

			if(rotation == 0.0f)
			{
				Mat2D.Identity(transform);
			}
			else
			{
				Mat2D.FromRotation(transform, rotation);
			}
			// Translate
			transform[4] = c.X;
			transform[5] = c.Y;
			// Scale
			float scaleX = c.ScaleX;
			float scaleY = c.ScaleY;
			transform[0] *= scaleX;
			transform[1] *= scaleX;
			transform[2] *= scaleY;
			transform[3] *= scaleY;
			// Skew
			float skew = c.Skew;
			if(skew != 0.0)
			{
				transform[2] = transform[0] * skew + transform[2];
				transform[3] = transform[1] * skew + transform[3];
			}

			Mat2D.Multiply(bone.WorldTransform, parentWorld, transform);
		}

		void Solve1(BoneChain fk1, Vec2D worldTargetTranslation)
		{
			Mat2D iworld = fk1.m_ParentWorldInverse;
			var pA = new Vec2D();
			fk1.m_Bone.GetWorldTranslation(pA);
			var pBT = new Vec2D(worldTargetTranslation);

			// To target in worldspace
			Vec2D toTarget = Vec2D.Subtract(new Vec2D(), pBT, pA);
			// Note this is directional, hence not transformMat2d
			Vec2D toTargetLocal = Vec2D.TransformMat2(new Vec2D(), toTarget, iworld);
			float r = (float)Math.Atan2(toTargetLocal[1], toTargetLocal[0]);
			
			ConstrainRotation(fk1, r);
			fk1.m_Angle = r;

		}

		void Solve2(BoneChain fk1, BoneChain fk2, Vec2D worldTargetTranslation)
		{
			ActorBone b1 = fk1.m_Bone;
			ActorBone b2 = fk2.m_Bone;
			BoneChain firstChild = m_FKChain[fk1.m_Index+1];

			Mat2D iworld = fk1.m_ParentWorldInverse;

			Vec2D pA = b1.GetWorldTranslation(new Vec2D());
			Vec2D pC = firstChild.m_Bone.GetWorldTranslation(new Vec2D());
			Vec2D pB = b2.GetTipWorldTranslation(new Vec2D());;
			Vec2D pBT = new Vec2D(worldTargetTranslation);

			pA = Vec2D.TransformMat2D(pA, pA, iworld);
			pC = Vec2D.TransformMat2D(pC, pC, iworld);
			pB = Vec2D.TransformMat2D(pB, pB, iworld);
			pBT = Vec2D.TransformMat2D(pBT, pBT, iworld);

			// http://mathworld.wolfram.com/LawofCosines.html
			Vec2D av = Vec2D.Subtract(new Vec2D(), pB, pC);
			float a = Vec2D.Length(av);

			Vec2D bv = Vec2D.Subtract(new Vec2D(), pC, pA);
			float b = Vec2D.Length(bv);

			Vec2D cv = Vec2D.Subtract(new Vec2D(), pBT, pA);
			float c = Vec2D.Length(cv);

			float A = (float)Math.Acos(Math.Max(-1,Math.Min(1,(-a*a+b*b+c*c)/(2*b*c))));
			float C = (float)Math.Acos(Math.Max(-1, Math.Min(1,(a*a+b*b-c*c)/(2*a*b))));

			float r1, r2;
			if(b2.Parent != b1)
			{
				BoneChain secondChild = m_FKChain[fk1.m_Index+2];
				
				Mat2D secondChildWorldInverse = secondChild.m_ParentWorldInverse;

				pC = firstChild.m_Bone.GetWorldTranslation(new Vec2D());
				pB = b2.GetTipWorldTranslation(new Vec2D());

				Vec2D avec = Vec2D.Subtract(new Vec2D(), pB, pC);
				Vec2D avLocal = Vec2D.TransformMat2(new Vec2D(), avec, secondChildWorldInverse);
				float angleCorrection = (float)-Math.Atan2(avLocal[1], avLocal[0]);

				if(m_InvertDirection)
				{
					r1 = (float)Math.Atan2(cv[1],cv[0]) - A;
					r2 = -C+PI + angleCorrection;
				}
				else
				{
					r1 = A + (float)Math.Atan2(cv[1],cv[0]);
					r2 = C-PI + angleCorrection;
				}
			}
			else if(m_InvertDirection)
			{
				r1 = (float)Math.Atan2(cv[1],cv[0]) - A;
				r2 = -C+PI;
			}
			else
			{
				r1 = A + (float)Math.Atan2(cv[1],cv[0]);
				r2 = C-PI;
			}

			ConstrainRotation(fk1, r1);
			ConstrainRotation(firstChild, r2);
			if(firstChild != fk2)
			{
				ActorBone bone = fk2.m_Bone;
				Mat2D.Multiply(bone.WorldTransform, bone.Parent.WorldTransform, bone.Transform);
			}

			// Simple storage, need this for interpolation.
			fk1.m_Angle = r1;
			firstChild.m_Angle = r2;
		}

        public override ActorComponent MakeInstance(Actor resetActor)
        {
			ActorIKConstraint instance = new ActorIKConstraint();
			instance.Copy(this, resetActor);
			return instance;
        }

        public void Copy(ActorIKConstraint node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_InvertDirection = node.m_InvertDirection;
			m_InfluencedBones = new InfluencedBone[node.m_InfluencedBones.Length];
			for(int i = 0; i < m_InfluencedBones.Length; i++)
			{
				InfluencedBone ib = new InfluencedBone();
				ib.m_BoneIdx = node.m_InfluencedBones[i].m_BoneIdx;
				m_InfluencedBones[i] = ib;
			}
		}
    }
}