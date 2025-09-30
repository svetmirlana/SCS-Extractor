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

            WriteRenamedSummary(outputRoot);
            WriteModifiedSummary(outputRoot);
            ProgressTracker?.CompleteExtraction("Extraction finished");
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

            var outputDir = Path.Combine(destination, DumpDirectory);

            foreach (var entry in notRecovered)
            {
                ThrowIfCancellationRequested();

                var fileBuffer = Reader.Extract(entry, string.Empty)[0];
                var fileType = FileTypeHelper.Infer(fileBuffer);
                var extension = FileTypeHelper.FileTypeToExtension(fileType);
                var fileName = entry.Hash.ToString("x16") + extension;
                var archivePath = $"/{DumpDirectory}/{fileName}";
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

        private TruckLib.HashFs.IHashFsReader CreateReaderClone()
        {
            var r = TruckLib.HashFs.HashFsReader.Open(ScsPath, ForceEntryTableAtEnd);
            r.Salt = Reader.Salt;
            return r;
        }
    }
}






