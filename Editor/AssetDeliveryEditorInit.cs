using UnityEditor;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.AssetDelivery.Editor
{
    /// <summary>
    /// Auto-registers the <c>BIZSIM_ASSETDELIVERY_INSTALLED</c> scripting define at editor load,
    /// so consumer shared code can use <c>#if BIZSIM_ASSETDELIVERY_INSTALLED</c> guards without
    /// manual Player Settings edits. Runs once per editor session via <see cref="InitializeOnLoadAttribute"/>.
    /// </summary>
    [InitializeOnLoad]
    internal static class AssetDeliveryEditorInit
    {
        static AssetDeliveryEditorInit()
        {
            BizSimDefineManager.AddDefine("BIZSIM_ASSETDELIVERY_INSTALLED",
                BizSimDefineManager.GetRelevantPlatforms());
        }
    }
}
