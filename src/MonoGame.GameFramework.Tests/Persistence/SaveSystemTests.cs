using System;
using System.IO;
using FluentAssertions;
using MonoGame.GameFramework.Persistence;
using Xunit;

namespace MonoGame.GameFramework.Tests.Persistence;

public class SaveSystemTests : IDisposable
{
  private class PlayerState
  {
    public string Name { get; set; }
    public int Level { get; set; }
  }

  private readonly string _path;
  private readonly SaveSystem _sys = new();

  public SaveSystemTests()
  {
    _path = Path.Combine(Path.GetTempPath(), $"gf-save-{Guid.NewGuid():N}.json");
  }

  public void Dispose()
  {
    if (File.Exists(_path)) File.Delete(_path);
  }

  [Fact]
  public void SaveThenLoad_PreservesData()
  {
    _sys.Save(_path, new PlayerState { Name = "dne", Level = 7 });
    bool loaded = _sys.TryLoad(_path, out SaveFile<PlayerState> file);
    loaded.Should().BeTrue();
    file.Data.Name.Should().Be("dne");
    file.Data.Level.Should().Be(7);
  }

  [Fact]
  public void Save_DefaultVersionIsOne()
  {
    _sys.Save(_path, new PlayerState { Name = "x", Level = 1 });
    _sys.TryLoad(_path, out SaveFile<PlayerState> file);
    file.Version.Should().Be(1);
  }

  [Fact]
  public void Save_PreservesExplicitVersion()
  {
    _sys.Save(_path, new PlayerState { Name = "x", Level = 1 }, version: 42);
    _sys.TryLoad(_path, out SaveFile<PlayerState> file);
    file.Version.Should().Be(42);
  }

  [Fact]
  public void TryLoad_MissingFile_ReturnsFalse()
  {
    bool loaded = _sys.TryLoad(_path, out SaveFile<PlayerState> file);
    loaded.Should().BeFalse();
    file.Should().BeNull();
  }

  [Fact]
  public void Exists_ReflectsFilePresence()
  {
    _sys.Exists(_path).Should().BeFalse();
    _sys.Save(_path, new PlayerState());
    _sys.Exists(_path).Should().BeTrue();
  }

  [Fact]
  public void Delete_RemovesExistingFile()
  {
    _sys.Save(_path, new PlayerState());
    _sys.Delete(_path).Should().BeTrue();
    _sys.Exists(_path).Should().BeFalse();
  }

  [Fact]
  public void Delete_MissingFile_ReturnsFalse()
  {
    _sys.Delete(_path).Should().BeFalse();
  }

  [Fact]
  public void Save_CreatesMissingDirectory()
  {
    string nestedDir = Path.Combine(Path.GetTempPath(), $"gf-nested-{Guid.NewGuid():N}");
    string nestedPath = Path.Combine(nestedDir, "save.json");
    try
    {
      _sys.Save(nestedPath, new PlayerState { Name = "n", Level = 2 });
      File.Exists(nestedPath).Should().BeTrue();
    }
    finally
    {
      if (Directory.Exists(nestedDir)) Directory.Delete(nestedDir, recursive: true);
    }
  }

  [Fact]
  public void TryLoad_OnCorruptJson_ReturnsFalseInsteadOfThrowing()
  {
    // A Try- method that propagates JsonReaderException is not a Try- method: a
    // hand-edited or half-written save took the game down at the exact moment
    // it tried to offer a Continue button.
    string path = Path.Combine(Path.GetTempPath(), $"mgf-save-{Path.GetRandomFileName()}.json");
    File.WriteAllText(path, "{ not json at all");

    SaveSystem sys = new();
    sys.Invoking(x => x.TryLoad(path, out SaveFile<int> _)).Should().NotThrow();
    sys.TryLoad(path, out SaveFile<int> file).Should().BeFalse();
    file.Should().BeNull();

    File.Delete(path);
  }

  [Fact]
  public void TryLoad_OnATruncatedFile_ReturnsFalse()
  {
    string path = Path.Combine(Path.GetTempPath(), $"mgf-save-{Path.GetRandomFileName()}.json");
    File.WriteAllText(path, "{\"Version\":1,\"Data\":{\"Node\":");

    SaveSystem sys = new();
    sys.TryLoad(path, out SaveFile<object> file).Should().BeFalse();
    file.Should().BeNull();

    File.Delete(path);
  }

  [Fact]
  public void TryLoad_OnALiteralNullFile_ReturnsFalse()
  {
    string path = Path.Combine(Path.GetTempPath(), $"mgf-save-{Path.GetRandomFileName()}.json");
    File.WriteAllText(path, "null");

    SaveSystem sys = new();
    sys.TryLoad(path, out SaveFile<int> file).Should().BeFalse();
    file.Should().BeNull();

    File.Delete(path);
  }

  [Fact]
  public void Save_LeavesNoTempFileBehind()
  {
    // The write goes to a sibling temp file and is moved into place, so an
    // interrupted write cannot truncate the previous save.
    string path = Path.Combine(Path.GetTempPath(), $"mgf-save-{Path.GetRandomFileName()}.json");
    SaveSystem sys = new();
    sys.Save(path, 42);

    File.Exists(path + ".tmp").Should().BeFalse();
    sys.TryLoad(path, out SaveFile<int> file).Should().BeTrue();
    file.Data.Should().Be(42);

    File.Delete(path);
  }

  [Fact]
  public void Save_OverAnExistingFile_ReplacesItAtomically()
  {
    string path = Path.Combine(Path.GetTempPath(), $"mgf-save-{Path.GetRandomFileName()}.json");
    SaveSystem sys = new();
    sys.Save(path, 1);
    sys.Save(path, 2);

    sys.TryLoad(path, out SaveFile<int> file).Should().BeTrue();
    file.Data.Should().Be(2);

    File.Delete(path);
  }

}
