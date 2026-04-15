using System.IO;
using NUnit.Framework;
using UnityEditor.PackageManager;
using UnityEditor;

namespace BizSim.Google.Play.AssetDelivery.EditorTests
{
    public class AsmdefPlatformConstraintTest
    {
        private static string PackageRoot()
        {
            // Works for both file:-installed and Git-URL-installed packages.
            var pkg = PackageInfo.FindForAssembly(typeof(AsmdefPlatformConstraintTest).Assembly);
            Assert.IsNotNull(pkg, "Cannot locate package from test assembly. " +
                                  "Ensure test asmdef references BizSim.Google.Play.AssetDelivery.");
            return pkg.resolvedPath;
        }

        [Test]
        public void RuntimeAsmdef_HasUnrestrictedIncludePlatforms_ADR014()
        {
            var path = Path.Combine(PackageRoot(), "Runtime", "BizSim.Google.Play.AssetDelivery.asmdef");
            var json = File.ReadAllText(path);
            StringAssert.Contains("\"includePlatforms\": []", json.Replace(" ", ""),
                "ADR-014: Runtime asmdef MUST have includePlatforms: [] (empty). " +
                "[\"Android\",\"Editor\"] breaks Addressables content build on Assembly-CSharp consumers. " +
                "See 05-adrs.md ADR-014 for the 2026-04-15 hot-fix postmortem.");
        }

        [Test]
        public void UniTaskSubAsmdef_HasUnrestrictedIncludePlatforms_ADR014()
        {
            var path = Path.Combine(PackageRoot(), "Runtime", "UniTaskSupport",
                "BizSim.Google.Play.AssetDelivery.UniTask.asmdef");
            var json = File.ReadAllText(path);
            StringAssert.Contains("\"includePlatforms\": []", json.Replace(" ", ""),
                "ADR-014: UniTask sub-asmdef MUST also have includePlatforms: [] — " +
                "BIZSIM_UNITASK define constraint is the only gating mechanism.");
        }

        [Test]
        public void EditorAsmdef_HasEditorOnlyIncludePlatforms()
        {
            var path = Path.Combine(PackageRoot(), "Editor",
                "BizSim.Google.Play.AssetDelivery.Editor.asmdef");
            var json = File.ReadAllText(path);
            StringAssert.Contains("\"includePlatforms\": [\"Editor\"]", json.Replace(" ", ""),
                "Editor asmdef is genuinely editor-only (no Player-build code path). " +
                "ADR-014 only relaxes Runtime asmdefs; Editor stays gated.");
        }
    }
}
