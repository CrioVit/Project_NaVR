﻿// Perfect Culling (C) 2021 Patrick König
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Koenigz.PerfectCulling
{
    [RequireComponent(typeof(UnityEngine.Terrain))]
    public class TerrainToMeshUtility : MonoBehaviour
    {
        private readonly string EditorOnlyTag = "EditorOnly";

        [Header("Creates double-sided mesh to make it not see-through from the other side.")]
        public bool DoubleSided = true;

        [Range(1, 2048)]
        public int MeshResolutionX = 512;
        
        [Range(1, 2048)]
        public int MeshResolutionZ = 512;

        public MeshRenderer meshRendererReference;
       
        public void CreateOrUpdateMesh()
        {
            UnityEngine.Terrain terrain = GetComponent<UnityEngine.Terrain>();

            Mesh mesh = CreateMesh(terrain, MeshResolutionX, MeshResolutionZ, DoubleSided);

            string terrainName = $"Mesh for {terrain.name} [EditorOnly]";

            if (meshRendererReference == null)
            {
                GameObject newGo = new GameObject(terrainName);
                newGo.tag = EditorOnlyTag;

                newGo.AddComponent<MeshFilter>().sharedMesh = mesh;
            
                meshRendererReference = newGo.AddComponent<MeshRenderer>();
                meshRendererReference.sharedMaterials = new Material[mesh.subMeshCount];
                meshRendererReference.enabled = false;
            }
            else
            {
                meshRendererReference.sharedMaterials = new Material[mesh.subMeshCount];
                
                MeshFilter mf = meshRendererReference.GetComponent<MeshFilter>();

                if (mf.sharedMesh != null)
                {
                    GameObject.DestroyImmediate(mf.sharedMesh);
                }
                
                mf.sharedMesh = mesh;
            }
            
            meshRendererReference.transform.SetPositionAndRotation(terrain.transform.position, Quaternion.identity);
        }

        public static Mesh CreateMesh(UnityEngine.Terrain terrain, int meshResolutionX, int meshResolutionZ, bool doubleSided)
        {
            string terrainName = $"Mesh for {terrain.name} [EditorOnly]";

            TerrainData terrainData = terrain.terrainData;

            if (terrainData == null)
            {
                PerfectCullingLogger.LogError("Terrain data is null.");
                
                return null;
            }

            float xSpacing = terrainData.size.x / meshResolutionX;
            float ySpacing = terrainData.size.z / meshResolutionZ;

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.name = terrainName;
            
            // terrainData.heightmapResolution actually returns 513 instead of 512
            bool[,] rawHoles = terrain.terrainData.GetHoles(0, 0, terrainData.heightmapResolution - 1, terrainData.heightmapResolution - 1);

            bool IsHole(int x, int y)
            {
                float fx = Mathf.Lerp(0, terrain.terrainData.heightmapResolution - 2, x / (float)meshResolutionX);
                float fy = Mathf.Lerp(0, terrain.terrainData.heightmapResolution - 2, y / (float)meshResolutionZ);
                    
                int nxFloored = Mathf.FloorToInt(fx);
                int nyFloored = Mathf.FloorToInt(fy);
                
                int nxCeiled = Mathf.CeilToInt(fx);
                int nyCeiled = Mathf.CeilToInt(fy);

                bool holeFloored = !rawHoles[nyFloored, nxFloored];
                bool holeCeiled = !rawHoles[nyCeiled, nxCeiled];

                return holeFloored || holeCeiled;
            }
            
            List<Vector3> vertices = new List<Vector3>((meshResolutionX + 1) * (meshResolutionZ + 1));
            for (int i = 0, y = 0; y <= meshResolutionZ; y++)
            {
                for (int x = 0; x <= meshResolutionX; x++, i++)
                {
                    float h = terrain.SampleHeight(new Vector3(x * xSpacing, 0f, y * ySpacing) + terrain.transform.position);
                        
                    vertices.Add(new Vector3(x * xSpacing, h, y * ySpacing));
                }
            }

            mesh.vertices = vertices.ToArray();

            int[] triangles = null;

            if (doubleSided)
            {
                triangles = new int[meshResolutionX * meshResolutionZ * 6 * 2];
                for (int ti = 0, vi = 0, y = 0; y < meshResolutionZ; y++, vi++)
                {
                    for (int x = 0; x < meshResolutionX; x++, ti += 6, vi++)
                    {
                        if (IsHole(x, y))
                        {
                            // Creates degenerate triangle so the mesh optimizer can detect and strip it easily.
                            // Ideally we wouldn't generate it in the first place though.
                            triangles[ti + 0] = vi;
                            triangles[ti + 1] = vi;
                            triangles[ti + 2] = vi;
                            triangles[ti + 3] = vi;
                            triangles[ti + 4] = vi;
                            triangles[ti + 5] = vi;

                            ti += 6;
                        
                            triangles[ti + 5] = vi;
                            triangles[ti + 4] = vi;
                            triangles[ti + 3] = vi;
                            triangles[ti + 2] = vi;
                            triangles[ti + 1] = vi;
                            triangles[ti + 0] = vi;
                            
                            continue;
                        }

                        triangles[ti + 0] = vi;
                        triangles[ti + 1] = vi + meshResolutionX + 1;
                        triangles[ti + 2] = vi + 1;
                        triangles[ti + 3] = vi + 1;
                        triangles[ti + 4] = vi + meshResolutionX + 1;
                        triangles[ti + 5] = vi + meshResolutionX + 2;

                        ti += 6;
                        
                        triangles[ti + 5] = vi;
                        triangles[ti + 4] = vi + meshResolutionX + 1;
                        triangles[ti + 3] = vi + 1;
                        triangles[ti + 2] = vi + 1;
                        triangles[ti + 1] = vi + meshResolutionX + 1;
                        triangles[ti + 0] = vi + meshResolutionX + 2;
                    }
                }
            }
            else
            {
                triangles = new int[meshResolutionX * meshResolutionZ * 6];
                for (int ti = 0, vi = 0, y = 0; y < meshResolutionZ; y++, vi++)
                {
                    for (int x = 0; x < meshResolutionX; x++, ti += 6, vi++)
                    {
                        if (IsHole(x, y))
                        {
                            // Creates degenerate triangle so the mesh optimizer can detect and strip it easily.
                            // Ideally we wouldn't generate it in the first place though.
                            triangles[ti + 0] = vi;
                            triangles[ti + 1] = vi;
                            triangles[ti + 2] = vi;
                            triangles[ti + 3] = vi;
                            triangles[ti + 4] = vi;
                            triangles[ti + 5] = vi;
                            
                            continue;
                        }
                        
                        triangles[ti] = vi;
                        triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                        triangles[ti + 4] = triangles[ti + 1] = vi + meshResolutionX + 1;
                        triangles[ti + 5] = vi + meshResolutionX + 2;
                    }
                }
            }

            mesh.triangles = triangles;
            
            int subMeshCount = 64;

            int remainingTris = triangles.Length;
            int indexStart = 0;

            mesh.subMeshCount = subMeshCount;

            int batchSize = Mathf.CeilToInt(remainingTris / (float)subMeshCount);

            for (int currentSubMeshIndex = 0; currentSubMeshIndex < subMeshCount; ++currentSubMeshIndex)
            {
                if (batchSize > remainingTris)
                {
                    batchSize = remainingTris;
                }
                
                // Make sure batchSize is divisible by 3
                if (batchSize % 3 != 0)
                {
                    batchSize += 3 - (batchSize % 3);
                }

                mesh.SetSubMesh(currentSubMeshIndex, new SubMeshDescriptor(indexStart, batchSize));

                indexStart += batchSize;
                remainingTris -= batchSize;
            }
            
            mesh.RecalculateBounds();
            
#if UNITY_EDITOR
            UnityEditor.MeshUtility.Optimize(mesh);
#endif
            
            return mesh;
        }
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TerrainToMeshUtility))]
    public class TerrainConvertHelperV1Editor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            TerrainToMeshUtility helper = target as TerrainToMeshUtility;
            
            DrawDefaultInspector();

            if (GUILayout.Button(helper.meshRendererReference != null ? "Update mesh" : "Create mesh"))
            {
                helper.CreateOrUpdateMesh();
            }

            if (helper.meshRendererReference != null)
            {
                if (GUILayout.Button("Select MeshRenderer"))
                {
                    UnityEditor.Selection.activeGameObject = helper.meshRendererReference.gameObject;
                }
            }
        }
    }
#endif
}