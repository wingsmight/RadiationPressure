using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class PngImporter : FileImporter
    {
        public override string FileExt
        {
            get { return ".png"; }
        }

        public override string IconPath
        {
            get { return "Importers/Png"; }
        }

        public override int Priority
        {
            get { return int.MinValue; }
        }

        public override IEnumerator Import(string filePath, string targetPath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            Texture2D texture = new Texture2D(4, 4);
            if(texture.LoadImage(bytes, false))
            {
                if (texture.format == TextureFormat.RGBA32 || texture.format == TextureFormat.ARGB32)
                {
                    bool opaque = true;
                    Color32[] pixels = texture.GetPixels32();
                    for (int i = 0; i < pixels.Length; ++i)
                    {
                        if (pixels[i].a != 255)
                        {
                            opaque = false;
                            break;
                        }
                    }

                    if (opaque)
                    {
                        texture.LoadImage(texture.EncodeToJPG(), false);
                    }
                }

                IProject project = IOC.Resolve<IProject>();
                IResourcePreviewUtility previewUtility = IOC.Resolve<IResourcePreviewUtility>();
                byte[] preview = previewUtility.CreatePreviewData(texture); 
                yield return project.Save(targetPath, texture, preview);
            }
            else
            {
                Debug.LogError("Unable to load image " + filePath);
            }
            
            Object.Destroy(texture);
        }
    }
}
