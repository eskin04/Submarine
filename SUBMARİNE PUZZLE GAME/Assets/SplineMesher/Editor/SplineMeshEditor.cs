// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using System;
using System.Collections.Generic;
using sc.modeling.splines.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if SPLINES
using UnityEngine.Splines;
using UnityEditor.Splines;
#endif

#if FBX_EXPORTER
using UnityEditor.Formats.Fbx.Exporter;
#endif

namespace sc.modeling.splines.editor
{
    public static class SplineMeshEditor
    {
        public const int ASSET_ID = 280289;
        public const string DOC_URL = "http://staggart.xyz/sm-docs";
        public const string FORUM_URL = "https://forum.unity.com/threads/1565389";
        public const string DISCORD_INVITE_URL = "https://discord.gg/GNjEaJc8gw";

        private const string MIN_SPLINES_VERSION = "2.4.0";
        
        private static bool STARTUP_PERFORMED
        {
            get => SessionState.GetBool("SPLINE_MESHER_EDITOR_STARTED", false);
            set => SessionState.SetBool("SPLINE_MESHER_EDITOR_STARTED", value);
        }
        
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            #if !SPLINES
            if (EditorUtility.DisplayDialog("Spline Mesher", $"This asset requires the \"Splines\" (v{MIN_SPLINES_VERSION}) package dependency, which is not installed.", "Install now", "Later"))
            {
                //Note: Mathematics package is a dependency, so will also be installed
                UnityEditor.PackageManager.Client.Add($"com.unity.splines@{MIN_SPLINES_VERSION}");
            }
            #endif
            
            #if !SM_DEV
            if (STARTUP_PERFORMED == false)
            #endif
            {
                VersionChecking.CheckForUpdate();
                STARTUP_PERFORMED = true;
            }

            Lightmapping.bakeStarted += OnLightBakeStart;
        }

       
        public static void OpenInPackageManager()
        {
            Application.OpenURL("com.unity3d.kharma:content/" + ASSET_ID);
        }
        
        public static void OpenReviewsPage()
        {
            Application.OpenURL($"https://assetstore.unity.com/packages/slug/{ASSET_ID}?aid=1011l7Uk8&pubref=smeditor#reviews");
        }
        
        private static void OnLightBakeStart()
        {
            SplineMesher[] splineMeshers = Object.FindObjectsByType<SplineMesher>(FindObjectsSortMode.None);
            
            List<Mesh> meshes = new List<Mesh>(); 
            
            //Find the spline meshes that still require lightmap UV's
            foreach (SplineMesher splineMesher in splineMeshers)
            {
                if (RequiresLightmapUV(splineMesher))
                {
                    MeshFilter mf = splineMesher.outputObject.GetComponent<MeshFilter>();
                    
                    //Debug.Log($"{splineMesher.name} requires new lightmap UV's");
                    meshes.Add(mf.sharedMesh);
                }
            }

            int meshCount = meshes.Count;

            if (meshCount > 0)
            {
                System.Diagnostics.Stopwatch lightmapUVUnwrapTimer = new System.Diagnostics.Stopwatch();
                lightmapUVUnwrapTimer.Start();
                
                UnwrapParam.SetDefaults(out var unwrapSettings);
                //unwrapSettings.packMargin = 0.02f;

                foreach (Mesh mesh in meshes)
                {
                    #if UNITY_2022_1_OR_NEWER
                    if (Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings) == false)
                    {
                        throw new Exception($"Lightmap UV generation for mesh \"{mesh.name}\" failed.");
                    }
                    #else
                    Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);
                    #endif
                }

                lightmapUVUnwrapTimer.Stop();

                Debug.Log($"[Spline Mesher] Lightmap UV created for {meshCount} spline meshes (Duration: {lightmapUVUnwrapTimer.ElapsedMilliseconds}ms)");
            }
        }

        public static bool RequiresLightmapUV(SplineMesher instance)
        {
            if (!instance.outputObject) return false;
            
            MeshFilter mf = instance.outputObject.GetComponent<MeshFilter>();
            
            //Improper set up
            if (mf == null) return false;
            
            StaticEditorFlags staticFlags = GameObjectUtility.GetStaticEditorFlags(mf.gameObject);
                
            //Mesh renderer marked as static
            if (staticFlags.HasFlag(StaticEditorFlags.ContributeGI))
            {
                Mesh mesh = mf.sharedMesh;

                //Conditions that indicate the mesh has no lightmap UV's
                //Note that rebuilding the spline mesh clears the UV2 channel, automatically marking it as 'dirty' again.
                if (mesh.uv2 == null || mesh.uv2.Length == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static void GenerateLightmapUV(SplineMesher instance)
        {
            MeshFilter mf = instance.outputObject.GetComponent<MeshFilter>();

            if (!mf) return;
            
            Mesh mesh = mf.sharedMesh;
            
            UnwrapParam.SetDefaults(out var unwrapSettings);
            
            #if UNITY_2022_1_OR_NEWER
            if (Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings) == false)
            {
                throw new Exception($"Lightmap UV generation for mesh \"{mesh.name}\" failed.");
            }
            #else
            Unwrapping.GenerateSecondaryUVSet(mesh, unwrapSettings);
            #endif
        }

        #region Editor menu additions
        #if SPLINES
        [MenuItem("CONTEXT/MeshFilter/Convert to Spline", true)]
        private static bool AddMesherToMeshFilterValidation(MenuCommand cmd)
        {
            MeshFilter meshFilter = (MeshFilter)cmd.context;
            
            return !meshFilter.GetComponent<SplineMesher>();
        }
        
        [MenuItem("CONTEXT/MeshFilter/Convert to Spline")]
        private static void AddMesherToMeshFilter(MenuCommand cmd)
        {
            MeshFilter meshFilter = (MeshFilter)cmd.context;

            if (meshFilter.sharedMesh == null)
            {
                throw new Exception("Mesh Filter component requires a mesh to convert");
            }
            
            if (EditorUtility.IsPersistent(meshFilter.sharedMesh) == false)
            {
                if (EditorUtility.DisplayDialog("Spline Mesher", "Mesh Filter uses a procedural mesh, it could have already been created from a spline.", "Continue", "Cancel") == false)
                {
                    return;
                }
            }

            SplineMesher component = meshFilter.gameObject.AddComponent<SplineMesher>();
            Undo.RegisterCreatedObjectUndo(component, "Created Spline Mesher");
            component.outputObject = meshFilter.gameObject;
            component.sourceMesh = meshFilter.sharedMesh;
            
            if (EditorUtility.DisplayDialog("Convert Mesh to Spline", "Create with a new spline?", "Yes", "No"))
            {
                SplineContainer splineContainer = meshFilter.gameObject.AddComponent<SplineContainer>();
                Undo.RegisterCreatedObjectUndo(splineContainer, "Created Spline Mesher");

                float meshLength = (meshFilter.sharedMesh.bounds.min.z - meshFilter.sharedMesh.bounds.max.z) * 2f;

                //Create new spline
                {
                    //One knot every 5 units
                    int knots = Mathf.RoundToInt(meshLength / 5f);
                    knots = Math.Max(2, knots);
                    
                    Spline spline = new Spline(knots, false);

                    for (int i = 0; i <= knots; i++)
                    {
                        float t = (float)i / (float)knots;
                        BezierKnot knot = new BezierKnot();
                        knot.Position = new Vector3(0, 0f, (t * meshLength) - (meshLength * 0.5f));
                        spline.Add(knot, TangentMode.Linear);
                    }

                    //Automatically recalculate tangents
                    spline.SetTangentMode(new SplineRange(0, spline.Count), TangentMode.AutoSmooth);
                    
                    //Spline container will be instantiated with a default spline, so overwrite it
                    splineContainer.Spline = spline;
                }

                component.splineContainer = splineContainer;

                //Activate the spline editor
                EditorApplication.delayCall += ToolManager.SetActiveContext<SplineToolContext>;
            }

            EditorUtility.SetDirty(meshFilter);
            
            component.Rebuild();

            if (Application.isPlaying == false) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            
        }
        
        [MenuItem("GameObject/3D Object/Spline Mesh", false, 0)]
        public static GameObject CreateSplineMesh()
        {
            Transform parent = Selection.activeGameObject ? Selection.activeGameObject.transform : null;
            GameObject gameObject = new GameObject(GameObjectUtility.GetUniqueNameForSibling(parent, "Spline Mesh"));
            Undo.RegisterCreatedObjectUndo(gameObject, "Created Spline Mesh Object");
            
            SplineMesher component = gameObject.AddComponent<SplineMesher>();
            
            SplineContainer splineContainer = gameObject.AddComponent<SplineContainer>();
            splineContainer.Splines = null;
            splineContainer.AddSpline(CreateDefaultSpline());

            component.splineContainer = splineContainer;

            SetupSplineRenderer(gameObject);
            
            component.Rebuild();

            if (parent) gameObject.transform.parent = parent;
            
            Selection.activeGameObject = gameObject;

            if (!parent) EditorApplication.ExecuteMenuItem("GameObject/Move To View");
            else gameObject.transform.localPosition = Vector3.zero;

            return gameObject;
        }

        private static Spline CreateDefaultSpline()
        {
            int knots = 5;
            float amplitude = 2f;
            float length = 15f;
            
            Spline spline = new Spline(knots, false);

            for (int i = 0; i < knots; i++)
            {
                float t = (float)i / (float)knots;
                
                BezierKnot knot = new BezierKnot();
                knot.Position = new Vector3(Mathf.Sin(t * length) * amplitude, 0f, (t * length) - (length * 0.5f));
                spline.Add(knot, TangentMode.Linear);
            }

            //Automatically recalculate tangents
            spline.SetTangentMode(new SplineRange(0, spline.Count), TangentMode.AutoSmooth);

            return spline;
        }

        [MenuItem("CONTEXT/SplineContainer/Add Spline Mesher", true)]
        private static bool AddMesherToSplineValidation(MenuCommand cmd)
        {
            SplineContainer splineContainer = (SplineContainer)cmd.context;
            
            return !splineContainer.GetComponent<SplineMesher>();
        }

        [MenuItem("CONTEXT/SplineContainer/Add Spline Mesher")]
        private static void AddMesherToSpline(MenuCommand cmd)
        {
            SplineContainer splineContainer = (SplineContainer)cmd.context;

            SplineMesher component = splineContainer.GetComponent<SplineMesher>();

            if (component) return;
            
            component = splineContainer.gameObject.AddComponent<SplineMesher>();
            Undo.RegisterCreatedObjectUndo(component, $"Add Spline Mesher to {splineContainer.name}");
            
            component.splineContainer = splineContainer;
            component.outputObject = component.gameObject;

            SetupSplineRenderer(component.outputObject);
            
            component.Rebuild();
            
            EditorUtility.SetDirty(splineContainer.gameObject);
        }
        #endif

        /// <summary>
        /// Adds a Mesh Filter and Mesh Renderer (with default material) if missing on the provided Game Object.
        /// </summary>
        /// <param name="targetObject"></param>
        public static void SetupSplineRenderer(GameObject targetObject)
        {
            MeshFilter meshFilter = targetObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = targetObject.AddComponent<MeshFilter>();
                Undo.RegisterCreatedObjectUndo(meshFilter, $"Setup Spline Mesher on {targetObject.name}");
            }
            
            MeshRenderer meshRenderer = targetObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = targetObject.AddComponent<MeshRenderer>();
                Undo.RegisterCreatedObjectUndo(meshRenderer, $"Setup Spline Mesher on {targetObject.name}");

                meshRenderer.sharedMaterial = UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline 
                    ? UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline.defaultMaterial :
                    AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            }
        }
        #endregion
        
        public static class FBX
        {
            /// <summary>
            /// Export any sort of meshes contained in the object, and load+reference them back to it
            /// </summary>
            /// <param name="mesher"></param>
            /// <param name="filePath"></param>
            /// <exception cref="Exception">Empty file path, or missing the FBX extension</exception>
            public static void SaveToFileAndReference(SplineMesher mesher, string filePath)
            {
                #if FBX_EXPORTER
                if (filePath == string.Empty)
                {
                    throw new Exception("Failed to save mesh(es) to an FBX file, file path is empty");
                }
                if (filePath.EndsWith(".fbx") == false)
                {
                    throw new Exception("Failed to save mesh(es) to an FBX file, file path must end with \".fbx\"");
                }

                GameObject gameObject = mesher.gameObject;

                MeshCollider collider = mesher.GetComponent<MeshCollider>();
                GameObject colliderObj = new GameObject();
                
                bool hasCollision = collider;
                if (hasCollision)
                {
                    //In order for the exporter to also export the collision mesh, it needs to be on a separate object
                    collider.transform.parent = mesher.transform;
                    collider.transform.localPosition = Vector3.zero;

                    MeshCollider tempCollider = colliderObj.AddComponent<MeshCollider>();
                    tempCollider.sharedMesh = collider.sharedMesh;
                    
                    MeshFilter tempMF = colliderObj.AddComponent<MeshFilter>();
                    tempMF.sharedMesh = collider.sharedMesh;
                }

                //Export
                ModelExporter.ExportObject(filePath, mesher);

                //Delete temp collider
                if (hasCollision)
                {
                    Object.DestroyImmediate(colliderObj);
                }
                
                //Import
                AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport);

                //Loading meshes and assigning them to MeshFilters
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(filePath);

                MeshFilter meshFilter = mesher.GetComponent<MeshFilter>();

                if (meshFilter)
                {
                    for (int i = 1; i < subAssets.Length; i++) //Skip the first, as it is the parent
                    {
                        if (subAssets[i] is Mesh)
                        {
                            if (hasCollision && subAssets[i].name.EndsWith("Collider"))
                            {
                                collider.sharedMesh = subAssets[i] as Mesh;
                                continue;
                            }
                            
                            meshFilter.sharedMesh = subAssets[i] as Mesh;
                        }
                    }
                }
                
                #endif
            }
        }
        
        internal static class VersionChecking
        {
            public static bool UPDATE_AVAILABLE
            {
                get => SessionState.GetBool("SPLINE_MESHER_UPDATE_AVAILABLE", false);
                set => SessionState.SetBool("SPLINE_MESHER_UPDATE_AVAILABLE", value);
            }
            
            public static string latestVersion = SplineMesher.VERSION;
            private static string apiResult;

            public static void CheckForUpdate()
            {
                var url = $"https://api.assetstore.unity3d.com/package/latest-version/{ASSET_ID}";

                using (System.Net.WebClient webClient = new System.Net.WebClient())
                {
                    webClient.DownloadStringCompleted += OnRetrievedAPIContent;
                    webClient.DownloadStringAsync(new System.Uri(url), apiResult);
                }
            }

            private class AssetStoreItem
            {
                public string name;
                public string version;
            }

            private static void OnRetrievedAPIContent(object sender, System.Net.DownloadStringCompletedEventArgs e)
            {
                if (e.Error == null && !e.Cancelled)
                {
                    string result = e.Result;

                    AssetStoreItem asset = (AssetStoreItem)JsonUtility.FromJson(result, typeof(AssetStoreItem));

                    latestVersion = asset.version;

                    Version remoteVersion = new Version(asset.version);
                    Version installedVersion = new Version(SplineMesher.VERSION);

                    UPDATE_AVAILABLE = remoteVersion > installedVersion;
                    
                    if (UPDATE_AVAILABLE)
                    {
                        //Debug.Log($"[{asset.name} v{installedVersion}] New version ({asset.version}) is available");
                    }
                }
            }
        }
    }
}