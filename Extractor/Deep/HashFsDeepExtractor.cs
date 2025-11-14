using Sprache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extractor.Progress;
using static Extractor.PathUtils;
using static Extractor.ConsoleUtils;
using TruckLib.Sii;
using System.Data;
using TruckLib.HashFs;
using TruckLib.Models;

namespace Extractor.Deep
{
    /// <summary>
    /// A HashFS extractor which scans entry contents for paths before extraction to simplify
    /// the extraction of archives which lack dictory listings.
    /// </summary>
    public class HashFsDeepExtractor : HashFsExtractor
    {
        /// <summary>
        /// If true, perform path search in a single thread.
        /// </summary>
        public bool SingleThreadedPathSearch { get; set; } = false;
        /// <summary>
        /// Number of parallel readers to use for deep search. Default: logical CPU count.
        /// </summary>
        public int ReaderPoolSize { get; set; } = Math.Max(1, Environment.ProcessorCount);
        /// <summary>
        /// Additional start paths which the user specified with the <c>--additional</c> parameter.
        /// </summary>
        public IList<string> AdditionalStartPaths { get; set; } = [];

        /// <summary>
        /// The directory to which files whose paths were not discovered
        /// will be written.
        /// </summary>
        private const string DumpDirectory = "_unknown";

        /// <summary>
        /// The directory to which decoy files will be written.
        /// </summary>
        private const string DecoyDirectory = "_decoy";

        /// <summary>
        /// The number of files whose paths were not discovered and therefore have been
        /// dumped to <see cref="DumpDirectory"/>.
        /// </summary>
        private int dumped;

        private HashFsPathFinder finder;

        private bool hasSearchedForPaths;

        private readonly Dictionary<string, ResourceRelocationPlan> resourcePlans =
            new(StringComparer.OrdinalIgnoreCase);
        private string currentOutputRoot;
        private bool resourceRelocationsFinalized;

        public HashFsDeepExtractor(string scsPath, bool overwrite, ushort? salt) 
            : base(scsPath, overwrite, salt)
        {
            IdentifyJunkEntries();
        }

        /// <inheritdoc/>
        public override void Extract(IList<string> pathFilter, string outputRoot)
        {
            ThrowIfCancellationRequested();
            Console.WriteLine("Searching for paths ...");
            FindPaths();
            ThrowIfCancellationRequested();
            var foundFiles = finder.FoundFiles.Order().ToArray();
            Extract(foundFiles, pathFilter, outputRoot, false);
        }

        public void Extract(string[] foundFiles, IList<string> pathFilter, string outputRoot, bool ignoreMissing)
        {
            var previousOutputRoot = currentOutputRoot;
            currentOutputRoot = outputRoot;
            resourcePlans.Clear();
            resourceRelocationsFinalized = false;

            try
            {
                ThrowIfCancellationRequested();
                bool filtersSet = !pathFilter.SequenceEqual(["/"]);

                substitutions = DeterminePathSubstitutions(foundFiles);

                var filteredFiles = filtersSet
                    ? foundFiles.Where(f => pathFilter.Any(f.StartsWith)).ToArray()
                    : foundFiles;

                ProgressTracker?.BeginExtraction(filteredFiles.Length, $"Extracting {Path.GetFileName(ScsPath)}");

                var foundDecoyFiles = finder.FoundDecoyFiles.Order().ToArray();
                var filteredDecoyFiles = filtersSet
                    ? foundDecoyFiles.Where(f => pathFilter.Any(f.StartsWith)).ToArray()
                    : foundDecoyFiles;

                if (filteredDecoyFiles.Length > 0)
                {
                    ProgressTracker?.AddExtractionWork(filteredDecoyFiles.Length);
                }

                ExtractFiles(filteredFiles, outputRoot, ignoreMissing);

                var decoyDestination = Path.Combine(outputRoot, DecoyDirectory);
                foreach (var decoyFile in filteredDecoyFiles)
                {
                    ThrowIfCancellationRequested();
                    ExtractFile(decoyFile, decoyDestination);
                }

                if (!filtersSet)
                {
                    DumpUnrecovered(outputRoot, filteredFiles.Concat(filteredDecoyFiles));
                }

                FinalizeResourceRelocations();
                resourceRelocationsFinalized = true;

                WriteRenamedSummary(outputRoot);
                WriteModifiedSummary(outputRoot);
                ProgressTracker?.CompleteExtraction("Extraction finished");
            }
            finally
            {
                try
                {
                    if (!resourceRelocationsFinalized)
                    {
                        FinalizeResourceRelocations();
                    }
                }
                finally
                {
                    currentOutputRoot = previousOutputRoot;
                    resourcePlans.Clear();
                    resourceRelocationsFinalized = false;
                }
            }
        }

        public (HashSet<string> FoundFiles, HashSet<string> ReferencedFiles) FindPaths()
        {
            if (!hasSearchedForPaths)
            {
                var totalCandidates = Reader.Entries.Values.Count(e => !e.IsDirectory);
                ProgressTracker?.BeginSearch(totalCandidates, $"Searching {Path.GetFileName(ScsPath)}");

                finder = new HashFsPathFinder(Reader, AdditionalStartPaths, junkEntries,
                    SingleThreadedPathSearch, CreateReaderClone, ReaderPoolSize, ProgressTracker);
                finder.Find();
                ProgressTracker?.CompleteSearch("Search complete");
                // Always populate detailed timing/metric fields for downstream reporting
                SearchExtractTime = finder.ExtractTime;
                SearchParseTime = finder.ParseTime;
                SearchFilesParsed = finder.FilesParsed;
                SearchBytesInflated = finder.BytesInflated;
                SearchExtractWallTime = finder.ExtractWallTime;
                // Report unique as the number of unique discovered files (stable across ST/MT)
                SearchUniqueFilesParsed = finder.FoundFiles?.Count;
                SearchTime = (finder.ExtractTime + finder.ParseTime);
                hasSearchedForPaths = true;
            }
            return (finder.FoundFiles, finder.ReferencedFiles);
        }

        /// <summary>
        /// Extracts files whose paths were not discovered.
        /// </summary>
        /// <param name="destination">The root output directory.</param>
        /// <param name="foundFiles">All discovered paths.</param>
        private void DumpUnrecovered(string destination, IEnumerable<string> foundFiles)
        {
            if (DryRun)
                return;

            var matchedEntries = foundFiles
                .Select(f =>
                {
                    if (Reader.EntryExists(f) != EntryType.NotFound)
                    {
                        return Reader.GetEntry(f);
                    }
                    junkEntries.TryGetValue(Reader.HashPath(f), out var retval);
                    return retval;
                })
                .Where(e => e is not null)
                .ToArray();

            var notRecovered = Reader.Entries.Values
                .Where(e => !e.IsDirectory)
                .Except(matchedEntries!)
                .Where(entry => entry is not null && !junkEntries.ContainsKey(entry.Hash) && !maybeJunkEntries.ContainsKey(entry.Hash))
                .ToList();

            if (notRecovered.Count == 0)
            {
                return;
            }

            ProgressTracker?.AddExtractionWork(notRecovered.Count);

            var dumpRoot = Path.Combine(destination, DumpDirectory);

            foreach (var entry in notRecovered)
            {
                ThrowIfCancellationRequested();

                var fileBuffer = Reader.Extract(entry, string.Empty)[0];
                var fileType = FileTypeHelper.Infer(fileBuffer);
                var extension = FileTypeHelper.FileTypeToExtension(fileType);
                var fileName = entry.Hash.ToString("x16") + extension;
                var normalizedExtension = extension.ToLowerInvariant();
                var subfolder = normalizedExtension switch
                {
                    ".pmd" or ".pmg" or ".pmc" or ".pma" or ".ppd" => "pmd_pmg_pmc_pma_ppd",
                    ".mat" => "mat",
                    ".dds" or ".tobj" => "tobj_dds",
                    _ => null,
                };
                var outputDir = subfolder is null ? dumpRoot : Path.Combine(dumpRoot, subfolder);
                var archivePath = subfolder is null
                    ? $"/{DumpDirectory}/{fileName}"
                    : $"/{DumpDirectory}/{subfolder}/{fileName}";
                var outputPath = Path.Combine(outputDir, fileName);
                Console.WriteLine($"Dumping {fileName} ...");
                if (!Overwrite && File.Exists(outputPath))
                {
                    skipped++;
                    ReportExtractionProgress(archivePath);
                }
                else
                {
                    ExtractToDisk(entry, archivePath, outputPath);
                    dumped++;
                    ReportExtractionProgress(archivePath);
                }
            }
        }

        public override void PrintPaths(IList<string> pathFilter, bool includeAll)
        {
            var finder = new HashFsPathFinder(Reader,
                singleThreaded: SingleThreadedPathSearch,
                readerFactory: CreateReaderClone,
                readerPoolSize: ReaderPoolSize,
                progressTracker: ProgressTracker);
            finder.Find();
            var paths = (includeAll
                ? finder.FoundFiles.Union(finder.ReferencedFiles)
                : finder.FoundFiles).Order();

            foreach (var path in paths)
            {
                ThrowIfCancellationRequested();
                Console.WriteLine(ReplaceControlChars(path));
            }
        }

        public override void PrintExtractionResult()
        {
            string times = string.Empty;
            if (PrintTimesEnabled)
            {
                var parts = new List<string>();
                if (OpenTime.HasValue) parts.Add($"open={OpenTime.Value.TotalMilliseconds:F0}ms");
                if (SearchTime.HasValue)
                {
                    var search = $"search={SearchTime.Value.TotalMilliseconds:F0}ms";
                    var details = new List<string>();
                    if (SearchExtractTime.HasValue) details.Add($"decomp={SearchExtractTime.Value.TotalMilliseconds:F0}ms");
                    if (SearchExtractWallTime.HasValue) details.Add($"decomp_wall={SearchExtractWallTime.Value.TotalMilliseconds:F0}ms");
                    if (SearchParseTime.HasValue) details.Add($"parse={SearchParseTime.Value.TotalMilliseconds:F0}ms");
                    if (SearchFilesParsed.HasValue) details.Add($"files={SearchFilesParsed.Value}");
                    if (SearchUniqueFilesParsed.HasValue) details.Add($"unique={SearchUniqueFilesParsed.Value}");
                    if (SearchBytesInflated.HasValue) details.Add($"bytes={SearchBytesInflated.Value}");
                    if (details.Count > 0)
                    {
                        search += " (" + string.Join(", ", details) + ")";
                    }
                    parts.Add(search);
                }
                if (ExtractTime.HasValue) parts.Add($"extract={ExtractTime.Value.TotalMilliseconds:F0}ms");
                if (parts.Count > 0)
                {
                    times = " | " + string.Join(", ", parts);
                }
            }
            Console.WriteLine($"{extracted} extracted " +
                $"({renamedFiles.Count} renamed, {modifiedFiles.Count} modified, {dumped} dumped), " +
                $"{skipped} skipped, {duplicate} junk, {failed} failed" + times);
            PrintRenameSummary(renamedFiles.Count, modifiedFiles.Count);
        }

        public override List<Tree.Directory> GetDirectoryTree(IList<string> pathFilter)
        {
            var finder = new HashFsPathFinder(Reader,
                singleThreaded: SingleThreadedPathSearch,
                readerFactory: CreateReaderClone,
                readerPoolSize: ReaderPoolSize,
                progressTracker: ProgressTracker);
            finder.Find();

            var trees = pathFilter
                .Select(startPath => PathListToTree(startPath, finder.FoundFiles))
                .ToList();
            return trees;
        }

        protected override (Func<string, string, string> Transform, Action<string, string> Callback,
            List<(string Original, string Replacement)> Replacements) GetPathSubstitutionHandlers(
                string archivePath, string sanitizedArchivePath, string outputPath)
        {
            if (string.IsNullOrEmpty(currentOutputRoot))
            {
                return base.GetPathSubstitutionHandlers(archivePath, sanitizedArchivePath, outputPath);
            }

            var replacements = new List<(string Original, string Replacement)>();
            var sanitized = sanitizedArchivePath ?? archivePath;
            EnsureHasInitialSlash(ref sanitized);

            var parent = GetParent(sanitized);
            if (string.IsNullOrWhiteSpace(parent))
            {
                parent = "/";
            }

            var baseName = Path.GetFileNameWithoutExtension(sanitized);
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = Path.GetFileName(sanitized);
            }
            if (string.IsNullOrEmpty(baseName))
            {
                baseName = "resource";
            }

            var modifiedFileExtension = Path.GetExtension(outputPath).ToLowerInvariant();

            Func<string, string, string> transform = (original, defaultReplacement) =>
            {
                var replacementCandidate = defaultReplacement;
                if (string.IsNullOrEmpty(replacementCandidate))
                {
                    replacementCandidate = original;
                }
                EnsureHasInitialSlash(ref replacementCandidate);

                var extension = Path.GetExtension(replacementCandidate);
                var plan = GetOrCreatePlan(replacementCandidate);
                var usageIndex = plan.RegisterUsage(baseName);
                var fileName = $"{baseName}_{usageIndex}{extension}";
                var relativePath = BuildRelativePath(parent, fileName);

                var usage = new UsageRecord(
                    modifiedFilePath: outputPath,
                    modifiedFileExtension: modifiedFileExtension,
                    parentPath: parent,
                    baseName: baseName,
                    extension: extension,
                    destinationRelativePath: relativePath,
                    destinationAbsolutePath: Path.Combine(currentOutputRoot, RemoveInitialSlash(relativePath)),
                    referenceString: relativePath,
                    index: usageIndex);

                plan.Usages.Add(usage);
                replacements.Add((original, relativePath));

                return relativePath;
            };

            return (transform, null, replacements);
        }

        private static string BuildRelativePath(string parent, string fileName)
        {
            if (string.IsNullOrEmpty(parent) || parent == "/")
            {
                return "/" + fileName;
            }

            if (parent.EndsWith('/'))
            {
                return parent + fileName;
            }

            return parent + "/" + fileName;
        }

        private ResourceRelocationPlan GetOrCreatePlan(string sanitizedRelativePath)
        {
            EnsureHasInitialSlash(ref sanitizedRelativePath);

            if (resourcePlans.TryGetValue(sanitizedRelativePath, out var existing))
                return existing;

            var sourceAbsolute = Path.Combine(currentOutputRoot, RemoveInitialSlash(sanitizedRelativePath));
            int? renameIndex = null;

            for (int i = 0; i < renamedFiles.Count; i++)
            {
                if (string.Equals(renamedFiles[i].SanitizedPath, sanitizedRelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    renameIndex = i;
                    break;
                }
            }

            var plan = new ResourceRelocationPlan(sanitizedRelativePath, sourceAbsolute, renameIndex);
            resourcePlans[sanitizedRelativePath] = plan;
            return plan;
        }

        private void FinalizeResourceRelocations()
        {
            if (DryRun || resourcePlans.Count == 0 || string.IsNullOrEmpty(currentOutputRoot))
            {
                resourcePlans.Clear();
                return;
            }

            foreach (var plan in resourcePlans.Values)
            {
                if (plan.Usages.Count == 0)
                    continue;

                if (!File.Exists(plan.SourceAbsolutePath))
                {
                    Console.Error.WriteLine($"Unable to relocate {ReplaceControlChars(plan.SanitizedRelativePath)}: source file not found");
                    continue;
                }

                foreach (var usage in plan.Usages)
                {
                    string desiredRelative;
                    if (plan.GetBaseCount(usage.BaseName) == 1)
                    {
                        var fileName = usage.BaseName + usage.Extension;
                        desiredRelative = BuildRelativePath(usage.ParentPath, fileName);
                    }
                    else
                    {
                        var fileName = $"{usage.BaseName}_{usage.Index}{usage.Extension}";
                        desiredRelative = BuildRelativePath(usage.ParentPath, fileName);
                    }

                    if (!desiredRelative.Equals(usage.DestinationRelativePath, StringComparison.OrdinalIgnoreCase))
                    {
                        UpdateModifiedFileReference(usage, desiredRelative);
                        usage.DestinationRelativePath = desiredRelative;
                        usage.DestinationAbsolutePath = Path.Combine(currentOutputRoot,
                            RemoveInitialSlash(desiredRelative));
                    }
                }

                var successfulDestinations = new List<string>();
                foreach (var usage in plan.Usages)
                {
                    try
                    {
                        var destDir = Path.GetDirectoryName(usage.DestinationAbsolutePath);
                        if (!string.IsNullOrEmpty(destDir))
                        {
                            Directory.CreateDirectory(destDir);
                        }

                        if (File.Exists(usage.DestinationAbsolutePath))
                        {
                            if (Overwrite)
                            {
                                File.Copy(plan.SourceAbsolutePath, usage.DestinationAbsolutePath, true);
                            }
                        }
                        else
                        {
                            File.Copy(plan.SourceAbsolutePath, usage.DestinationAbsolutePath, false);
                        }

                        successfulDestinations.Add(usage.DestinationRelativePath);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Unable to copy {ReplaceControlChars(plan.SanitizedRelativePath)} -> {ReplaceControlChars(usage.DestinationRelativePath)}: {ex.Message}");
                        failed++;
                    }
                }

                if (successfulDestinations.Count == plan.Usages.Count)
                {
                    try
                    {
                        File.Delete(plan.SourceAbsolutePath);
                        DeleteEmptyParentDirectories(Path.GetDirectoryName(plan.SourceAbsolutePath));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Unable to delete {ReplaceControlChars(plan.SanitizedRelativePath)}: {ex.Message}");
                    }
                }

                UpdateRenamedSummary(plan, successfulDestinations);
            }

            resourcePlans.Clear();
        }

        private void UpdateModifiedFileReference(UsageRecord usage, string newRelativePath)
        {
            if (usage.ReferenceString.Equals(newRelativePath, StringComparison.OrdinalIgnoreCase))
                return;

            try
            {
                if (usage.ModifiedFileExtension == ".tobj")
                {
                    var buffer = File.ReadAllBytes(usage.ModifiedFilePath);
                    var tobj = Tobj.Load(buffer);
                    tobj.TexturePath = newRelativePath;
                    using var ms = new MemoryStream();
                    using var writer = new BinaryWriter(ms);
                    tobj.Serialize(writer);
                    File.WriteAllBytes(usage.ModifiedFilePath, ms.ToArray());
                }
                else
                {
                    var encoding = Encoding.UTF8;
                    var text = File.ReadAllText(usage.ModifiedFilePath, encoding);
                    if (!text.Contains(usage.ReferenceString, StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine($"Unable to locate reference {ReplaceControlChars(usage.ReferenceString)} in {ReplaceControlChars(usage.ModifiedFilePath)}");
                        failed++;
                    }
                    else
                    {
                        text = ReplaceFirstOccurrence(text, usage.ReferenceString, newRelativePath);
                        File.WriteAllText(usage.ModifiedFilePath, text, encoding);
                    }
                }
                usage.ReferenceString = newRelativePath;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to update references in {ReplaceControlChars(usage.ModifiedFilePath)}: {ex.Message}");
                failed++;
            }
        }

        private void UpdateRenamedSummary(ResourceRelocationPlan plan, List<string> successfulDestinations)
        {
            if (successfulDestinations.Count == 0)
                return;

            var matchingIndices = new List<int>();
            for (int i = 0; i < renamedFiles.Count; i++)
            {
                if (string.Equals(renamedFiles[i].SanitizedPath, plan.SanitizedRelativePath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    matchingIndices.Add(i);
                }
            }

            if (matchingIndices.Count == 0)
                return;

            var targetIndex = plan.RenameEntryIndex ?? matchingIndices[0];
            if (targetIndex < 0 || targetIndex >= renamedFiles.Count)
                return;

            var original = renamedFiles[targetIndex].ArchivePath;
            renamedFiles[targetIndex] = (original, successfulDestinations[0]);

            int matchIdx = 1;
            int destIdx = 1;

            while (matchIdx < matchingIndices.Count && destIdx < successfulDestinations.Count)
            {
                var idx = matchingIndices[matchIdx];
                renamedFiles[idx] = (original, successfulDestinations[destIdx]);
                matchIdx++;
                destIdx++;
            }

            for (; destIdx < successfulDestinations.Count; destIdx++)
            {
                renamedFiles.Add((original, successfulDestinations[destIdx]));
            }
        }

        private static string ReplaceFirstOccurrence(string text, string oldValue, string newValue)
        {
            var idx = text.IndexOf(oldValue, StringComparison.Ordinal);
            if (idx < 0)
                return text;

            return string.Concat(text.AsSpan(0, idx), newValue, text.AsSpan(idx + oldValue.Length));
        }

        private void DeleteEmptyParentDirectories(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return;

            try
            {
                var root = Path.GetFullPath(currentOutputRoot);
                var current = Path.GetFullPath(directory);

                while (current.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
                {
                    if (!Directory.Exists(current))
                        break;

                    if (Directory.EnumerateFileSystemEntries(current).Any())
                        break;

                    Directory.Delete(current);
                    current = Path.GetDirectoryName(current);
                    if (string.IsNullOrEmpty(current))
                        break;
                    current = Path.GetFullPath(current);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to clean up empty directories near {ReplaceControlChars(directory)}: {ex.Message}");
            }
        }

        private sealed class ResourceRelocationPlan
        {
            public ResourceRelocationPlan(string sanitizedRelativePath, string sourceAbsolutePath,
                int? renameEntryIndex)
            {
                SanitizedRelativePath = sanitizedRelativePath;
                SourceAbsolutePath = sourceAbsolutePath;
                RenameEntryIndex = renameEntryIndex;
            }

            public string SanitizedRelativePath { get; }
            public string SourceAbsolutePath { get; }
            public int? RenameEntryIndex { get; }
            public List<UsageRecord> Usages { get; } = [];

            private readonly Dictionary<string, int> baseCounts = new(StringComparer.OrdinalIgnoreCase);

            public int RegisterUsage(string baseName)
            {
                if (!baseCounts.TryGetValue(baseName, out var current))
                {
                    current = 0;
                }
                current++;
                baseCounts[baseName] = current;
                return current;
            }

            public int GetBaseCount(string baseName)
            {
                if (baseCounts.TryGetValue(baseName, out var count))
                {
                    return count;
                }
                return 0;
            }
        }

        private sealed class UsageRecord
        {
            public UsageRecord(string modifiedFilePath, string modifiedFileExtension,
                string parentPath, string baseName, string extension,
                string destinationRelativePath, string destinationAbsolutePath,
                string referenceString, int index)
            {
                ModifiedFilePath = modifiedFilePath;
                ModifiedFileExtension = modifiedFileExtension;
                ParentPath = parentPath;
                BaseName = baseName;
                Extension = extension;
                DestinationRelativePath = destinationRelativePath;
                DestinationAbsolutePath = destinationAbsolutePath;
                ReferenceString = referenceString;
                Index = index;
            }

            public string ModifiedFilePath { get; }
            public string ModifiedFileExtension { get; }
            public string ParentPath { get; }
            public string BaseName { get; }
            public string Extension { get; }
            public string DestinationRelativePath { get; set; }
            public string DestinationAbsolutePath { get; set; }
            public string ReferenceString { get; set; }
            public int Index { get; }
        }

        private TruckLib.HashFs.IHashFsReader CreateReaderClone()
        {
            var r = TruckLib.HashFs.HashFsReader.Open(ScsPath, ForceEntryTableAtEnd);
            r.Salt = Reader.Salt;
            return r;
        }
    }
}






