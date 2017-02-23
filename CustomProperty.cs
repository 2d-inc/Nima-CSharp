using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public enum CustomPropertyType
	{
		Integer = 0,
		Float = 1,
		String = 2
	};

	public abstract class CustomProperty : ActorComponent
	{
		protected CustomPropertyType m_PropertyType;

		public CustomPropertyType PropertyType
		{
			get
			{
				return m_PropertyType;
			}
		}

		public void Copy(CustomProperty prop, Actor resetActor)
		{
			base.Copy(prop, resetActor);
			m_PropertyType = prop.m_PropertyType;
		}

		public override void ResolveComponentIndices(ActorComponent[] components)
		{
			base.ResolveComponentIndices(components);
			ActorComponent component = components[ParentIdx];
			if(component != null)
			{
				component.AddCustomProperty(this);
			}
		}

		public float FloatValue
		{
			get
			{
				CustomFloatProperty f = this as CustomFloatProperty;
				if(f != null)
				{
					return f.Value;
				}
				return 0.0f;
			}
		}
		public string StringValue
		{
			get
			{
				CustomStringProperty f = this as CustomStringProperty;
				if(f != null)
				{
					return f.Value;
				}
				return "";
			}
		}
		public int IntValue
		{
			get
			{
				CustomIntProperty f = this as CustomIntProperty;
				if(f != null)
				{
					return f.Value;
				}
				return 0;
			}
		}
	}

	public class CustomIntProperty : CustomProperty
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
			property.m_PropertyType = CustomPropertyType.Integer;
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
	}

	public class CustomFloatProperty : CustomProperty
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
			property.m_PropertyType = CustomPropertyType.Float;
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
	}

	public class CustomStringProperty : CustomProperty
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
			property.m_PropertyType = CustomPropertyType.String;
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
	}
}