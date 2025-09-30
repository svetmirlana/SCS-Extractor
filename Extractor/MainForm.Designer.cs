namespace Extractor
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblInput = new System.Windows.Forms.Label();
            txtInputPath = new System.Windows.Forms.TextBox();
            btnBrowseInput = new System.Windows.Forms.Button();
            browseMenu = new System.Windows.Forms.ContextMenuStrip(components);
            menuBrowseFile = new System.Windows.Forms.ToolStripMenuItem();
            menuBrowseFolder = new System.Windows.Forms.ToolStripMenuItem();
            lblDestination = new System.Windows.Forms.Label();
            txtDestination = new System.Windows.Forms.TextBox();
            btnBrowseDestination = new System.Windows.Forms.Button();
            chkRecommended = new System.Windows.Forms.CheckBox();
            grpOptions = new System.Windows.Forms.GroupBox();
            chkDryRun = new System.Windows.Forms.CheckBox();
            chkSuppressLog = new System.Windows.Forms.CheckBox();
            chkLegacySanitize = new System.Windows.Forms.CheckBox();
            chkTimes = new System.Windows.Forms.CheckBox();
            chkSingleThread = new System.Windows.Forms.CheckBox();
            chkSeparate = new System.Windows.Forms.CheckBox();
            chkSkipExisting = new System.Windows.Forms.CheckBox();
            chkRaw = new System.Windows.Forms.CheckBox();
            chkDeep = new System.Windows.Forms.CheckBox();
            lblSalt = new System.Windows.Forms.Label();
            txtSalt = new System.Windows.Forms.TextBox();
            lblIoReaders = new System.Windows.Forms.Label();
            numIoReaders = new System.Windows.Forms.NumericUpDown();
            txtLog = new System.Windows.Forms.TextBox();
            progressBar = new System.Windows.Forms.ProgressBar();
            btnExtract = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            lblCurrentArchive = new System.Windows.Forms.Label();
            lblStatus = new System.Windows.Forms.Label();
            logSectionLayout = new System.Windows.Forms.TableLayoutPanel();
            bottomLayout = new System.Windows.Forms.TableLayoutPanel();
            progressContainer = new System.Windows.Forms.Panel();
            lblProgressPercent = new System.Windows.Forms.Label();
            actionButtonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            btnHelp = new System.Windows.Forms.Button();
            browseMenu.SuspendLayout();
            grpOptions.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numIoReaders).BeginInit();
            logSectionLayout.SuspendLayout();
            bottomLayout.SuspendLayout();
            progressContainer.SuspendLayout();
            actionButtonsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // lblInput
            // 
            lblInput.AutoSize = true;
            lblInput.Location = new System.Drawing.Point(14, 20);
            lblInput.Name = "lblInput";
            lblInput.Size = new System.Drawing.Size(77, 20);
            lblInput.TabIndex = 0;
            lblInput.Text = "Input path";
            // 
            // txtInputPath
            // 
            txtInputPath.AllowDrop = true;
            txtInputPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtInputPath.Location = new System.Drawing.Point(14, 44);
            txtInputPath.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtInputPath.Name = "txtInputPath";
            txtInputPath.Size = new System.Drawing.Size(669, 27);
            txtInputPath.TabIndex = 1;
            txtInputPath.TextChanged += txtInputPath_TextChanged;
            txtInputPath.DragDrop += txtInputPath_DragDrop;
            txtInputPath.DragEnter += txtInputPath_DragEnter;
            txtInputPath.Leave += txtInputPath_Leave;
            // 
            // btnBrowseInput
            // 
            btnBrowseInput.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBrowseInput.Location = new System.Drawing.Point(690, 44);
            btnBrowseInput.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnBrowseInput.Name = "btnBrowseInput";
            btnBrowseInput.Size = new System.Drawing.Size(117, 37);
            btnBrowseInput.TabIndex = 2;
            btnBrowseInput.Text = "Browse...";
            btnBrowseInput.UseVisualStyleBackColor = true;
            btnBrowseInput.Click += btnBrowseInput_Click;
            // 
            // browseMenu
            // 
            browseMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            browseMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuBrowseFile, menuBrowseFolder });
            browseMenu.Name = "browseMenu";
            browseMenu.Size = new System.Drawing.Size(172, 52);
            // 
            // menuBrowseFile
            // 
            menuBrowseFile.Name = "menuBrowseFile";
            menuBrowseFile.Size = new System.Drawing.Size(171, 24);
            menuBrowseFile.Text = "Select file...";
            menuBrowseFile.Click += menuBrowseFile_Click;
            // 
            // menuBrowseFolder
            // 
            menuBrowseFolder.Name = "menuBrowseFolder";
            menuBrowseFolder.Size = new System.Drawing.Size(171, 24);
            menuBrowseFolder.Text = "Select folder...";
            menuBrowseFolder.Click += menuBrowseFolder_Click;
            // 
            // lblDestination
            // 
            lblDestination.AutoSize = true;
            lblDestination.Location = new System.Drawing.Point(14, 91);
            lblDestination.Name = "lblDestination";
            lblDestination.Size = new System.Drawing.Size(85, 20);
            lblDestination.TabIndex = 3;
            lblDestination.Text = "Destination";
            // 
            // txtDestination
            // 
            txtDestination.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtDestination.Location = new System.Drawing.Point(14, 115);
            txtDestination.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtDestination.Name = "txtDestination";
            txtDestination.Size = new System.Drawing.Size(669, 27);
            txtDestination.TabIndex = 4;
            txtDestination.TextChanged += txtDestination_TextChanged;
            // 
            // btnBrowseDestination
            // 
            btnBrowseDestination.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBrowseDestination.Location = new System.Drawing.Point(690, 115);
            btnBrowseDestination.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            btnBrowseDestination.Name = "btnBrowseDestination";
            btnBrowseDestination.Size = new System.Drawing.Size(117, 37);
            btnBrowseDestination.TabIndex = 5;
            btnBrowseDestination.Text = "Browse...";
            btnBrowseDestination.UseVisualStyleBackColor = true;
            btnBrowseDestination.Click += btnBrowseDestination_Click;
            // 
            // chkRecommended
            // 
            chkRecommended.AutoSize = true;
            chkRecommended.Location = new System.Drawing.Point(14, 167);
            chkRecommended.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkRecommended.Name = "chkRecommended";
            chkRecommended.Size = new System.Drawing.Size(211, 24);
            chkRecommended.TabIndex = 6;
            chkRecommended.Text = "Use recommended settings";
            chkRecommended.UseVisualStyleBackColor = true;
            chkRecommended.CheckedChanged += chkRecommended_CheckedChanged;
            // 
            // grpOptions
            // 
            grpOptions.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            grpOptions.Controls.Add(chkDryRun);
            grpOptions.Controls.Add(chkSuppressLog);
            grpOptions.Controls.Add(chkLegacySanitize);
            grpOptions.Controls.Add(chkTimes);
            grpOptions.Controls.Add(chkSingleThread);
            grpOptions.Controls.Add(chkSeparate);
            grpOptions.Controls.Add(chkSkipExisting);
            grpOptions.Controls.Add(chkRaw);
            grpOptions.Controls.Add(chkDeep);
            grpOptions.Controls.Add(lblSalt);
            grpOptions.Controls.Add(txtSalt);
            grpOptions.Controls.Add(lblIoReaders);
            grpOptions.Controls.Add(numIoReaders);
            grpOptions.Location = new System.Drawing.Point(14, 203);
            grpOptions.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            grpOptions.Name = "grpOptions";
            grpOptions.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            grpOptions.Size = new System.Drawing.Size(793, 218);
            grpOptions.TabIndex = 7;
            grpOptions.TabStop = false;
            grpOptions.Text = "Options";
            // 
            // chkDryRun
            // 
            chkDryRun.AutoSize = true;
            chkDryRun.Location = new System.Drawing.Point(287, 136);
            chkDryRun.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkDryRun.Name = "chkDryRun";
            chkDryRun.Size = new System.Drawing.Size(79, 24);
            chkDryRun.TabIndex = 7;
            chkDryRun.Text = "Dry run";
            chkDryRun.UseVisualStyleBackColor = true;
            // 
            // chkSuppressLog
            // 
            chkSuppressLog.AutoSize = true;
            chkSuppressLog.Checked = true;
            chkSuppressLog.CheckState = System.Windows.Forms.CheckState.Checked;
            chkSuppressLog.Location = new System.Drawing.Point(18, 169);
            chkSuppressLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkSuppressLog.Name = "chkSuppressLog";
            chkSuppressLog.Size = new System.Drawing.Size(164, 24);
            chkSuppressLog.TabIndex = 8;
            chkSuppressLog.Text = "Suppress log output";
            chkSuppressLog.UseVisualStyleBackColor = true;
            chkSuppressLog.CheckedChanged += chkSuppressLog_CheckedChanged;
            // 
            // chkLegacySanitize
            // 
            chkLegacySanitize.AutoSize = true;
            chkLegacySanitize.Location = new System.Drawing.Point(287, 102);
            chkLegacySanitize.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkLegacySanitize.Name = "chkLegacySanitize";
            chkLegacySanitize.Size = new System.Drawing.Size(131, 24);
            chkLegacySanitize.TabIndex = 6;
            chkLegacySanitize.Text = "Legacy sanitize";
            chkLegacySanitize.UseVisualStyleBackColor = true;
            // 
            // chkTimes
            // 
            chkTimes.AutoSize = true;
            chkTimes.Location = new System.Drawing.Point(287, 69);
            chkTimes.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkTimes.Name = "chkTimes";
            chkTimes.Size = new System.Drawing.Size(108, 24);
            chkTimes.TabIndex = 5;
            chkTimes.Text = "Print timing";
            chkTimes.UseVisualStyleBackColor = true;
            // 
            // chkSingleThread
            // 
            chkSingleThread.AutoSize = true;
            chkSingleThread.Location = new System.Drawing.Point(287, 36);
            chkSingleThread.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkSingleThread.Name = "chkSingleThread";
            chkSingleThread.Size = new System.Drawing.Size(167, 24);
            chkSingleThread.TabIndex = 4;
            chkSingleThread.Text = "Single-thread search";
            chkSingleThread.UseVisualStyleBackColor = true;
            // 
            // chkSeparate
            // 
            chkSeparate.AutoSize = true;
            chkSeparate.Location = new System.Drawing.Point(18, 136);
            chkSeparate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkSeparate.Name = "chkSeparate";
            chkSeparate.Size = new System.Drawing.Size(138, 24);
            chkSeparate.TabIndex = 3;
            chkSeparate.Text = "Separate output";
            chkSeparate.UseVisualStyleBackColor = true;
            chkSeparate.CheckedChanged += chkSeparate_CheckedChanged;
            // 
            // chkSkipExisting
            // 
            chkSkipExisting.AutoSize = true;
            chkSkipExisting.Location = new System.Drawing.Point(18, 102);
            chkSkipExisting.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkSkipExisting.Name = "chkSkipExisting";
            chkSkipExisting.Size = new System.Drawing.Size(114, 24);
            chkSkipExisting.TabIndex = 2;
            chkSkipExisting.Text = "Skip existing";
            chkSkipExisting.UseVisualStyleBackColor = true;
            // 
            // chkRaw
            // 
            chkRaw.AutoSize = true;
            chkRaw.Location = new System.Drawing.Point(18, 69);
            chkRaw.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkRaw.Name = "chkRaw";
            chkRaw.Size = new System.Drawing.Size(129, 24);
            chkRaw.TabIndex = 1;
            chkRaw.Text = "Raw extraction";
            chkRaw.UseVisualStyleBackColor = true;
            // 
            // chkDeep
            // 
            chkDeep.AutoSize = true;
            chkDeep.Location = new System.Drawing.Point(18, 36);
            chkDeep.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            chkDeep.Name = "chkDeep";
            chkDeep.Size = new System.Drawing.Size(100, 24);
            chkDeep.TabIndex = 0;
            chkDeep.Text = "Deep scan";
            chkDeep.UseVisualStyleBackColor = true;
            // 
            // lblSalt
            // 
            lblSalt.AutoSize = true;
            lblSalt.Location = new System.Drawing.Point(549, 80);
            lblSalt.Name = "lblSalt";
            lblSalt.Size = new System.Drawing.Size(34, 20);
            lblSalt.TabIndex = 10;
            lblSalt.Text = "Salt";
            // 
            // txtSalt
            // 
            txtSalt.Location = new System.Drawing.Point(651, 77);
            txtSalt.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            txtSalt.Name = "txtSalt";
            txtSalt.Size = new System.Drawing.Size(126, 27);
            txtSalt.TabIndex = 9;
            // 
            // lblIoReaders
            // 
            lblIoReaders.AutoSize = true;
            lblIoReaders.Location = new System.Drawing.Point(549, 36);
            lblIoReaders.Name = "lblIoReaders";
            lblIoReaders.Size = new System.Drawing.Size(77, 20);
            lblIoReaders.TabIndex = 8;
            lblIoReaders.Text = "IO readers";
            // 
            // numIoReaders
            // 
            numIoReaders.Location = new System.Drawing.Point(651, 35);
            numIoReaders.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            numIoReaders.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
            numIoReaders.Name = "numIoReaders";
            numIoReaders.Size = new System.Drawing.Size(126, 27);
            numIoReaders.TabIndex = 9;
            // 
            // txtLog
            // 
            txtLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            txtLog.Location = new System.Drawing.Point(0, 0);
            txtLog.Margin = new System.Windows.Forms.Padding(0, 0, 0, 16);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtLog.Size = new System.Drawing.Size(793, 204);
            txtLog.TabIndex = 8;
            // 
            // progressBar
            // 
            progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            progressBar.Location = new System.Drawing.Point(0, 0);
            progressBar.Margin = new System.Windows.Forms.Padding(0);
            progressBar.Name = "progressBar";
            progressBar.Size = new System.Drawing.Size(757, 42);
            progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            progressBar.TabIndex = 0;
            // 
            // btnExtract
            // 
            btnExtract.Location = new System.Drawing.Point(144, 0);
            btnExtract.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            btnExtract.MinimumSize = new System.Drawing.Size(114, 48);
            btnExtract.Name = "btnExtract";
            btnExtract.Size = new System.Drawing.Size(126, 48);
            btnExtract.TabIndex = 10;
            btnExtract.Text = "Extract";
            btnExtract.UseVisualStyleBackColor = true;
            btnExtract.Click += btnExtract_Click;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Location = new System.Drawing.Point(288, 0);
            btnCancel.Margin = new System.Windows.Forms.Padding(0);
            btnCancel.MinimumSize = new System.Drawing.Size(114, 48);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(126, 48);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblCurrentArchive
            // 
            lblCurrentArchive.AutoEllipsis = true;
            bottomLayout.SetColumnSpan(lblCurrentArchive, 2);
            lblCurrentArchive.Dock = System.Windows.Forms.DockStyle.Fill;
            lblCurrentArchive.Location = new System.Drawing.Point(18, 16);
            lblCurrentArchive.Margin = new System.Windows.Forms.Padding(0, 0, 0, 5);
            lblCurrentArchive.Name = "lblCurrentArchive";
            lblCurrentArchive.Size = new System.Drawing.Size(757, 20);
            lblCurrentArchive.TabIndex = 12;
            lblCurrentArchive.Text = "Current archive: (none)";
            lblCurrentArchive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            lblStatus.Location = new System.Drawing.Point(18, 126);
            lblStatus.Margin = new System.Windows.Forms.Padding(0, 16, 18, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new System.Drawing.Size(298, 48);
            lblStatus.TabIndex = 13;
            lblStatus.Text = "Ready";
            lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // logSectionLayout
            // 
            logSectionLayout.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            logSectionLayout.ColumnCount = 1;
            logSectionLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            logSectionLayout.Controls.Add(txtLog, 0, 0);
            logSectionLayout.Controls.Add(bottomLayout, 0, 1);
            logSectionLayout.Location = new System.Drawing.Point(14, 443);
            logSectionLayout.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            logSectionLayout.Name = "logSectionLayout";
            logSectionLayout.RowCount = 2;
            logSectionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            logSectionLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            logSectionLayout.Size = new System.Drawing.Size(793, 410);
            logSectionLayout.TabIndex = 8;
            // 
            // bottomLayout
            // 
            bottomLayout.AutoSize = true;
            bottomLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            bottomLayout.BackColor = System.Drawing.Color.FromArgb(248, 249, 253);
            bottomLayout.ColumnCount = 2;
            bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            bottomLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            bottomLayout.Controls.Add(lblCurrentArchive, 0, 0);
            bottomLayout.Controls.Add(progressContainer, 0, 1);
            bottomLayout.Controls.Add(lblStatus, 0, 2);
            bottomLayout.Controls.Add(actionButtonsPanel, 1, 2);
            bottomLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            bottomLayout.Location = new System.Drawing.Point(0, 220);
            bottomLayout.Margin = new System.Windows.Forms.Padding(0);
            bottomLayout.Name = "bottomLayout";
            bottomLayout.Padding = new System.Windows.Forms.Padding(18, 16, 18, 16);
            bottomLayout.RowCount = 3;
            bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 69F));
            bottomLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            bottomLayout.Size = new System.Drawing.Size(793, 190);
            bottomLayout.TabIndex = 0;
            // 
            // progressContainer
            // 
            bottomLayout.SetColumnSpan(progressContainer, 2);
            progressContainer.Controls.Add(progressBar);
            progressContainer.Controls.Add(lblProgressPercent);
            progressContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            progressContainer.Location = new System.Drawing.Point(18, 57);
            progressContainer.Margin = new System.Windows.Forms.Padding(0, 16, 0, 11);
            progressContainer.Name = "progressContainer";
            progressContainer.Size = new System.Drawing.Size(757, 42);
            progressContainer.TabIndex = 1;
            // 
            // lblProgressPercent
            // 
            lblProgressPercent.BackColor = System.Drawing.Color.Transparent;
            lblProgressPercent.Dock = System.Windows.Forms.DockStyle.Fill;
            lblProgressPercent.Location = new System.Drawing.Point(0, 0);
            lblProgressPercent.Name = "lblProgressPercent";
            lblProgressPercent.Size = new System.Drawing.Size(757, 42);
            lblProgressPercent.TabIndex = 1;
            lblProgressPercent.Text = "0%";
            lblProgressPercent.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // actionButtonsPanel
            // 
            actionButtonsPanel.AutoSize = true;
            actionButtonsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            actionButtonsPanel.Controls.Add(btnCancel);
            actionButtonsPanel.Controls.Add(btnExtract);
            actionButtonsPanel.Controls.Add(btnHelp);
            actionButtonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            actionButtonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            actionButtonsPanel.Location = new System.Drawing.Point(352, 126);
            actionButtonsPanel.Margin = new System.Windows.Forms.Padding(18, 16, 0, 0);
            actionButtonsPanel.Name = "actionButtonsPanel";
            actionButtonsPanel.Padding = new System.Windows.Forms.Padding(0, 0, 9, 0);
            actionButtonsPanel.Size = new System.Drawing.Size(423, 48);
            actionButtonsPanel.TabIndex = 3;
            actionButtonsPanel.WrapContents = false;
            // 
            // btnHelp
            // 
            btnHelp.Location = new System.Drawing.Point(0, 0);
            btnHelp.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            btnHelp.MinimumSize = new System.Drawing.Size(114, 48);
            btnHelp.Name = "btnHelp";
            btnHelp.Size = new System.Drawing.Size(126, 48);
            btnHelp.TabIndex = 12;
            btnHelp.Text = "Help";
            btnHelp.UseVisualStyleBackColor = true;
            btnHelp.Click += btnHelp_Click;
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(821, 872);
            Controls.Add(logSectionLayout);
            Controls.Add(grpOptions);
            Controls.Add(chkRecommended);
            Controls.Add(btnBrowseDestination);
            Controls.Add(txtDestination);
            Controls.Add(lblDestination);
            Controls.Add(btnBrowseInput);
            Controls.Add(txtInputPath);
            Controls.Add(lblInput);
            Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            MinimumSize = new System.Drawing.Size(836, 751);
            Name = "MainForm";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Extractor";
            DragDrop += MainForm_DragDrop;
            DragEnter += MainForm_DragEnter;
            browseMenu.ResumeLayout(false);
            grpOptions.ResumeLayout(false);
            grpOptions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numIoReaders).EndInit();
            logSectionLayout.ResumeLayout(false);
            logSectionLayout.PerformLayout();
            bottomLayout.ResumeLayout(false);
            bottomLayout.PerformLayout();
            progressContainer.ResumeLayout(false);
            actionButtonsPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblInput;
        private System.Windows.Forms.TextBox txtInputPath;
        private System.Windows.Forms.Button btnBrowseInput;
        private System.Windows.Forms.ContextMenuStrip browseMenu;
        private System.Windows.Forms.ToolStripMenuItem menuBrowseFile;
        private System.Windows.Forms.ToolStripMenuItem menuBrowseFolder;
        private System.Windows.Forms.Label lblDestination;
        private System.Windows.Forms.TextBox txtDestination;
        private System.Windows.Forms.Button btnBrowseDestination;
        private System.Windows.Forms.CheckBox chkRecommended;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.NumericUpDown numIoReaders;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button btnExtract;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblCurrentArchive;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TableLayoutPanel logSectionLayout;
        private System.Windows.Forms.TableLayoutPanel bottomLayout;
        private System.Windows.Forms.Panel progressContainer;
        private System.Windows.Forms.Label lblProgressPercent;
        private System.Windows.Forms.FlowLayoutPanel actionButtonsPanel;
        private System.Windows.Forms.Button btnHelp;
        private System.Windows.Forms.Label lblIoReaders;
        private System.Windows.Forms.Label lblSalt;
        private System.Windows.Forms.TextBox txtSalt;
        private System.Windows.Forms.CheckBox chkDeep;
        private System.Windows.Forms.CheckBox chkRaw;
        private System.Windows.Forms.CheckBox chkSkipExisting;
        private System.Windows.Forms.CheckBox chkSeparate;
        private System.Windows.Forms.CheckBox chkSingleThread;
        private System.Windows.Forms.CheckBox chkTimes;
        private System.Windows.Forms.CheckBox chkLegacySanitize;
        private System.Windows.Forms.CheckBox chkSuppressLog;
        private System.Windows.Forms.CheckBox chkDryRun;
    }
}

