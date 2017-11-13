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

		public IList<KeyFrame> KeyFrames
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
					case PropertyTypes.Trigger:
						keyFrameReader = KeyFrameTrigger.Read;
						break;
					case PropertyTypes.IntProperty:
						keyFrameReader = KeyFrameIntProperty.Read;
						break;
					case PropertyTypes.FloatProperty:
						keyFrameReader = KeyFrameFloatProperty.Read;
						break;
					case PropertyTypes.StringProperty:
						keyFrameReader = KeyFrameStringProperty.Read;
						break;
					case PropertyTypes.BooleanProperty:
						keyFrameReader = KeyFrameBooleanProperty.Read;
						break;
					case PropertyTypes.CollisionEnabled:
						keyFrameReader = KeyFrameCollisionEnabledProperty.Read;
						break;
					case PropertyTypes.ActiveChildIndex:
						keyFrameReader = KeyFrameActiveChild.Read;
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

		public int ComponentIndex
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
				if(propertyAnimation != null)
				{
					propertyAnimation.Apply(time, components[m_ComponentIndex], mix);
				}
			}
		}
	}

	public class AnimationEventArgs : EventArgs
	{
		private string m_Name;
		private ActorComponent m_Component;
		private PropertyTypes m_PropertyType;
		private float m_KeyFrameTime;
		private float m_ElapsedTime;

		public AnimationEventArgs(string name, ActorComponent component, PropertyTypes type, float keyframeTime, float elapsedTime)
		{
			m_Name = name;
			m_Component = component;
			m_PropertyType = type;
			m_KeyFrameTime = keyframeTime;
			m_ElapsedTime = elapsedTime;
		}		

		public string Name
		{
			get 
			{
				return m_Name;
			}
		}

		public ActorComponent Component
		{
			get 
			{
				return m_Component;
			}
		}

		public PropertyTypes PropertyType
		{
			get 
			{
				return m_PropertyType;
			}
		}

		public float KeyFrameTime
		{
			get 
			{
				return m_KeyFrameTime;
			}
		}

		public float ElapsedTime
		{
			get 
			{
				return m_ElapsedTime;
			}
		}
	}

	public class ActorAnimation
	{
		private string m_Name;
		private int m_FPS;
		private float m_Duration;
		private bool m_IsLooping;
		private ComponentAnimation[] m_Components;
		private ComponentAnimation[] m_TriggerComponents;

		public string Name
		{
			get
			{
				return m_Name;
			}
		}

		public bool IsLooping
		{
			get
			{
				return m_IsLooping;
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
				return m_Components;
			}
		}

		//Animation.prototype.triggerEvents = function(actorComponents, fromTime, toTime, triggered)
		/*
									name:component._Name,
									component:component,
									propertyType:property._Type,
									keyFrameTime:toTime,
									elapsed:0*/
		public void TriggerEvents(ActorComponent[] components, float fromTime, float toTime, IList<AnimationEventArgs> triggerEvents)
		{
			for(int i = 0; i < m_TriggerComponents.Length; i++)
			{
				ComponentAnimation keyedComponent = m_TriggerComponents[i];
				foreach(PropertyAnimation property in keyedComponent.Properties)
				{
					switch(property.PropertyType)
					{
						case PropertyTypes.Trigger:
							IList<KeyFrame> keyFrames = property.KeyFrames;

							int kfl = keyFrames.Count;
							if(kfl == 0)
							{
								continue;
							}

							int idx = 0;
							// Binary find the keyframe index.
							{
								int mid = 0;
								float element = 0.0f;
								int start = 0;
								int end = kfl-1;

								while (start <= end) 
								{
							    	mid = ((start + end) >> 1);
									element = keyFrames[mid].Time;
									if (element < toTime) 
									{
										start = mid + 1;
									} 
									else if (element > toTime) 
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

							//int idx = keyFrameLocation(toTime, keyFrames, 0, keyFrames.length-1);
							if(idx == 0)
							{
								if(kfl > 0 && keyFrames[0].Time == toTime)
								{
									ActorComponent component = components[keyedComponent.ComponentIndex];
									triggerEvents.Add(new AnimationEventArgs(component.Name, component, property.PropertyType, toTime, 0.0f));
								}
							}
							else
							{
								for(int k = idx-1; k >= 0; k--)
								{
									KeyFrame frame = keyFrames[k];	

									if(frame.Time > fromTime)
									{
										ActorComponent component = components[keyedComponent.ComponentIndex];
										triggerEvents.Add(new AnimationEventArgs(component.Name, component, property.PropertyType, frame.Time, toTime-frame.Time));
										/*triggered.push({
											name:component._Name,
											component:component,
											propertyType:property._Type,
											keyFrameTime:frame._Time,
											elapsed:toTime-frame._Time
										});*/
									}
									else
									{
										break;
									}
								}
							}
							break;
						default:
							break;
					}
				}
			}
		}

		public void Apply(float time, Actor actor, float mix)
		{
			foreach(ComponentAnimation componentAnimation in m_Components)
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
			//animation.m_Components = new ComponentAnimation[numKeyedComponents];

			// We distinguish between animated and triggered components as ActorEvents are currently only used to trigger events and don't need
			// the full animation cycle. This lets them optimize them out of the regular animation cycle.
			int animatedComponentCount = 0;
			int triggerComponentCount = 0;

			ComponentAnimation[] animatedComponents = new ComponentAnimation[numKeyedComponents];
			for(int i = 0; i < numKeyedComponents; i++)
			{
				ComponentAnimation componentAnimation = ComponentAnimation.Read(reader, components);
				animatedComponents[i] = componentAnimation;
				if(componentAnimation != null)
				{
					ActorComponent actorComponent = components[componentAnimation.ComponentIndex];
					if(actorComponent != null)
					{
						if(actorComponent is ActorEvent)
						{
							triggerComponentCount++;
						}
						else
						{
							animatedComponentCount++;
						}
					}
				}
			}

			animation.m_Components = new ComponentAnimation[animatedComponentCount];
			animation.m_TriggerComponents = new ComponentAnimation[triggerComponentCount];

			// Put them in their respective lists.
			int animatedComponentIndex = 0;
			int triggerComponentIndex = 0;
			for(int i = 0; i < numKeyedComponents; i++)
			{
				ComponentAnimation componentAnimation = animatedComponents[i];
				if(componentAnimation != null)
				{
					ActorComponent actorComponent = components[componentAnimation.ComponentIndex];
					if(actorComponent != null)
					{
						if(actorComponent is ActorEvent)
						{
							animation.m_TriggerComponents[triggerComponentIndex++] = componentAnimation;
						}
						else
						{
							animation.m_Components[animatedComponentIndex++] = componentAnimation;
						}
					}
				}
			}

			return animation;
		}
	}

	public class ActorAnimationInstance
	{
		private Actor m_Actor;
		private ActorAnimation m_Animation;
		private float m_Time;
		private float m_Min;
		private float m_Max;
		private float m_Range;
		private bool m_Loop;

        public event EventHandler<AnimationEventArgs> AnimationEvent;

		public ActorAnimationInstance(Actor actor, ActorAnimation animation)
		{
			m_Actor = actor;
			m_Animation = animation;
			m_Time = 0.0f;
			m_Min = 0.0f;
			m_Max = animation.Duration;
			m_Range = m_Max - m_Min;
			m_Loop = animation.IsLooping;
		}	

		public bool Loop
		{
			get
			{
				return m_Loop;
			}
			set
			{
				m_Loop = value;
			}	
		}

		public float MinTime
		{
			get
			{
				return m_Min;
			}
		}

		public float MaxTime
		{
			get
			{
				return m_Max;
			}
		}
		
		public float Time
		{
			get
			{
				return m_Time;
			}
			set
			{
				float delta = value - m_Time;
				float time = m_Time + (delta % m_Range);

				if(time < m_Min)
				{
					if(m_Loop)
					{
						time = m_Max - (m_Min - time);	
					}
					else
					{
						time = m_Min;
					}
				}
				else if(time > m_Max)
				{
					if(m_Loop)
					{
						time = m_Min + (time - m_Max);
					}
					else
					{
						time = m_Max;
					}
				}
				m_Time = time;
			}	
		}
		public void Advance(float seconds)
		{
			IList<AnimationEventArgs> triggeredEvents = new List<AnimationEventArgs>();
			float time = m_Time;
			time += seconds % m_Range;
			if(time < m_Min)
			{
				if(m_Loop)
				{
					m_Animation.TriggerEvents(m_Actor.AllComponents, time, m_Time, triggeredEvents);
					time = m_Max - (m_Min - time);
					m_Animation.TriggerEvents(m_Actor.AllComponents, time, m_Max, triggeredEvents);
				}
				else
				{
					time = m_Min;
					if(m_Time != time)
					{
						m_Animation.TriggerEvents(m_Actor.AllComponents, m_Min, m_Time, triggeredEvents);
					}
				}
			}
			else if(time > m_Max)
			{
				if(m_Loop)
				{
					m_Animation.TriggerEvents(m_Actor.AllComponents, time, m_Time, triggeredEvents);
					time = m_Min + (time - m_Max);
					m_Animation.TriggerEvents(m_Actor.AllComponents, m_Min-0.001f, time, triggeredEvents);
				}
				else
				{
					time = m_Max;
					if(m_Time != time)
					{
						m_Animation.TriggerEvents(m_Actor.AllComponents, m_Time, m_Max, triggeredEvents);
					}
				}
			}
			else if(time > m_Time)
			{
				m_Animation.TriggerEvents(m_Actor.AllComponents, m_Time, time, triggeredEvents);
			}
			else
			{
				m_Animation.TriggerEvents(m_Actor.AllComponents, time, m_Time, triggeredEvents);
			}

			foreach(AnimationEventArgs ev in triggeredEvents)
			{
	            if (AnimationEvent != null)
	            {
	                AnimationEvent(this, ev);
	            }
	            m_Actor.OnAnimationEvent(ev);
			}
			/*for(var i = 0; i < triggeredEvents.length; i++)
			{
				var event = triggeredEvents[i];
				this.dispatch("animationEvent", event);
				m_Actor.dispatch("animationEvent", event);
			}*/
			m_Time = time;
		}

		public void Apply(float mix)
		{
			m_Animation.Apply(m_Time, m_Actor, mix);
		}
	}
}