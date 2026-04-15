using NUnit.Framework;
using BizSim.Google.Play.AssetDelivery;

public class AssetPackNameValidatorTests
{
    [TestCase("level_1", true)]
    [TestCase("boss_arena_final", true)]
    [TestCase("a", true)]
    [TestCase("level_boss_1_final_revised", true)]
    [TestCase("Level1", false)]          // uppercase
    [TestCase("1level", false)]          // leading digit
    [TestCase("level-1", false)]         // hyphen
    [TestCase("", false)]                // empty
    [TestCase(null, false)]              // null
    [TestCase("level_with_CAPS", false)] // mixed case
    public void IsValid_Regex(string name, bool expected) =>
        Assert.AreEqual(expected, AssetPackNameValidator.IsValid(name));

    [Test]
    public void IsValid_50CharBoundary_Passes() =>
        Assert.IsTrue(AssetPackNameValidator.IsValid(new string('a', 50)));

    [Test]
    public void IsValid_51CharBoundary_Fails() =>
        Assert.IsFalse(AssetPackNameValidator.IsValid(new string('a', 51)));

    [Test]
    public void ThrowIfInvalid_BadName_Throws() =>
        Assert.Throws<System.ArgumentException>(
            () => AssetPackNameValidator.ThrowIfInvalid("Level1"));
}
