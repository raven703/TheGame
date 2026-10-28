using System;
using System.IO;

/// <summary>Path-scoped v4 file adapter; production and isolated tests use the same writer.</summary>
public sealed class EquipmentFileStore
{
    private readonly string path;
    private readonly Action<string> beforeReplace;
    public EquipmentFileStore(string path, Action<string> beforeReplace = null)
    { this.path = path ?? throw new ArgumentNullException(nameof(path)); this.beforeReplace = beforeReplace; }
    public void Write(string json)
    {
        PlayerEquipment.Parse(json, false);
        string pending = path + ".pending";
        try
        {
            using (var stream = new FileStream(pending, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (File.ReadAllText(pending) != json) throw new IOException("Не пройдена проверка записанного оснащения.");
            // Infrastructure fault injection, never used by the production manager.
            beforeReplace?.Invoke(pending);
            if (File.Exists(path)) File.Replace(pending, path, path + ".previous");
            else File.Move(pending, path);
        }
        finally { if (File.Exists(pending)) File.Delete(pending); }
    }
}