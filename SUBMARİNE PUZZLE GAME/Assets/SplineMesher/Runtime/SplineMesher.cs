// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if MATHEMATICS
using Unity.Mathematics;
using UnityEngine.Profiling;
#endif

#if SPLINES
using UnityEngine.Splines;
#endif

namespace sc.modeling.splines.runtime
{
    [ExecuteInEditMode]
    [AddComponentMenu("Splines/Spline Mesher")]
    [HelpURL("https://staggart.xyz/sm-docs/")]
    [Icon("Assets/SplineMesher/Editor/spline-mesher-icon-64px.psd")]
    public partial class SplineMesher : MonoBehaviour
    {
        public const string VERSION = "1.1.3";

        /// <summary>
        /// The input mesh to be used for mesh generation
        /// </summary>
        public Mesh sourceMesh;
        [Tooltip("The axis of the mesh that's considered to its forward direction." +
                 "\n\nConventionally, the Z-axis is forward. If you have to change this it's strongly recommend to fix the mesh's orientation instead!")]
        public Vector3 rotation;

        /// <summary>
        /// The output GameObject to which a <see cref="MeshFilter"/> component may be added. The output mesh will be assigned here.
        /// </summary>
        [Tooltip("The GameObject to which a Mesh Filter component may be added. The output mesh will be assigned here.")]
        public GameObject outputObject;
        [Obsolete("Set the Rebuild Trigger flag \"On Start\" instead", false)]
        public bool rebuildOnStart;
        
        [Flags]
        public enum RebuildTriggers
        {
            [InspectorName("Via scripting")]
            None = 0,
            OnSplineChanged = 1,
            OnSplineAdded = 2,
            OnSplineRemoved = 4,
            [InspectorName("On Start()")]
            OnStart = 8
        }

        [Tooltip("Control which sort of events cause the mesh to be regenerated." +
                 "\n\n" +
                 "For instance when the spline changes (default), or on the component's Start() function." +
                 "\n\n" +
                 "The \"Via scripting\" option assumes you call the Rebuild() function through code.")]
        public RebuildTriggers rebuildTriggers = RebuildTriggers.OnSplineAdded | RebuildTriggers.OnSplineRemoved | RebuildTriggers.OnSplineChanged;
        
        [Tooltip("Add a Mesh Collider component and also generate a collision mesh for it")]
        public bool enableCollider;
        [Tooltip("The \"Box\" type is an automatically created collider mesh, based on the source mesh's bounding box.")]
        public Settings.ColliderType colliderType;
        [Min(0)]
        [Tooltip("Subdivide the collision box, ensures it bends better in curves.")]
        public int colliderBoxSubdivisions = 0;
        public Mesh collisionMesh;
        [SerializeField]
        private MeshCollider meshCollider;

        /// <summary>
        /// Settings used for mesh generation
        /// </summary>
        public Settings settings = new Settings();
        
        #pragma warning disable CS0067
        public delegate void Action(SplineMesher instance);
        /// <summary>
        /// Pre- and post-build callbacks. The instance being passed is the Spline Mesher being rebuild.
        /// </summary>
        public static event Action onPreRebuildMesh, onPostRebuildMesh;

        /// <summary>
        /// UnityEvent for a GameObject's function to be executed when river is rebuild. This is exposed in the inspector.
        /// </summary>
        [Serializable]
        public class RebuildEvent : UnityEvent { }
        /// <summary>
        /// UnityEvent, fires whenever the spline is rebuild (eg. editing nodes) or parameters are changed
        /// </summary>
        [HideInInspector]
        public RebuildEvent onPreRebuild, onPostRebuild;
        #pragma warning restore CS0067
        
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("meshFilter")]
        private MeshFilter m_meshFilter;
        /// <summary>
        /// The MeshFilter component added to the output GameObject
        /// </summary>
        public MeshFilter meshFilter
        {
            private set => m_meshFilter = value;
            get => m_meshFilter;
        }

        private void Reset()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter)
            {
                outputObject = meshFilter.gameObject;
            
                #if UNITY_EDITOR
                //If the mesh is saved to disk, consider it a viable source mesh
                if (UnityEditor.EditorUtility.IsPersistent(meshFilter.sharedMesh))
                {
                    sourceMesh = meshFilter.sharedMesh;
                }
                #endif
            }
            #if SPLINES
            splineContainer = GetComponentInParent<SplineContainer>();
            #endif
        }

        private void Start()
        {
            //In this case the component was likely copied somewhere, or prefabbed. Mesh data will have been lost, so regenerating it is an alternative
            if(rebuildTriggers.HasFlag(RebuildTriggers.OnStart)) {Rebuild();}
        }

        private void OnEnable()
        {
            #if SPLINES
            SubscribeSplineCallbacks();
            #endif
        }

        private void OnDisable()
        {
            #if SPLINES
            UnsubscribeSplineCallbacks();
            #endif
        }
        
        #if SPLINES
        private partial void SubscribeSplineCallbacks();
        private partial void UnsubscribeSplineCallbacks();
        #endif

        #if UNITY_EDITOR
        private readonly System.Diagnostics.Stopwatch rebuildTimer = new System.Diagnostics.Stopwatch();
        #endif

        /// <summary>
        /// Checks for the presence of a <see cref="MeshFilter"/> and <see cref="MeshRenderer"/> component on the assigned output object
        /// </summary>
        public void ValidateOutput()
        {
            //Upgrade to v1.1.0
            if (!outputObject)
            {
                meshFilter = GetComponent<MeshFilter>();
                if(meshFilter) outputObject = meshFilter.gameObject;
            }
            else
            {
                //Note: Targeting a specific GameObject, rather than a MeshFilter directly.
                //This makes it easier to add support for multiple output meshes, or LOD Groups, which involves adding components or child objects
                if (!meshFilter) meshFilter = outputObject.GetComponent<MeshFilter>();
                if (!meshFilter)
                {
                    meshFilter = outputObject.AddComponent<MeshFilter>();
                    
                    MeshRenderer meshRenderer = outputObject.GetComponent<MeshRenderer>();
                    if (meshRenderer == false)
                    {
                        meshRenderer = outputObject.AddComponent<MeshRenderer>();
                    }
                }
            }
        }
        
        private Mesh inputMesh;

        /// <summary>
        /// Regenerates the output mesh for all the splines within the assigned <see cref="SplineContainer"/>. Also recreates the collision mesh.
        /// </summary>
        public void Rebuild()
        {
            #if SPLINES && MATHEMATICS
            if (!splineContainer) return;

            ValidateOutput();

            if (!outputObject) return;
            
            onPreRebuildMesh?.Invoke(this);
            onPreRebuild?.Invoke();
            
            Profiler.BeginSample("Spline Mesher: Rebuild", this);

            ValidateData(splineContainer);
 
            #if UNITY_EDITOR
            rebuildTimer.Reset();
            rebuildTimer.Start();
            #endif
            
            //Avoid self-collision
            var collision = enableCollider && meshCollider;
            if (collision) meshCollider.enabled = false;

            if (meshFilter && sourceMesh)
            {
                if (Application.isPlaying && sourceMesh.isReadable == false)
                {
                    throw new Exception($"[Spline Mesher] To use this at runtime, the mesh \"{sourceMesh.name}\" requires the Read/Write option enabled in its import settings.");
                }
                
                inputMesh = SplineMeshGenerator.TransformMesh(sourceMesh, rotation, settings.deforming.scale.x < 0, settings.deforming.scale.y < 0);
                
                Profiler.BeginSample("Spline Mesher: Create Mesh", this);
                
                meshFilter.sharedMesh = SplineMeshGenerator.CreateMesh(splineContainer, inputMesh, outputObject.transform.worldToLocalMatrix, settings, scaleData);
                
                Profiler.EndSample();
            }
            
            if (collision) meshCollider.enabled = true;

            CreateCollider();
            
            #if UNITY_EDITOR
            rebuildTimer.Stop();
            #endif
            
            Profiler.EndSample();

            onPostRebuildMesh?.Invoke(this);
            onPostRebuild?.Invoke();
            #endif
        }

        /// <summary>
        /// Returns the build time, in milliseconds, of the last rebuild operation
        /// </summary>
        /// <returns></returns>
        public float GetLastRebuildTime()
        {
            #if UNITY_EDITOR
            return rebuildTimer.ElapsedMilliseconds;
            #else
            return 0;
            #endif
        }

        private void CreateCollider()
        {
            #if SPLINES && MATHEMATICS
            if (!splineContainer) return;

            if (enableCollider)
            {
                if (!meshCollider) meshCollider = outputObject.GetComponent<MeshCollider>();
                if (!meshCollider) meshCollider = outputObject.AddComponent<MeshCollider>();

                var m_collisionMesh = collisionMesh;

                if (colliderType == Settings.ColliderType.Box)
                {
                    m_collisionMesh = SplineMeshGenerator.CreateBoundsMesh(inputMesh, colliderBoxSubdivisions);
                }
                else
                {
                    m_collisionMesh = SplineMeshGenerator.TransformMesh(collisionMesh, rotation, settings.deforming.scale.x < 0, settings.deforming.scale.y < 0);
                }

                if (m_collisionMesh)
                {
                    //Skip cleaning of degenerate triangles
                    //meshCollider.cookingOptions = MeshColliderCookingOptions.None;
                    
                    //If the visual mesh and collision mesh are identical, simply use that
                    if (m_collisionMesh.GetHashCode() == sourceMesh.GetHashCode())
                    {
                        meshCollider.sharedMesh = meshFilter.sharedMesh;
                    }
                    else
                    {
                        meshCollider.sharedMesh = null; //Avoid self-collision with raycasts
                        
                        meshCollider.sharedMesh = SplineMeshGenerator.CreateMesh(splineContainer, m_collisionMesh, meshCollider.transform.worldToLocalMatrix, settings, scaleData);
                        meshCollider.sharedMesh.name += " Collider";
                    }
                }
                else
                {
                    meshCollider.sharedMesh = null;
                }
            }
            else if(meshCollider)
            {
                DestroyImmediate(meshCollider);
            }
            #endif
        }

        #if SPLINES && MATHEMATICS
        /// <summary>
        /// Sample the mesh scale data on the spline. If no data is present, a default scale of (1,1,1) is returned.
        /// </summary>
        /// <param name="distance">The distance along the spline curve</param>
        /// <param name="splineIndex">Spline index number</param>
        /// <returns></returns>
        public float3 SampleScale(float distance, int splineIndex)
        {
            float3 splineScale = 1f;
            
            if (scaleData != null)
            {
                if (scaleData[splineIndex].Count > 0)
                {
                    splineScale = scaleData[splineIndex].Evaluate(splineContainer.Splines[splineIndex], distance, scaleData[splineIndex].PathIndexUnit, SplineMeshGenerator.Float3Interpolator);
                }
            }

            return splineScale;
        }
        
        /// <summary>
        /// Given a world-space position, attempt to find the nearest point on the spline, then sample the mesh scale data there.
        /// If all fails, a default scale of (1,1,1) is returned.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public float3 SampleScale(Vector3 worldPosition)
        {
            Vector3 localPosition = splineContainer.transform.InverseTransformPoint(worldPosition);

            //Unclear how one would find the nearest spline, so default to the first
            int splineIndex = 0;
            
            //Find the position on the spline that's nearest to the box's center
            SplineUtility.GetNearestPoint(splineContainer.Splines[splineIndex], localPosition, out var nearestPoint, out float t, SplineUtility.PickResolutionMin, 2);

            //Convert the normalized t-index to the distances on the spline
            float distance = splineContainer.Splines[splineIndex].ConvertIndexUnit(t, PathIndexUnit.Normalized, scaleData[splineIndex].PathIndexUnit);

            return SampleScale(distance, splineIndex);
        }
        #endif

        private void OnDrawGizmosSelected()
        {
            #if SPLINES && MATHEMATICS
            if (splineContainer && splineContainer.transform.hasChanged && Time.frameCount % 2 == 0)
            {
                Rebuild();
                splineContainer.transform.hasChanged = false;
            }
            /*
            if (meshFilter && meshFilter.sharedMesh)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < meshFilter.sharedMesh.vertices.Length; i++)
                {
                    Vector3 positionWS = meshFilter.transform.localToWorldMatrix.MultiplyPoint(meshFilter.sharedMesh.vertices[i]);
                    Vector3 normalWS = meshFilter.transform.TransformVector(meshFilter.sharedMesh.normals[i]);
                    Gizmos.DrawLine(positionWS, positionWS + normalWS);
                }
            }
            */
            #endif
        }
    }
}