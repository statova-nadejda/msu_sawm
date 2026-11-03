using System.Text;
using System.Text.Json;
using Lab5.Models;

namespace Lab5.Services;

public class SecurityLog
{
    private const long MaxFileBytes = 1024 * 1024;
    private const int MaxFiles = 30;

    private readonly string _directory;
    private readonly ILogger<SecurityLog> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);

    public SecurityLog(
        IWebHostEnvironment environment,
        ILogger<SecurityLog> logger)
    {
        _directory = Path.Combine(
            environment.ContentRootPath,
            "Logs");

        _logger = logger;
    }

    public async Task WriteAsync(
        HttpContext context,
        string eventName,
        string action,
        AccessUser? user,
        int? targetId,
        int statusCode)
    {
        var record = new
        {
            TimeUtc = DateTimeOffset.UtcNow,
            Event = eventName,
            ActorId = user?.Id,
            Role = user?.Role,
            Action = action,
            TargetId = targetId,
            Method = context.Request.Method,
            StatusCode = statusCode,
            RequestId = context.TraceIdentifier
        };

        var line = JsonSerializer.Serialize(record)
            + Environment.NewLine;

        await _lock.WaitAsync();

        try
        {
            Directory.CreateDirectory(_directory);

            var path = GetWritableFile(
                Encoding.UTF8.GetByteCount(line));

            await File.AppendAllTextAsync(
                path,
                line,
                Encoding.UTF8);

            DeleteOldFiles();
        }
        catch (Exception ex)
            when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(
                ex,
                "Не удалось записать событие безопасности.");
        }
        finally
        {
            _lock.Release();
        }
    }

    private string GetWritableFile(int newRecordBytes)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        for (var index = 1; ; index++)
        {
            var filename = $"security-{date}-{index:D6}.log";
            var path = Path.Combine(_directory, filename);

            if (!File.Exists(path))
            {
                return path;
            }

            if (new FileInfo(path).Length + newRecordBytes
                <= MaxFileBytes)
            {
                return path;
            }
        }
    }

    private void DeleteOldFiles()
    {
        var files = Directory.GetFiles(
                _directory,
                "security-*.log")
            .OrderByDescending(path => Path.GetFileName(path),
                StringComparer.Ordinal)
            .Skip(MaxFiles);

        foreach (var file in files)
        {
            File.Delete(file);
        }
    }

    public async Task<IReadOnlyList<string>> ReadRecentAsync()
    {
        await _lock.WaitAsync();

        try
        {
            if (!Directory.Exists(_directory))
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();

            var files = Directory.GetFiles(
                    _directory,
                    "security-*.log")
                .OrderByDescending(path => Path.GetFileName(path),
                    StringComparer.Ordinal);

            foreach (var file in files)
            {
                var tail = new Queue<string>();

                foreach (var line in File.ReadLines(file))
                {
                    tail.Enqueue(line);

                    if (tail.Count > 200)
                    {
                        tail.Dequeue();
                    }
                }

                result.AddRange(
                    tail.Reverse().Take(200 - result.Count));

                if (result.Count >= 200)
                {
                    break;
                }
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }
}