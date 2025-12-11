// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if MATHEMATICS
using Unity.Mathematics;
#endif
#if SPLINES
using UnityEngine.Splines;
#endif

namespace sc.modeling.splines.runtime
{
    public partial class SplineMesher
    {
        #if SPLINES
        /// <summary>
        /// Geometry will be created from splines within this container.
        /// </summary>
        public SplineContainer splineContainer;
        //[HideInInspector]
        /// <summary>
        /// Scale information for each spline within the container. <seealso cref="SampleScale(float,int)"/>
        /// </summary>
        public List<SplineData<float3>> scaleData = new List<SplineData<float3>>();

        private partial void SubscribeSplineCallbacks()
        {
            #if MATHEMATICS
            SplineContainer.SplineAdded += OnSplineAdded;
            SplineContainer.SplineRemoved += OnSplineRemoved;
            Spline.Changed += OnSplineChanged;
            #endif
        }
        
        private partial void UnsubscribeSplineCallbacks()
        {
            #if MATHEMATICS
            SplineContainer.SplineAdded -= OnSplineAdded;
            SplineContainer.SplineRemoved -= OnSplineRemoved;
            Spline.Changed -= OnSplineChanged;
            #endif
        }
        
        private void OnSplineChanged(Spline spline, int index, SplineModification modificationType)
        {
            if (!splineContainer) return;

            if (rebuildTriggers.HasFlag(RebuildTriggers.OnSplineChanged) == false) return;

            //Spline belongs to the assigned container?
            var splineIndex = Array.IndexOf(splineContainer.Splines.ToArray(), spline);
            if (splineIndex < 0)
                return;

            Rebuild();
        }
        
        private void OnSplineAdded(SplineContainer container, int index)
        {
            if (!splineContainer) return;
            
            if (rebuildTriggers.HasFlag(RebuildTriggers.OnSplineAdded) == false) return;

            if (container.GetHashCode() != splineContainer.GetHashCode())
                return;

            Rebuild();
        }

        private void OnSplineRemoved(SplineContainer container, int index)
        {
            if (!splineContainer) return;

            if (rebuildTriggers.HasFlag(RebuildTriggers.OnSplineRemoved) == false) return;

            if (container != splineContainer)
                return;

            if (index < scaleData.Count)
            {
                #if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(this, "Deleting Spline Mesh Scale data");
                #endif
                
                scaleData.RemoveAt(index);
            }

            Rebuild();
        }
        
        /// <summary>
        /// Clears the scale data for every spline. If no scale data is found for a spline, a default value is used.
        /// </summary>
        public void ResetScaleData()
        {
            if (!splineContainer) return;
            
            scaleData.Clear();
            ValidateData(splineContainer);
            
            Rebuild();
        }
        
        public void ReverseSpline()
        {
            if (!splineContainer) return;
            
            for (int s = 0; s < splineContainer.Splines.Count; s++)
            {
                SplineUtility.ReverseFlow(splineContainer.Splines[s]);
            }
        }

        public void ValidateData(SplineContainer container)
        {
            #if MATHEMATICS
            int splineCount = container.Splines.Count;
            if (scaleData.Count < splineCount)
            {
                var delta = splineCount - scaleData.Count;
                
                for (var i = 0; i < delta; i++)
                {
                    #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(this, "Modifying Spline Mesh Scale");
                    #endif
                    
                    float length = container.Splines[i].CalculateLength(container.transform.localToWorldMatrix);
                    
                    SplineData<float3> data = new SplineData<float3>();
                    data.DefaultValue = Vector3.one;
                    data.PathIndexUnit = PathIndexUnit.Distance;
                    //data.AddDataPointWithDefaultValue(0f);
                    //data.AddDataPointWithDefaultValue(length);
                    
                    scaleData.Add(data);
                }
            }
            #endif
        }

        /// <summary>
        /// Converts an array of positions in world-space to a series of <see cref="BezierKnot"/>s for a new spline. This spline gets added to the assigned <see cref="SplineContainer"/>
        /// Any existing splines in the assigned container will be cleared.
        /// </summary>
        /// <param name="positions">Positions in world-space, at least 2</param>
        /// <param name="smooth">Enable to smooth the curve's tangent between points</param>
        public void CreateSplineFromPoints(Vector3[] positions, bool smooth)
        {
            if (!splineContainer)
            {
                throw new Exception("Failed to create a spline from a position array. No SplineContainer is assigned to the component");
            }
            
            int pointCount = positions.Length;

            if (pointCount < 2)
            {
                throw new Exception("Failed to create a spline from a position array. At least 2 points need to be provided");
            }
            
            //Note: Editing a spline's knots does not appear to have any effect. Hence a brand new one is created, then added.
            
            //First, delete all existing splines
            for (int s = 0; s < splineContainer.Splines.Count; s++)
            {
                splineContainer.RemoveSpline(splineContainer.Splines[s]);
            }

            Spline spline = new Spline(pointCount);
            
            for (int i = 0; i < pointCount; i++)
            {
                BezierKnot knot = new BezierKnot();
                knot.Position = splineContainer.transform.InverseTransformPoint(positions[i]);
                knot.Rotation = Quaternion.identity;
                
                spline.Add(knot, smooth ? TangentMode.AutoSmooth : TangentMode.Linear);
            }
            
            //Automatically recalculate tangents
            spline.SetTangentMode(new SplineRange(0, spline.Count), smooth ? TangentMode.AutoSmooth : TangentMode.Linear);
   
            //Adding a spline automatically rebuilds the mesh
            splineContainer.AddSpline(spline);
        }
        #endif //SPLINES
    }
}