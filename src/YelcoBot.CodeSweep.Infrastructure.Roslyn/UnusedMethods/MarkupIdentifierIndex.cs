using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.UnusedMethods
{
    /// <summary>
    /// Identifiers found in the markup files (XAML, Web Forms, Razor, Ribbon XML…) under the folders of every project
    /// in the solution. Markup wires handlers by name (Click="Save_Click", OnClick="Save_Click", @onclick="Save"),
    /// and Roslyn does not always see those references.
    /// Files are read from disk and cached by last write time; the folder is re-enumerated at most every two seconds.
    /// </summary>
    public sealed class MarkupIdentifierIndex
    {
        private static readonly TimeSpan RecheckInterval = TimeSpan.FromSeconds(2);

        private static readonly HashSet<string> SkippedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "node_modules", "packages", "TestResults"
        };

        private readonly object _gate = new object();
        private readonly Dictionary<string, FileEntry> _files = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

        private string? _key;
        private long _signature;
        private DateTime _checkedAt;
        private HashSet<string> _identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ISet<string> GetIdentifiers(Solution solution, string? extensions)
        {
            HashSet<string> extensionSet = ParseExtensions(extensions);
            List<string> roots = GetRoots(solution);
            if (extensionSet.Count == 0 || roots.Count == 0)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string key = string.Join("|", roots) + "#" + string.Join(";", extensionSet.OrderBy(e => e, StringComparer.OrdinalIgnoreCase));

            lock (_gate)
            {
                if (key == _key && DateTime.UtcNow - _checkedAt < RecheckInterval)
                    return _identifiers;

                List<FileInfo> files = roots.SelectMany(root => EnumerateFiles(root, extensionSet)).ToList();
                long signature = ComputeSignature(files);

                if (key != _key || signature != _signature)
                {
                    _identifiers = Build(files);
                    _key = key;
                    _signature = signature;
                }

                _checkedAt = DateTime.UtcNow;
                return _identifiers;
            }
        }

        internal static IEnumerable<string> Tokenize(string text)
        {
            foreach (Match match in IdentifierPattern.Matches(text))
            {
                yield return match.Value;
            }
        }

        private static readonly Regex IdentifierPattern = new Regex(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private HashSet<string> Build(List<FileInfo> files)
        {
            HashSet<string> identifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (FileInfo file in files)
            {
                seen.Add(file.FullName);

                if (!_files.TryGetValue(file.FullName, out FileEntry? entry) || entry.LastWriteUtc != file.LastWriteTimeUtc || entry.Length != file.Length)
                {
                    entry = new FileEntry(file.LastWriteTimeUtc, file.Length, ReadIdentifiers(file.FullName));
                    _files[file.FullName] = entry;
                }

                identifiers.UnionWith(entry.Identifiers);
            }

            foreach (string stale in _files.Keys.Where(path => !seen.Contains(path)).ToList())
            {
                _files.Remove(stale);
            }

            return identifiers;
        }

        private static string[] ReadIdentifiers(string path)
        {
            try
            {
                return Tokenize(File.ReadAllText(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        private static long ComputeSignature(List<FileInfo> files)
        {
            unchecked
            {
                long signature = files.Count;
                foreach (FileInfo file in files)
                {
                    signature = signature * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(file.FullName);
                    signature = signature * 31 + file.LastWriteTimeUtc.Ticks;
                    signature = signature * 31 + file.Length;
                }

                return signature;
            }
        }

        private static IEnumerable<FileInfo> EnumerateFiles(string root, HashSet<string> extensions)
        {
            Stack<DirectoryInfo> pending = new Stack<DirectoryInfo>();
            pending.Push(new DirectoryInfo(root));

            while (pending.Count > 0)
            {
                DirectoryInfo directory = pending.Pop();
                FileSystemInfo[] entries;
                try
                {
                    entries = directory.GetFileSystemInfos();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue;
                }

                foreach (FileSystemInfo entry in entries)
                {
                    if (entry is DirectoryInfo child)
                    {
                        if (!child.Name.StartsWith(".", StringComparison.Ordinal) && !SkippedFolders.Contains(child.Name))
                        {
                            pending.Push(child);
                        }
                    }
                    else if (entry is FileInfo file && extensions.Contains(file.Extension))
                    {
                        yield return file;
                    }
                }
            }
        }

        /// <summary>Distinct project folders; a folder inside another one is scanned only once.</summary>
        private static List<string> GetRoots(Solution solution)
        {
            List<string> folders = solution.Projects
                .Select(p => p.FilePath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => Path.GetDirectoryName(p!))
                .Where(d => !string.IsNullOrEmpty(d) && Directory.Exists(d))
                .Select(d => d!.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d.Length)
                .ToList();

            List<string> roots = new List<string>();
            foreach (string folder in folders)
            {
                if (!roots.Any(root => folder.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
                {
                    roots.Add(folder);
                }
            }

            return roots;
        }

        private static HashSet<string> ParseExtensions(string? extensions)
        {
            return new HashSet<string>(
                (extensions ?? string.Empty)
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => e.Length > 0)
                    .Select(e => e.StartsWith(".", StringComparison.Ordinal) ? e : "." + e),
                StringComparer.OrdinalIgnoreCase);
        }

        private sealed class FileEntry
        {
            public FileEntry(DateTime lastWriteUtc, long length, string[] identifiers)
            {
                LastWriteUtc = lastWriteUtc;
                Length = length;
                Identifiers = identifiers;
            }

            public DateTime LastWriteUtc { get; }

            public long Length { get; }

            public string[] Identifiers { get; }
        }
    }
}
