using System;
using System.Threading;

#nullable enable

namespace Extractor.Progress
{
    /// <summary>
    /// High-level phases reported during archive processing.
    /// </summary>
    public enum ExtractionProgressPhase
    {
        Initializing,
        SearchingPaths,
        Extracting,
        Completed
    }

    /// <summary>
    /// Immutable payload for progress updates.
    /// </summary>
    public readonly record struct ExtractionProgressUpdate(
        string Archive,
        ExtractionProgressPhase Phase,
        double PhaseFraction,
        double OverallFraction,
        int? CompletedItems,
        int? TotalItems,
        string? Detail);

    /// <summary>
    /// Implemented by consumers that wish to receive extraction progress updates.
    /// </summary>
    public interface IExtractionProgressSink
    {
        void Report(in ExtractionProgressUpdate update);
    }

    /// <summary>
    /// Coordinates progress allocation across multiple archives.
    /// </summary>
    public sealed class ExtractionProgressManager
    {
        private readonly IExtractionProgressSink? _sink;
        private readonly int _totalArchives;
        private int _created;

        public ExtractionProgressManager(IExtractionProgressSink? sink, int totalArchives)
        {
            _sink = sink;
            _totalArchives = Math.Max(1, totalArchives);
            _created = 0;
        }

        public ExtractionProgressTracker CreateTracker(string archive, bool includeSearchPhase)
        {
            var index = Interlocked.Increment(ref _created) - 1;
            if (index < 0)
            {
                index = 0;
            }

            var width = 1d / _totalArchives;
            var start = Math.Clamp(width * index, 0d, 1d);
            return new ExtractionProgressTracker(_sink, archive, start, width, includeSearchPhase);
        }
    }

    /// <summary>
    /// Tracks search/extraction stages for a single archive and emits normalized progress.
    /// </summary>
    public sealed class ExtractionProgressTracker
    {
        private readonly IExtractionProgressSink? _sink;
        private readonly string _archive;
        private readonly double _start;
        private readonly double _width;
        private readonly double _searchWeight;
        private readonly double _extractWeight;

        private int _searchTotal;
        private int _extractTotal;
        private int _searchCompleted;
        private int _extractCompleted;
        private int _lastPercent;

        public ExtractionProgressTracker(IExtractionProgressSink? sink, string archive, double start, double width, bool includeSearchPhase)
        {
            _sink = sink;
            _archive = archive;
            _start = Math.Clamp(start, 0d, 1d);
            _width = Math.Clamp(width, 0d, 1d - _start);
            _searchWeight = includeSearchPhase ? _width * 0.5d : 0d;
            _extractWeight = _width - _searchWeight;
            _lastPercent = -1;
            Publish(ExtractionProgressPhase.Initializing, 0d, null, null, detail: null);
        }

        public void BeginSearch(int totalCandidates, string? detail = null)
        {
            _searchTotal = Math.Max(0, totalCandidates);
            _searchCompleted = 0;
            Publish(ExtractionProgressPhase.SearchingPaths, _searchTotal == 0 ? 1d : 0d, 0, _searchTotal, detail);
            if (_searchTotal == 0)
            {
                // No search work - treat as immediately complete.
                CompleteSearch(detail);
            }
        }

        public void IncrementSearch(int delta = 1, string? detail = null)
        {
            if (_searchWeight <= 0d)
            {
                return;
            }

            var completed = Interlocked.Add(ref _searchCompleted, Math.Max(0, delta));
            var fraction = _searchTotal > 0 ? Math.Clamp((double)completed / _searchTotal, 0d, 1d) : 1d;
            Publish(ExtractionProgressPhase.SearchingPaths, fraction, completed, _searchTotal, detail);
        }

        public void CompleteSearch(string? detail = null)
        {
            if (_searchWeight <= 0d)
            {
                return;
            }

            Interlocked.Exchange(ref _searchCompleted, _searchTotal);
            Publish(ExtractionProgressPhase.SearchingPaths, 1d, _searchTotal, _searchTotal, detail);
        }

        public void BeginExtraction(int totalItems, string? detail = null)
        {
            _extractTotal = Math.Max(0, totalItems);
            _extractCompleted = 0;
            Publish(ExtractionProgressPhase.Extracting, 0d, 0, _extractTotal, detail);
        }

        public void AddExtractionWork(int additionalItems)
        {
            if (additionalItems <= 0)
            {
                return;
            }

            Interlocked.Add(ref _extractTotal, additionalItems);
        }

        public void IncrementExtraction(int delta = 1, string? detail = null)
        {
            var completed = Interlocked.Add(ref _extractCompleted, Math.Max(0, delta));
            var fraction = _extractTotal > 0 ? Math.Clamp((double)completed / _extractTotal, 0d, 1d) : 1d;
            Publish(ExtractionProgressPhase.Extracting, fraction, completed, _extractTotal, detail);
        }

        public void CompleteExtraction(string? detail = null)
        {
            Interlocked.Exchange(ref _extractCompleted, _extractTotal);
            Publish(ExtractionProgressPhase.Extracting, 1d, _extractTotal, _extractTotal, detail);
            Publish(ExtractionProgressPhase.Completed, 1d, _extractTotal, _extractTotal, detail);
        }

        private void Publish(ExtractionProgressPhase phase, double phaseFraction, int? completed, int? total, string? detail)
        {
            if (_sink is null)
            {
                return;
            }

            var clampedPhase = Math.Clamp(phaseFraction, 0d, 1d);
            double overall = _start;
            switch (phase)
            {
                case ExtractionProgressPhase.SearchingPaths:
                    overall += _searchWeight * clampedPhase;
                    break;
                case ExtractionProgressPhase.Extracting:
                    overall += _searchWeight + _extractWeight * clampedPhase;
                    break;
                case ExtractionProgressPhase.Completed:
                    overall = _start + _width;
                    break;
                default:
                    break;
            }

            overall = Math.Clamp(overall, 0d, 1d);
            var percent = (int)Math.Round(overall * 100d);
            if (percent < 0)
            {
                percent = 0;
            }

            if (percent == _lastPercent && detail is null)
            {
                return;
            }

            _lastPercent = percent;
            _sink.Report(new ExtractionProgressUpdate(_archive, phase, clampedPhase, overall, completed, total, detail));
        }
    }
}



