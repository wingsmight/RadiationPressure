using Battlehub.RTCommon;
using Battlehub.RTSL.Interface;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class JpgImporterExample : FileImporter
    {
        public override string FileExt
        {
            get { return ".jpg"; }
        }

        public override string IconPath
        {
            get { return "Importers/Jpg"; }
        }

        public override IEnumerator Import(string filePath, string targetPath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            Texture2D texture = new Texture2D(4, 4);
            if (texture.LoadImage(bytes, false))
            {
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
