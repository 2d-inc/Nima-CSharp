using System;
using System.IO;
using System.Collections.Generic;

namespace Nima
{
    public class DependencySorter
    {
        private HashSet<ActorComponent> m_Perm;
		private HashSet<ActorComponent> m_Temp;
        private IList<ActorComponent> m_Order;

        public DependencySorter()
        {
            m_Perm = new HashSet<ActorComponent>();
		    m_Temp = new HashSet<ActorComponent>();
        }

        public IList<ActorComponent> Sort(ActorComponent root)
        {
            m_Order = new List<ActorComponent>();
            if(!Visit(root))
            {
                return null;
            }
            return m_Order;
        }

        private bool Visit(ActorComponent n)
        {
            if(m_Perm.Contains(n))
            {
                return true;
            }
            if(m_Temp.Contains(n))
            {
                Console.WriteLine("Dependency cycle!");
                return false;
            }

            m_Temp.Add(n);

            IList<ActorComponent> dependents = n.m_Dependents;
            if(dependents != null)
            {
                foreach(ActorComponent d in dependents)
                {
                    if(!Visit(d))
                    {
                        return false;
                    }
                }
            }
            m_Perm.Add(n);
            m_Order.Insert(0, n);

            return true;
        }
    }
}