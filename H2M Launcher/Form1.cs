using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace H2M_Launcher
{
    enum LauncherStatus
    {
        idle,
        ready,
        failed,
        downloadingGame,
        downloadingUpdate,
        downloadingDLC
    }

    public partial class Form1 : Form
    {
        private string gamePath;
        private string gameExe;

        private string gameZip;
        private string gameExtZip;
        private string gameZipUrl;

        private string versionFile;
        private string versionFileUrl;

        private LauncherStatus _status;
        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.idle:
                        CurrentProgressBar.Visible = false;
                        MainButton.Text = "Select Game Directory";
                        MainButton.Enabled = true;
                        CurrentStatusLabel.Text = "Idle";
                        break;
                    case LauncherStatus.ready:
                        CurrentProgressBar.Visible = false;
                        MainButton.Text = "Play";
                        MainButton.Enabled = true;
                        CurrentStatusLabel.Text = "Ready";
                        break;
                    case LauncherStatus.failed:
                        CurrentProgressBar.Visible = false;
                        MainButton.Text = "Download Failed - Retry";
                        MainButton.Enabled = true;
                        CurrentStatusLabel.Text = "Download Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        CurrentProgressBar.Visible = true;
                        MainButton.Text = "Please Wait...";
                        MainButton.Enabled = false;
                        CurrentStatusLabel.Text = "Downloading Files";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        CurrentProgressBar.Visible = true;
                        MainButton.Text = "Please Wait...";
                        MainButton.Enabled = false;
                        CurrentStatusLabel.Text = "Downloading Update";
                        break;
                    case LauncherStatus.downloadingDLC:
                        CurrentProgressBar.Visible = true;
                        MainButton.Text = "Please Wait...";
                        MainButton.Enabled = false;
                        CurrentStatusLabel.Text = "Downloading Files";
                        break;
                    default:
                        break;
                }
            }
        }

        public Form1()
        {
            InitializeComponent();

            if (!Directory.Exists("LauncherCache"))
            {
                Directory.CreateDirectory("LauncherCache");
            }

            if (!File.Exists("LauncherCache/GamePath.txt"))
            {
                File.WriteAllText("LauncherCache/GamePath.txt", Directory.GetCurrentDirectory());
            }

            gamePath = File.ReadAllText("LauncherCache/GamePath.txt");
            gameExe = Path.Combine(gamePath, "h2m-mod.exe");

            gameZip = Path.Combine(gamePath, "h2m-mod.zip");
            gameExtZip = Path.Combine(gamePath, "h2m-mod");
            gameZipUrl = "https://spyderrock.com/gN584506-h2m-mod.zip";

            versionFile = "LauncherCache/Version.txt";
            versionFileUrl = "https://spyderrock.com/pniQ3212-Version.txt";
        }

        private void MainButton_Click(object sender, EventArgs e)
        {
            if (MainButton.Text == "Select Game Directory" && Status == LauncherStatus.idle)
            {
                string dummyFileName = "Save Here";

                SaveFileDialog sf = new SaveFileDialog();
                sf.FileName = dummyFileName;

                if (sf.ShowDialog() == DialogResult.OK)
                {
                    string savePath = Path.GetDirectoryName(sf.FileName);
                    File.WriteAllText("LauncherCache/GamePath.txt", savePath);
                    CheckForUpdates();
                }
            }
            else if (MainButton.Text == "Play" && File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = gamePath;
                Process.Start(startInfo);

                Close();
            }
            else
            {
                MessageBox.Show("Error!");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("LauncherCache/Version.txt"))
                VersionLabel.Text = File.ReadAllText("LauncherCache/Version.txt");

            if (File.Exists(gameExe))
            {
                Status = LauncherStatus.ready;
            }
            else
            {
                Status = LauncherStatus.idle;
            }
        }

        private void InstallGameFiles(bool _isUpdate, Version _onlineVersion)
        {
            try
            {
                WebClient webClient = new WebClient();
                if (_isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                    _onlineVersion = new Version(webClient.DownloadString(versionFileUrl));
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                }

                webClient.DownloadProgressChanged += (s, e) =>
                {
                    CurrentProgressBar.Value = e.ProgressPercentage;
                };

                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadGameCompletedCallback);
                webClient.DownloadFileAsync(new Uri(gameZipUrl), gameZip, _onlineVersion);
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex.Message}");
            }
        }

        private async void DownloadGameCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            try
            {
                CurrentStatusLabel.Text = "Extracting Files";

                string onlineVersion = ((Version)e.UserState).ToString();
                await ZipExtractor.ProcessZipAsync(gameZip, gamePath);
                File.Delete(gameZip);

                File.WriteAllText(versionFile, onlineVersion);

                VersionLabel.Text = File.ReadAllText(versionFile);
                Status = LauncherStatus.ready;

                CurrentStatusLabel.Text = "Ready";
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error finishing download: {ex.Message}");
            }
        }

        private void CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionLabel.Text = localVersion.ToString();

                try
                {
                    WebClient webClient = new WebClient();
                    Version onlineVersion = new Version(webClient.DownloadString(versionFileUrl));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        //MessageBox.Show("Version mismatch!");
                        InstallGameFiles(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex.Message}");
                }
            }
            else
            {
                //MessageBox.Show("Version file not found!");
                InstallGameFiles(false, Version.zero);
            }
        }

        struct Version
        {
            internal static Version zero = new Version(0, 0, 0);

            private short major;
            private short minor;
            private short subMinor;

            internal Version(short _major, short _minor, short _subMinor)
            {
                major = _major;
                minor = _minor;
                subMinor = _subMinor;
            }
            internal Version(string _version)
            {
                string[] versionStrings = _version.Split('.');
                if (versionStrings.Length != 3)
                {
                    major = 0;
                    minor = 0;
                    subMinor = 0;
                    return;
                }

                major = short.Parse(versionStrings[0]);
                minor = short.Parse(versionStrings[1]);
                subMinor = short.Parse(versionStrings[2]);
            }

            internal bool IsDifferentThan(Version _otherVersion)
            {
                if (major != _otherVersion.major)
                {
                    return true;
                }
                else
                {
                    if (minor != _otherVersion.minor)
                    {
                        return true;
                    }
                    else
                    {
                        if (subMinor != _otherVersion.subMinor)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public override string ToString()
            {
                return $"{major}.{minor}.{subMinor}";
            }
        }
    }
}
