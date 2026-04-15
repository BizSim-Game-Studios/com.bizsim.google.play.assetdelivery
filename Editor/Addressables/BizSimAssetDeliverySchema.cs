// Copyright 2026 BizSim Game Studios. All rights reserved.
// Licensed under the MIT License — see LICENSE.md.
//
// BizSimAssetDeliverySchema — per-Addressables-group schema controlling how the
// group's content is routed to Google Play asset packs.

#if BIZSIM_ADDRESSABLES
using System.ComponentModel;
using System.Text.RegularExpressions;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Delivery mode for a BizSim Addressables group.
    /// Maps directly to the Google Play Asset Delivery delivery types.
    /// </summary>
    public enum BizSimAddressablesDeliveryMode
    {
        /// <summary>Pack is installed at the same time as the app. No download step needed.</summary>
        InstallTime = 0,
        /// <summary>Pack downloads automatically in the background shortly after install.</summary>
        FastFollow  = 1,
        /// <summary>Pack is downloaded explicitly on demand.</summary>
        OnDemand    = 2,
        /// <summary>Group content is NOT routed through BizSim Asset Delivery packs.</summary>
        None        = 3,
    }

    /// <summary>
    /// Addressables group schema that assigns a group's bundled content to a
    /// Google Play asset pack via the <c>BizSim Asset Delivery</c> build script.
    /// <para>
    /// Add this schema to any Addressables group whose bundles should be served
    /// as a Google Play asset pack. Then select
    /// <b>Addressables Groups → Build → New Build → BizSim Asset Delivery</b>.
    /// </para>
    /// </summary>
    [DisplayName("BizSim Asset Delivery")]
    public sealed class BizSimAssetDeliverySchema : AddressableAssetGroupSchema
    {
        // ─── Serialized fields ────────────────────────────────────────────────────

        [SerializeField]
        private BizSimAddressablesDeliveryMode _deliveryMode = BizSimAddressablesDeliveryMode.OnDemand;

        [SerializeField]
        private string _packName = "";

        [SerializeField]
        private bool _usePlayCoreFetch = true;

        // ─── Public properties ────────────────────────────────────────────────────

        /// <summary>
        /// Delivery mode for the asset pack that will contain this group's content.
        /// Defaults to <see cref="BizSimAddressablesDeliveryMode.OnDemand"/>.
        /// </summary>
        public BizSimAddressablesDeliveryMode DeliveryMode
        {
            get => _deliveryMode;
            set
            {
                if (_deliveryMode == value) return;
                _deliveryMode = value;
                SetDirty(true);
            }
        }

        /// <summary>
        /// Gradle asset pack name for this group's content.
        /// Defaults to the group name sanitized to a valid pack identifier (letters, digits, underscores).
        /// Must start with a letter.
        /// </summary>
        public string PackName
        {
            get => _packName;
            set
            {
                if (_packName == value) return;
                _packName = value;
                SetDirty(true);
            }
        }

        /// <summary>
        /// When <c>true</c> (default), on-demand and fast-follow packs are fetched through
        /// <see cref="AssetDeliveryController"/> (stall detector, retry, cellular consent, analytics).
        /// When <c>false</c>, the provider falls through to the base <c>AssetBundleProvider</c>
        /// — useful for packs the consumer manages manually.
        /// </summary>
        public bool UsePlayCoreFetch
        {
            get => _usePlayCoreFetch;
            set
            {
                if (_usePlayCoreFetch == value) return;
                _usePlayCoreFetch = value;
                SetDirty(true);
            }
        }

        // ─── Derived helpers ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns a sanitized, valid Gradle asset pack name derived from <paramref name="groupName"/>.
        /// Strips non-alphanumeric/underscore characters and prepends "Group" if the result starts with a digit.
        /// </summary>
        public static string SanitizePackName(string groupName)
        {
            if (string.IsNullOrEmpty(groupName)) return "Group";
            var sanitized = Regex.Replace(groupName, "[^A-Za-z0-9_]", "");
            if (sanitized.Length == 0) return "Group";
            if (!char.IsLetter(sanitized[0])) sanitized = "Group" + sanitized;
            return sanitized;
        }

        /// <summary>
        /// Returns <see cref="PackName"/> if non-empty, otherwise the sanitized group name.
        /// </summary>
        public string ResolvedPackName(string groupName)
            => string.IsNullOrEmpty(_packName) ? SanitizePackName(groupName) : _packName;

        // ─── Inspector ────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public override void OnGUI()
        {
            var so = new UnityEditor.SerializedObject(this);
            so.Update();

            UnityEditor.EditorGUILayout.PropertyField(
                so.FindProperty("_deliveryMode"),
                new GUIContent("Delivery Mode", "Google Play delivery type for this group's asset pack."));
            UnityEditor.EditorGUILayout.PropertyField(
                so.FindProperty("_packName"),
                new GUIContent("Pack Name", "Gradle asset pack name. Leave blank to auto-derive from group name."));
            UnityEditor.EditorGUILayout.PropertyField(
                so.FindProperty("_usePlayCoreFetch"),
                new GUIContent("Route via BizSim Controller",
                    "When true, on-demand packs use AssetDeliveryController (enterprise features). " +
                    "When false, the base AssetBundleProvider handles the load."));

            so.ApplyModifiedProperties();
        }

        /// <inheritdoc/>
        public override void OnGUIMultiple(System.Collections.Generic.List<AddressableAssetGroupSchema> otherSchemas)
            => OnGUI();
    }
}
#endif // BIZSIM_ADDRESSABLES
