﻿using Battlehub.RTCommon;

namespace Battlehub.RTEditor
{
    public class SceneView : RuntimeCameraWindow
    {
        protected override void AwakeOverride()
        {   
            ActivateOnAnyKey = true;
            WindowType = RuntimeWindowType.Scene;
            base.AwakeOverride();   

            if(RenderPipelineInfo.Type != RPType.Standard)
            {
                RTEGraphicsLayer graphicsLayer = GetComponent<RTEGraphicsLayer>();
                DestroyImmediate(graphicsLayer);
            }
        }

        /*
        protected override void OnDeactivated()
        {
            base.OnDeactivated();
            Background.enabled = true;
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            Background.enabled = false;
        }
        */

        protected virtual void Start()
        {
            if (!GetComponent<SceneViewInput>())
            {
                gameObject.AddComponent<SceneViewInput>();
            }

            if (!GetComponent<SceneViewImpl>())
            {
                gameObject.AddComponent<SceneViewImpl>();
            }
        }

        /*
        protected override void UpdateOverride()
        {
            base.UpdateOverride();

            //This is to allow worldspace UI
            IsPointerOver =  RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Editor.Input.GetPointerXY(0), Canvas.worldCamera);
        }*/
    }
}
