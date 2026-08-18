using System.IO;
using Newtonsoft.Json;

namespace MonoGame.GameFramework.Persistence;

public class SaveSystem
{
  /// <summary>
  /// Write a save. The JSON goes to a sibling temp file and is then moved over
  /// the real path, so an interrupted write leaves the previous save intact
  /// rather than truncating the only copy — which was precisely the corrupt
  /// file <see cref="TryLoad"/> then had to survive.
  /// </summary>
  public void Save<T>(string path, T data, int version = 1)
  {
    SaveFile<T> file = new() { Version = version, Data = data };
    string json = JsonConvert.SerializeObject(file, Formatting.Indented);

    string directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

    string temp = path + ".tmp";
    File.WriteAllText(temp, json);
    File.Move(temp, path, overwrite: true);
  }

  /// <summary>
  /// Load a save, returning false rather than throwing on anything unreadable.
  /// A Try- method that propagates JsonReaderException is not a Try- method:
  /// a half-written or hand-edited save would take the game down at the point
  /// it tried to offer a Continue button.
  /// </summary>
  public bool TryLoad<T>(string path, out SaveFile<T> file)
  {
    file = null;
    if (!File.Exists(path)) return false;

    try
    {
      string json = File.ReadAllText(path);
      file = JsonConvert.DeserializeObject<SaveFile<T>>(json);
    }
    catch (JsonException)
    {
      return false;
    }
    catch (IOException)
    {
      return false;
    }

    return file != null;
  }

  public bool Exists(string path) => File.Exists(path);

  public bool Delete(string path)
  {
    if (!File.Exists(path)) return false;
    File.Delete(path);
    return true;
  }
}
