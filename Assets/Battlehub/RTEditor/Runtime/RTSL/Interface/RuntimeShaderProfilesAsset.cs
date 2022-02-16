﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.RTSL
{
    public enum RTShaderPropertyType
    {
        //     Color Property.
        Color = 0,
        //     Vector Property.
        Vector = 1,
        //     Float Property.
        Float = 2,
        //     Range Property.
        Range = 3,
        //     Texture Property.
        TexEnv = 4,

        Unknown,

        Procedural
    }

    [System.Serializable]
    public class RuntimeShaderInfo
    {
        public string Name;

        [System.Serializable]
        public struct RangeLimits
        {
            public float Def;
            public float Min;
            public float Max;

            public RangeLimits(float def, float min, float max)
            {
                Def = def;
                Min = min;
                Max = max;
            }
        }

        public int PropertyCount;
        public string[] PropertyDescriptions;
        public string[] PropertyNames;
        public RTShaderPropertyType[] PropertyTypes;
        public RangeLimits[] PropertyRangeLimits;
        public TextureDimension[] PropertyTexDims;
        public bool[] IsHidden;
    }


    public class RuntimeShaderProfilesAsset : ScriptableObject
    {
        public List<RuntimeShaderInfo> ShaderInfo;   
    }
}
