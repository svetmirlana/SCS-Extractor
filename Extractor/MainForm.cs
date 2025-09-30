using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Extractor.Progress;
using System.Windows.Forms;

#nullable enable

namespace Extractor
{
    public partial class MainForm : Form
    {
        private const int ProgressUpdateThrottleMilliseconds = 200;
        private bool _isDirectoryInput;
        private bool _updatingOptions;
        private bool _separateTouched;
        private bool _destinationTouched;
        private bool _suppressDestinationChange;
        private string? _lastAutoDestination;
        private OptionsSnapshot? _manualSnapshot;
        private CancellationTokenSource? _extractionCts;
        private bool _isBusy;
        private GuiProgressSink? _progressSink;
        private double _lastReportedProgress;
        private bool _suppressLog = true;
        private readonly System.Windows.Forms.Timer _progressUpdateTimer;
        private (ExtractionProgressUpdate Update, double EffectiveFraction)? _pendingProgressUpdate;
        private DateTime _lastProgressUpdateUtc;

        public MainForm()
        {
            InitializeComponent();
            _progressUpdateTimer = new System.Windows.Forms.Timer
            {
                Interval = ProgressUpdateThrottleMilliseconds
            };
            _progressUpdateTimer.Tick += ProgressUpdateTimer_Tick;
            components?.Add(_progressUpdateTimer);
            Icon = Properties.Resources.AppIcon;
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            _destinationTouched = false;
            _suppressDestinationChange = false;
            _lastAutoDestination = null;
            SetDestinationText(string.Empty, markAsAuto: true);
            chkRecommended.Checked = true;
            ApplyRecommendedProfile();
            _suppressLog = chkSuppressLog.Checked;
            UpdateOptionsEnabledState();
            UpdateExtractButtonState();
            SetCurrentArchiveLabel(string.Empty);
            SetProgressPercent(0);
        }

        

        private void SetProgressPercent(int percent)
        {
            if (lblProgressPercent.InvokeRequired)
            {
                lblProgressPercent.BeginInvoke(new Action<int>(SetProgressPercent), percent);
                return;
            }

            var bounded = Math.Clamp(percent, progressBar.Minimum, progressBar.Maximum);
            var textValue = progressBar.Style == ProgressBarStyle.Marquee
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, "{0}%", bounded);

            if (!string.Equals(lblProgressPercent.Text, textValue, StringComparison.Ordinal))
            {
                lblProgressPercent.Text = textValue;
            }
        }

        private void btnBrowseInput_Click(object sender, EventArgs e)
        {
            if (browseMenu is null)
            {
                return;
            }

            var button = (Button)sender;
            var location = new Point(0, button.Height);
            browseMenu.Show(button, location);
        }

        private void menuBrowseFile_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "SCS Archives (*.scs)|*.scs|All files (*.*)|*.*",
                Title = "Select .scs archive"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                SetInputPath(dialog.FileName);
            }
        }

        private void menuBrowseFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select folder containing .scs files"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                SetInputPath(dialog.SelectedPath);
            }
        }

        private void btnBrowseDestination_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select extraction destination",
                SelectedPath = txtDestination.Text
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                SetDestinationText(dialog.SelectedPath, markAsAuto: false);
            }
        }

        private void txtDestination_TextChanged(object sender, EventArgs e)
        {
            if (_suppressDestinationChange)
            {
                return;
            }

            _destinationTouched = true;
            _lastAutoDestination = null;
        }

        private void chkSuppressLog_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingOptions)
            {
                return;
            }

            _suppressLog = chkSuppressLog.Checked;
        }

        private void chkRecommended_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingOptions)
            {
                return;
            }

            if (chkRecommended.Checked)
            {
                _manualSnapshot = CaptureOptions();
                ApplyRecommendedProfile();
            }
            else if (_manualSnapshot is not null)
            {
                RestoreOptions(_manualSnapshot);
            }

            UpdateOptionsEnabledState();
        }

        private void chkSeparate_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingOptions)
            {
                return;
            }

            _separateTouched = true;
        }

        private void txtInputPath_TextChanged(object sender, EventArgs e)
        {
            UpdateExtractButtonState();
        }

        private void txtInputPath_Leave(object sender, EventArgs e)
        {
            UpdateInputKind(txtInputPath.Text);
            if (chkRecommended.Checked)
            {
                ApplyRecommendedProfile();
            }
            else if (_isDirectoryInput && !_separateTouched)
            {
                SetSeparateChecked(true);
            }
        }

        private void txtInputPath_DragEnter(object sender, DragEventArgs e)
        {
            HandleDragEnter(e);
        }

        private void txtInputPath_DragDrop(object sender, DragEventArgs e)
        {
            HandleDragDrop(e);
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            HandleDragEnter(e);
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            HandleDragDrop(e);
        }

        private void HandleDragEnter(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void HandleDragDrop(DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true)
            {
                return;
            }

            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                SetInputPath(files[0]);
            }
        }

        private void SetInputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            txtInputPath.Text = path;
            UpdateInputKind(path);

            if (chkRecommended.Checked)
            {
                ApplyRecommendedProfile();
            }
            else if (_isDirectoryInput && !_separateTouched)
            {
                SetSeparateChecked(true);
            }
        }

        private void ApplyRecommendedProfile()
        {
            _updatingOptions = true;
            chkDeep.Checked = true;
            chkRaw.Checked = false;
            chkSkipExisting.Checked = false;
            chkSeparate.Checked = _isDirectoryInput;
            chkSingleThread.Checked = false;
            chkTimes.Checked = false;
            chkLegacySanitize.Checked = false;
            chkDryRun.Checked = false;
            chkSuppressLog.Checked = true;
            numIoReaders.Value = 0;
            txtSalt.Text = string.Empty;
            _updatingOptions = false;
            _suppressLog = chkSuppressLog.Checked;
            _separateTouched = false;
            _destinationTouched = false;
            UpdateDefaultDestination();
        }

        private OptionsSnapshot CaptureOptions()
        {
            return new OptionsSnapshot
            {
                Deep = chkDeep.Checked,
                Raw = chkRaw.Checked,
                SkipExisting = chkSkipExisting.Checked,
                Separate = chkSeparate.Checked,
                SingleThread = chkSingleThread.Checked,
                Times = chkTimes.Checked,
                LegacySanitize = chkLegacySanitize.Checked,
                DryRun = chkDryRun.Checked,
                SuppressLog = chkSuppressLog.Checked,
                IoReaders = numIoReaders.Value,
                Salt = txtSalt.Text,
                SeparateTouched = _separateTouched
            };
        }

        private void RestoreOptions(OptionsSnapshot snapshot)
        {
            _updatingOptions = true;
            chkDeep.Checked = snapshot.Deep;
            chkRaw.Checked = snapshot.Raw;
            chkSkipExisting.Checked = snapshot.SkipExisting;
            chkSeparate.Checked = snapshot.Separate;
            chkSingleThread.Checked = snapshot.SingleThread;
            chkTimes.Checked = snapshot.Times;
            chkLegacySanitize.Checked = snapshot.LegacySanitize;
            chkDryRun.Checked = snapshot.DryRun;
            chkSuppressLog.Checked = snapshot.SuppressLog;
            numIoReaders.Value = ClampToRange(snapshot.IoReaders, numIoReaders.Minimum, numIoReaders.Maximum);
            txtSalt.Text = snapshot.Salt;
            _updatingOptions = false;
            _suppressLog = chkSuppressLog.Checked;
            _separateTouched = snapshot.SeparateTouched;
        }

        private decimal ClampToRange(decimal value, decimal minimum, decimal maximum)
        {
            if (value < minimum)
            {
                return minimum;
            }
            if (value > maximum)
            {
                return maximum;
            }
            return value;
        }

        private void SetSeparateChecked(bool value)
        {
            _updatingOptions = true;
            chkSeparate.Checked = value;
            _updatingOptions = false;
        }

        private async void btnExtract_Click(object sender, EventArgs e)
        {
            if (_isBusy)
            {
                return;
            }

            if (!ValidateInputs(out var error))
            {
                MessageBox.Show(this, error, "Validation error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var args = BuildArguments();
            _extractionCts = new CancellationTokenSource();

            try
            {
                SetUiBusy(true);
                AppendLog($"Starting extraction ({DateTime.Now:HH:mm:ss})...");
                SetStatus("Running");

                var exitCode = await RunExtractionAsync(args, _extractionCts.Token);

                if (exitCode == 0)
                {
                    AppendLog("Extraction completed successfully.", isImportant: true);
                    SetStatus("Completed");
                }
                else
                {
                    AppendLog($"Extraction finished with exit code {exitCode}.", isImportant: true);
                    SetStatus("Failed");
                }
            }
            catch (OperationCanceledException)
            {
                AppendLog("Extraction cancelled.", isImportant: true);
                SetStatus("Cancelled");
            }
            catch (Exception ex)
            {
                AppendLog($"Error: {ex.Message}", isError: true, isImportant: true);
                SetStatus("Failed");
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _extractionCts?.Dispose();
                _extractionCts = null;
                SetUiBusy(false);
                if (lblStatus.Text == "Running")
                {
                    SetStatus("Ready");
                }
            }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            using var dlg = new HelpForm();
            dlg.ShowDialog(this);
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_extractionCts == null)
            {
                return;
            }

            AppendLog("Cancelling extraction...", isImportant: true);
            _extractionCts.Cancel();
            btnCancel.Enabled = false;
        }

        private void UpdateOptionsEnabledState()
        {
            grpOptions.Enabled = !_isBusy && !chkRecommended.Checked;
        }

        private void UpdateExtractButtonState()
        {
            btnExtract.Enabled = !_isBusy && !string.IsNullOrWhiteSpace(txtInputPath.Text);
        }

        private void SetUiBusy(bool busy)
        {
            _isBusy = busy;
            btnBrowseInput.Enabled = !busy;
            btnBrowseDestination.Enabled = !busy;
            if (browseMenu is not null)
            {
                browseMenu.Enabled = !busy;
            }
            txtInputPath.Enabled = !busy;
            txtDestination.Enabled = !busy;
            chkRecommended.Enabled = !busy;
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.MarqueeAnimationSpeed = 0;
            if (busy)
            {
                _suppressLog = chkSuppressLog.Checked;
                _progressUpdateTimer.Stop();
                _pendingProgressUpdate = null;
                _lastProgressUpdateUtc = DateTime.MinValue;
                TrySetProgressValue(0);
                _lastReportedProgress = 0;
                SetCurrentArchiveLabel(string.Empty);
            }
            btnCancel.Enabled = busy;
            if (!busy)
            {
                _progressSink = null;
                _progressUpdateTimer.Stop();
                _pendingProgressUpdate = null;
                _lastProgressUpdateUtc = DateTime.UtcNow;
                _lastReportedProgress = 0;
                TrySetProgressValue(0);
                SetCurrentArchiveLabel(string.Empty);
            }

            UpdateOptionsEnabledState();
            UpdateExtractButtonState();
        }

        private bool ValidateInputs(out string error)
        {
            var input = txtInputPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Please select a file or folder.";
                return false;
            }

            UpdateInputKind(input);

            if (_isDirectoryInput)
            {
                if (!Directory.Exists(input))
                {
                    error = "Selected folder does not exist.";
                    return false;
                }
            }
            else
            {
                if (!File.Exists(input))
                {
                    error = "Selected file does not exist.";
                    return false;
                }

            }
            if (!_destinationTouched)
            {
                UpdateDefaultDestination();
            }

            var destination = txtDestination.Text.Trim();
            if (string.IsNullOrWhiteSpace(destination))
            {
                error = "Please choose a destination folder.";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(txtSalt.Text) && !ushort.TryParse(txtSalt.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                error = "Salt must be a number between 0 and 65535.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        private List<string> BuildArguments()
        {
            var args = new List<string>();
            var input = txtInputPath.Text.Trim();
            args.Add(input);

            if (_isDirectoryInput)
            {
                args.Add("--all");
                if (chkSeparate.Checked && !args.Contains("--separate"))
                {
                    args.Add("--separate");
                }
            }

            if (chkDeep.Checked)
            {
                args.Add("--deep");
            }
            if (chkRaw.Checked)
            {
                args.Add("--raw");
            }
            if (chkSkipExisting.Checked)
            {
                args.Add("--skip-existing");
            }
            if (chkSeparate.Checked && !_isDirectoryInput)
            {
                args.Add("--separate");
            }
            if (chkSingleThread.Checked)
            {
                args.Add("--single-thread");
            }
            if (chkTimes.Checked)
            {
                args.Add("--times");
            }
            if (chkLegacySanitize.Checked)
            {
                args.Add("--legacy-sanitize");
            }
            if (chkDryRun.Checked)
            {
                args.Add("--dry-run");
            }

            if (numIoReaders.Value > 0)
            {
                args.Add($"--io-readers={numIoReaders.Value}");
            }

            if (!string.IsNullOrWhiteSpace(txtSalt.Text))
            {
                args.Add($"--salt={txtSalt.Text.Trim()}");
            }

            args.Add($"--dest={txtDestination.Text.Trim()}");
            return args;
        }

        private Task<int> RunExtractionAsync(IReadOnlyList<string> args, CancellationToken cancellationToken)
        {
            var argumentArray = new string[args.Count];
            for (var i = 0; i < args.Count; i++)
            {
                argumentArray[i] = args[i];
            }

            var sink = new GuiProgressSink(this);
            _progressSink = sink;

            return Task.Run(() =>
            {
                using var stdout = new GuiTextWriter((line, isError) => AppendLog(line, isError), isError: false);
                using var stderr = new GuiTextWriter((line, isError) => AppendLog(line, isError), isError: true);
                return Program.RunExtraction(argumentArray, stdout, stderr, cancellationToken, sink);
            }, cancellationToken);
        }

        private async Task ConsumeStreamAsync(StreamReader reader, bool isError, CancellationToken cancellationToken)
        {
            try
            {
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                    {
                        break;
                    }
                    AppendLog(isError ? $"[stderr] {line}" : line);
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream disposed due to cancellation.
            }
            catch (IOException) when (cancellationToken.IsCancellationRequested)
            {
                // Expected when cancelling and closing the process.
            }
            catch (Exception ex)
            {
                AppendLog($"Stream error: {ex.Message}");
            }
        }
        private void AppendLog(string message, bool isError = false, bool isImportant = false)
        {
            if (_suppressLog && !isError && !isImportant)
            {
                return;
            }

            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action<string, bool, bool>(AppendLog), message, isError, isImportant);
                return;
            }

            var text = message ?? string.Empty;
            txtLog.AppendText(text + Environment.NewLine);
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void SetStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.BeginInvoke(new Action<string>(SetStatus), status);
                return;
            }

            if (string.Equals(lblStatus.Text, status, StringComparison.Ordinal))
            {
                return;
            }

            lblStatus.Text = status;
        }

        private void SetCurrentArchiveLabel(string archive)
        {
            if (lblCurrentArchive.InvokeRequired)
            {
                lblCurrentArchive.BeginInvoke(new Action<string>(SetCurrentArchiveLabel), archive);
                return;
            }

            var text = string.IsNullOrWhiteSpace(archive)
                ? "Current archive: (none)"
                : $"Current archive: {archive}";

            if (string.Equals(lblCurrentArchive.Text, text, StringComparison.Ordinal))
            {
                return;
            }

            lblCurrentArchive.Text = text;
        }

        private void TrySetProgressValue(int value)
        {
            var clamped = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, value));
            if (progressBar.Value != clamped)
            {
                progressBar.Value = clamped;
            }

            SetProgressPercent(clamped);
        }

        private void ApplyProgressUpdate(ExtractionProgressUpdate update, double effectiveFraction)
        {
            _lastProgressUpdateUtc = DateTime.UtcNow;

            var clampedFraction = Math.Clamp(effectiveFraction, 0d, 1d);
            var percent = (int)Math.Round(clampedFraction * 100);
            TrySetProgressValue(percent);

            var archiveName = string.IsNullOrWhiteSpace(update.Archive)
                ? string.Empty
                : Path.GetFileName(update.Archive);

            var phaseText = update.Phase switch
            {
                ExtractionProgressPhase.SearchingPaths => "Searching paths",
                ExtractionProgressPhase.Extracting => "Extracting",
                ExtractionProgressPhase.Completed => "Completed",
                _ => "Preparing"
            };

            SetCurrentArchiveLabel(archiveName);

            var statusMessage = $"{phaseText} ({percent}%)";

            var detailParts = new List<string>(capacity: 2);
            if (update.CompletedItems.HasValue && update.TotalItems.HasValue && update.TotalItems.Value > 0)
            {
                var total = Math.Max(0, update.TotalItems.Value);
                var completed = Math.Clamp(update.CompletedItems.Value, 0, total);
                detailParts.Add(string.Format(CultureInfo.InvariantCulture, "{0:N0}/{1:N0}", completed, total));
            }

            var detail = FormatProgressDetail(update.Detail);
            if (!string.IsNullOrEmpty(detail))
            {
                detailParts.Add(detail);
            }

            if (detailParts.Count > 0)
            {
                statusMessage += " - " + string.Join(", ", detailParts);
            }

            SetStatus(statusMessage);
        }

        private void ProgressUpdateTimer_Tick(object? sender, EventArgs e)
        {
            _progressUpdateTimer.Stop();
            if (_pendingProgressUpdate.HasValue)
            {
                var (update, fraction) = _pendingProgressUpdate.Value;
                _pendingProgressUpdate = null;
                ApplyProgressUpdate(update, fraction);
            }
        }

        private static string FormatProgressDetail(string? detail)
        {
            if (string.IsNullOrWhiteSpace(detail))
            {
                return string.Empty;
            }

            const int maxLength = 60;
            var normalized = detail.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            return "..." + normalized[^maxLength..];
        }

        private void OnProgress(ExtractionProgressUpdate update)
        {
            var effectiveFraction = Math.Max(update.OverallFraction, _lastReportedProgress);
            _lastReportedProgress = effectiveFraction;

            if (_progressUpdateTimer.Enabled)
            {
                _pendingProgressUpdate = (update, effectiveFraction);
                return;
            }

            var now = DateTime.UtcNow;
            if ((now - _lastProgressUpdateUtc).TotalMilliseconds < ProgressUpdateThrottleMilliseconds)
            {
                _pendingProgressUpdate = (update, effectiveFraction);
                _progressUpdateTimer.Start();
                return;
            }

            ApplyProgressUpdate(update, effectiveFraction);
        }

        private void UpdateInputKind(string path)
        {
            if (Directory.Exists(path))
            {
                _isDirectoryInput = true;
            }
            else if (File.Exists(path))
            {
                _isDirectoryInput = false;
            }
            else
            {
                _isDirectoryInput = path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar);
            }

            UpdateDefaultDestination();
        }

        private void UpdateDefaultDestination()
        {
            if (_destinationTouched)
            {
                return;
            }

            var input = txtInputPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                if (!string.IsNullOrEmpty(txtDestination.Text))
                {
                    SetDestinationText(string.Empty, markAsAuto: true);
                }
                return;
            }

            var destination = GenerateDefaultDestination(input, _isDirectoryInput);
            if (string.IsNullOrEmpty(destination))
            {
                return;
            }

            if (!string.Equals(destination, _lastAutoDestination, StringComparison.OrdinalIgnoreCase))
            {
                SetDestinationText(destination, markAsAuto: true);
            }
        }

        private void SetDestinationText(string value, bool markAsAuto)
        {
            _suppressDestinationChange = true;
            txtDestination.Text = value;
            _suppressDestinationChange = false;

            if (markAsAuto)
            {
                _destinationTouched = false;
                _lastAutoDestination = value;
            }
            else
            {
                _destinationTouched = true;
                _lastAutoDestination = null;
            }
        }

        private static string GenerateDefaultDestination(string input, bool isDirectory)
        {
            try
            {
                var fullPath = Path.GetFullPath(input);

                if (isDirectory)
                {
                    var trimmed = Path.TrimEndingDirectorySeparator(fullPath);
                    var name = SanitizeName(Path.GetFileName(trimmed), "folder");
                    var parent = Path.GetDirectoryName(trimmed);
                    if (string.IsNullOrEmpty(parent))
                    {
                        parent = trimmed;
                    }
                    return Path.Combine(parent, $"{name}_extracted_all");
                }

                var directory = Path.GetDirectoryName(fullPath);
                if (string.IsNullOrEmpty(directory))
                {
                    directory = Environment.CurrentDirectory;
                }
                var baseName = SanitizeName(Path.GetFileNameWithoutExtension(fullPath), "archive");
                return Path.Combine(directory, $"{baseName}_extracted");
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string SanitizeName(string name, string fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return fallback;
            }

            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Trim().ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                {
                    chars[i] = '_';
                }
            }

            var result = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(result) ? fallback : result;
        }

        private sealed class GuiProgressSink : IExtractionProgressSink
        {
            private readonly MainForm _owner;

            public GuiProgressSink(MainForm owner)
            {
                _owner = owner;
            }

            public void Report(in ExtractionProgressUpdate update)
            {
                if (!ReferenceEquals(_owner._progressSink, this) || _owner.IsDisposed)
                {
                    return;
                }

                var payload = update;

                void Apply()
                {
                    if (!ReferenceEquals(_owner._progressSink, this) || _owner.IsDisposed)
                    {
                        return;
                    }

                    _owner.OnProgress(payload);
                }

                if (_owner.InvokeRequired)
                {
                    try
                    {
                        _owner.BeginInvoke((Action)Apply);
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                    return;
                }

                Apply();
            }
        }
        private sealed class OptionsSnapshot
        {
            public bool Deep { get; init; }
            public bool Raw { get; init; }
            public bool SkipExisting { get; init; }
            public bool Separate { get; init; }
            public bool SingleThread { get; init; }
            public bool Times { get; init; }
            public bool LegacySanitize { get; init; }
            public bool DryRun { get; init; }
            public decimal IoReaders { get; init; }
            public string Salt { get; init; } = string.Empty;
            public bool SeparateTouched { get; init; }
            public bool SuppressLog { get; init; }
        }
    }
}
