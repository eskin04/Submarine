// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

#if MATHEMATICS
using Unity.Mathematics;
#endif
#if SPLINES
using UnityEngine.Splines;
using Interpolators = UnityEngine.Splines.Interpolators;
#endif

namespace sc.modeling.splines.runtime
{
    public static class SplineMeshGenerator
    {
        //Mesh data
        private static readonly List<Vector3> vertices = new List<Vector3>();
        private static readonly List<Vector3> normals = new List<Vector3>();
        private static readonly List<Vector4> tangents = new List<Vector4>();
        private static readonly List<Vector2> uv0 = new List<Vector2>();
        //Holds an array of indices for each submesh
        private static readonly List<List<int>> triangles = new List<List<int>>();
        private static readonly List<Color> colors = new List<Color>();
        
        private static Vector3[] sourceVertices;
        private static List<int[]> sourceTriangles = new List<int[]>();
        private static Vector3[] sourceNormals;
        private static Vector2[] sourceUv0;
        private static Vector4[] sourceTangents;
        private static Color[] sourceColors;

        private static bool hasTangents;
        private static bool hasUV;
        private static bool hasVertexColor;
        
        private static List<CombineInstance> combineInstances = new List<CombineInstance>();

        #if MATHEMATICS
        private static Bounds bounds;
        private static float3 boundsMin;
        private static float3 boundsMax;
        
        private static float4x4 splineLocalToWorld;
        #endif
        
        #if SPLINES
        public static readonly Interpolators.LerpFloat3 Float3Interpolator = new Interpolators.LerpFloat3();

        /// <summary>
        /// Tiles and deforms the sourceMesh along splines within the container
        /// </summary>
        /// <param name="splineContainer"></param>
        /// <param name="sourceMesh">Input mesh</param>
        /// <param name="worldToLocalMatrix">Transform matrix of the renderer the mesh is to be used on</param>
        /// <param name="settings"></param>
        /// <param name="scaleData"></param>
        /// <returns></returns>
        /// <exception cref="Exception">Bails out if the spline is too short</exception>
        public static Mesh CreateMesh(SplineContainer splineContainer, Mesh sourceMesh, Matrix4x4 worldToLocalMatrix, Settings settings, List<SplineData<float3>> scaleData = null)
        {
            #if MATHEMATICS
            Mesh outputMesh = new Mesh();
            int submeshCount = sourceMesh.subMeshCount;
            
            var splineCount = splineContainer.Splines.Count;
            //Note: every submesh requires its own CombineInstance
            combineInstances.Clear();

            //Debug.Log($"Generating for {splineCount} spline(s) from {sourceMesh.name} with {submeshCount} submesh(es)");
            
            boundsMin = Vector3.one * -math.INFINITY;
            boundsMax = Vector3.one * math.INFINITY;
            

            //Get initial arrays
            sourceVertices = sourceMesh.vertices;
            int sourceVertexCount = sourceVertices.Length;
            sourceNormals = sourceMesh.normals;
            sourceUv0 = sourceMesh.uv;
            sourceTangents = sourceMesh.tangents;
            sourceColors = sourceMesh.colors;
            bounds = sourceMesh.bounds;
            
            sourceTriangles.Clear();
            for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
            {
                //Input
                sourceTriangles.Add(sourceMesh.GetTriangles(submeshIndex));
            }

            hasUV = sourceUv0.Length > 0;
            hasTangents = sourceTangents.Length > 0;
            hasVertexColor = sourceColors.Length > 0;
            
            splineLocalToWorld = splineContainer.transform.localToWorldMatrix;

            float meshLength = math.max(0.1f, bounds.size.z * settings.deforming.scale.z);
            float meshHeight = bounds.size.y;
            float segmentLength = meshLength + settings.distribution.spacing;
            float trim = settings.distribution.trimEnd + settings.distribution.trimStart;
            
            int vertexCount = 0;

            Profiler.BeginSample("Spline Mesher: Bend Vertices Along Splines");
            
            RaycastHit hit = new RaycastHit();
            
            for (int splineIndex = 0; splineIndex < splineCount; splineIndex++)
            {
                float splineLength = splineContainer.Splines[splineIndex].CalculateLength(splineLocalToWorld);
                float splineLengthTrimmed = splineLength - trim;

                if (splineLengthTrimmed <= 0.02f)
                {
                    //Debug.LogError($"Spline #{splineIndex} in {splineContainer.name} is too short ({splineLengthTrimmed})");
                    continue;
                }

                //Too short
                if (splineLength < meshLength)
                {
                    //Debug.LogWarning($"Input mesh ({sourceMesh.name}) is larger ({meshLength}) than the length of the spline #{splineIndex} ({splineLength}), no output mesh possible");
                    continue;
                }
                
                int CalculateSegments()
                {
                    if (settings.distribution.evenOnly == false)
                    {
                        return (int)math.ceil((splineLengthTrimmed / segmentLength));
                    }
                    
                    return (int)math.floor((splineLengthTrimmed / segmentLength));
                }
                int segments = CalculateSegments();
                if (segments == 0) continue;
                
                var splineMesh = new Mesh();
                splineMesh.subMeshCount = submeshCount;
                
                //Clear data for current spline, which is to get its own mesh.
                triangles.Clear();
                for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
                {
                    triangles.Add(new List<int>());
                }
                vertices.Clear();
                normals.Clear();
                tangents.Clear();
                uv0.Clear();
                colors.Clear();


                float trimStart = (settings.distribution.trimStart / splineLengthTrimmed);
                float trimEnd = (settings.distribution.trimEnd / splineLengthTrimmed);

                float3 origin = 0f;
                float3 tangent = 0f;
                float3 up = 0f;
                float3 forward = 0f;
                float3 right = 0f;
                quaternion rotation = quaternion.identity;
                quaternion normalRotation = quaternion.identity;
                float3 splineScale = new float3(1f);

                //int splineSamples = 0;

                for (int i = 0; i < segments; i++)
                {
                    float segmentOffset = ((float)i * (segmentLength));
                    float prevZ = -1;
                    
                    for (int v = 0; v < sourceVertexCount; v++)
                    {
                        //t-value of vertex over the length of the mesh. Normalized value 0-1
                        float localVertPos = (sourceVertices[v].z - bounds.min.z) / (bounds.max.z - bounds.min.z);
                        //localVertPos = math.clamp(localVertPos, 0f, 1f);
                        
                        //Check if Z-value of vertex is changing, meaning sampling moves forward
                        var resample = (math.abs(localVertPos - prevZ) > 0f);
                        if (resample) prevZ = localVertPos;

                        float3 splinePoint = origin;
                        
                        //Important optimization. If a mesh has edge loops (vertices sharing the same Z-value) the spline gets unnecessarily re-sampled
                        //In this case, all the spline-related information is identical, so doesn't need to be recalculated.
                        if (resample)
                        {
                            float distance = (localVertPos * meshLength) + segmentOffset;
                            
                            float t = distance / splineLength;
                            
                            Profiler.BeginSample("Spline Mesher: Sample Spline");
                            {
                                t = math.clamp(t + trimStart, trimStart, 1f - trimEnd);
                                t = math.clamp(t, 0.000001f, 0.999999f); //Ensure a tangent can always be derived

                                splineContainer.Splines[splineIndex].Evaluate(t, out origin, out tangent, out up);
                                //SplineCache.Evaluate(splineContainer, splineIndex, t, out origin, out tangent, out up);
                                //splineSamples++;

                                forward = math.normalize(tangent);
                                right = math.cross(up, forward);
                                rotation = quaternion.LookRotation(forward, up);

                                if (settings.deforming.ignoreRoll)
                                {
                                    rotation = RollCorrectedRotation(forward);
                                    right = math.rotate(rotation, math.right());
                                }
                            }
                            Profiler.EndSample();
                            
                            if (scaleData != null)
                            {
                                if (scaleData[splineIndex].Count > 0)
                                {
                                    splineScale = scaleData[splineIndex].Evaluate(splineContainer.Splines[splineIndex], distance, scaleData[splineIndex].PathIndexUnit, Float3Interpolator);
                                }
                                else
                                {
                                    splineScale = new float3(1f);
                                }
                            }
                            
                            //Counter scale of spline container transform
                            splineScale /= splineContainer.transform.lossyScale;
                            splineScale.x *= settings.deforming.scale.x;
                            splineScale.y *= settings.deforming.scale.y;
                            splineScale.z = 0f;

                            //Update
                            splinePoint = origin;
                            normalRotation = rotation;
                        }
                        
                        if (settings.conforming.enable)
                        {
                            //Vertex position in spline's local space to world-space
                            float3 positionWS = math.transform(splineLocalToWorld, splinePoint);
                            
                            float dist = math.max(meshHeight + settings.conforming.seekDistance, 1f);
                            
                            if (Physics.Raycast(positionWS + (math.up() * dist), -math.up(), out hit, dist * 2f, settings.conforming.layerMask, QueryTriggerInteraction.Ignore))
                            {
                                var validHit = true;

                                if (settings.conforming.terrainOnly)
                                {
                                    validHit = hit.collider.GetType() == typeof(TerrainCollider);;
                                }
                                
                                if (validHit)
                                {
                                    //Convert information from world-space back to spline's local space
                                    hit.point = splineContainer.transform.InverseTransformPoint(hit.point);
                                    hit.normal = splineContainer.transform.InverseTransformVector(hit.normal);
                                    
                                    splinePoint.y = hit.point.y;

                                    quaternion hitRotation = quaternion.LookRotationSafe(forward, hit.normal);
                                    
                                    //Copy normal of surface, to be used for deforming
                                    if (settings.conforming.align)
                                    {
                                        rotation = hitRotation;
                                    }

                                    if (settings.conforming.blendNormal)
                                    {
                                        normalRotation = hitRotation;
                                    }
                                }
                            }
                        }

                        splinePoint += right * settings.deforming.offset.x;
                        splinePoint.y += settings.deforming.offset.y;

                        Profiler.BeginSample("Spline Mesher: Transform Vertices");

                        float3 vertexPositionLocal = (float3)sourceVertices[v] + (math.forward() * settings.distribution.spacing);
                        
                        //Transform vertex to spline point and rotation (spline's local-space)
                        float3 position = splinePoint + math.rotate(rotation, vertexPositionLocal * splineScale);
                        
                        //Transform position from spline's local-space into world-space
                        float3 vertexPosition = math.mul(splineLocalToWorld, new float4(position, 1.0f)).xyz;

                        //Make that the local-space position of the mesh filter
                        vertexPosition = math.mul(worldToLocalMatrix, new float4(vertexPosition, 1.0f)).xyz;

                        //Also transform the normal
                        float3 vertexNormal = math.rotate(normalRotation, sourceNormals[v]);
                        
                        Profiler.EndSample();

                        //Extend bounds as it expands
                        boundsMin = math.min(position, boundsMin);
                        boundsMax = math.max(position, boundsMax);
                        
                        //Assign vertex attributes
                        vertices.Add(vertexPosition);
                        
                        normals.Add(vertexNormal);
                        
                        if(hasTangents) tangents.Add(sourceTangents[v]);
                        if(hasUV) uv0.Add(sourceUv0[v]);
                        if(hasVertexColor) colors.Add(sourceColors[v]);
                    }
                    
                    for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
                    {
                        var triCount = sourceTriangles[submeshIndex].Length;
                        for (int v = 0; v < triCount; v++)
                        {
                            triangles[submeshIndex].Insert(i * triCount + v, sourceTriangles[submeshIndex][v] + (sourceVertexCount * i));
                        }
                    }
                }

                var splineVertCount = vertices.Count;
                vertexCount += splineVertCount * submeshCount;

                //Debug.Log($"Estimated spline samples: {segments * sourceVertexCount}. Actual spline samples performed: {splineSamples}. Improvement: {100f/(1f/((float)(segments * sourceVertexCount) / (float)splineSamples) * 100f)*100f}%");
                
                splineMesh.indexFormat = splineVertCount >= 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                splineMesh.SetVertices(vertices, 0, splineVertCount, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);
                splineMesh.SetNormals(normals);
                
                if(hasTangents) splineMesh.SetTangents(tangents);
                if(hasUV) splineMesh.SetUVs(0, uv0);
                if(hasVertexColor) splineMesh.SetColors(colors);
                
                for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
                {
                    splineMesh.SetIndices(triangles[submeshIndex], MeshTopology.Triangles, submeshIndex, false);
                    
                    CombineInstance combineInstance = new CombineInstance()
                    {
                        mesh = splineMesh,
                        subMeshIndex = submeshIndex
                    };
                    
                    combineInstances.Add(combineInstance);
                }
                
                //Debug.Log($"Generated mesh for spline #{splineIndex} with {submeshCount} submeshs");
            }
            
            Profiler.EndSample();
            
            Profiler.BeginSample("Spline Mesher: Composite Output Mesh");

            outputMesh.indexFormat = vertexCount >= 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            
            //Note: Warning about Instance X being null is attributed to a Spline having a length of 0. Therefor it was counted, but no mesh was created for it.
            //Bug: if submeshes aren't merged, a submesh is created for each spline
            outputMesh.CombineMeshes(combineInstances.ToArray(), submeshCount == 1, false);
            
            //Debug.Log($"Combined {splineCount} spline meshes from {sourceMesh.name} with {outputMesh.subMeshCount} submeshes");
            
            outputMesh.UploadMeshData(true);
            outputMesh.bounds.SetMinMax(boundsMin, boundsMax);

            outputMesh.name = $"{sourceMesh.name} Spline";
            
            Profiler.EndSample();
            //outputMesh.RecalculateNormals();

            return outputMesh;
            #else
            return null;
            #endif
        }
        #endif

        /// <summary>
        /// Apply generic transforms to the mesh
        /// </summary>
        /// <param name="input"></param>
        /// <param name="rotation">Euler rotation</param>
        /// <param name="flipX"></param>
        /// <param name="flipY"></param>
        /// <returns></returns>
        public static Mesh TransformMesh(Mesh input, Vector3 rotation, bool flipX, bool flipY)
        {
            var rotationAmount = math.abs(math.length(rotation));
            
            if (rotationAmount > 0.01f || flipX || flipY)
            {
                Vector3[] outputVertices = input.vertices;
                int vertexCount = outputVertices.Length;
                Vector3[] outputNormals = input.normals;
                int[] outputTriangles = input.triangles;
                int triCount = outputTriangles.Length;

                Bounds outputBounds = input.bounds;
                if (rotationAmount > 0.01f)
                {
                    (rotation.x, rotation.z) = (rotation.z, rotation.x);
                    
                    outputBounds = new Bounds();

                    Quaternion m_meshRotation = Quaternion.Euler(rotation);
                    for (int i = 0; i < vertexCount; i++)
                    {
                        outputVertices[i] = math.rotate(m_meshRotation, outputVertices[i]);

                        outputBounds.Encapsulate(outputVertices[i]);

                        outputNormals[i] = math.rotate(m_meshRotation, outputNormals[i]);
                    }
                }

                if (flipX || flipY)
                {
                    //Reverse triangle order if negatively scaled
                    var triangleCount = triCount / 3;
                    for (int j = 0; j < triangleCount; j++)
                    {
                        (outputTriangles[j * 3], outputTriangles[j * 3 + 1]) = (outputTriangles[j * 3 + 1], outputTriangles[j * 3]);
                    }
                    
                    //Rotate normals
                    Quaternion m_meshRotation = Quaternion.Euler(flipY ? 180f : 0f, flipX ? 180f : 0f, 0f);
                    for (int i = 0; i < vertexCount; i++)
                    {
                        outputNormals[i] = math.rotate(m_meshRotation, outputNormals[i]);
                    }
                }

                Mesh output = new Mesh();
                output.name = input.name;
                output.SetVertices(outputVertices, 0, vertexCount, MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds);

                output.triangles = outputTriangles;
                
                //output.RecalculateBounds();
                output.bounds = outputBounds;
                
                output.uv = input.uv;
                output.uv2 = input.uv2;
                output.normals = outputNormals;
                output.colors = input.colors;
                output.tangents = input.tangents;

                return output;
            }
            
            return input;
        }

        public static quaternion RollCorrectedRotation(float3 forward)
        {
            float3 euler = Quaternion.LookRotation(forward, math.up()).eulerAngles;
            return quaternion.AxisAngle(math.up(), euler.y * Mathf.Deg2Rad);
        }

        private static readonly Vector2[] corners = new[]
        {
            new Vector2(-0.5f, -0.5f), //Bottom-left
            new Vector2(-0.5f, 0.5f), //Top-left
            new Vector2(0.5f, 0.5f),  //Top-right
            new Vector2(0.5f, -0.5f), //Bottom-right
            new Vector2(-0.5f, -0.5f), //Bottom-right
        };
        
        /// <summary>
        /// Creates a cube mesh from the input mesh's bounds.
        /// </summary>
        /// <param name="sourceMesh"></param>
        /// <param name="subdivisions">Number of edge loops across the length</param>
        /// <param name="caps">Create two triangles at the front and back</param>
        /// <returns></returns>
        public static Mesh CreateBoundsMesh(Mesh sourceMesh, int subdivisions = 0, bool caps = false)
        {
            //TODO: Refactor to create separate faces per side so that the normals are perpendicular
            
            Bounds m_bounds  = sourceMesh.bounds;
            
            Mesh boundsMesh = new Mesh();
            boundsMesh.name = $"{sourceMesh.name} Bounds";

            Vector3 scale = m_bounds.size;
            Vector3 offset = m_bounds.center;
            
            int edges = 4;
            subdivisions = Mathf.Max(0, subdivisions);
            int lengthSegments = subdivisions+1;

            int xCount = edges + 1;
            int zCount = lengthSegments + 1;
            int numVertices = xCount * zCount;

            List<Vector3> mVertices = new List<Vector3>();
            List<int> mTriangles = new List<int>();
            
            float scaleZ = scale.z / lengthSegments;
            
            for (int z = 0; z < zCount; z++)
            {
                //Move clockwise to position vertices in each corner around the edge loop
                for (int x = 0; x < xCount; x++)
                {
                    Vector3 vertex;
                    
                    vertex.x = (corners[x].x * scale.x) + offset.x;
                    vertex.y = (corners[x].y * scale.y) + offset.y;
                    vertex.z = z * scaleZ - (scale.z * 0.5f) + offset.z;

                    mVertices.Add(vertex);
                }
                
                if (z < zCount-1) //Stop at 2nd last row
                {
                    for (int x = 0; x < edges; x++)
                    {
                        mTriangles.Insert(0, (z * xCount) + x);
                        mTriangles.Insert(1, ((z + 1) * xCount) + x);
                        mTriangles.Insert(2, (z * xCount) + x + 1);

                        mTriangles.Insert(3, ((z + 1) * xCount) + x);
                        mTriangles.Insert(4, ((z + 1) * xCount) + x + 1);
                        mTriangles.Insert(5, (z * xCount) + x + 1);
                    }
                }
            }

            if (caps)
            {
                //Back quad
                
                mTriangles.Add(1);
                mTriangles.Add(2);
                mTriangles.Add(0);
                
                mTriangles.Add(2);
                mTriangles.Add(3);
                mTriangles.Add(0);
                
                //Front quad
                mTriangles.Add(numVertices-4);
                mTriangles.Add(numVertices-5);
                mTriangles.Add(numVertices-3);
                
                mTriangles.Add(numVertices-2);
                mTriangles.Add(numVertices-3);
                mTriangles.Add(numVertices-5);

            }
            
            boundsMesh.SetVertices(mVertices, 0, numVertices, MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            boundsMesh.subMeshCount = 1;
            boundsMesh.SetIndices(mTriangles, MeshTopology.Triangles, 0, false);
            boundsMesh.RecalculateNormals();
            boundsMesh.bounds = m_bounds;

            return boundsMesh;
        }
    }
}