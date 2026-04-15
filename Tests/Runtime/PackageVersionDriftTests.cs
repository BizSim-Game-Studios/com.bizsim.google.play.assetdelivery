#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor.PackageManager;
using BizSim.Google.Play.AssetDelivery;

namespace BizSim.Google.Play.AssetDelivery.Tests
{
    public class PackageVersionDriftTests
    {
        [Test]
        public void PackageVersion_Current_MatchesPackageJson()
        {
            var asm = typeof(PackageVersion).Assembly;
            var pkgInfo = PackageInfo.FindForAssembly(asm);
            Assert.NotNull(pkgInfo,
                "PackageInfo.FindForAssembly returned null — is BizSim.Google.Play.AssetDelivery installed via UPM?");

            var pkgJsonPath = Path.Combine(pkgInfo.resolvedPath, "package.json");
            Assert.IsTrue(File.Exists(pkgJsonPath), $"package.json not found at {pkgJsonPath}");

            var pkgJson = File.ReadAllText(pkgJsonPath);
            var m = Regex.Match(pkgJson, "\"version\"\\s*:\\s*\"([^\"]+)\"");
            Assert.IsTrue(m.Success, "package.json has no version field");
            Assert.AreEqual(m.Groups[1].Value, PackageVersion.Current,
                "PackageVersion.Current drifted from package.json — bump skill missed a file?");
        }
    }
}
#endif
