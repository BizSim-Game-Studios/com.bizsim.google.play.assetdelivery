using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Pack Inspector EditorWindow (ADR-024, §C1). Displays a live scrollable table of all
    /// declared packs with state, progress, and QA actions. Refreshes at 8 Hz when focused,
    /// 1 Hz when unfocused.
    ///
    /// Feature Interaction I8 — Inject Error isolation:
    ///   Enabled only when active provider is Mock or LocalHttp.
    ///   Before injection sets AssetDeliveryController.SuppressAnalytics = true (one-shot).
    ///   Retry loop short-circuits when SuppressAnalytics is set.
    /// </summary>
    public sealed class AssetDeliveryPackInspector : EditorWindow
    {
        // Refresh rates
        private const double FocusedHz   = 8.0;
        private const double UnfocusedHz = 1.0;

        // Error injection dropdown state
        private int[]  _injectErrorIndex;
        private bool   _initialized;
        private double _nextRepaint;

        private Vector2 _scroll;

        // Columns: Stable Pack ID | Display Name | Delivery Mode | Current State | Progress %
        //          | Bytes (cur/total) | Last Error | Actions
        private static readonly string[] Columns =
        {
            "StablePackId", "Display Name", "Mode", "State", "Progress",
            "Bytes", "Last Error", "Actions"
        };

        [MenuItem("BizSim/Google Play/Asset Delivery/Pack Inspector", false, 200)]
        public static void ShowWindow()
        {
            var w = GetWindow<AssetDeliveryPackInspector>("AD Pack Inspector");
            w.minSize = new Vector2(900, 320);
            w.Show();
        }

        private void OnEnable()
        {
            _initialized = false;
            _nextRepaint = EditorApplication.timeSinceStartup;
        }

        private void Update()
        {
            double hz    = focusedWindow == this ? FocusedHz : UnfocusedHz;
            double delta = 1.0 / hz;
            if (EditorApplication.timeSinceStartup >= _nextRepaint)
            {
                _nextRepaint = EditorApplication.timeSinceStartup + delta;
                Repaint();
            }
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawTable();
        }

        // ─── Drawing ──────────────────────────────────────────────────────────────

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUILayout.LabelField("Pack Inspector", EditorStyles.boldLabel,
                    GUILayout.Width(120));
                GUILayout.FlexibleSpace();

                bool isPlaying = Application.isPlaying;
                string providerLabel = isPlaying
                    ? AssetDeliveryController.Instance?.ProviderKind.ToString() ?? "n/a"
                    : "editor (not playing)";
                EditorGUILayout.LabelField($"Provider: {providerLabel}", GUILayout.Width(220));
            }
        }

        private void DrawTable()
        {
            var settings = AssetDatabase.LoadAssetAtPath<AssetDeliverySettings>(
                AssetDeliverySettings.AssetDatabasePath);

            if (settings == null || settings.PackDescriptors == null ||
                settings.PackDescriptors.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No PackDescriptors declared in AssetDeliverySettings.asset. " +
                    "Open BizSim → Google Play → Asset Delivery → Configuration to add pack descriptors.",
                    MessageType.Info);
                return;
            }

            int count = settings.PackDescriptors.Length;

            // Lazily init per-pack error injection dropdowns.
            if (!_initialized || _injectErrorIndex == null || _injectErrorIndex.Length != count)
            {
                _injectErrorIndex = new int[count];
                _initialized      = true;
            }

            // Column header
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var col in Columns)
                    EditorGUILayout.LabelField(col, EditorStyles.boldLabel, GUILayout.Width(ColumnWidth(col)));
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < count; i++)
            {
                var desc = settings.PackDescriptors[i];
                DrawPackRow(i, desc);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPackRow(int idx, AssetPackDescriptor desc)
        {
            // Read live state if in play mode.
            AssetPackState state = default;
            bool hasLiveState    = false;
            if (Application.isPlaying && AssetDeliveryController.Instance != null)
            {
                var tracked = AssetDeliveryController.Instance.TrackedStates;
                if (tracked.TryGetValue(desc.StablePackId, out var s))
                {
                    state        = s;
                    hasLiveState = true;
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(desc.StablePackId, GUILayout.Width(ColumnWidth("StablePackId")));
                EditorGUILayout.LabelField(desc.DisplayName, GUILayout.Width(ColumnWidth("Display Name")));
                EditorGUILayout.LabelField(desc.DeliveryMode.ToString(), GUILayout.Width(ColumnWidth("Mode")));
                EditorGUILayout.LabelField(hasLiveState ? state.Status.ToString() : "-",
                    GUILayout.Width(ColumnWidth("State")));
                EditorGUILayout.LabelField(hasLiveState ? $"{state.TransferProgressPercentage}%" : "-",
                    GUILayout.Width(ColumnWidth("Progress")));
                string bytes = hasLiveState
                    ? $"{state.BytesDownloaded / 1048576f:F1}M / {state.TotalBytesToDownload / 1048576f:F1}M"
                    : "-";
                EditorGUILayout.LabelField(bytes, GUILayout.Width(ColumnWidth("Bytes")));
                EditorGUILayout.LabelField(hasLiveState ? state.ErrorCode.ToString() : "-",
                    GUILayout.Width(ColumnWidth("Last Error")));

                DrawActionsCell(idx, desc);
            }
        }

        private void DrawActionsCell(int idx, AssetPackDescriptor desc)
        {
            bool canInject = CanInjectErrors();

            if (GUILayout.Button("Force Fetch", GUILayout.Width(85)))
            {
                if (Application.isPlaying && AssetDeliveryController.Instance != null)
                    _ = AssetDeliveryController.Instance.FetchAsync(desc.StablePackId);
                else
                    Debug.LogWarning(BizSimLogger.Prefix + "Force Fetch requires play mode.");
            }

            if (GUILayout.Button("Force Remove", GUILayout.Width(95)))
            {
                if (Application.isPlaying && AssetDeliveryController.Instance != null)
                    _ = AssetDeliveryController.Instance.RemovePackAsync(
                        desc.StablePackId,
                        new RemovePackOptions(forceUnload: true));
                else
                    Debug.LogWarning(BizSimLogger.Prefix + "Force Remove requires play mode.");
            }

            using (new EditorGUI.DisabledScope(!canInject))
            {
                string errorLabel = GetErrorLabel(_injectErrorIndex[idx]);
                _injectErrorIndex[idx] = EditorGUILayout.Popup(
                    _injectErrorIndex[idx],
                    GetErrorCodeLabels(),
                    GUILayout.Width(120));

                string injectTooltip = canInject
                    ? "Inject the selected error code into the mock provider"
                    : "Inject Error requires Mock or LocalHttp provider";

                if (GUILayout.Button(new GUIContent("Inject Error", injectTooltip), GUILayout.Width(90)))
                {
                    InjectError(desc.StablePackId, _injectErrorIndex[idx]);
                }
            }
        }

        // ─── Error injection ──────────────────────────────────────────────────────

        private static bool CanInjectErrors()
        {
            if (!Application.isPlaying) return false;
            var inst = AssetDeliveryController.Instance;
            if (inst == null) return false;
            return inst.ProviderKind == ProviderKind.Mock || inst.ProviderKind == ProviderKind.LocalHttp;
        }

        private static void InjectError(string packName, int errorIndex)
        {
            var inst = AssetDeliveryController.Instance;
            if (inst == null) { Debug.LogWarning(BizSimLogger.Prefix + "Controller is null."); return; }

            var codes  = GetAllErrorCodes();
            if (errorIndex < 0 || errorIndex >= codes.Length) return;
            var code   = codes[errorIndex];

            // I8: set SuppressAnalytics one-shot before injection.
#if UNITY_EDITOR
            inst.SuppressAnalytics = true;
#endif

            var state = new AssetPackState(packName, AssetPackStatus.Failed,
                code, 0, 0, 0, DateTime.UtcNow);

            // Route via public state-change path.
            inst.HandleStateFromListener(packName, state);

            Debug.Log(BizSimLogger.Prefix +
                $"Injected error {code} for pack '{packName}'. " +
                "Analytics suppressed for this transition (I8).");
        }

        // ─── Error code helpers ───────────────────────────────────────────────────

        private static AssetPackErrorCode[] _allCodes;
        private static string[]             _allCodeLabels;

        private static AssetPackErrorCode[] GetAllErrorCodes()
        {
            if (_allCodes == null)
                _allCodes = (AssetPackErrorCode[])Enum.GetValues(typeof(AssetPackErrorCode));
            return _allCodes;
        }

        private static string[] GetErrorCodeLabels()
        {
            if (_allCodeLabels != null) return _allCodeLabels;
            var codes  = GetAllErrorCodes();
            _allCodeLabels = new string[codes.Length];
            for (int i = 0; i < codes.Length; i++)
                _allCodeLabels[i] = $"{(int)codes[i]}: {codes[i]}";
            return _allCodeLabels;
        }

        private static string GetErrorLabel(int index)
        {
            var codes = GetAllErrorCodes();
            if (index < 0 || index >= codes.Length) return "?";
            return codes[index].ToString();
        }

        // ─── Column widths ────────────────────────────────────────────────────────

        private static float ColumnWidth(string col)
        {
            switch (col)
            {
                case "StablePackId":  return 130;
                case "Display Name":  return 110;
                case "Mode":          return 80;
                case "State":         return 90;
                case "Progress":      return 60;
                case "Bytes":         return 130;
                case "Last Error":    return 100;
                case "Actions":       return 420;
                default:              return 80;
            }
        }
    }
}
