using System;
using UnityEngine;

namespace sc.modeling.splines.runtime
{
    [Serializable]
    public class Settings
    {
        public enum ColliderType
        {
            Box,
            Mesh
        }

        [Serializable]
        public class Distribution
        {
            [Tooltip("Ensure the input mesh is repeated evenly, instead of cutting it off when it doesn't fit on the remainder of the spline.")]
            public bool evenOnly = false;
            
            [Space]
            
            [Min(0f)]
            [Tooltip("Shift the mesh X number of units from the start of the spline")]
            public float trimStart;
            [Min(0f)]
            [Tooltip("Shift the mesh X number of units from the end of the spline")]
            public float trimEnd;
            
            [Space]
            
            [Min(0f)]
            [Tooltip("Space between each mesh segment")]
            public float spacing = 0f; //WIP
        }
        public Distribution distribution = new Distribution();

        [Serializable]
        public class Deforming
        {
            [Tooltip("Ignore the spline's roll rotation and ensure the geometry stays flat")]
            public bool ignoreRoll = false;

            [Space]
            
            [Tooltip("Note that offsetting can cause vertices to sort of bunch up." +
                     "\n\nFor the best results, create a separate spline parallel to the one you are trying to offset from.")]
            public Vector2 offset;
            public Vector3 scale = Vector3.one;
        }
        public Deforming deforming = new Deforming();

        [Serializable]
        public class Conforming
        {
            [Tooltip("Project the spline curve into the geometry underneath it. Relies on physics raycasts.")]
            public bool enable;

            [Space]

            [Tooltip("A ray is shot this high above every vertex, and reach this much units below it." +
                     "\n\n" +
                     "If a spline is dug into the terrain too much, increase this value to still get valid raycast hits." +
                     "\n\n" +
                     "Internally, the minimum distance is always higher than the mesh's total height.")]
            public float seekDistance = 5f;
            
            [Header("Filtering")]
            [Tooltip("Ignore raycast hits from colliders that aren't from a Terrain")]
            public bool terrainOnly;
            [Tooltip("Only accept raycast hits from colliders on these layers")]
            public LayerMask layerMask = -1;
            [Space]

            [Header("Operations")]
            [Tooltip("Rotate the geometry to match the orientation of the surface beneath it")]
            public bool align = true;
            [Tooltip("Reorient the geometry normals to match the surface hit, for correct lighting")]
            public bool blendNormal = true;
        }
        public Conforming conforming = new Conforming();
    }
}