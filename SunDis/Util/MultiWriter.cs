namespace SunDis;

public class MultiWriter(string path) : IDisposable
{
    private FileStream _fs = null!;
    private StreamWriter _sw = null!;

    public List<string> FileHistory = [];

    public void ChangeFile(string file, bool log = true)
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

        _fs = new FileStream(fullPath, FileMode.Create);
        _sw = new StreamWriter(_fs);
        if (log)
            FileHistory.Add(file);
    }

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
