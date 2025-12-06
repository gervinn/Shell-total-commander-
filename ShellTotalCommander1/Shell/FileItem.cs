using System;
using System.IO;

namespace ShellTotalCommander1.Shell;

public sealed class FileItem
{
    public FileItem(FileSystemInfo info)
    {
        Name = info.Name;
        FullPath = info.FullName;
        IsDirectory = info is DirectoryInfo;
        Size = info is FileInfo file ? file.Length : null;
        Modified = info.LastWriteTime;
    }

    public FileItem(string name, string fullPath, bool isDirectory, long? size, DateTime? modified)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        Size = size;
        Modified = modified;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public long? Size { get; }

    public DateTime? Modified { get; }

    public static FileItem FromPath(string path)
    {
        if (File.Exists(path))
        {
            return new FileItem(new FileInfo(path));
        }

        if (Directory.Exists(path))
        {
            return new FileItem(new DirectoryInfo(path));
        }

        throw new FileNotFoundException($"Path '{path}' was not found.", path);
    }
}
