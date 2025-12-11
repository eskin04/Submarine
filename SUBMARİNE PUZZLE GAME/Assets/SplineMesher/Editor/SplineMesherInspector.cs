// Staggart Creations (http://staggart.xyz)
// Copyright protected under Unity Asset Store EULA
// Copying or referencing source code for the production of new asset store, or public content, is strictly prohibited!

//If disabled rebuilding occurs after all editor UI has repainted
//Issues fewer rebuild commands, but appears to be choppy
//#define SMOOTH_REBUILD

using System;
using System.Collections.Generic;
using System.Reflection;
using sc.modeling.splines.runtime;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEngine;

#if SPLINES
using UnityEditor.Splines;
using UnityEngine.Splines;
#endif

namespace sc.modeling.splines.editor
{
    [CustomEditor(typeof(SplineMesher))]
    [CanEditMultipleObjects]
    public class SplineMesherInspector : Editor
    {
        private SerializedProperty splineContainer;
        
        private SerializedProperty sourceMesh;
        private SerializedProperty rotation;

        private SerializedProperty outputObject;
        private SerializedProperty rebuildTriggers;

        private SerializedProperty enableCollider;
        private SerializedProperty colliderType;
        private SerializedProperty colliderBoxSubdivisions;
        private SerializedProperty collisionMesh;
        
        //Distribution
        private SerializedProperty evenOnly;
        private SerializedProperty trimStart;
        private SerializedProperty trimEnd;
        private SerializedProperty spacing;
        
        //Deforming
        private SerializedProperty ignoreRoll;
        private SerializedProperty offset;
        private SerializedProperty scale;
        
        //Conforming
        private SerializedProperty enableConforming;
        private SerializedProperty seekDistance;
        private SerializedProperty terrainOnly;
        private SerializedProperty layerMask;
        private SerializedProperty align;
        private SerializedProperty blendNormal;
        
        private SerializedProperty onPreRebuild, onPostRebuild;

        private bool requiresRebuild = false;
        [NonSerialized]
        private bool requiresLightmapUV;
        
        private static bool ExpandSetup
        {
            get => SessionState.GetBool("SM_EXPAND_SETUP", true);
            set => SessionState.SetBool("SM_EXPAND_SETUP", value);
        }
        private static bool PreviewMesh
        {
            get => SessionState.GetBool("SM_PREVIEW_MESH", true);
            set => SessionState.SetBool("SM_PREVIEW_MESH", value);
        }
        private MeshPreview sourceMeshPreview;
        private PreviewRenderUtility meshPreviewUtility;
        
        private UI.Section colliderSection;
        private UI.Section distributionSection;
        private UI.Section deformingSection;
        private UI.Section conformingSection;
        private UI.Section eventsSection;
        private List<UI.Section> sections = new List<UI.Section>();
        
        private void OnEnable()
        {
            sections = new List<UI.Section>();
            sections.Add(colliderSection = new UI.Section(this, "COLLIDER", new GUIContent("Collider")));
            sections.Add(distributionSection = new UI.Section(this, "DIST", new GUIContent("Distribution")));
            sections.Add(deformingSection = new UI.Section(this, "DEFORMING", new GUIContent("Deforming")));
            sections.Add(conformingSection = new UI.Section(this, "CONFORMING", new GUIContent("Conforming")));
            sections.Add(eventsSection = new UI.Section(this, "EVENTS", new GUIContent("Events")));
            
            #if SPLINES
            splineContainer = serializedObject.FindProperty("splineContainer");
            #endif
            
            sourceMesh = serializedObject.FindProperty("sourceMesh");
            rotation = serializedObject.FindProperty("rotation");
            
            outputObject = serializedObject.FindProperty("outputObject");
            rebuildTriggers = serializedObject.FindProperty("rebuildTriggers");
 
            enableCollider = serializedObject.FindProperty("enableCollider");
            colliderType = serializedObject.FindProperty("colliderType");
            colliderBoxSubdivisions = serializedObject.FindProperty("colliderBoxSubdivisions");
            collisionMesh = serializedObject.FindProperty("collisionMesh");

            SerializedProperty settings = serializedObject.FindProperty("settings");
            {
                SerializedProperty settingsDistribution = settings.FindPropertyRelative("distribution");
                evenOnly = settingsDistribution.FindPropertyRelative("evenOnly");
                trimStart = settingsDistribution.FindPropertyRelative("trimStart");
                trimEnd = settingsDistribution.FindPropertyRelative("trimEnd");
                spacing = settingsDistribution.FindPropertyRelative("spacing");
                
                SerializedProperty settingsDeforming = settings.FindPropertyRelative("deforming");
                ignoreRoll = settingsDeforming.FindPropertyRelative("ignoreRoll");
                offset = settingsDeforming.FindPropertyRelative("offset");
                scale = settingsDeforming.FindPropertyRelative("scale");
                
                SerializedProperty settingsConforming = settings.FindPropertyRelative("conforming");
                enableConforming = settingsConforming.FindPropertyRelative("enable");
                seekDistance = settingsConforming.FindPropertyRelative("seekDistance");
                terrainOnly = settingsConforming.FindPropertyRelative("terrainOnly");
                layerMask = settingsConforming.FindPropertyRelative("layerMask");
                align = settingsConforming.FindPropertyRelative("align");
                blendNormal = settingsConforming.FindPropertyRelative("blendNormal");
            }
            
            onPreRebuild = serializedObject.FindProperty("onPreRebuild");
            onPostRebuild = serializedObject.FindProperty("onPostRebuild");

            Undo.undoRedoPerformed += OnUndoRedo;
            
            //Upgrade to v1.0.1
            ((SplineMesher)target).ValidateOutput();
            
            sourceMeshPreview = new MeshPreview(new Mesh());

            //Override zoom level
            meshPreviewUtility = (PreviewRenderUtility)typeof(MeshPreview).GetField("m_PreviewUtility", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(sourceMeshPreview);
            meshPreviewUtility.camera.fieldOfView = 15;
            meshPreviewUtility.camera.backgroundColor = Color.white * 0.09f;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            
            if (sourceMeshPreview != null)
            {
                sourceMeshPreview.Dispose();
                sourceMeshPreview = null;
            }
        }

        private void OnUndoRedo()
        {
            Rebuild();
        }

        private static string iconPrefix => EditorGUIUtility.isProSkin ? "d_" : string.Empty;
        
        public override void OnInspectorGUI()
        {
            #if !SPLINES || !MATHEMATICS
            #if !SPLINES
            EditorGUILayout.HelpBox("The Spline package isn't installed, please install this through the Package Manager", MessageType.Error);
            #endif
            #if !MATHEMATICS
            EditorGUILayout.HelpBox("The Mathematics package isn't installed or outdated, please install this through the Package Manager", MessageType.Error);
            #endif
            
            return;
            #else
            //Reset
            requiresRebuild = false;
            requiresLightmapUV = SplineMeshEditor.RequiresLightmapUV(((SplineMesher)target));

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Version {SplineMesher.VERSION} " + (SplineMeshEditor.VersionChecking.UPDATE_AVAILABLE ? "(update available)" : "(latest)"), EditorStyles.centeredGreyMiniLabel);
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(iconPrefix + "Help").image, "Help window"), EditorStyles.miniButtonMid, GUILayout.Width(30f)))
                {
                    HelpWindow.ShowWindow();
                }
                
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent(iconPrefix + "Settings").image, "Utility functions"), EditorStyles.miniButtonMid, GUILayout.Width(30f)))
                {
                    GenericMenu menu = new GenericMenu();

                    SplineMesher component = (SplineMesher)target;
                    
                    menu.AddItem(new GUIContent("Clear scale data"), false, () =>
                    {
                        component.ResetScaleData();
                        EditorUtility.SetDirty(component);
                    });
                    menu.AddItem(new GUIContent("Reverse spline"), false, () => component.ReverseSpline());
                    menu.AddItem(new GUIContent("Generate lightmap UVs"), false, () => SplineMeshEditor.GenerateLightmapUV(component));
                    
                    menu.ShowAsContext();
                }
            }

            EditorGUI.BeginChangeCheck();
            serializedObject.Update();

            //ExpandSetup = UI.DrawHeader(new GUIContent("Setup"), ExpandSetup);
            //EditorGUILayout.BeginFadeGroup(ExpandSetup ? 1f : 0.001f);
            {
                //EditorGUILayout.Space();

                EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    #if SPLINES
                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUILayout.PropertyField(splineContainer);
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (splineContainer.objectReferenceValue)
                        {
                            foreach (var target in targets)
                            {
                                ((SplineMesher)target).ValidateData(splineContainer.objectReferenceValue as SplineContainer);
                            }

                            requiresRebuild = true;
                        }
                    }

                    EditorGUI.BeginDisabledGroup(splineContainer.objectReferenceValue == null);
                    if (GUILayout.Button("Edit", EditorStyles.miniButton, GUILayout.Width(50f)))
                    {
                        Selection.activeGameObject = ((SplineMesher)target).splineContainer.gameObject;
                        EditorApplication.delayCall += ToolManager.SetActiveContext<SplineToolContext>;
                    }

                    EditorGUI.EndDisabledGroup();
                    #endif
                }
                if (splineContainer.objectReferenceValue == false)
                {
                    EditorGUILayout.HelpBox("A source Spline Container must be assigned", MessageType.Error);
                }
                
                if (splineContainer.objectReferenceValue)
                {
                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(sourceMesh);
                        if (EditorGUI.EndChangeCheck()) requiresRebuild = true;

                        if (sourceMesh.objectReferenceValue)
                        {
                            PreviewMesh = GUILayout.Toggle(PreviewMesh,
                                new GUIContent(EditorGUIUtility.IconContent(iconPrefix + (PreviewMesh ? "animationvisibilitytoggleon" : "animationvisibilitytoggleoff")).image,
                                    "Toggle mesh inspector"), "Button", GUILayout.MaxWidth(40f));
                        }
                    }

                    if (sourceMesh.objectReferenceValue == false)
                    {
                        EditorGUILayout.HelpBox("An input mesh must be assigned", MessageType.Error);
                    }
                    else
                    {
                        if (PreviewMesh)
                        {
                            Mesh mesh = (Mesh)sourceMesh.objectReferenceValue;

                            if (sourceMeshPreview.mesh != mesh) sourceMeshPreview.mesh = mesh;
                            Rect previewRect = EditorGUILayout.GetControlRect(false, 150f);

                            if (previewRect.Contains(Event.current.mousePosition))
                            {
                                sourceMeshPreview.OnPreviewGUI(previewRect, GUIStyle.none);
                            }
                            else
                            {
                                if (Event.current.type == EventType.Repaint)
                                {
                                    GUI.DrawTexture(previewRect, sourceMeshPreview.RenderStaticPreview((int)previewRect.width, (int)previewRect.height));
                                }
                            }
                            previewRect.y += previewRect.height - 22f;
                            previewRect.x += 5f;
                            previewRect.height = 22f;

                            GUI.Label(previewRect, MeshPreview.GetInfoString(mesh), EditorStyles.miniLabel);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                sourceMeshPreview.OnPreviewSettings();
                            }
                            
                            EditorGUILayout.Space();
                        }
                        
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(rotation);
                        EditorGUI.indentLevel--;
                        if (EditorGUI.EndChangeCheck()) requiresRebuild = true;
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(outputObject);
                            if (EditorGUI.EndChangeCheck())
                            {
                                requiresLightmapUV = SplineMeshEditor.RequiresLightmapUV(((SplineMesher)target));
                                requiresRebuild = true;
                            }
                            
                            if (GUILayout.Button("This", EditorStyles.miniButton, GUILayout.Width(50f)))
                            {
                                SplineMesher component = ((SplineMesher)target);
                                
                                outputObject.objectReferenceValue = component.gameObject;
                                
                                if (outputObject.objectReferenceValue.GetHashCode() != component.gameObject.GetHashCode()) requiresRebuild = true;

                                SplineMeshEditor.SetupSplineRenderer(component.gameObject);
                            }
                        }
                        if (outputObject.objectReferenceValue == false)
                        {
                            EditorGUILayout.HelpBox("An output GameObject must be assigned", MessageType.Warning);
                        }
                        else
                        {
                            if (requiresLightmapUV)
                            {
                                EditorGUILayout.HelpBox("Invalid lightmap UVs, will be (re)generated, once light baking starts.", MessageType.None);
                            }
                        }
                    }
 
                    if (((int)rebuildTriggers.intValue & (int)SplineMesher.RebuildTriggers.OnStart) != (int)SplineMesher.RebuildTriggers.OnStart && (PrefabUtility.IsPartOfPrefabInstance((SplineMesher)target) ||PrefabStageUtility.GetCurrentPrefabStage()))
                    {
                        EditorGUILayout.HelpBox("Procedurally created geometry cannot be used in a prefab." +
                                                "\n\nMesh data will be lost when the prefab is used outside of the scene it was created in." +
                                                "\n\nExport the created mesh to an FBX file, and use that instead. Or enable the \"On Start()\" option under Rebuild Triggers.", MessageType.Warning);
                    }
                    
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(rebuildTriggers, new GUIContent("Rebuild triggers", rebuildTriggers.tooltip), GUILayout.Width(EditorGUIUtility.labelWidth + 140f));
                }
                
                EditorGUILayout.Space(10f);
            }
            //EditorGUILayout.EndFadeGroup();

            colliderSection.DrawHeader(() => SwitchSection(colliderSection));
            EditorGUILayout.BeginFadeGroup(colliderSection.anim.faded);
            {
                if (colliderSection.Expanded)
                {
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();
                    {
                        EditorGUILayout.PropertyField(enableCollider, new GUIContent("Enable", enableCollider.tooltip));

                        EditorGUILayout.Space();

                        if (enableCollider.boolValue)
                        {
                            EditorGUILayout.PropertyField(colliderType, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 80f));
                            EditorGUI.indentLevel++;
                            if (colliderType.intValue == (int)Settings.ColliderType.Mesh)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.PropertyField(collisionMesh);
                                    if (GUILayout.Button(new GUIContent("Same", "Use the same mesh for collision as the source mesh"), EditorStyles.miniButton, GUILayout.Width(50f)))
                                    {
                                        collisionMesh.objectReferenceValue = sourceMesh.objectReferenceValue;
                                    }
                                }
                            }
                            else if (colliderType.intValue == (int)Settings.ColliderType.Box)
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.PrefixLabel(new GUIContent("Subdivisions", colliderBoxSubdivisions.tooltip));
                                    using (new EditorGUI.DisabledScope(colliderBoxSubdivisions.intValue <= 0))
                                    {
                                        if (GUILayout.Button("-", EditorStyles.miniButtonLeft, GUILayout.Width(25f)))
                                        {
                                            colliderBoxSubdivisions.intValue--;
                                        }
                                    }
                                    GUILayout.Space(-15f);
                                    EditorGUILayout.PropertyField(colliderBoxSubdivisions, GUIContent.none, GUILayout.MaxWidth(40f));
                                    if (GUILayout.Button("+", EditorStyles.miniButtonRight, GUILayout.Width(25f)))
                                    {
                                        colliderBoxSubdivisions.intValue++;
                                    }
                                }
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                    if (EditorGUI.EndChangeCheck()) requiresRebuild = true;

                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndFadeGroup();
                
            distributionSection.DrawHeader(() => SwitchSection(distributionSection));
            EditorGUILayout.BeginFadeGroup(distributionSection.anim.faded);
            {
                if (distributionSection.Expanded)
                {
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(evenOnly);
                    
                    EditorGUILayout.PropertyField(trimStart, GUILayout.Width(EditorGUIUtility.labelWidth + 60f));
                    EditorGUILayout.PropertyField(trimEnd, GUILayout.Width(EditorGUIUtility.labelWidth + 60f));
                    
                    EditorGUILayout.PropertyField(spacing, GUILayout.Width(EditorGUIUtility.labelWidth + 60f));

                    if (EditorGUI.EndChangeCheck()) requiresRebuild = true;

                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndFadeGroup();

            deformingSection.DrawHeader(() => SwitchSection(deformingSection));
            EditorGUILayout.BeginFadeGroup(deformingSection.anim.faded);
            {
                if (deformingSection.Expanded)
                {
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(ignoreRoll);
                    EditorGUILayout.PropertyField(offset);
                    EditorGUILayout.PropertyField(scale);

                    if (EditorGUI.EndChangeCheck()) requiresRebuild = true;

                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndFadeGroup();

            conformingSection.DrawHeader(() => SwitchSection(conformingSection));
            EditorGUILayout.BeginFadeGroup(conformingSection.anim.faded);
            {
                if (conformingSection.Expanded)
                {
                    EditorGUILayout.Space();

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.PropertyField(enableConforming);

                    if (enableConforming.boolValue)
                    {
                        EditorGUILayout.PropertyField(seekDistance);
                        EditorGUILayout.PropertyField(terrainOnly);
                        EditorGUILayout.PropertyField(layerMask);
                        EditorGUILayout.PropertyField(align);
                        EditorGUILayout.PropertyField(blendNormal);
                    }

                    if (EditorGUI.EndChangeCheck()) requiresRebuild = true;

                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndFadeGroup();
                
            eventsSection.DrawHeader(() => SwitchSection(eventsSection));
            EditorGUILayout.BeginFadeGroup(eventsSection.anim.faded);
            {
                if (eventsSection.Expanded)
                {
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(onPreRebuild);
                    EditorGUILayout.PropertyField(onPostRebuild);

                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndFadeGroup();
                
            /*
            EditorGUILayout.Separator();
            
            EditorGUILayout.LabelField("FBX Export", EditorStyles.boldLabel);
            #if !FBX_EXPORTER
            EditorGUILayout.HelpBox("This functionality requires the FBX Exporter package to be installed", MessageType.Info);
            #else
            
            #endif
            */
            

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                if (requiresRebuild)
                {
                    Rebuild();
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Rebuild"))
                {
                    Rebuild();
                }
                GUILayout.FlexibleSpace();
            }
            
            #if SM_DEV
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{((SplineMesher)target).GetLastRebuildTime()}ms", EditorStyles.centeredGreyMiniLabel);
            }
            #endif
            #endif
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("- Staggart Creations -", EditorStyles.centeredGreyMiniLabel);
        }
        
        private void Rebuild()
        {
            requiresRebuild = false;

            #if !SMOOTH_REBUILD
            EditorApplication.delayCall += () =>
            #endif
            {
                foreach (var m_target in targets)
                {
                    ((SplineMesher)m_target).Rebuild();
                }
            };
        }
        
        private void SwitchSection(UI.Section targetSection)
        {
            //Classic foldout behaviour
            //targetSection.Expanded = !targetSection.Expanded;
            
            //Accordion behaviour
            foreach (var section in sections)
            {
                section.Expanded = (targetSection == section) && !section.Expanded;
                //section.Expanded = true;
            }
        }
    }
}