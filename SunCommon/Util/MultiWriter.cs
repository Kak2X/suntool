using System.Runtime.CompilerServices;

namespace SunCommon;

public interface IMultiWriter : IDisposable
{
    void ChangeFile(string file, bool log = true, bool append = false);
    void ChangeFile(string file, Func<string> onCreate, Func<string, string> onAppend, bool log = true);
    void Write(string text);
    void WriteLine(string line);
    void WriteIndent(string line);
    void WriteCommand(string command, params string?[] args);
    string MakeIndent(string line);
    string MakeCommand(string command, params string?[] args);
}

public class MultiWriter(string path) : IMultiWriter
{
    private FileStream _fs = null!;
    private StreamWriter _sw = null!;

    public List<string> FileHistory = [];

    public void ChangeFile(string file, bool log = true, bool append = false)
    {
        var fullPath = PrepareNextFile(path, file);
        ChangeFile(fullPath, append);
        if (log)
            FileHistory.Add(file);
    }

    public void ChangeFile(string file, Func<string> onCreate, Func<string, string> onAppend, bool log = true)
    {
        var fullPath = PrepareNextFile(path, file);
        var initial = File.Exists(fullPath)
            ? onAppend?.Invoke(File.ReadAllText(fullPath))
            : onCreate?.Invoke();
        ChangeFile(fullPath, false);
        if (initial != null)
            _sw.Write(initial);
        if (log)
            FileHistory.Add(file);
    }

    public string MakeIndent(string line) => $"\t{line}\r\n";
    public string MakeCommand(string command, params string?[] args)
    {
        // Split between actual args and everything past the first comment
        var comments = "";
        for (var i = 0; i < args.Length; i++)
            if (args[i]?.StartsWith(';') ?? false)
            {
                comments = " " + string.Join(" ", args[i..]);
                args = args[..i];
                break;
            }

        var strArgs = args.Length > 0 ? $" {string.Join(", ", args)}" : string.Empty;
        return $"\t{command}{strArgs}{comments}\r\n";
    }

    public void Write(string text) => _sw.Write(text);
    public void WriteLine(string line) => _sw.WriteLine(line);
    public void WriteIndent(string line) => _sw.Write(MakeIndent(line));
    public void WriteCommand(string command, params string?[] args) => _sw.Write(MakeCommand(command, args));

    private string PrepareNextFile(string path, string file)
    {
        if (_sw != null)
        {
            _sw.Flush();
            _sw.Dispose();
            _fs!.Dispose();
        }

        // Create folders as needed
        var fullPath = Path.Combine(path, file);
        var targetDir = Path.GetDirectoryName(fullPath);
        if (targetDir != null && !File.Exists(targetDir))
            Directory.CreateDirectory(targetDir);
        return fullPath;
    }

    private void ChangeFile(string fullPath, bool append)
    {
        _fs = new FileStream(fullPath, append ? FileMode.Append : FileMode.Create);
        _sw = new StreamWriter(_fs);
    }

    private bool _disposed;
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sw?.Flush();
                _sw?.Dispose();
                _fs?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
