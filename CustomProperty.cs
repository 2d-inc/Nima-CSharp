using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class CustomIntProperty : ActorComponent
	{
		protected int m_Value;

		public int Value
		{
			get
			{
				return m_Value;
			}
			set
			{
				m_Value = value;
			}
		}

		public static CustomIntProperty Read(Actor actor, BinaryReader reader, CustomIntProperty property = null)
		{
			if(property == null)
			{
				property = new CustomIntProperty();
			}
			ActorComponent.Read(actor, reader, property);
			property.Value = reader.ReadInt32();
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			CustomIntProperty instanceEvent = new CustomIntProperty();
			instanceEvent.Copy(this, resetActor);
			return instanceEvent;
		}

		public void Copy(CustomIntProperty prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Value = prop.m_Value;
		}

		public override void ResolveComponentIndices(ActorComponent[] components)
		{
			ActorComponent component = components[ParentIdx];
			if(component != null)
			{
				component.AddCustomIntProperty(this);
			}
		}
	}

	public class CustomFloatProperty : ActorComponent
	{
		float m_Value;

		public float Value
		{
			get
			{
				return m_Value;
			}
			set
			{
				m_Value = value;
			}
		}

		public static CustomFloatProperty Read(Actor actor, BinaryReader reader, CustomFloatProperty property = null)
		{
			if(property == null)
			{
				property = new CustomFloatProperty();
			}
			ActorComponent.Read(actor, reader, property);
			property.Value = reader.ReadSingle();
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			CustomFloatProperty instanceEvent = new CustomFloatProperty();
			instanceEvent.Copy(this, resetActor);
			return instanceEvent;
		}

		public void Copy(CustomFloatProperty prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Value = prop.m_Value;
		}

		public override void ResolveComponentIndices(ActorComponent[] components)
		{
			ActorComponent component = components[ParentIdx];
			if(component != null)
			{
				component.AddCustomFloatProperty(this);
			}
		}
	}

	public class CustomStringProperty : ActorComponent
	{
		string m_Value;

		public string Value
		{
			get
			{
				return m_Value;
			}
			set
			{
				m_Value = value;
			}
		}

		public static CustomStringProperty Read(Actor actor, BinaryReader reader, CustomStringProperty property = null)
		{
			if(property == null)
			{
				property = new CustomStringProperty();
			}
			ActorComponent.Read(actor, reader, property);
			property.Value = Actor.ReadString(reader);
			return property;
		}

		public override ActorComponent MakeInstance(Actor resetActor)
		{
			CustomStringProperty instanceEvent = new CustomStringProperty();
			instanceEvent.Copy(this, resetActor);
			return instanceEvent;
		}

		public void Copy(CustomStringProperty prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_Value = prop.m_Value;
		}

		public override void ResolveComponentIndices(ActorComponent[] components)
		{
			ActorComponent component = components[ParentIdx];
			if(component != null)
			{
				component.AddCustomStringProperty(this);
			}
		}
	}
}