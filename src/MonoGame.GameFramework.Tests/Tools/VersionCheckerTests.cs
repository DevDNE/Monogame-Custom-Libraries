using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using MonoGame.GameFramework.Tools;
using Xunit;

namespace MonoGame.GameFramework.Tests.Tools;

public class VersionCheckerTests
{
  sealed record Proj(string Name, string Tfm, params (string Package, string Version)[] Packages);

  /// <summary>Writes a throwaway repo root containing src/&lt;Name&gt;/&lt;Name&gt;.csproj per spec.</summary>
  static string WriteRepo(params Proj[] projects)
  {
    string root = Directory.CreateTempSubdirectory("mgf-version-test-").FullName;
    foreach (Proj p in projects)
    {
      string dir = Path.Combine(root, "src", p.Name);
      Directory.CreateDirectory(dir);
      string refs = string.Concat(p.Packages.Select(pkg =>
        $"\n    <PackageReference Include=\"{pkg.Package}\" Version=\"{pkg.Version}\" />"));
      string tfm = p.Tfm is null ? "" : $"\n    <TargetFramework>{p.Tfm}</TargetFramework>";
      File.WriteAllText(Path.Combine(dir, $"{p.Name}.csproj"),
        $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>{tfm}\n  </PropertyGroup>\n  <ItemGroup>{refs}\n  </ItemGroup>\n</Project>");
    }
    return root;
  }

  [Fact]
  public void ConsistentProjects_ProduceNoFailures()
  {
    string root = WriteRepo(
      new Proj("A", "net9.0", ("Newtonsoft.Json", "13.0.3")),
      new Proj("B", "net9.0", ("Newtonsoft.Json", "13.0.3")));
    var result = VersionChecker.Check(root);
    result.Projects.Should().HaveCount(2);
    result.Failures.Should().BeEmpty();
  }

  [Fact]
  public void MismatchedTargetFramework_IsAFailureAgainstTheMajority()
  {
    string root = WriteRepo(
      new Proj("A", "net9.0"),
      new Proj("B", "net9.0"),
      new Proj("Odd", "net8.0"));
    var result = VersionChecker.Check(root);
    result.Failures.Should().ContainSingle()
      .Which.Should().Contain("TargetFramework mismatch")
      .And.Contain("Odd").And.Contain("net8.0").And.Contain("net9.0");
  }

  [Fact]
  public void SamePackageAtTwoVersions_IsAFailure()
  {
    string root = WriteRepo(
      new Proj("A", "net9.0", ("Newtonsoft.Json", "13.0.3")),
      new Proj("B", "net9.0", ("Newtonsoft.Json", "12.0.1")));
    var result = VersionChecker.Check(root);
    result.Failures.Should().ContainSingle()
      .Which.Should().Contain("Newtonsoft.Json").And.Contain("multiple versions")
      .And.Contain("13.0.3").And.Contain("12.0.1");
  }

  [Fact]
  public void SoloPackage_IsInfoNotFailure()
  {
    string root = WriteRepo(
      new Proj("A", "net9.0", ("dotenv.net", "3.1.3")),
      new Proj("B", "net9.0"));
    var result = VersionChecker.Check(root);
    result.Failures.Should().BeEmpty();
    result.Info.Should().ContainSingle()
      .Which.Should().Contain("dotenv.net").And.Contain("used only by A");
  }

  [Fact]
  public void PackageSharedByTwoProjectsAtOneVersion_IsNeitherFailureNorInfo()
  {
    string root = WriteRepo(
      new Proj("A", "net9.0", ("Newtonsoft.Json", "13.0.3")),
      new Proj("B", "net9.0", ("Newtonsoft.Json", "13.0.3")));
    var result = VersionChecker.Check(root);
    result.Failures.Should().BeEmpty();
    result.Info.Should().BeEmpty();
  }

  [Fact]
  public void ProjectWithoutTargetFramework_IsExcludedFromComparison()
  {
    string root = WriteRepo(
      new Proj("A", "net9.0"),
      new Proj("NoTfm", null));
    var result = VersionChecker.Check(root);
    result.Failures.Should().BeEmpty();
    result.Projects.Should().Contain(p => p.TargetFramework == "(unspecified)");
  }

  [Fact]
  public void CsprojUnderBinOrObj_IsSkipped()
  {
    string root = WriteRepo(new Proj("A", "net9.0"));
    string strayDir = Path.Combine(root, "src", "A", "obj", "Debug");
    Directory.CreateDirectory(strayDir);
    File.WriteAllText(Path.Combine(strayDir, "A.csproj"),
      "<Project><PropertyGroup><TargetFramework>net6.0</TargetFramework></PropertyGroup></Project>");

    var result = VersionChecker.Check(root);
    result.Projects.Should().ContainSingle();
    result.Failures.Should().BeEmpty();
  }

  [Fact]
  public void PackageVersionAsChildElement_IsParsed()
  {
    string root = Directory.CreateTempSubdirectory("mgf-version-test-").FullName;
    string dir = Path.Combine(root, "src", "A");
    Directory.CreateDirectory(dir);
    File.WriteAllText(Path.Combine(dir, "A.csproj"), """
      <Project Sdk="Microsoft.NET.Sdk">
        <PropertyGroup><TargetFramework>net9.0</TargetFramework></PropertyGroup>
        <ItemGroup>
          <PackageReference Include="Newtonsoft.Json"><Version>13.0.3</Version></PackageReference>
        </ItemGroup>
      </Project>
      """);

    var result = VersionChecker.Check(root);
    result.Projects.Should().ContainSingle()
      .Which.Packages.Should().Contain(new KeyValuePair<string, string>("Newtonsoft.Json", "13.0.3"));
  }

  [Fact]
  public void EmptyRepo_ProducesNoProjectsAndNoFailures()
  {
    string root = Directory.CreateTempSubdirectory("mgf-version-test-").FullName;
    var result = VersionChecker.Check(root);
    result.Projects.Should().BeEmpty();
    result.Failures.Should().BeEmpty();
  }
}
