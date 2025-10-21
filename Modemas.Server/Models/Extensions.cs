namespace Modemas.Server.Models;

public static class FileExtensions
{
    public static string ReadAllFileText(this string filePath)
    {
        if (!File.Exists(filePath))
        {
            var defaultJson = "[]";
            File.WriteAllText(filePath, defaultJson);
            return defaultJson;
        }

        try
        {
            var text = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                File.WriteAllText(filePath, "[]");
                return "[]";
            }

            return text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read {filePath}: {ex.Message}. Resetting to empty JSON.");
            File.WriteAllText(filePath, "[]");
            return "[]";
        }
    }
}
