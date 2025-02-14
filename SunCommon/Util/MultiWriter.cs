namespace SunCommon;

public class MultiWriter(string path) : IDisposable
{
    private FileStream _fs = null!;
    private StreamWriter _sw = null!;

    public List<string> FileHistory = [];

    public void ChangeFile(string file, bool log = true, bool append = false)
    {
        var fullPath = PrepareNextFile(path, file);
        var exists = File.Exists(fullPath);
        ChangeFile(fullPath, append);
        if (!exists && log)
            FileHistory.Add(file);
    }

    public void ChangeFile(string file, Func<string> onCreate, Func<string, string> onAppend, bool log = true)
    {
        var fullPath = PrepareNextFile(path, file);
        var exists = File.Exists(fullPath);

        var initial = exists
            ? onAppend?.Invoke(File.ReadAllText(fullPath)) 
            : onCreate?.Invoke();
        ChangeFile(fullPath, false);
        if (initial != null)
            _sw.Write(initial);
        if (!exists && log)
            FileHistory.Add(file);
    }

    public void Write(string text) => _sw.Write(text);
    public void WriteLine(string line) => _sw.WriteLine(line);
    public void WriteIndent(string line) => _sw.WriteLine($"\t{line}");
    
    public void WriteCommand(string command, params string[] args)
    {
        // Split between actual args and everything past the first comment
        var comments = "";
        for (var i = 0; i < args.Length; i++)
            if (args[i].StartsWith(';'))
            {
                comments = " " + string.Join(" ", args[i..]);
                args = args[..i];
                break;
            }

        var strArgs = args.Length > 0 ? $" {string.Join(", ", args)}" : string.Empty;
        _sw.WriteLine($"\t{command}{strArgs}{comments}"); // .PadRight(10)
    }

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
