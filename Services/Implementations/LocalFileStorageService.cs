using markly.Services.Interfaces;
using System.Security;

namespace markly.Services.Implementations;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public LocalFileStorageService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName) || folderName.Contains("..") || Path.IsPathRooted(folderName))
        {
            throw new ArgumentException("Invalid folder name", nameof(folderName));
        }

        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, folderName);
        var fullUploadsFolderPath = Path.GetFullPath(uploadsFolder);

        if (!fullUploadsFolderPath.StartsWith(_webHostEnvironment.WebRootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new SecurityException("Path traversal attempt detected");
        }

        if (!Directory.Exists(fullUploadsFolderPath))
        {
            Directory.CreateDirectory(fullUploadsFolderPath);
        }

        // Sanitize filename to prevent path traversal attacks
        var sanitizedFileName = Path.GetFileName(file.FileName);
        var uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;
        var filePath = Path.Combine(fullUploadsFolderPath, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        return Path.Combine(folderName, uniqueFileName).Replace("\\", "/");
    }

    public Task DeleteFileAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || filePath.Contains(".."))
        {
            return Task.CompletedTask;
        }

        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, filePath);
        var resolvedPath = Path.GetFullPath(fullPath);

        if (!resolvedPath.StartsWith(_webHostEnvironment.WebRootPath, StringComparison.OrdinalIgnoreCase))
        {
            // TODO: log security event
            return Task.CompletedTask;
        }

        if (File.Exists(resolvedPath))
        {
            File.Delete(resolvedPath);
        }
        return Task.CompletedTask;
    }
}
