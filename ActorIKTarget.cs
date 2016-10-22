using System;
using System.IO;
using Nima.Math2D;

namespace Nima
{
	public class ActorIKTarget : ActorNode, ISolver
	{
		public class InfluencedBone
		{
			public ushort m_BoneIdx;
			public ActorBone m_Bone;
		}

		private InfluencedBone[] m_InfluencedBones;
		private bool m_InvertDirection;
		private float m_Strength;
		private int m_Order;
		

		private ActorBone m_Bone1;
		private ActorBone m_Bone1Child;
		private ActorBone m_Bone2;

		private class BoneChain
		{
			public ActorBone m_Bone;
			public float m_Angle;
			public bool m_Included;
		}

		private BoneChain[] m_Chain;

		public static ActorIKTarget Read(Actor actor, BinaryReader reader, ActorIKTarget node = null)
		{
			if(node == null)
			{
				node = new ActorIKTarget();
			}
			
			ActorNode.Read(actor, reader, node);

			node.m_Order = reader.ReadUInt16();
			node.m_Strength = reader.ReadSingle();
			node.m_InvertDirection = reader.ReadByte() == 1;
			
			int numInfluencedBones = (int)reader.ReadByte();
			if(numInfluencedBones > 0)
			{
				node.m_InfluencedBones = new InfluencedBone[numInfluencedBones];

				for(int i = 0; i < numInfluencedBones; i++)
				{
					InfluencedBone ib = new InfluencedBone();
					ib.m_BoneIdx = reader.ReadUInt16();
					node.m_InfluencedBones[i] = ib;
				}
			}

			return node;
		}

		private bool DoesInfluence(ActorBone bone)
		{
			if(bone == null)
			{
				return false;
			}
			foreach(InfluencedBone b in m_InfluencedBones)
			{
				if(b.m_Bone == bone)
				{
					return true;
				}
			}	
			return false;
		}

		public override void ResolveNodeIndices(ActorNode[] nodes)
		{
			base.ResolveNodeIndices(nodes);
			if(m_InfluencedBones != null)
			{
				for(int i = 0; i < m_InfluencedBones.Length; i++)
				{
					InfluencedBone ib = m_InfluencedBones[i];
					ib.m_Bone = nodes[ib.m_BoneIdx] as ActorBone;
					if(ib.m_Bone != null)
					{
						ib.m_Bone.AddDependent(this);
					}
				}	

				if(m_InfluencedBones.Length > 0)
				{
					m_Bone1 = m_InfluencedBones[0].m_Bone;
					m_Bone2 = m_InfluencedBones[m_InfluencedBones.Length-1].m_Bone;

					ActorBone b1c = m_Bone2;
					ActorBone b1 = m_Bone1;
					while(b1c != null && b1c.Parent != b1)
					{
						b1c = b1c.Parent as ActorBone;
					}
					m_Bone1Child = b1c;
					
					int chainCount = 0;
					ActorBone end = m_Bone2;
					while(end != null && end != b1.Parent)
					{
						chainCount++;
						end = end.Parent as ActorBone;
					}

					m_Chain = new BoneChain[chainCount];
					end = m_Bone2;
					int chainIdx = 0;
					while(end != null && end != b1.Parent)
					{
						BoneChain bc = new BoneChain();
						bc.m_Bone = end;
						bc.m_Angle = 0.0f;
						bc.m_Included = DoesInfluence(end) || DoesInfluence(end.Parent as ActorBone);
						m_Chain[chainIdx++] = bc;
						end = end.Parent as ActorBone;
					}
				}
			}
		}

		public override ActorNode MakeInstance(Actor resetActor)
		{
			ActorIKTarget instanceNode = new ActorIKTarget();
			instanceNode.Copy(this, resetActor);
			return instanceNode;
		}

		public void Copy(ActorIKTarget node, Actor resetActor)
		{
			base.Copy(node, resetActor);

			m_Order = node.m_Order;
			m_InvertDirection = node.m_InvertDirection;
			m_Strength = node.m_Strength;
			m_InfluencedBones = new InfluencedBone[node.m_InfluencedBones.Length];
			for(int i = 0; i < m_InfluencedBones.Length; i++)
			{
				InfluencedBone ib = new InfluencedBone();
				ib.m_BoneIdx = node.m_InfluencedBones[i].m_BoneIdx;
				m_InfluencedBones[i] = ib;
			}
		}
		
		public float Strength
		{
			get
			{
				return m_Strength;
			}
			set
			{
				m_Strength = value;
				MarkDirty();
			}
		}

		public int Order
		{
			get
			{
				return m_Order;
			}
		}
		
		public bool NeedsSolve
		{
			get
			{
				return IsWorldDirty || IsDirty;
			}
		}

		public void SolveStart()
		{
			if(m_Bone1 == null)
			{
				return;
			}

			// Reset all rotation overrides to FK ones,
			if(m_Bone1Child != m_Bone2)
			{
				m_Bone1Child.SetRotationOverride(m_Bone1Child.Rotation);
			}

			foreach(InfluencedBone b in m_InfluencedBones)
			{
				b.m_Bone.SetRotationOverride(b.m_Bone.Rotation);
			}
		}

		private void Solve2(ActorBone b1, ActorBone b2, float[] worldTargetTranslation, bool invert)
		{
			float[] world = b1.Parent.WorldTransform;
			ActorBone b1c = b2;
			while(b1c != null && b1c.Parent != b1)
			{
				b1c = b1c.Parent as ActorBone;
			}
			// Transform to root bone space
			ActorBone b1p = b1.Parent as ActorBone;
			// If it's an ActorRootBone, it has no length.
			if(b1p != null && b1p.Length > 0.0f)
			{
				float[] t = Mat2D.Create();
				t[4] = b1p.Length;
				Mat2D.Multiply(t, world, t);
				world = t;
			}

			float[] iworld = Mat2D.Create();
			Mat2D.Invert(iworld, world);

			float[] pA = b1.GetWorldTranslation(Vec2D.Create());
			float[] pC = b1.GetTipWorldTranslation(Vec2D.Create());
			float[] pB = b2.GetTipWorldTranslation(Vec2D.Create());
			float[] pBT = Vec2D.Clone(worldTargetTranslation);

			Vec2D.TransformMat2D(pA, pA, iworld);
			Vec2D.TransformMat2D(pC, pC, iworld);
			Vec2D.TransformMat2D(pB, pB, iworld);
			 Vec2D.TransformMat2D(pBT, pBT, iworld);

			// http://mathworld.wolfram.com/LawofCosines.html
			float[] av = Vec2D.Subtract(Vec2D.Create(), pB, pC);
			float a = Vec2D.Length(av);

			float[] bv = Vec2D.Subtract(Vec2D.Create(), pC, pA);
			float b = Vec2D.Length(bv);

			float[] cv = Vec2D.Subtract(Vec2D.Create(), pBT, pA);
			float c = Vec2D.Length(cv);

			float A = (float)Math.Acos((double)Math.Max(-1.0f,Math.Min(1.0f,(-a*a+b*b+c*c)/(2.0f*b*c))));
			float C = (float)Math.Acos((double)Math.Max(-1.0f, Math.Min(1.0f,(a*a+b*b-c*c)/(2.0f*a*b))));

			float angleCorrection = 0;
			if(b1c != b2)
			{
				float[] iworld2 = Mat2D.Create();
				Mat2D.Invert(iworld2, b1c.WorldTransform);

				float[] pa2 = b2.GetTipWorldTranslation(Vec2D.Create());
				float[] tipBone2Local = Vec2D.TransformMat2D(pa2, pa2, iworld2);
				angleCorrection = -(float)Math.Atan2((double)tipBone2Local[1], (double)tipBone2Local[0]);
			}
			if(invert)
			{
				b1.SetRotationOverride((float)Math.Atan2((double)pBT[1],(double)pBT[0]) - A);
				b1c.SetRotationOverride(-C+(float)Math.PI+angleCorrection);
			}
			else
			{
				b1.SetRotationOverride(A+(float)Math.Atan2((double)pBT[1],(double)pBT[0]));
				b1c.SetRotationOverride(C-(float)Math.PI+angleCorrection);
			}
		}
		
		private void Solve1(ActorBone b1, float[] worldTargetTranslation)
		{
			float[] iworld2 = Mat2D.Create();
			Mat2D.Invert(iworld2, b1.WorldTransform);

			float[] targetLocal = Vec2D.TransformMat2D(Vec2D.Create(), worldTargetTranslation, iworld2);
			float a = (float)Math.Atan2((double)targetLocal[1], (double)targetLocal[0]);

			b1.SetRotationOverride(b1.OverrideRotationValue+a);
		}

		public void Solve()
		{
			if(m_Chain == null)
			{
				return;
			}
			float[] worldTargetTranslation = Vec2D.Create();
			float[] wt = WorldTransform;
			worldTargetTranslation[0] = wt[4];
			worldTargetTranslation[1] = wt[5];

			for(int i = 0; i < m_Chain.Length; i++)
			{
				BoneChain fk = m_Chain[i];
				fk.m_Angle = fk.m_Bone.OverrideRotationValue;
			}

			if(m_InfluencedBones.Length == 1)
			{
				Solve1(m_InfluencedBones[0].m_Bone, worldTargetTranslation);
			}
			else if(m_InfluencedBones.Length == 2)
			{
				Solve2(m_InfluencedBones[0].m_Bone, m_InfluencedBones[1].m_Bone, worldTargetTranslation, m_InvertDirection);
			}
			else
			{
				for(int i = 0; i < m_InfluencedBones.Length-1; i++)
				{
					Solve2(m_InfluencedBones[i].m_Bone, m_Bone2, worldTargetTranslation, m_InvertDirection);
				}
			}

			// At the end, mix the FK angle with the IK angle by strength
			float m = m_Strength;
			if(m != 1.0f)
			{
				float im = 1.0f-m_Strength;
				for(int i = 0; i < m_Chain.Length; i++)
				{
					BoneChain fk = m_Chain[i];
					if(fk.m_Included)
					{
						fk.m_Bone.SetRotationOverride(fk.m_Bone.OverrideRotationValue * m + fk.m_Angle * im);
					}
				}
			}
		}
	}
}