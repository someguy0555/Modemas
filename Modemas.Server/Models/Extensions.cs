using System.IO;
using System.Text;

namespace Modemas.Server.Models;

public static class FileExtensions
{
    public static string ReadAllFileText(this string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var stringBuilder = new StringBuilder();

        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            stringBuilder.AppendLine(line);
        }

        return stringBuilder.ToString();
    }
}
