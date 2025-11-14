using Extractor.Deep;
using Extractor.Zip;
using Extractor.Progress;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Extractor.PathUtils;

namespace Extractor
{
    class Program
    {
        private const string Version = "2025-09-18";
        private static bool launchedByExplorer = false;
        private static bool consoleAllocated = false;
        private static readonly object RunLock = new();
        private static Options opt;

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                LaunchGui();
                return;
            }

            var exitCode = RunCore(args, Console.Out, Console.Error, CancellationToken.None, isGuiHosted: false, progressSink: null);
            Environment.ExitCode = exitCode;
        }

        public static int RunExtraction(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken, IExtractionProgressSink progressSink = null)
        {
            return RunCore(args, stdout, stderr, cancellationToken, isGuiHosted: true, progressSink);
        }

        private static int RunCore(string[] args, TextWriter stdout, TextWriter stderr, CancellationToken cancellationToken, bool isGuiHosted, IExtractionProgressSink progressSink)
        {
            lock (RunLock)
            {
                var originalOut = Console.Out;
                var originalError = Console.Error;

                try
                {
                    if (stdout is not null && !ReferenceEquals(stdout, originalOut))
                    {
                        Console.SetOut(stdout);
                    }

                    if (stderr is not null && !ReferenceEquals(stderr, originalError))
                    {
                        Console.SetError(stderr);
                    }

                    consoleAllocated = false;
                    launchedByExplorer = false;

                    if (!isGuiHosted && OperatingSystem.IsWindows() && !Console.IsOutputRedirected && !Console.IsErrorRedirected)
                    {
                        consoleAllocated = ConsoleManager.EnsureConsole();
                        launchedByExplorer = !Debugger.IsAttached && consoleAllocated;
                    }
                    else
                    {
                        launchedByExplorer = false;
                    }

                    if (!Console.IsOutputRedirected)
                    {
                        try
                        {
                            Console.OutputEncoding = Encoding.UTF8;
                        }
                        catch (IOException)
                        {
                        }
                        catch (ArgumentException)
                        {
                        }
                    }

                    opt = new Options();
                    opt.Parse(args);

                    PathUtils.LegacyReplaceMode = opt.LegacySanitize;

                    if (opt.PrintHelp)
                    {
                        Console.WriteLine($"Extractor {Version}");
                        Console.WriteLine();
                        Console.WriteLine("Usage:");
                        Console.WriteLine("  extractor path... [options]");
                        Console.WriteLine();
                        Console.WriteLine("Options:");
                        opt.OptionSet.WriteOptionDescriptions(Console.Out);
                        if (!isGuiHosted)
                        {
                            PauseIfNecessary();
                        }
                        return 0;
                    }

                    if (opt.InputPaths.Count == 0)
                    {
                        Console.Error.WriteLine("No input paths specified.");
                        if (!isGuiHosted)
                        {
                            PauseIfNecessary();
                        }
                        return 1;
                    }

                    var exitCode = Run(cancellationToken, progressSink);

                    if (!isGuiHosted)
                    {
                        PauseIfNecessary();
                    }

                    return exitCode;
                }
                finally
                {
                    Console.SetOut(originalOut);
                    Console.SetError(originalError);
                }
            }
        }

        private static int Run(CancellationToken cancellationToken, IExtractionProgressSink progressSink)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var scsPaths = GetScsPathsFromArgs();
            if (scsPaths.Length == 0)
            {
                Console.Error.WriteLine("No .scs files were found.");
                return 1;
            }

            var progressManager = progressSink is null ? null : new ExtractionProgressManager(progressSink, scsPaths.Length);

            if (opt.Benchmark)
            {
                return RunBenchmark(scsPaths, cancellationToken);
            }

            if (opt.UseDeepExtractor && scsPaths.Length > 1)
            {
                return DoMultiDeepExtraction(scsPaths, cancellationToken, progressManager);
            }

            var hadError = false;

            foreach (var scsPath in scsPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var swOpen = Stopwatch.StartNew();
                var extractor = CreateExtractor(scsPath, cancellationToken);
                swOpen.Stop();

                var archiveDisplayName = Path.GetFileName(scsPath) ?? scsPath;
                var tracker = progressManager?.CreateTracker(archiveDisplayName, extractor is HashFsDeepExtractor);

                if (extractor is null)
                {
                    tracker?.CompleteExtraction("Failed to open archive");
                    hadError = true;
                    continue;
                }

                extractor.ProgressTracker = tracker;

                try
                {
                    extractor.CancellationToken = cancellationToken;
                    extractor.DryRun = opt.DryRun;
                    extractor.PrintTimesEnabled = opt.Times;
                    if (extractor is HashFsDeepExtractor hdeep)
                    {
                        hdeep.SingleThreadedPathSearch = opt.SingleThread;
                        if (!opt.SingleThread)
                        {
                            hdeep.ReaderPoolSize = opt.IoReaders > 0 ? opt.IoReaders : Math.Max(1, Environment.ProcessorCount);
                        }
                    }
                    if (opt.Times)
                    {
                        extractor.OpenTime = swOpen.Elapsed;
                    }

                    if (opt.ListEntries)
                    {
                        if (extractor is HashFsExtractor)
                        {
                            ListEntries(extractor);
                        }
                        else
                        {
                            Console.Error.WriteLine("--list can only be used with HashFS archives.");
                            hadError = true;
                        }

                        extractor.ProgressTracker?.CompleteExtraction("Listing entries");
                    }
                    else if (opt.ListPaths)
                    {
                        extractor.PrintPaths(opt.PathFilter, opt.ListAll);
                        extractor.ProgressTracker?.CompleteExtraction("Listing paths");
                    }
                    else if (opt.PrintTree)
                    {
                        if (opt.UseRawExtractor)
                        {
                            Console.Error.WriteLine("--tree and --raw cannot be combined.");
                            hadError = true;
                        }
                        else
                        {
                            var trees = extractor.GetDirectoryTree(opt.PathFilter);
                            var scsName = Path.GetFileName(extractor.ScsPath);
                            Tree.TreePrinter.Print(trees, scsName);
                        }

                        extractor.ProgressTracker?.CompleteExtraction("Generated tree");
                    }
                    else
                    {
                        if (extractor is HashFsDeepExtractor hde)
                        {
                            if (opt.Times)
                            {
                                Console.WriteLine("Searching for paths ...");
                                var swSearch = Stopwatch.StartNew();
                                var (found, _) = hde.FindPaths();
                                extractor.SearchTime = swSearch.Elapsed;
                                var foundFiles = found.Order().ToArray();
                                cancellationToken.ThrowIfCancellationRequested();
                                var swExtract = Stopwatch.StartNew();
                                hde.Extract(foundFiles, opt.PathFilter, GetDestination(scsPath), false);
                                extractor.ExtractTime = swExtract.Elapsed;
                            }
                            else
                            {
                                hde.Extract(opt.PathFilter, GetDestination(scsPath));
                            }
                        }
                        else
                        {
                            if (opt.Times)
                            {
                                var swExtract = Stopwatch.StartNew();
                                extractor.Extract(opt.PathFilter, GetDestination(scsPath));
                                extractor.ExtractTime = swExtract.Elapsed;
                            }
                            else
                            {
                                extractor.Extract(opt.PathFilter, GetDestination(scsPath));
                            }
                        }
                        cancellationToken.ThrowIfCancellationRequested();
                        extractor.PrintExtractionResult();
                    }
                }
                finally
                {
                    extractor.Dispose();
                }
            }

            return hadError ? 1 : 0;
        }

        private static string GetDestination(string scsPath)
        {
            if (opt.Separate)
            {
                var scsName = Path.GetFileNameWithoutExtension(scsPath);
                var destination = Path.Combine(opt.Destination, scsName);
                return destination;
            }
            return opt.Destination;
        }

        private static Extractor CreateExtractor(string scsPath, CancellationToken cancellationToken)
        {
            if (!File.Exists(scsPath))
            {
                Console.Error.WriteLine($"{scsPath} is not a file or does not exist.");
                return null;
            }

            Extractor extractor = null;
            try
            {
                // Check if the file begins with "SCS#", the magic bytes of a HashFS file.
                // Anything else is assumed to be a ZIP file because simply checking for "PK"
                // would miss ZIP files with invalid local file headers.
                if (IsHashFs(scsPath))
                {
                    extractor = CreateHashFsExtractor(scsPath);
                }
                else
                {
                    extractor = new ZipExtractor(scsPath, !opt.SkipIfExists);
                }
            }
            catch (InvalidDataException)
            {
                Console.Error.WriteLine($"Unable to open {scsPath}: Not a HashFS or ZIP archive");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unable to open {scsPath}: {ex.Message}");
            }
            if (extractor is not null)
            {
                extractor.CancellationToken = cancellationToken;
            }
            return extractor;
        }

        private static int DoMultiDeepExtraction(string[] scsPaths, CancellationToken cancellationToken, ExtractionProgressManager progressManager)
        {
            List<Extractor> extractors = new();
            var hadError = false;

            try
            {
                foreach (var scsPath in scsPaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var swOpen = Stopwatch.StartNew();
                    var extractor = CreateExtractor(scsPath, cancellationToken);
                    swOpen.Stop();

                    var archiveDisplayName = Path.GetFileName(scsPath) ?? scsPath;
                    var tracker = progressManager?.CreateTracker(archiveDisplayName, extractor is HashFsDeepExtractor);

                    if (extractor is null)
                    {
                        tracker?.CompleteExtraction("Failed to open archive");
                        hadError = true;
                        continue;
                    }

                    extractor.DryRun = opt.DryRun;
                    extractor.ProgressTracker = tracker;
                    extractor.PrintTimesEnabled = opt.Times;
                    if (extractor is HashFsDeepExtractor hd)
                    {
                        hd.SingleThreadedPathSearch = opt.SingleThread;
                        if (!opt.SingleThread)
                        {
                            hd.ReaderPoolSize = opt.IoReaders > 0 ? opt.IoReaders : Math.Max(1, Environment.ProcessorCount);
                        }
                    }
                    if (opt.Times)
                    {
                        extractor.OpenTime = swOpen.Elapsed;
                    }
                    extractors.Add(extractor);
                }

                var bag = new System.Collections.Concurrent.ConcurrentBag<string>();
                var parallelOptions = new ParallelOptions
                {
                    CancellationToken = cancellationToken
                };

                Parallel.ForEach(extractors, parallelOptions, extractor =>
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                    Console.WriteLine($"Searching for paths in {Path.GetFileName(extractor.ScsPath)} ...");
                    if (extractor is HashFsDeepExtractor hashFs)
                    {
                        var swSearch = Stopwatch.StartNew();
                        var (found, referenced) = hashFs.FindPaths();
                        swSearch.Stop();
                        if (opt.Times)
                        {
                            extractor.SearchTime = swSearch.Elapsed;
                        }
                        foreach (var p in found)
                        {
                            parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                            bag.Add(p);
                        }
                        foreach (var p in referenced)
                        {
                            parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                            bag.Add(p);
                        }
                    }
                    else if (extractor is ZipExtractor zip)
                    {
                        var finder = new ZipPathFinder(zip.Reader);
                        var swSearch = Stopwatch.StartNew();
                        finder.Find();
                        swSearch.Stop();
                        if (opt.Times)
                        {
                            extractor.SearchTime = swSearch.Elapsed;
                        }
                        foreach (var p in finder.ReferencedFiles)
                        {
                            parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                            bag.Add(p);
                        }
                        foreach (var p in zip.Reader.Entries.Keys.Select(p => '/' + p))
                        {
                            parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                            bag.Add(p);
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Unhandled extractor type");
                    }
                });

                var everything = new HashSet<string>(bag);

                foreach (var extractor in extractors)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var existing = everything.Where(extractor.FileSystem.FileExists).ToArray();

                    if (opt.ListPaths)
                    {
                        foreach (var path in existing.Order())
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            Console.WriteLine(ReplaceControlChars(path));
                        }

                        extractor.ProgressTracker?.CompleteExtraction("Listing paths");
                    }
                    else if (opt.PrintTree)
                    {
                        var trees = opt.PathFilter
                            .Select(startPath => PathListToTree(startPath, existing))
                            .ToList();
                        Tree.TreePrinter.Print(trees, Path.GetFileName(extractor.ScsPath));

                        extractor.ProgressTracker?.CompleteExtraction("Generated tree");
                    }
                    else
                    {
                        if (extractor is HashFsDeepExtractor hashFs)
                        {
                            var swExtract = Stopwatch.StartNew();
                            hashFs.Extract(existing.ToArray(), opt.PathFilter, GetDestination(extractor.ScsPath), true);
                            swExtract.Stop();
                            if (opt.Times)
                            {
                                extractor.ExtractTime = swExtract.Elapsed;
                            }
                        }
                        else
                        {
                            var swExtract = Stopwatch.StartNew();
                            extractor.Extract(opt.PathFilter, GetDestination(extractor.ScsPath));
                            swExtract.Stop();
                            if (opt.Times)
                            {
                                extractor.ExtractTime = swExtract.Elapsed;
                            }
                        }
                    }
                }

                if (!opt.ListPaths && !opt.PrintTree)
                {
                    foreach (var extractor in extractors)
                    {
                        Console.Write($"{Path.GetFileName(extractor.ScsPath)}: ");
                        extractor.PrintExtractionResult();
                    }
                }

                return hadError ? 1 : 0;
            }
            finally
            {
                foreach (var extractor in extractors)
                {
                    extractor.ProgressTracker = null;
                    extractor.Dispose();
                }
            }
        }

        private static bool IsHashFs(string scsPath)
        {
            try
            {
                using var fs = File.OpenRead(scsPath);
                using var r = new BinaryReader(fs, Encoding.ASCII);
                var magic = r.ReadChars(4);
                return magic.SequenceEqual(['S','C','S','#']);
            }
            catch { return false; }
        }

        private static int RunBenchmark(string[] scsPaths, CancellationToken cancellationToken)
        {
            var hadError = false;

            foreach (var scsPath in scsPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!IsHashFs(scsPath))
                {
                    Console.Error.WriteLine($"Benchmark: skipping {Path.GetFileName(scsPath)} (not a HashFS archive)");
                    hadError = true;
                    continue;
                }

                Console.WriteLine($"Benchmarking {Path.GetFileName(scsPath)} (deep scan, dry-run) ...");

                HashFsDeepExtractor MakeExtractor(bool singleThread)
                {
                    var x = new HashFsDeepExtractor(scsPath, overwrite: !opt.SkipIfExists, opt.Salt)
                    {
                        ForceEntryTableAtEnd = opt.ForceEntryTableAtEnd,
                        PrintNotFoundMessage = !opt.ExtractAllInDir,
                        AdditionalStartPaths = opt.AdditionalStartPaths,
                        SingleThreadedPathSearch = singleThread,
                        DryRun = true,
                    };
                    if (!singleThread && opt.IoReaders > 0)
                    {
                        x.ReaderPoolSize = Math.Max(1, opt.IoReaders);
                    }
                    x.CancellationToken = cancellationToken;
                    return x;
                }

                HashFsDeepExtractor single = null;
                HashFsDeepExtractor multi = null;
                try
                {
                    single = MakeExtractor(true);
                    var swSingle = Stopwatch.StartNew();
                    var (foundSingle, _) = single.FindPaths();
                    swSingle.Stop();
                    single.SearchTime = swSingle.Elapsed;

                    cancellationToken.ThrowIfCancellationRequested();

                    multi = MakeExtractor(false);
                    var swMulti = Stopwatch.StartNew();
                    var (foundMulti, _) = multi.FindPaths();
                    swMulti.Stop();
                    multi.SearchTime = swMulti.Elapsed;

                    void PrintBench(string title, HashFsDeepExtractor e, int found)
                    {
                        Console.WriteLine(
                            $"  {title}: search={e.SearchTime.Value.TotalMilliseconds:F0}ms " +
                            $"(decomp={e.SearchExtractTime.Value.TotalMilliseconds:F0}ms, " +
                            $"decomp_wall={e.SearchExtractWallTime.Value.TotalMilliseconds:F0}ms, " +
                            $"parse={e.SearchParseTime.Value.TotalMilliseconds:F0}ms, " +
                            $"files={e.SearchFilesParsed.Value}, " +
                            $"unique={e.SearchUniqueFilesParsed.Value}, " +
                            $"bytes={e.SearchBytesInflated.Value}), " +
                            $"found={found}");
                    }

                    PrintBench("single-thread", single, foundSingle.Count);
                    PrintBench("multi-thread", multi, foundMulti.Count);

                    if (single.SearchTime.HasValue && multi.SearchTime.HasValue)
                    {
                        var speedup = single.SearchTime.Value.TotalMilliseconds / Math.Max(1, multi.SearchTime.Value.TotalMilliseconds);
                        Console.WriteLine($"  speedup: x{speedup:F2}");
                    }
                }
                finally
                {
                    single?.Dispose();
                    multi?.Dispose();
                }
            }

            return hadError ? 1 : 0;
        }

        private static Extractor CreateHashFsExtractor(string scsPath)
        {
            if (opt.UseRawExtractor)
            {
                return new HashFsRawExtractor(scsPath, !opt.SkipIfExists, opt.Salt)
                {
                    ForceEntryTableAtEnd = opt.ForceEntryTableAtEnd,
                    PrintNotFoundMessage = !opt.ExtractAllInDir,
                };
            }
            else if (opt.UseDeepExtractor)
            {
                return new HashFsDeepExtractor(scsPath, !opt.SkipIfExists, opt.Salt)
                {
                    ForceEntryTableAtEnd = opt.ForceEntryTableAtEnd,
                    PrintNotFoundMessage = !opt.ExtractAllInDir,
                    AdditionalStartPaths = opt.AdditionalStartPaths,
                };
            }
            return new HashFsExtractor(scsPath, !opt.SkipIfExists, opt.Salt)
            {
                ForceEntryTableAtEnd = opt.ForceEntryTableAtEnd,
                PrintNotFoundMessage = !opt.ExtractAllInDir,
            };
        }

        private static void ListEntries(Extractor extractor)
        {
            if (extractor is HashFsExtractor hExt)
            {
                Console.WriteLine($"  {"Offset",-10}  {"Hash",-16}  {"Cmp. Size",-10}  {"Uncmp.Size",-10}");
                foreach (var (_, entry) in hExt.Reader.Entries)
                {
                    Console.WriteLine($"{(entry.IsDirectory ? "*" : " ")} " +
                        $"{entry.Offset,10}  {entry.Hash,16:x}  {entry.CompressedSize,10}  {entry.Size,10}");
                }
            }
        }

        private static string[] GetScsPathsFromArgs()
        {
            List<string> scsPaths;
            if (opt.ExtractAllInDir)
            {
                scsPaths = [];
                foreach (var inputPath in opt.InputPaths)
                {
                    if (!Directory.Exists(inputPath))
                    {
                        Console.Error.WriteLine($"{inputPath} is not a directory or does not exist.");
                        continue;
                    }
                    scsPaths.AddRange(GetAllScsFiles(inputPath));
                }
            }
            else
            {
                scsPaths = opt.InputPaths;
            }
            return scsPaths.Distinct().ToArray();
        }

        private static void LaunchGui()
        {
            if (OperatingSystem.IsWindows())
            {
                ConsoleManager.ReleaseConsole(allocated: true);
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        private static void PauseIfNecessary()
        {
            if (launchedByExplorer && !Console.IsOutputRedirected)
            {
                Console.WriteLine("Press any key to continue ...");
                Console.ReadKey(intercept: true);
            }
        }
    }
}












