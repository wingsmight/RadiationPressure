using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.RTSL
{
    public interface IRuntimeShaderUtil
    {
        RuntimeShaderInfo GetShaderInfo(Shader shader);
    }

    public class RuntimeShaderUtil : IRuntimeShaderUtil
    {
        private Dictionary<string, RuntimeShaderInfo> m_nameToShaderInfo;

        public RuntimeShaderUtil()
        {
            m_nameToShaderInfo = new Dictionary<string, RuntimeShaderInfo>();

            RuntimeShaderProfilesAsset asset = Resources.Load<RuntimeShaderProfilesAsset>("Lists/ShaderProfiles");
            if (asset == null)
            {
                Debug.LogWarning("Unable to find RuntimeShaderProfilesAsset. Click Tools->Runtime SaveLoad->Update Libraries");
                return;
            }
            for (int i = 0; i < asset.ShaderInfo.Count; ++i)
            {
                RuntimeShaderInfo info = asset.ShaderInfo[i];
                if (info != null)
                {
                    if (m_nameToShaderInfo.ContainsKey(info.Name))
                    {
                        Debug.LogWarning("Shader with " + info.Name + " already exists.");
                    }
                    else
                    {
                        m_nameToShaderInfo.Add(info.Name, info);
                    }
                }
            }
        }

        public RuntimeShaderInfo GetShaderInfo(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }

            RuntimeShaderInfo shaderInfo = null;
            if (m_nameToShaderInfo.TryGetValue(shader.name, out shaderInfo))
            {
                return shaderInfo;
            }
            return null;
        }
    }
}


