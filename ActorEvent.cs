using System;
using System.IO;
using System.Collections.Generic;
using Nima.Math2D;

namespace Nima
{
	public class ActorEvent : ActorComponent
	{
		public static ActorComponent Read(Actor actor, BinaryReader reader, ActorEvent component = null)
		{
			if(component == null)
			{
				component = new ActorEvent();
			}

			ActorComponent.Read(actor, reader, component);

			return component;
		}
	
		public override ActorComponent MakeInstance(Actor resetActor)
		{
			ActorEvent instanceEvent = new ActorEvent();
			instanceEvent.Copy(this, resetActor);
			return instanceEvent;
		}
	}
}