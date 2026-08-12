using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class SpriteConventionCheckerTests
{
  const string FontBlock = """
    #begin fonts/Arial.spritefont
    /importer:FontDescriptionImporter
    /processor:FontDescriptionProcessor
    /processorParam:TextureFormat=Compressed
    /build:fonts/Arial.spritefont
    """;

  static string TextureBlock(
    string asset = "sprites/hero.png",
    string format = "Color",
    string resizeToPowerOfTwo = "False",
    string makeSquare = "False") => $"""
    #begin {asset}
    /importer:TextureImporter
    /processor:TextureProcessor
    /processorParam:PremultiplyAlpha=True
    /processorParam:ResizeToPowerOfTwo={resizeToPowerOfTwo}
    /processorParam:MakeSquare={makeSquare}
    /processorParam:TextureFormat={format}
    /build:{asset}
    """;

  static string WriteProject(string mgcbBody, string csContents = "class X { }")
  {
    string proj = Directory.CreateTempSubdirectory("mgf-sprite-test-").FullName;
    Directory.CreateDirectory(Path.Combine(proj, "Content"));
    File.WriteAllText(Path.Combine(proj, "Content", "Content.mgcb"), mgcbBody);
    File.WriteAllText(Path.Combine(proj, "PlayState.cs"), csContents);
    return proj;
  }

  [Fact]
  public void ProjectWithNoTextures_IsSkippedEntirely()
  {
    // The seven rectangle-only samples must stay silent, including their
    // bare Begin() calls — there is nothing to get wrong without textures.
    string proj = WriteProject(FontBlock, "class X { void D() { sb.Begin(); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.HasTextures.Should().BeFalse();
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void MissingMgcb_IsNotAFailure()
  {
    string proj = Directory.CreateTempSubdirectory("mgf-sprite-test-").FullName;
    var result = SpriteConventionChecker.Check(proj);
    result.HasTextures.Should().BeFalse();
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void CompliantTextureProject_HasNoViolations()
  {
    string proj = WriteProject(
      FontBlock + "\n\n" + TextureBlock(),
      "class X { void D() { sb.Begin(samplerState: SamplerState.PointClamp); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.HasTextures.Should().BeTrue();
    result.TextureAssets.Should().ContainSingle().Which.Should().Be("sprites/hero.png");
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void CompressedTextureFormat_IsFlagged()
  {
    string proj = WriteProject(TextureBlock(format: "Compressed"),
      "class X { void D() { sb.Begin(samplerState: SamplerState.PointClamp); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.Violations.Should().ContainSingle()
      .Which.Description.Should().Contain("TextureFormat=Compressed").And.Contain("Use Color");
  }

  [Fact]
  public void FontBlockUsingCompressed_IsNotFlagged()
  {
    // Compressed is correct for glyph atlases — only TextureImporter blocks
    // are subject to the rule.
    string proj = WriteProject(FontBlock + "\n\n" + TextureBlock(),
      "class X { void D() { sb.Begin(SpriteSortMode.Deferred); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.Violations.Should().NotContain(v => v.Description.Contains("Arial"));
  }

  [Theory]
  [InlineData("ResizeToPowerOfTwo")]
  [InlineData("MakeSquare")]
  public void PaddingProcessorParams_AreFlagged(string param)
  {
    string mgcb = param == "ResizeToPowerOfTwo"
      ? TextureBlock(resizeToPowerOfTwo: "True")
      : TextureBlock(makeSquare: "True");
    string proj = WriteProject(mgcb, "class X { void D() { sb.Begin(samplerState: s); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.Violations.Should().ContainSingle()
      .Which.Description.Should().Contain(param).And.Contain("shifts every source rectangle");
  }

  [Fact]
  public void BareBeginInTextureProject_IsFlaggedWithLineNumber()
  {
    string proj = WriteProject(TextureBlock(),
      "class X\n{\n  void D()\n  {\n    sb.Begin();\n  }\n}");
    var result = SpriteConventionChecker.Check(proj);
    result.Violations.Should().ContainSingle()
      .Which.Description.Should().Contain("PlayState.cs:5").And.Contain("PointClamp");
  }

  [Fact]
  public void BeginWithArguments_IsLeftAlone()
  {
    // Judging whether an arbitrary overload passes a sampler is the
    // compiler's job; a false CI failure is worse than a missed warning.
    string proj = WriteProject(TextureBlock(),
      "class X { void D() { sb.Begin(transformMatrix: m, samplerState: SamplerState.PointClamp); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void GeneratedCodeUnderObjAndBin_IsSkipped()
  {
    string proj = WriteProject(TextureBlock(),
      "class X { void D() { sb.Begin(samplerState: s); } }");
    string objDir = Path.Combine(proj, "obj");
    Directory.CreateDirectory(objDir);
    File.WriteAllText(Path.Combine(objDir, "Generated.cs"), "class Y { void D() { sb.Begin(); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.Violations.Should().BeEmpty();
  }

  [Fact]
  public void MultipleTextures_AreAllCollected()
  {
    string mgcb = string.Join("\n\n",
      FontBlock,
      TextureBlock("sprites/a.png"),
      TextureBlock("sprites/b.png"),
      TextureBlock("sprites/c.png", format: "Compressed"));
    string proj = WriteProject(mgcb, "class X { void D() { sb.Begin(samplerState: s); } }");
    var result = SpriteConventionChecker.Check(proj);
    result.TextureAssets.Should().BeEquivalentTo("sprites/a.png", "sprites/b.png", "sprites/c.png");
    result.Violations.Should().ContainSingle()
      .Which.Description.Should().StartWith("sprites/c.png");
  }
}
