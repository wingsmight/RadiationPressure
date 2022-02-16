using Battlehub.RTSL.Interface;
using System;
using UnityEngine;

namespace Battlehub.RTSL
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(RTSLIgnore))]
    public class RTSLDepsGuids : RTSLDeps<Guid>
    {
        private IAssetDB<Guid> m_assetDB;
        protected override IAssetDB<Guid> AssetDB
        {
            get
            {
                if (m_assetDB == null)
                {
                    m_assetDB = new AssetDB<Guid>(obj => Guid.NewGuid());
                }
                return m_assetDB;
            }
        }


        private ProjectGuids m_project;
        protected override IProject Project
        {
            get
            {
                if (m_project == null)
                {
                    m_project = FindObjectOfType<ProjectGuids>();
                    if (m_project == null)
                    {
                        m_project = gameObject.AddComponent<ProjectGuids>();
                    }
                }
                return m_project;
            }
        }
    }

}
