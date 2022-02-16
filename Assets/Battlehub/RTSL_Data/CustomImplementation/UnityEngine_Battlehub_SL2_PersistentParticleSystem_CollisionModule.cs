#if !RTSL_MAINTENANCE
using Battlehub.RTSL;
using ProtoBuf;
using UnityEngine;

namespace UnityEngine.Battlehub.SL2
{
    [CustomImplementation]
    public partial class PersistentParticleSystemNestedCollisionModule<TID> 
    {        

        [ProtoMember(1)]
        public TID[] m_planes;

        public override object WriteTo(object obj)
        {
            obj = base.WriteTo(obj);
            if (obj == null)
            {
                return null;
            }

            ParticleSystem.CollisionModule o = (ParticleSystem.CollisionModule)obj;
            if (m_planes == null)
            {
                for (int i = 0; i < o.planeCount; ++i)
                {
                    o.SetPlane(i, null);
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Min(o.planeCount, m_planes.Length); ++i)
                {
                    o.SetPlane(i, FromID<Transform>(m_planes[i]));
                }
            }

            return obj;
        }

        public override void ReadFrom(object obj)
        {
            base.ReadFrom(obj);
            if (obj == null)
            {
                return;
            }

            ParticleSystem.CollisionModule o = (ParticleSystem.CollisionModule)obj;
            m_planes = new TID[o.planeCount];
            for (int i = 0; i < o.planeCount; ++i)
            {
                m_planes[i] = ToID(o.GetPlane(i));
            }
        }

        public override void GetDeps(GetDepsContext<TID> context)
        {
            base.GetDeps(context);
            AddDep(m_planes, context);
        }

        public override void GetDepsFrom(object obj, GetDepsFromContext context)
        {
            base.GetDepsFrom(obj, context);
            if (obj == null)
            {
                return;
            }

            ParticleSystem.CollisionModule o = (ParticleSystem.CollisionModule)obj;
            for(int i = 0; i < o.planeCount; ++i)
            {
                AddDep(o.GetPlane(i), context);
            }
        }
    }
}
#endif

