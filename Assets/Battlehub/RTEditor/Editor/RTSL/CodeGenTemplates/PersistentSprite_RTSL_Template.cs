//#define RTSL_COMPILE_TEMPLATES
#if RTSL_COMPILE_TEMPLATES
//<TEMPLATE_USINGS_START>
using Battlehub.RTSL;
using ProtoBuf;
using System;
using Battlehub.Utils;
using UnityEngine;
using UnityEngine.Battlehub.SL2;
//<TEMPLATE_USINGS_END>
#else
using UnityEngine;
#endif

namespace Battlehub.RTSL.Internal
{
   // [PersistentTemplate("UnityEngine.Sprite", new string[0], new[] { "UnityEngine.Vector2", "UnityEngine.Texture2D" })]
    public class PersistentSprite_RTSL_Template : PersistentSurrogateTemplate
    {
#if RTSL_COMPILE_TEMPLATES
        //<TEMPLATE_BODY_START>

        [ProtoMember(1)]
        public PersistentTexture2D<TID> m_texture;

        [ProtoMember(2)]
        public Vector2 m_position;

        [ProtoMember(3)]
        public Vector2 m_size;

        [ProtoMember(4)]
        public Vector2 m_pivot;


        public override void ReadFrom(object obj)
        {
            base.ReadFrom(obj);

            Sprite o = (Sprite)obj;
            if (o == null)
            {
                return;
            }
            m_position = o.rect.position;
            m_size = o.rect.size;
            m_pivot = o.pivot;

            if (o.texture != null)
            {
                m_texture = new PersistentTexture2D<TID>();
                m_texture.ReadFrom(o.texture);
            }
        }


        public override void GetDeps(GetDepsContext<TID> context)
        {
            base.GetDeps(context);

            if (m_texture != null)
            {
                m_texture.GetDeps(context);
            }
        }

        public override void GetDepsFrom(object obj, GetDepsFromContext context)
        {
            base.GetDepsFrom(obj, context);

            Sprite o = (Sprite)obj;
            if (o != null && o.texture != null)
            {
                PersistentTexture2D<TID> persistentTexture2D = new PersistentTexture2D<TID>();
                persistentTexture2D.GetDepsFrom(o.texture, context);
            }
        }

        public override bool CanInstantiate(Type type)
        {
            return true;
        }

        public override object Instantiate(Type type)
        {
            Texture2D texture;
            if (m_texture != null)
            {
                texture = (Texture2D)m_texture.Instantiate(typeof(Texture2D));
                m_texture.WriteTo(texture);
            }
            else
            {
                texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            }
            return Sprite.Create(texture, new Rect(m_position, m_size), new Vector2(0.5f, 0.5f));
        }
        //<TEMPLATE_BODY_END>
#endif
    }
}


