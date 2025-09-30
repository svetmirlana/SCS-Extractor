using System;
using System.Windows.Forms;

namespace Extractor
{
    public partial class HelpForm : Form
    {
        public HelpForm()
        {
            InitializeComponent();
            PopulateTexts();
        }

        private void PopulateTexts()
        {
            txtIntro.Text =
                "Welcome to Extractor (SCS) Help." + Environment.NewLine +
                Environment.NewLine +
                "This app extracts the contents of .scs archives (and folders containing .scs files) " +
                "from games based on the SCS engine. You can drag and drop a file/folder into the input box, " +
                "choose a destination, and start extraction." + Environment.NewLine +
                Environment.NewLine +
                "Use the tabs to learn quick usage, the meaning of each option, advanced settings, " +
                "troubleshooting, and tips.";

            txtQuickStart.Text =
                "Quick start:" + Environment.NewLine +
                "1. Pick a .scs file or a folder:\r\n   - Click 'Browse...' or drag & drop into 'Input path'." + Environment.NewLine +
                "2. Check the destination:\r\n   - A default destination is auto-generated (you can change it anytime)." + Environment.NewLine +
                "3. (Optional) Keep 'Use recommended settings' checked for a safe default profile." + Environment.NewLine +
                "4. Click 'Extract' to begin." + Environment.NewLine +
                "5. Watch the progress bar and status. Use 'Cancel' to stop." + Environment.NewLine +
                Environment.NewLine +
                "Tip: When selecting a folder with multiple .scs files, enable 'Separate output' to create a subfolder per file.";

            txtOptions.Text =
                "Options (match the checkboxes on screen):" + Environment.NewLine +
                "- Deep scan: Deeper search for internal paths and known types." + Environment.NewLine +
                "- Raw extraction: Extracts without additional adjustments/renaming; raw layout." + Environment.NewLine +
                "- Skip existing: Do not overwrite files that already exist at destination." + Environment.NewLine +
                "- Separate output: When processing multiple .scs files, create a subfolder per file." + Environment.NewLine +
                "- Single-thread search: Limit path search to 1 thread (useful for slow I/O)." + Environment.NewLine +
                "- Print timing: Show execution timings in the log." + Environment.NewLine +
                "- Legacy sanitize: Use legacy filename sanitization rules." + Environment.NewLine +
                "- Dry run: Simulate extraction without writing files." + Environment.NewLine +
                "- IO readers: Number of parallel I/O readers (0 = automatic)." + Environment.NewLine +
                "- Salt: Number (0–65535) used to help with hash-based name resolution when needed." + Environment.NewLine +
                "- Suppress log output: Hide less important log messages during execution." + Environment.NewLine +
                Environment.NewLine +
                "Destination:\r\n- Auto-generated from the file/folder name. You can change it anytime.";

            txtAdvanced.Text =
                "Advanced and performance:" + Environment.NewLine +
                "- Deep scan: May be slower but finds more paths; recommended in most cases." + Environment.NewLine +
                "- IO readers: Increase on fast SSDs; decrease on slow HDDs. 0 lets the app decide." + Environment.NewLine +
                "- Single-thread: Use when you see disk saturation or low CPU availability." + Environment.NewLine +
                "- Suppress log: When enabled, reduces log verbosity and improves status readability." + Environment.NewLine +
                Environment.NewLine +
                "Default destination generation:\r\n- File: creates '<name>_extracted' next to the .scs.\r\n- Folder: creates '<name>_extracted_all' next to the folder.";

            txtTroubleshooting.Text =
                "Troubleshooting:" + Environment.NewLine +
                "- 'Selected file does not exist': Check the typed/dropped path." + Environment.NewLine +
                "- 'Selected folder does not exist': Ensure the folder is still accessible." + Environment.NewLine +
                "- Permissions: If writing to destination fails, pick another folder or run with appropriate permissions." + Environment.NewLine +
                "- Disk space: Ensure you have enough free space for extracted files." + Environment.NewLine +
                "- Corrupted archive: Try re-downloading/copying the .scs file." + Environment.NewLine +
                "- Stopping operation: Use 'Cancel'; it may take a few seconds to shut down safely.";

            txtTips.Text =
                "Tips:" + Environment.NewLine +
                "- Extracting a folder with many .scs files? Enable 'Separate output' to organize by file." + Environment.NewLine +
                "- Need more details? Uncheck 'Suppress log output' to see full logs." + Environment.NewLine +
                "- Tune 'IO readers' for your hardware (more on fast SSDs)." + Environment.NewLine +
                "- Use 'Dry run' to preview actions without writing to disk." + Environment.NewLine +
                "- 'Salt' can help in cases involving hash-based filenames when needed.";

            var version = Application.ProductVersion;
            txtAbout.Text =
                "Extractor (SCS)" + Environment.NewLine +
                $"Version: {version}" + Environment.NewLine +
                Environment.NewLine +
                "Extraction utility for SCS archives with a simple graphical interface." + Environment.NewLine +
                "Thank you for using it!";
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
