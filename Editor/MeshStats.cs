using System;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace Nyako.MeshStats
{
    [InitializeOnLoad]
    internal static class MeshStats
    {
        private const string HarmonyId = "cat.nyako.mesh-stats";

        static MeshStats()
        {
            var harmony = new Harmony(HarmonyId);
            try
            {
                var editor = AccessTools.TypeByName("UnityEditor.SkinnedMeshRendererEditor");
                var target = AccessTools.DeclaredMethod(editor, "OnInspectorGUI", Type.EmptyTypes)
                             ?? throw new MissingMethodException("SkinnedMeshRendererEditor", "OnInspectorGUI");
                harmony.Patch(target, postfix: new HarmonyMethod(typeof(MeshStats), nameof(Draw)));
                AssemblyReloadEvents.beforeAssemblyReload += () => harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                harmony.UnpatchAll(HarmonyId);
                Debug.LogWarning("Mesh Stats unavailable: " + ex.Message);
            }
        }

        private static void Draw(Editor __instance)
        {
            var mesh = (__instance.target as SkinnedMeshRenderer)?.sharedMesh;
            if (mesh == null) return;

            var submeshCount = mesh.subMeshCount;
            long triCount = 0;
            for (var i = 0; i < submeshCount; i++) triCount += Triangles(mesh, i);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Mesh Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Triangles: {triCount:N0}");
            EditorGUILayout.LabelField($"Vertices: {mesh.vertexCount:N0}");
            EditorGUILayout.LabelField($"Submeshes: {submeshCount}");

            if (submeshCount > 1)
            {
                EditorGUILayout.Space(2);
                for (var i = 0; i < submeshCount; i++)
                    EditorGUILayout.LabelField($"  Submesh {i}: {Triangles(mesh, i):N0} tris");
            }

            EditorGUILayout.EndVertical();
        }

        // Index counts avoid copying the whole triangle array every repaint
        private static long Triangles(Mesh mesh, int submesh) =>
            mesh.GetTopology(submesh) == MeshTopology.Triangles ? (long)mesh.GetIndexCount(submesh) / 3 : 0;
    }
}
