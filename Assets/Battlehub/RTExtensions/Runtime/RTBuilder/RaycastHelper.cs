using Battlehub.MeshTools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.RTBuilder
{
    public static class RaycastHelper 
    {
        public static GameObject Raycast(Ray pointer)
        {
            RaycastHit hit;
            if (Physics.Raycast(pointer, out hit))
            {
                if (hit.collider.GetComponentsInChildren<Renderer>().Length > 0)
                {
                    return hit.collider.gameObject;
                }
            }
            return null;
        }

        public static int Raycast(Ray pointer, out Renderer result)
        {
            result = null;
            RaycastHit hit;
            if (!Physics.Raycast(pointer, out hit))
            {
                return -1;
            }

            int triangleIndex = -1;
            Mesh mesh = null;
            GameObject[] gameObjects = hit.collider.GetComponentsInChildren<Transform>().Select(t => t.gameObject).ToArray();
            foreach (GameObject obj in gameObjects)
            {
                MeshFilter f = obj.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null || f.sharedMesh.GetTopology(0) != MeshTopology.Triangles)
                {
                    continue;
                }

                int[] tris = f.sharedMesh.triangles;
                int range = tris.Length / 3;
                if (triangleIndex < hit.triangleIndex && hit.triangleIndex <= triangleIndex + range)
                {
                    mesh = f.sharedMesh;
                    result = f.GetComponent<Renderer>();
                    if (result != null)
                    {
                        break;
                    }
                }
                triangleIndex += range;
            }

            if (result == null)
            {
                return -1;
            }

            triangleIndex++;
            triangleIndex = hit.triangleIndex - triangleIndex;

            int lookupIdx0 = mesh.triangles[triangleIndex * 3];
            int lookupIdx1 = mesh.triangles[triangleIndex * 3 + 1];
            int lookupIdx2 = mesh.triangles[triangleIndex * 3 + 2];

            int subMeshCount = mesh.subMeshCount;
            for (int materialIdx = 0; materialIdx < subMeshCount; ++materialIdx)
            {
                int[] tris = mesh.GetTriangles(materialIdx);
                for (var t = 0; t < tris.Length; t += 3)
                {
                    if (tris[t] == lookupIdx0 && tris[t + 1] == lookupIdx1 && tris[t + 2] == lookupIdx2)
                    {
                        return materialIdx;
                    }
                }
            }

            Debug.Log("Triangle was not found");
            return -1;
        }

        public static Mesh CreateColliderMesh(params GameObject[] gameObjects)
        {
            if (gameObjects == null)
            {
                throw new System.ArgumentNullException("gameObjects");
            }

            if (gameObjects.Length == 0)
            {
                return null;
            }

            GameObject target = gameObjects[0];

            //save parents and unparent selected objects
            Transform[] selectionParents = new Transform[gameObjects.Length];
            for (int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject obj = gameObjects[i];
                if ((obj.hideFlags & HideFlags.HideInHierarchy) != 0)
                {
                    continue;
                }
                selectionParents[i] = obj.transform.parent;
                obj.transform.SetParent(null, true);
            }

            Matrix4x4 targetRotationMatrix = Matrix4x4.TRS(Vector3.zero, target.transform.rotation, Vector3.one);// target.transform.localScale);
            Matrix4x4 targetToLocal = target.transform.worldToLocalMatrix;

            List<CombineInstance> colliderCombine = new List<CombineInstance>();
            List<Mesh> meshes = new List<Mesh>();
            foreach (GameObject obj in gameObjects)
            {
                if((obj.hideFlags & HideFlags.HideInHierarchy) != 0)
                {
                    continue;
                }

                MeshFilter f = obj.GetComponent<MeshFilter>();
                if (f != null && f.sharedMesh != null)
                {
                    meshes.AddRange(MeshUtils.Separate(f.sharedMesh));
                }
                for (int i = 0; i < meshes.Count; i++)
                {
                    Mesh mesh = meshes[i];
                    CombineInstance colliderCombineInstance = new CombineInstance();
                    colliderCombineInstance.mesh = mesh;
                    //convert to active selected object's local coordinate system
                    colliderCombineInstance.transform = targetToLocal * obj.transform.localToWorldMatrix;
                    colliderCombine.Add(colliderCombineInstance);
                }
                meshes.Clear();
            }

            Mesh finalColliderMesh = null;
            if (colliderCombine.Count != 0)
            {
                Mesh colliderMesh = new Mesh();
                colliderMesh.CombineMeshes(colliderCombine.ToArray());

                CombineInstance[] removeColliderRotation = new CombineInstance[1];
                removeColliderRotation[0].mesh = colliderMesh;
                removeColliderRotation[0].transform = Matrix4x4.identity;// targetRotationMatrix;

                finalColliderMesh = new Mesh();
                finalColliderMesh.name = target.name + "Collider";
                finalColliderMesh.CombineMeshes(removeColliderRotation);
            }

            for (int i = 0; i < gameObjects.Length; ++i)
            {
                GameObject obj = gameObjects[i];
                if ((obj.hideFlags & HideFlags.HideInHierarchy) != 0)
                {
                    continue;
                }
                obj.transform.SetParent(selectionParents[i], true);
            }

            return finalColliderMesh;
        }
    }

}

