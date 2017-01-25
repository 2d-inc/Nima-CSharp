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
		
		public static PropertyAnimation Read(BlockReader reader, ActorNode node)
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

				Func<BinaryReader, ActorNode, KeyFrame> keyFrameReader = null;
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
					KeyFrame frame = keyFrameReader(propertyBlock, node);
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

		public void Apply(float time, ActorNode node, float mix)
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
				m_KeyFrames[0].Apply(node, mix);
			}
			else
			{
				if(idx < m_KeyFrames.Length)
				{
					KeyFrame fromFrame = m_KeyFrames[idx-1];
					KeyFrame toFrame = m_KeyFrames[idx];
					if(time == toFrame.Time)
					{
						toFrame.Apply(node, mix);
					}
					else
					{
						fromFrame.ApplyInterpolation(node, time, toFrame, mix);
					}
				}
				else
				{
					m_KeyFrames[idx-1].Apply(node, mix);
				}
			}
		}
	}

	public class NodeAnimation
	{
		private ushort m_NodeIndex;
		private PropertyAnimation[] m_Properties;

		public int NodeIndex
		{
			get
			{
				return m_NodeIndex;
			}
		}

		public IEnumerable<PropertyAnimation> Properties
		{
			get
			{
				return m_Properties;
			}
		}

		public static NodeAnimation Read(BlockReader reader, ActorNode[] nodes)
		{
			NodeAnimation nodeAnimation = new NodeAnimation();

			nodeAnimation.m_NodeIndex = reader.ReadUInt16();
			int numProperties = (int)reader.ReadUInt16();

			nodeAnimation.m_Properties = new PropertyAnimation[numProperties];
			for(int i = 0; i < numProperties; i++)
			{
				nodeAnimation.m_Properties[i] = PropertyAnimation.Read(reader, nodes[nodeAnimation.m_NodeIndex]);
			}

			return nodeAnimation;
		}

		public void Apply(float time, ActorNode[] nodes, float mix)
		{
			foreach(PropertyAnimation propertyAnimation in m_Properties)
			{
				propertyAnimation.Apply(time, nodes[m_NodeIndex], mix);
			}
		}
	}

	public class ActorAnimation
	{
		private string m_Name;
		private int m_FPS;
		private float m_Duration;
		private bool m_IsLooping;
		private NodeAnimation[] m_AnimatedNodes;

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

		public IEnumerable<NodeAnimation> AnimatedNodes
		{
			get
			{
				return m_AnimatedNodes;
			}
		}

		public void Apply(float time, Actor actor, float mix)
		{
			foreach(NodeAnimation nodeAnimation in m_AnimatedNodes)
			{
				nodeAnimation.Apply(time, actor.AllNodes, mix);
			}
		}

		public static ActorAnimation Read(BlockReader reader, ActorNode[] nodes)
		{
			ActorAnimation animation = new ActorAnimation();
			animation.m_Name = Actor.ReadString(reader);
			animation.m_FPS = (int)reader.ReadByte();
			animation.m_Duration = reader.ReadSingle();
			animation.m_IsLooping = reader.ReadByte() != 0;

			int numKeyedNodes = reader.ReadUInt16();
			animation.m_AnimatedNodes = new NodeAnimation[numKeyedNodes];
			for(int i = 0; i < numKeyedNodes; i++)
			{
				animation.m_AnimatedNodes[i] = NodeAnimation.Read(reader, nodes);
			}

			return animation;
		}
	}/*

	public enum AnimationInstanceState
	{
		Init = 0,
		Fading = 1,
		In = 2,
		Out = 3
	};

	public class AnimationInstance
	{
		// Position in timeline in seconds.
		private float m_Time;

		// User supplied mix value (modulated by fade).
		private float m_Mix;

		// Fade values
		float m_FadeMix;
		private float m_FadeMixFrom;
		private float m_FadeMixTo;
		private float m_FadeTime;
		private float m_FadeDuration;

		private ActorAnimation m_Animation;
		private bool m_Loop;
		private ActorInstance m_ActorInstance;
		private AnimationInstanceState m_State;
		private bool m_RemoveWhenComplete;


		public float Time
		{
			get
			{
				return m_Time;
			}
		}

		public float Mix
		{
			get
			{
				return m_Mix;
			}
			set
			{
				m_Mix = value;
			}
		}

		public bool RemoveWhenComplete
		{
			get
			{
				return m_RemoveWhenComplete;
			}
			set
			{
				m_RemoveWhenComplete = value;
			}
		}

		public AnimationInstance(ActorInstance actorInstance, ActorAnimation animation, bool loop)
		{
			m_RemoveWhenComplete = true;
			m_Animation = animation;
			m_Loop = loop;
			m_ActorInstance = actorInstance;
			m_Time = 0.0f;
			m_Mix = 1.0f;
			m_FadeMix = 1.0f;
			// Store the init state in case FadeIn gets called before the animation advances (we want to set fade mix to 0.0f in that case).
			m_State = AnimationInstanceState.Init;
		}

		public void FadeIn(float seconds)
		{
			if(m_State == AnimationInstanceState.Init)
			{
				m_FadeMix = 0.0f;
			}
			m_State = AnimationInstanceState.Fading;
			m_FadeTime = 0.0f;
			m_FadeDuration = seconds;

			m_FadeMixFrom = m_FadeMix;
			m_FadeMixTo = 1.0f;
		}

		public void FadeOut(float seconds)
		{
			m_State = AnimationInstanceState.Fading;
			m_FadeTime = 0.0f;
			m_FadeDuration = seconds;

			m_FadeMixFrom = m_FadeMix;
			m_FadeMixTo = 0.0f;
		}

		public void Set(float seconds)
		{
			m_Time = seconds;
			if(m_Time < 0)
			{
				if(m_Loop)
				{
					m_Time += m_Animation.Duration;	
				}
				else
				{
					m_Time = 0.0f;
				}
			}
			if(m_Loop && m_Time > m_Animation.Duration)
			{
				m_Time %= m_Animation.Duration;
			}
		}

		public bool Advance(float seconds)
		{
			switch(m_State)
			{
				case AnimationInstanceState.Init:
					m_State = AnimationInstanceState.In;
					break;
				case AnimationInstanceState.Fading:
					m_FadeTime += seconds;
					m_FadeMix = m_FadeMixFrom + (m_FadeMixTo - m_FadeMixFrom) * Math.Min(1.0f, m_FadeTime / m_FadeDuration);
					if(m_FadeTime >= m_FadeDuration)
					{
						if(m_FadeMix == 0.0f)
						{
							m_State = AnimationInstanceState.Out;
						}
						else
						{
							m_State = AnimationInstanceState.In;
						}
					}
					break;
			}
			Set(m_Time + seconds);
			float applyMix = m_FadeMix * m_Mix;
			if(applyMix > 0.0f)
			{
				m_Animation.Apply(m_Time, m_ActorInstance.AllNodes, applyMix);
			}

			if(!m_Loop && m_Time > m_Animation.Duration)
			{
				return true;
			}

			return m_RemoveWhenComplete && m_State == AnimationInstanceState.Out;
		}
	}*/
}