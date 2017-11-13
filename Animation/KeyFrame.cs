using System;
using System.IO;
using System.Collections.Generic;

namespace Nima.Animation
{
	public abstract class KeyFrameInterpolator
	{
		abstract public bool SetNextFrame(KeyFrameWithInterpolation frame, KeyFrame nextFrame);
	}

	// TODO: CSS style curve interoplation
	public class ProgressionTimeCurveInterpolator : KeyFrameInterpolator
	{
		public override bool SetNextFrame(KeyFrameWithInterpolation frame, KeyFrame nextFrame)
		{
			return false;
		}
	}
	
	public abstract class KeyFrame
	{
		public enum InterpolationTypes
		{
			Hold = 0,
			Linear = 1,
			Mirrored = 2,
			Asymmetric = 3,
			Disconnected = 4,
			Progression = 5
		};

		protected float m_Time;

		public float Time
		{
			get
			{
				return m_Time;
			}
		}

		public static bool Read(BinaryReader reader, KeyFrame frame)
		{
			frame.m_Time = (float)reader.ReadDouble();

			return true;
		}

		public abstract void SetNext(KeyFrame frame);
		public abstract void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix);
		public abstract void Apply(ActorComponent component, float mix);
	}

	public abstract class KeyFrameWithInterpolation : KeyFrame
	{
		protected InterpolationTypes m_InterpolationType;
		protected KeyFrameInterpolator m_Interpolator;

		public InterpolationTypes InterpolationType
		{
			get
			{
				return m_InterpolationType;
			}
		}
		
		public KeyFrameInterpolator Interpolator
		{
			get
			{
				return m_Interpolator;
			}
		}

		public static bool Read(BinaryReader reader, KeyFrameWithInterpolation frame)
		{
			if(!KeyFrame.Read(reader, frame))
			{
				return false;
			}
			int type = reader.ReadByte();
			if(!Enum.IsDefined(typeof(InterpolationTypes), type))
			{
				return false;
			}
			
			frame.m_InterpolationType = (InterpolationTypes)type;
			switch(frame.m_InterpolationType)
			{
				case KeyFrame.InterpolationTypes.Mirrored:
				case KeyFrame.InterpolationTypes.Asymmetric:
				case KeyFrame.InterpolationTypes.Disconnected:
				case KeyFrame.InterpolationTypes.Hold:
					frame.m_Interpolator = ValueTimeCurveInterpolator.Read(reader, frame.m_InterpolationType);
					break;

				default:
					frame.m_Interpolator = null;
					break;

			}
			return true;
		}

		public override void SetNext(KeyFrame frame)
		{
			// Null out the interpolator if the next frame doesn't validate.
			if(m_Interpolator != null && !m_Interpolator.SetNextFrame(this, frame))
			{
				m_Interpolator = null;
			}
		}
	}

	public abstract class KeyFrameNumeric : KeyFrameWithInterpolation
	{
		private float m_Value;

		public float Value
		{
			get
			{
				return m_Value;
			}
		}

		public static bool Read(BinaryReader reader, KeyFrameNumeric frame)
		{
			if(!KeyFrameWithInterpolation.Read(reader, frame))
			{
				return false;
			}
			frame.m_Value = reader.ReadSingle();
			/*if(frame.m_Interpolator != null)
			{
				// TODO: in the future, this could also be a progression curve.
				ValueTimeCurveInterpolator vtci = frame.m_Interpolator as ValueTimeCurveInterpolator;
				if(vtci != null)
				{
					vtci.SetKeyFrameValue(m_Value);
				}
			}*/
			return true;
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			switch(m_InterpolationType)
			{
				case KeyFrame.InterpolationTypes.Mirrored:
				case KeyFrame.InterpolationTypes.Asymmetric:
				case KeyFrame.InterpolationTypes.Disconnected:
				{
					ValueTimeCurveInterpolator interpolator = m_Interpolator as ValueTimeCurveInterpolator;
					if(interpolator != null)
					{
						float v = (float)interpolator.Get((double)time);
						SetValue(component, v, mix);
					}
					break;
				}

				case KeyFrame.InterpolationTypes.Hold:
				{
					SetValue(component, m_Value, mix);
					break;
				}

				case KeyFrame.InterpolationTypes.Linear:
				{
					KeyFrameNumeric to = toFrame as KeyFrameNumeric;

					float f = (time - m_Time)/(to.m_Time-m_Time);
					SetValue(component, m_Value * (1.0f-f) + to.m_Value * f, mix);
					break;
				}
			}
		}
		
		public override void Apply(ActorComponent component, float mix)
		{
			SetValue(component, m_Value, mix);
		}

		protected abstract void SetValue(ActorComponent component, float value, float mix);
	}

	public abstract class KeyFrameInt : KeyFrameWithInterpolation
	{
		private int m_Value;

		public int Value
		{
			get
			{
				return m_Value;
			}
		}

		public static bool Read(BinaryReader reader, KeyFrameInt frame)
		{
			if(!KeyFrameWithInterpolation.Read(reader, frame))
			{
				return false;
			}
			frame.m_Value = reader.ReadInt32();
			return true;
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			switch(m_InterpolationType)
			{
				case KeyFrame.InterpolationTypes.Mirrored:
				case KeyFrame.InterpolationTypes.Asymmetric:
				case KeyFrame.InterpolationTypes.Disconnected:
				{
					ValueTimeCurveInterpolator interpolator = m_Interpolator as ValueTimeCurveInterpolator;
					if(interpolator != null)
					{
						float v = (float)interpolator.Get((double)time);
						SetValue(component, v, mix);
					}
					break;
				}

				case KeyFrame.InterpolationTypes.Hold:
				{
					SetValue(component, m_Value, mix);
					break;
				}

				case KeyFrame.InterpolationTypes.Linear:
				{
					KeyFrameInt to = toFrame as KeyFrameInt;

					float f = (time - m_Time)/(to.m_Time-m_Time);
					SetValue(component, m_Value * (1.0f-f) + to.m_Value * f, mix);
					break;
				}
			}
		}
		
		public override void Apply(ActorComponent component, float mix)
		{
			SetValue(component, m_Value, mix);
		}

		protected abstract void SetValue(ActorComponent component, float value, float mix);
	}

	public class KeyFrameIntProperty : KeyFrameInt
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameIntProperty frame = new KeyFrameIntProperty();
			if(KeyFrameInt.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			CustomIntProperty node = component as CustomIntProperty;
			node.Value = (int)Math.Round(node.Value * (1.0f - mix) + value * mix);
		}
	}

	public class KeyFrameFloatProperty : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameFloatProperty frame = new KeyFrameFloatProperty();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			CustomFloatProperty node = component as CustomFloatProperty;
			node.Value = node.Value * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameStringProperty : KeyFrame
	{
		string m_Value;
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameStringProperty frame = new KeyFrameStringProperty();
			if(!KeyFrame.Read(reader, frame))
			{
				return null;
			}
			frame.m_Value = Actor.ReadString(reader);
			return frame;
		}

		public override void SetNext(KeyFrame frame)
		{
			// Do nothing.
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			Apply(component, mix);
		}

		public override void Apply(ActorComponent component, float mix)
		{
			CustomStringProperty prop = component as CustomStringProperty;
			prop.Value = m_Value;
		}
	}

	public class KeyFrameBooleanProperty : KeyFrame
	{
		private bool m_Value;
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameBooleanProperty frame = new KeyFrameBooleanProperty();
			if(!KeyFrame.Read(reader, frame))
			{
				return null;
			}
			frame.m_Value = reader.ReadByte() == 1;
			return frame;
		}

		public override void SetNext(KeyFrame frame)
		{
			// Do nothing.
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			Apply(component, mix);
		}

		public override void Apply(ActorComponent component, float mix)
		{
			CustomBooleanProperty prop = component as CustomBooleanProperty;
			prop.Value = m_Value;
		}
	}

	public class KeyFrameCollisionEnabledProperty : KeyFrame
	{
		private bool m_Value;
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameCollisionEnabledProperty frame = new KeyFrameCollisionEnabledProperty();
			if(!KeyFrame.Read(reader, frame))
			{
				return null;
			}
			frame.m_Value = reader.ReadByte() == 1;
			return frame;
		}

		public override void SetNext(KeyFrame frame)
		{
			// Do nothing.
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			Apply(component, mix);
		}

		public override void Apply(ActorComponent component, float mix)
		{
			ActorCollider collider = component as ActorCollider;
			collider.IsCollisionEnabled = m_Value;
		}
	}


	public class KeyFramePosX : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFramePosX frame = new KeyFramePosX();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorNode node = component as ActorNode;
			node.X = node.X * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFramePosY : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFramePosY frame = new KeyFramePosY();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorNode node = component as ActorNode;
			node.Y = node.Y * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameScaleX : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameScaleX frame = new KeyFrameScaleX();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorNode node = component as ActorNode;
			node.ScaleX = node.ScaleX * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameScaleY : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameScaleY frame = new KeyFrameScaleY();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorNode node = component as ActorNode;
			node.ScaleY = node.ScaleY * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameRotation : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameRotation frame = new KeyFrameRotation();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorNode node = component as ActorNode;
			node.Rotation = node.Rotation * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameOpacity : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameOpacity frame = new KeyFrameOpacity();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorNode node = component as ActorNode;
			node.Opacity = node.Opacity * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameLength : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameLength frame = new KeyFrameLength();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorBone bone = component as ActorBone;
			if(bone == null)
			{
				return;
			}
			bone.Length = bone.Length * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameIKStrength : KeyFrameNumeric
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameIKStrength frame = new KeyFrameIKStrength();
			if(KeyFrameNumeric.Read(reader, frame))
			{
				return frame;
			}
			return null;
		}

		protected override void SetValue(ActorComponent component, float value, float mix)
		{
			ActorIKTarget target = component as ActorIKTarget;
			if(target == null)
			{
				return;
			}
			target.Strength = target.Strength * (1.0f - mix) + value * mix;
		}
	}

	public class KeyFrameDrawOrder : KeyFrame
	{
		private struct DrawOrderIndex
		{
			public ushort nodeIdx;
			public ushort order;
		}
		private DrawOrderIndex[] m_OrderedNodes;

		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameDrawOrder frame = new KeyFrameDrawOrder();
			if(!KeyFrame.Read(reader, frame))
			{
				return null;
			}
			int numOrderedNodes = reader.ReadUInt16();
			frame.m_OrderedNodes = new DrawOrderIndex[numOrderedNodes];
			for(int i = 0; i < numOrderedNodes; i++)
			{
				DrawOrderIndex drawOrder = new DrawOrderIndex();
				drawOrder.nodeIdx = reader.ReadUInt16();
				drawOrder.order = reader.ReadUInt16();
				frame.m_OrderedNodes[i] = drawOrder;
			}
			return frame;
		}

		public override void SetNext(KeyFrame frame)
		{
			// Do nothing.
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			Apply(component, mix);
		}

		public override void Apply(ActorComponent component, float mix)
		{
			Actor actor = component.Actor;

			foreach(DrawOrderIndex doi in m_OrderedNodes)
			{
				ActorImage actorImage = actor[doi.nodeIdx] as ActorImage;
				if(actorImage != null)
				{
					actorImage.DrawOrder = doi.order;
				}
			}
		}
	}

	public class KeyFrameVertexDeform : KeyFrameWithInterpolation
	{
		private float[] m_Vertices;

		public float[] Vertices
		{
			get
			{
				return m_Vertices;
			}
		}

		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameVertexDeform frame = new KeyFrameVertexDeform();
			if(!KeyFrameWithInterpolation.Read(reader, frame))
			{
				return null;
			}

			ActorImage imageNode = component as ActorImage;
			frame.m_Vertices = new float[imageNode.VertexCount * 2];
			Actor.ReadFloat32Array(reader, frame.m_Vertices);
			
			imageNode.DoesAnimationVertexDeform = true;

			return frame;
		}

		public void TransformVertices(Nima.Math2D.Mat2D wt)
		{
			int aiVertexCount = m_Vertices.Length/2;
			float[] fv = m_Vertices;

			int vidx = 0;
			for(int j = 0; j < aiVertexCount; j++)
			{
				float x = fv[vidx];
				float y = fv[vidx+1];

				fv[vidx] = wt[0] * x + wt[2] * y + wt[4];
				fv[vidx+1] = wt[1] * x + wt[3] * y + wt[5];

				vidx += 2;
			}
		}

		public override void SetNext(KeyFrame frame)
		{
			// Do nothing.
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
			ActorImage imageNode = component as ActorImage;
			float[] wr = imageNode.AnimationDeformedVertices;
			float[] to = (toFrame as KeyFrameVertexDeform).m_Vertices;
			int l = m_Vertices.Length;

			float f = (time - m_Time)/(toFrame.Time-m_Time);
			float fi = 1.0f - f;
			if(mix == 1.0f)
			{
				for(int i = 0; i < l; i++)
				{
					wr[i] = m_Vertices[i] * fi + to[i] * f;
				}
			}
			else
			{
				float mixi = 1.0f - mix;
				for(int i = 0; i < l; i++)
				{
					float v = m_Vertices[i] * fi + to[i] * f;

					wr[i] = wr[i] * mixi + v * mix;
				}
			}

			imageNode.IsVertexDeformDirty = true;
		}
		
		public override void Apply(ActorComponent component, float mix)
		{
			ActorImage imageNode = component as ActorImage;
			int l = m_Vertices.Length;
			float[] wr = imageNode.AnimationDeformedVertices;
			if(mix == 1.0f)
			{
				for(int i = 0; i < l; i++)
				{
					wr[i] = m_Vertices[i];
				}
			}
			else
			{
				float mixi = 1.0f - mix;
				for(int i = 0; i < l; i++)
				{
					wr[i] = wr[i] * mixi + m_Vertices[i] * mix;
				}
			}

			imageNode.IsVertexDeformDirty = true;
		}
	}

	public class KeyFrameTrigger : KeyFrame
	{
		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
		{
			KeyFrameTrigger frame = new KeyFrameTrigger();
			if(!KeyFrame.Read(reader, frame))
			{
				return null;
			}
			return frame;
		}

		public override void SetNext(KeyFrame frame)
		{
			// Do nothing.
		}

		public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
		{
		}

		public override void Apply(ActorComponent component, float mix)
		{
		}
	}


	public class KeyFrameActiveChild : KeyFrame
    {
		private uint m_Value;

		public static KeyFrame Read(BinaryReader reader, ActorComponent component)
        {
			KeyFrameActiveChild frame = new KeyFrameActiveChild();
            if (!KeyFrame.Read(reader, frame))
            {
                return null;
            }
			frame.m_Value = (uint)reader.ReadSingle();
            return frame;
        }

        public override void SetNext(KeyFrame frame)
        {
            // No Interpolation
        }

        public override void ApplyInterpolation(ActorComponent component, float time, KeyFrame toFrame, float mix)
        {
			Apply(component, mix);
        }

        public override void Apply(ActorComponent component, float mix)
        {
			ActorNodeSolo soloNode = component as ActorNodeSolo;
			soloNode.ActiveChildIndex = m_Value;
        }
    }
}