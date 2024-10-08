namespace WoWFontChanger
{
    public partial class WoWFontChanger : Form
    {
        string[] fontFiles = Array.Empty<string>(); // Initialize the array to avoid null issues
        ToolTip toolTip = new ToolTip(); // Tooltip instance
        private readonly string[] validWoWFolders = { "_retail_", "_ptr_", "_classic_", "_beta_" }; // Valid WoW folder names

        public WoWFontChanger()
        {
            InitializeComponent();

            // Enable drag-and-drop for the form
            this.AllowDrop = true;

            // Hook up the drag-and-drop event handlers
            this.DragEnter += WoWFontChanger_DragEnter;
            this.DragDrop += WoWFontChanger_DragDrop;

            // Set up tooltips for buttons and other UI elements
            SetupTooltips();
        }

        private void SetupTooltips()
        {
            toolTip.SetToolTip(btnBrowse, "Click to browse your World of Warcraft installation folder.");
            toolTip.SetToolTip(btnReplace, "Click to replace the selected fonts.");
            toolTip.SetToolTip(btnRestoreDefaults, "Click to restore default WoW fonts by deleting the custom Fonts folder.");

            // Detailed tooltips for checkboxes
            toolTip.SetToolTip(chkUIFont, "Main UI Font (Friz Quadrata) - Used throughout most of the game interface, including player names, tooltips, and dialogues.");
            toolTip.SetToolTip(chkNormalFont, "Normal Numbers Font (Arial Narrow) - Used for chat windows, quest XP rewards, vendor quantities, and action button numbers.");
            toolTip.SetToolTip(chkHugeFont, "Huge Numbers Font (Skurri) - Appears over player and pet portraits during combat, showing incoming healing or damage.");
            toolTip.SetToolTip(chkQuestFont, "Quest Log Font (Morpheus) - Used for quest title headers, mail text, and readable in-game books.");
        }

        private void WoWFontChanger_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // Allow copy operation
            }
        }

        private void WoWFontChanger_DragDrop(object sender, DragEventArgs e)
        {
            fontFiles = (string[])e.Data.GetData(DataFormats.FileDrop);
            string loadedFonts = string.Empty;

            foreach (string file in fontFiles)
            {
                if (Path.GetExtension(file).ToLower() == ".ttf" || Path.GetExtension(file).ToLower() == ".otf")
                {
                    loadedFonts += Path.GetFileName(file) + Environment.NewLine;
                }
                else
                {
                    MessageBox.Show("Invalid file type. Only .ttf and .otf files are accepted.");
                }
            }

            lblLoadedFont.Text = !string.IsNullOrEmpty(loadedFonts)
                ? $"Loaded Fonts:\n{loadedFonts}"
                : "No valid font files loaded.";

            if (!string.IsNullOrEmpty(loadedFonts))
            {
                MessageBox.Show("Font(s) loaded successfully! Ready to press 'Replace Fonts'.");
            }
        }

        private void btnReplace_Click(object sender, EventArgs e)
        {
            string wowPath = txtWoWPath.Text;

            // Ensure the path is not empty
            if (string.IsNullOrEmpty(wowPath))
            {
                MessageBox.Show("Please select a valid World of Warcraft installation folder.");
                return;
            }

            // Validate the WoW path
            if (!IsValidWoWFolder(wowPath))
            {
                MessageBox.Show("Please select a valid World of Warcraft folder (_retail_, _ptr_, _classic_, or _beta_).");
                return;
            }

            // Check if at least one font option is selected
            if (!chkUIFont.Checked && !chkNormalFont.Checked && !chkHugeFont.Checked && !chkQuestFont.Checked)
            {
                MessageBox.Show("Please select at least one font to replace.");
                return;
            }

            // Check if WoW is running
            if (IsWoWRunning())
            {
                MessageBox.Show("World of Warcraft is currently running. The font changes will take effect after you restart WoW.");
            }

            string fontsDir = Path.Combine(wowPath, "Fonts");
            string backupDir = Path.Combine(fontsDir, "backup");

            if (fontFiles == null || fontFiles.Length == 0)
            {
                MessageBox.Show("No font files were added. Please drag and drop font files before replacing.");
                return;
            }

            Directory.CreateDirectory(fontsDir);
            Directory.CreateDirectory(backupDir);

            try
            {
                foreach (string file in fontFiles)
                {
                    if (Path.GetExtension(file).ToLower() != ".ttf" && Path.GetExtension(file).ToLower() != ".otf")
                    {
                        MessageBox.Show($"Skipping non-font file: {Path.GetFileName(file)}");
                        continue;
                    }

                    ReplaceSelectedFonts(file, fontsDir, backupDir);
                }

                MessageBox.Show("Fonts replaced successfully.");
            }
            catch (Exception ex)
            {
                LogError(ex);
                MessageBox.Show($"Error replacing fonts: {ex.Message}");
            }
        }

        private void ReplaceSelectedFonts(string file, string fontsDir, string backupDir)
        {
            if (chkUIFont.Checked) BackupAndReplace(fontsDir, backupDir, "FRIZQT__.TTF", file);
            if (chkNormalFont.Checked) BackupAndReplace(fontsDir, backupDir, "ARIALN.TTF", file);
            if (chkHugeFont.Checked) BackupAndReplace(fontsDir, backupDir, "SKURRI.TTF", file);
            if (chkQuestFont.Checked) BackupAndReplace(fontsDir, backupDir, "MORPHEUS.TTF", file);
        }

        private void BackupAndReplace(string fontsDir, string backupDir, string wowFontFileName, string newFontFile)
        {
            string wowFontPath = Path.Combine(fontsDir, wowFontFileName);
            string backupPath = Path.Combine(backupDir, wowFontFileName);

            if (!File.Exists(backupPath) && File.Exists(wowFontPath))
            {
                File.Copy(wowFontPath, backupPath);
            }

            File.Copy(newFontFile, wowFontPath, true);
        }

        private bool IsValidWoWFolder(string wowPath)
        {
            string folderName = new DirectoryInfo(wowPath).Name;
            return validWoWFolders.Contains(folderName.ToLower());
        }

        private void btnRestoreDefaults_Click(object sender, EventArgs e)
        {
            string wowPath = txtWoWPath.Text;

            if (string.IsNullOrEmpty(wowPath) || !Directory.Exists(wowPath))
            {
                MessageBox.Show("Please select a valid World of Warcraft installation folder.");
                return;
            }

            string fontsDir = Path.Combine(wowPath, "Fonts");

            if (Directory.Exists(fontsDir))
            {
                try
                {
                    RemoveReadOnlyAttribute(fontsDir);
                    Directory.Delete(fontsDir, true);
                    MessageBox.Show("Fonts folder deleted. World of Warcraft will use default fonts now.");
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Error: You don't have permission to delete the Fonts folder. {ex.Message}");
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Error: The Fonts folder is in use or locked. Please make sure World of Warcraft is closed. {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting Fonts folder: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("No custom Fonts folder found. The default fonts are already in use.");
            }
        }

        private void RemoveReadOnlyAttribute(string folderPath)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(folderPath);

            if ((dirInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                dirInfo.Attributes &= ~FileAttributes.ReadOnly;
            }

            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                if ((file.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    file.Attributes &= ~FileAttributes.ReadOnly;
                }
            }
        }

        private bool IsWoWRunning()
        {
            return System.Diagnostics.Process.GetProcessesByName("Wow").Length > 0;
        }

        private void LogError(Exception ex)
        {
            string logPath = Path.Combine(Application.StartupPath, "ErrorLog.txt");
            using (StreamWriter writer = new StreamWriter(logPath, true))
            {
                writer.WriteLine($"{DateTime.Now}: {ex.Message}");
                writer.WriteLine(ex.StackTrace);
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select your World of Warcraft installation folder";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = folderDialog.SelectedPath;

                    if (IsValidWoWFolder(selectedPath))
                    {
                        txtWoWPath.Text = selectedPath;
                    }
                    else
                    {
                        MessageBox.Show("Invalid folder selected. Please select a World of Warcraft folder (_retail_, _ptr_, _classic_, or _beta_).");
                    }
                }
            }
        }
    }
}
