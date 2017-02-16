using System;
using System.IO;
using System.Collections.Generic;
//using System.Diagnostics;

//using UnityEngine;

namespace Nima.Animation
{
	public class PropertyAnimation
	{
		private PropertyTypes m_Type;
		private KeyFrame[] m_KeyFrames;

		public PropertyTypes PropertyType
		{
			get
			{
				return m_Type;
			}
		}

		public IEnumerable<KeyFrame> KeyFrames
		{
			get
			{
				return m_KeyFrames;
			}
		}
		
		public static PropertyAnimation Read(BlockReader reader, ActorComponent component)
		{
			BlockReader propertyBlock = reader.ReadNextBlock();
			if(propertyBlock == null)
			{
				return null;
			}
			PropertyAnimation propertyAnimation = new PropertyAnimation();
			int type = propertyBlock.BlockType;
			if(!Enum.IsDefined(typeof(PropertyTypes), type))
			{
				return null;
			}
			else
			{
				propertyAnimation.m_Type = (PropertyTypes)type;

				Func<BinaryReader, ActorComponent, KeyFrame> keyFrameReader = null;
				switch(propertyAnimation.m_Type)
				{
					case PropertyTypes.PosX:
						keyFrameReader = KeyFramePosX.Read;
						break;
					case PropertyTypes.PosY:
						keyFrameReader = KeyFramePosY.Read;
						break;
					case PropertyTypes.ScaleX:
						keyFrameReader = KeyFrameScaleX.Read;
						break;
					case PropertyTypes.ScaleY:
						keyFrameReader = KeyFrameScaleY.Read;
						break;
					case PropertyTypes.Rotation:
						keyFrameReader = KeyFrameRotation.Read;
						break;
					case PropertyTypes.Opacity:
						keyFrameReader = KeyFrameOpacity.Read;
						break;
					case PropertyTypes.DrawOrder:
						keyFrameReader = KeyFrameDrawOrder.Read;
						break;
					case PropertyTypes.Length:
						keyFrameReader = KeyFrameLength.Read;
						break;
					case PropertyTypes.VertexDeform:
						keyFrameReader = KeyFrameVertexDeform.Read;
						break;
					case PropertyTypes.IKStrength:
						keyFrameReader = KeyFrameIKStrength.Read;
						break;

				}

				int keyFrameCount = propertyBlock.ReadUInt16();
				propertyAnimation.m_KeyFrames = new KeyFrame[keyFrameCount];
				KeyFrame lastKeyFrame = null;
				for(int i = 0; i < keyFrameCount; i++)
				{
					KeyFrame frame = keyFrameReader(propertyBlock, component);
					propertyAnimation.m_KeyFrames[i] = frame;
					if(lastKeyFrame != null)
					{
						lastKeyFrame.SetNext(frame);
					}
					lastKeyFrame = frame;
				}
			}

			return propertyAnimation;
		}

		public void Apply(float time, ActorComponent component, float mix)
		{
			if(m_KeyFrames.Length == 0)
			{
				return;
			}

			int idx = 0;
			// Binary find the keyframe index.
			{
				int mid = 0;
				float element = 0.0f;
				int start = 0;
				int end = m_KeyFrames.Length-1;

				while (start <= end) 
				{
			    	mid = ((start + end) >> 1);
					element = m_KeyFrames[mid].Time;
					if (element < time) 
					{
						start = mid + 1;
					} 
					else if (element > time) 
					{
						end = mid - 1;
					} 
					else 
					{
						start = mid;
						break;
					}
				}

				idx = start;
			}

			if(idx == 0)
			{
				m_KeyFrames[0].Apply(component, mix);
			}
			else
			{
				if(idx < m_KeyFrames.Length)
				{
					KeyFrame fromFrame = m_KeyFrames[idx-1];
					KeyFrame toFrame = m_KeyFrames[idx];
					if(time == toFrame.Time)
					{
						toFrame.Apply(component, mix);
					}
					else
					{
						fromFrame.ApplyInterpolation(component, time, toFrame, mix);
					}
				}
				else
				{
					m_KeyFrames[idx-1].Apply(component, mix);
				}
			}
		}
	}

	public class ComponentAnimation
	{
		private ushort m_ComponentIndex;
		private PropertyAnimation[] m_Properties;

		public int NodeIndex
		{
			get
			{
				return m_ComponentIndex;
			}
		}

		public IEnumerable<PropertyAnimation> Properties
		{
			get
			{
				return m_Properties;
			}
		}

		public static ComponentAnimation Read(BlockReader reader, ActorComponent[] components)
		{
			ComponentAnimation componentAnimation = new ComponentAnimation();

			componentAnimation.m_ComponentIndex = reader.ReadUInt16();
			int numProperties = (int)reader.ReadUInt16();

			componentAnimation.m_Properties = new PropertyAnimation[numProperties];
			for(int i = 0; i < numProperties; i++)
			{
				componentAnimation.m_Properties[i] = PropertyAnimation.Read(reader, components[componentAnimation.m_ComponentIndex]);
			}

			return componentAnimation;
		}

		public void Apply(float time, ActorComponent[] components, float mix)
		{
			foreach(PropertyAnimation propertyAnimation in m_Properties)
			{
				propertyAnimation.Apply(time, components[m_ComponentIndex], mix);
			}
		}
	}

	public class ActorAnimation
	{
		private string m_Name;
		private int m_FPS;
		private float m_Duration;
		private bool m_IsLooping;
		private ComponentAnimation[] m_AnimatedComponents;

		public string Name
		{
			get
			{
				return m_Name;
			}
		}

		public float Duration
		{
			get
			{
				return m_Duration;
			}
		}

		public IEnumerable<ComponentAnimation> AnimatedComponents
		{
			get
			{
				return m_AnimatedComponents;
			}
		}

		public void Apply(float time, Actor actor, float mix)
		{
			foreach(ComponentAnimation componentAnimation in m_AnimatedComponents)
			{
				componentAnimation.Apply(time, actor.AllComponents, mix);
			}
		}

		public static ActorAnimation Read(BlockReader reader, ActorComponent[] components)
		{
			ActorAnimation animation = new ActorAnimation();
			animation.m_Name = Actor.ReadString(reader);
			animation.m_FPS = (int)reader.ReadByte();
			animation.m_Duration = reader.ReadSingle();
			animation.m_IsLooping = reader.ReadByte() != 0;

			int numKeyedComponents = reader.ReadUInt16();
			animation.m_AnimatedComponents = new ComponentAnimation[numKeyedComponents];
			for(int i = 0; i < numKeyedComponents; i++)
			{
				animation.m_AnimatedComponents[i] = ComponentAnimation.Read(reader, components);
			}

			return animation;
		}
	}
}