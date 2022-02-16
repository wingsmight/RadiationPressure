using System;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;

namespace Battlehub.ProBuilderIntegration
{
    public static class PBBuiltinMaterials
    {
        public const string pointShader = "Battlehub/RTBuilder/PointBillboard";
        public const string dotShader = "Battlehub/RTBuilder/VertexShader";
        public const string lineShader = "Battlehub/RTBuilder/LineBillboard";

        private static Material s_FacePickerMaterial;
        private static Material s_VertexPickerMaterial;
        private static Material s_EdgePickerMaterial;
        private static Shader s_SelectionPickerShader;

        private static Material m_defaultMaterial;

        public static Material DefaultMaterial
        {
            get
            {
                if(GraphicsSettings.renderPipelineAsset == null)
                {
                    return BuiltinMaterials.defaultMaterial;
                }
                else
                {
                    if(m_defaultMaterial == null)
                    {
                        Type pipelineType = GraphicsSettings.renderPipelineAsset.GetType();
                        if (pipelineType.Name == "UniversalRenderPipelineAsset")
                        {
                            m_defaultMaterial = Resources.Load<Material>("ProBuilder Default URP");
                        }
                        else if(pipelineType.Name == "HDRenderPipelineAsset" )
                        {
                            m_defaultMaterial = Resources.Load<Material>("ProBuilder Default HDRP");
                        }
                        else
                        {
                            m_defaultMaterial = BuiltinMaterials.defaultMaterial;
                        }
                    }

                    return m_defaultMaterial;
                }

                
            }
        }
 
        private static Material m_linesMaterial;
        public static Material LinesMaterial
        {
            get
            {
                if(m_linesMaterial == null)
                {
                    m_linesMaterial = new Material(Shader.Find("Battlehub/RTBuilder/LineBillboard"));
                }
                return m_linesMaterial;
            }
        }

        private static bool s_GeometryShadersSupported;
        public static bool geometryShadersSupported
        {
            get
            {
                Init();
                return s_GeometryShadersSupported;
            }
        }

        internal static Shader selectionPickerShader
        {
            get
            {
                Init();
                return s_SelectionPickerShader;
            }
        }

        internal static Material facePickerMaterial
        {
            get
            {
                Init();
                return s_FacePickerMaterial;
            }
        }

        internal static Material vertexPickerMaterial
        {
            get
            {
                Init();
                return s_VertexPickerMaterial;
            }
        }
        
        internal static Material edgePickerMaterial
        {
            get
            {
                Init();
                return s_EdgePickerMaterial;
            }
        }

        private static bool s_IsInitialized;
        static void Init()
        {
            if (s_IsInitialized)
                return;

            s_IsInitialized = true;

            var geo = Shader.Find(lineShader);
            s_GeometryShadersSupported = geo != null && geo.isSupported;
            //Debug.Log("Geometry Shaders Support: " + s_GeometryShadersSupported);

            s_SelectionPickerShader = (Shader)Shader.Find("Battlehub/RTBuilder/SelectionPicker");

            if ((s_FacePickerMaterial = Resources.Load<Material>("Materials/PBFacePicker")) == null)
            {
                s_FacePickerMaterial = new Material(Shader.Find("Battlehub/RTBuilder/FacePicker"));
            }

            if ((s_VertexPickerMaterial = Resources.Load<Material>("Materials/PBVertexPicker")) == null)
            {
                s_VertexPickerMaterial = new Material(Shader.Find("Battlehub/RTBuilder/VertexPicker"));
            }

            if ((s_EdgePickerMaterial = Resources.Load<Material>("Materials/PBEdgePicker")) == null)
            {                
                s_EdgePickerMaterial = new Material(Shader.Find("Battlehub/RTBuilder/EdgePicker"));
            }
        }
    }
}
