//#define RTSL_COMPILE_TEMPLATES
#if RTSL_COMPILE_TEMPLATES
//<TEMPLATE_USINGS_START>
using ProtoBuf;
using UnityEngine;
//<TEMPLATE_USINGS_END>
#else
using UnityEngine;
#endif

namespace Battlehub.RTSL.Internal
{
    [PersistentTemplate("UnityEngine.ParticleSystem+CollisionModule")]
    public partial class PersistentParticleSystemNestedCollisionModule_RTSL_Template : PersistentSurrogateTemplate
    {
#if RTSL_COMPILE_TEMPLATES
        //<TEMPLATE_BODY_START>

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
                for (int i = 0; i < o.maxPlaneCount; ++i)
                {
                    o.SetPlane(i, null);
                }
            }
            else
            {
                for (int i = 0; i < Mathf.Min(o.maxPlaneCount, m_planes.Length); ++i)
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
            m_planes = new TID[o.maxPlaneCount];
            for (int i = 0; i < o.maxPlaneCount; ++i)
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
            for(int i = 0; i < o.maxPlaneCount; ++i)
            {
                AddDep(o.GetPlane(i), context);
            }
        }

        //<TEMPLATE_BODY_END>
#endif
    }
}
