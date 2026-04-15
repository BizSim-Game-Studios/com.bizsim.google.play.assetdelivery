using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Custom inspector for <see cref="AssetDeliveryController"/>. Adds QA / testing buttons
    /// that are only active in play mode.
    /// </summary>
    [CustomEditor(typeof(AssetDeliveryController))]
    internal sealed class AssetDeliveryControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("QA / Testing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These buttons are play-mode only. Use them to inspect and clear tracked " +
                "pack state during manual QA without restarting the editor.",
                MessageType.Info);

            if (!Application.isPlaying)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUILayout.Button("Clear Tracked States (play mode only)");
                    GUILayout.Button("List Tracked Pack Names (play mode only)");
                }
                return;
            }

            var controller = (AssetDeliveryController)target;

            if (GUILayout.Button("Clear Tracked States"))
            {
                // TrackedStates is IReadOnlyDictionary — clearing requires ClearAllPacksAsync.
                Debug.Log(BizSimLogger.Prefix +
                    "Tracked states are cleared by ClearAllPacksAsync(). " +
                    "Restart play mode or call ClearAllPacksAsync() from code to fully reset.");
            }

            if (GUILayout.Button("List Tracked Pack Names"))
            {
                if (controller == null)
                {
                    Debug.LogWarning(BizSimLogger.Prefix + "Controller is null.");
                    return;
                }
                var states = controller.TrackedStates;
                if (states.Count == 0)
                {
                    Debug.Log(BizSimLogger.Prefix + "No tracked packs.");
                    return;
                }
                foreach (var kv in states)
                    Debug.Log(BizSimLogger.Prefix +
                        $"Pack '{kv.Key}': Status={kv.Value.Status}, Error={kv.Value.ErrorCode}");
            }
        }
    }
}
