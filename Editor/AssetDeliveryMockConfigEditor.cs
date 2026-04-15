using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="AssetDeliveryMockConfig"/>. Organizes the packs array
    /// with foldouts and a per-pack summary header.
    /// </summary>
    [CustomEditor(typeof(AssetDeliveryMockConfig))]
    internal sealed class AssetDeliveryMockConfigEditor : UnityEditor.Editor
    {
        private bool   _globalFoldout = true;
        private bool[] _packFoldouts  = System.Array.Empty<bool>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _globalFoldout = EditorGUILayout.Foldout(_globalFoldout, "Global Settings", true);
            if (_globalFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("GetStatesLatencyMs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ConfirmationDialogLatencyMs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("SuppressAnalytics"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Simulated Packs", EditorStyles.boldLabel);

            var packsProperty = serializedObject.FindProperty("Packs");
            int count = packsProperty.arraySize;

            // Ensure foldout array is sized correctly.
            if (_packFoldouts.Length != count)
            {
                var old = _packFoldouts;
                _packFoldouts = new bool[count];
                for (int i = 0; i < Mathf.Min(old.Length, count); i++)
                    _packFoldouts[i] = old[i];
                for (int i = old.Length; i < count; i++)
                    _packFoldouts[i] = true;
            }

            for (int i = 0; i < count; i++)
            {
                var pack = packsProperty.GetArrayElementAtIndex(i);
                var nameProp = pack.FindPropertyRelative("Name");
                string label = string.IsNullOrEmpty(nameProp.stringValue)
                    ? $"Pack [{i}]"
                    : nameProp.stringValue;

                EditorGUILayout.BeginHorizontal();
                _packFoldouts[i] = EditorGUILayout.Foldout(_packFoldouts[i], label, true);
                if (GUILayout.Button("-", GUILayout.Width(24)))
                {
                    packsProperty.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                if (_packFoldouts[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("Name"));
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("FinalStatus"));
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("DownloadDurationSeconds"));
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("TotalBytesToDownload"));
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("Error"));
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("StopAt"));
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("SimulatedBundleFileNames"), true);
                    EditorGUILayout.PropertyField(pack.FindPropertyRelative("IsInstallTime"));
                    EditorGUI.indentLevel--;
                }
            }

            if (GUILayout.Button("Add Pack"))
            {
                packsProperty.arraySize++;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
