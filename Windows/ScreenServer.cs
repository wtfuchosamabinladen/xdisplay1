using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace XDisplay
{
    public class MainForm : Form
    {
        private const int VIDEO_PORT = 5555;
        private const int INPUT_PORT = 5556;

        private TcpListener videoListener;
        private TcpListener inputListener;
        private bool running = false;

        private Label statusLabel;
        private Button startBtn;
        private TrackBar qualityBar;
        private Label qualityValueLabel;
        private ComboBox fpsBox;
        private int jpegQuality = 60;
        private int targetFps = 30;

        // P/Invoke for mouse simulation
        [DllImport("user32.dll")] static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")] static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr extra);
        const uint MOUSEEVENTF_LEFTDOWN  = 0x0002;
        const uint MOUSEEVENTF_LEFTUP    = 0x0004;

        public MainForm()
        {
            Text = "XDisplay Server";
            Size = new Size(440, 340);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            var title = new Label
            {
                Text = "XDisplay Server",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                Top = 18, Left = 0, Width = 440,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White, BackColor = Color.Transparent
            };

            string myIp = GetLocalIP();
            var ipLabel = new Label
            {
                Text = "Your Windows IP:  " + myIp,
                Font = new Font("Segoe UI", 10),
                Top = 58, Left = 20, Width = 400,
                ForeColor = Color.LightGray, BackColor = Color.Transparent
            };

            statusLabel = new Label
            {
                Text = "● Stopped",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Top = 82, Left = 20, Width = 400,
                ForeColor = Color.OrangeRed, BackColor = Color.Transparent
            };

            var qLabel = new Label { Text = "Quality:", Top = 120, Left = 20, Width = 70, ForeColor = Color.LightGray, BackColor = Color.Transparent };
            qualityBar = new TrackBar { Minimum = 10, Maximum = 100, Value = jpegQuality, Top = 115, Left = 90, Width = 210, TickFrequency = 10, BackColor = Color.FromArgb(30, 30, 30) };
            qualityValueLabel = new Label { Text = jpegQuality + "%", Top = 120, Left = 305, Width = 50, ForeColor = Color.White, BackColor = Color.Transparent };
            qualityBar.ValueChanged += (s, e) => { jpegQuality = qualityBar.Value; qualityValueLabel.Text = jpegQuality + "%"; };

            var fLabel = new Label { Text = "FPS:", Top = 158, Left = 20, Width = 70, ForeColor = Color.LightGray, BackColor = Color.Transparent };
            fpsBox = new ComboBox { Top = 155, Left = 90, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(50, 50, 50), ForeColor = Color.White };
            fpsBox.Items.AddRange(new object[] { "10", "15", "20", "30" });
            fpsBox.SelectedItem = "30";
            fpsBox.SelectedIndexChanged += (s, e) => int.TryParse(fpsBox.SelectedItem?.ToString(), out targetFps);

            startBtn = new Button
            {
                Text = "▶  Start Server",
                Top = 200, Left = 20, Width = 180, Height = 44,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 160, 80), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            startBtn.FlatAppearance.BorderSize = 0;
            startBtn.Click += ToggleServer;

            var usbBtn = new Button
            {
                Text = "🔌  Setup USB (ADB)",
                Top = 200, Left = 215, Width = 190, Height = 44,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(50, 100, 170), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
            usbBtn.FlatAppearance.BorderSize = 0;
            usbBtn.Click += SetupUSB;

            var hint = new Label
            {
                Text = "WiFi: Enter your Windows IP in the Android app.\nUSB: Click 'Setup USB' with device plugged in, then use 127.0.0.1 in the app.",
                Top = 260, Left = 20, Width = 400,
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray, BackColor = Color.Transparent
            };

            Controls.AddRange(new Control[] { title, ipLabel, statusLabel, qLabel, qualityBar, qualityValueLabel, fLabel, fpsBox, startBtn, usbBtn, hint });
        }

        string GetLocalIP()
        {
            try
            {
                using (var s = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    s.Connect("8.8.8.8", 65530);
                    return ((IPEndPoint)s.LocalEndPoint).Address.ToString();
                }
            }
            catch { return "Unknown"; }
        }

        void ToggleServer(object sender, EventArgs e)
        {
            if (!running) StartServer(); else StopServer();
        }

        void StartServer()
        {
            running = true;
            startBtn.Text = "■  Stop Server";
            startBtn.BackColor = Color.FromArgb(160, 40, 40);
            SetStatus("● Waiting for Android...", Color.DarkOrange);
            new Thread(VideoServerLoop) { IsBackground = true }.Start();
            new Thread(InputServerLoop) { IsBackground = true }.Start();
        }

        void StopServer()
        {
            running = false;
            try { videoListener?.Stop(); } catch { }
            try { inputListener?.Stop(); } catch { }
            startBtn.Text = "▶  Start Server";
            startBtn.BackColor = Color.FromArgb(40, 160, 80);
            SetStatus("● Stopped", Color.OrangeRed);
        }

        void VideoServerLoop()
        {
            try
            {
                videoListener = new TcpListener(IPAddress.Any, VIDEO_PORT);
                videoListener.Start();
                while (running)
                {
                    var client = videoListener.AcceptTcpClient();
                    client.SendBufferSize = 2 * 1024 * 1024;
                    SetStatus("● Connected — streaming!", Color.LimeGreen);
                    new Thread(() => HandleVideoClient(client)) { IsBackground = true }.Start();
                }
            }
            catch { }
        }

        void InputServerLoop()
        {
            try
            {
                inputListener = new TcpListener(IPAddress.Any, INPUT_PORT);
                inputListener.Start();
                while (running)
                {
                    var client = inputListener.AcceptTcpClient();
                    new Thread(() => HandleInputClient(client)) { IsBackground = true }.Start();
                }
            }
            catch { }
        }

        void HandleVideoClient(TcpClient client)
        {
            var stream = client.GetStream();
            var screen = Screen.PrimaryScreen.Bounds;
            int interval = 1000 / Math.Max(1, targetFps);

            // Send resolution header: magic=0xFFFFFFFF (big-endian), width, height
            WriteIntBE(stream, -1);
            WriteIntBE(stream, screen.Width);
            WriteIntBE(stream, screen.Height);

            var codec = GetJpegCodec();
            var encParams = new EncoderParameters(1);

            while (running && client.Connected)
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);
                    using (var bmp = new Bitmap(screen.Width, screen.Height, PixelFormat.Format32bppRgb))
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(screen.Location, Point.Empty, screen.Size, CopyPixelOperation.SourceCopy);
                        using (var ms = new MemoryStream())
                        {
                            bmp.Save(ms, codec, encParams);
                            byte[] data = ms.ToArray();
                            WriteIntBE(stream, data.Length);
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
                catch { break; }

                int elapsed = (int)sw.ElapsedMilliseconds;
                if (elapsed < interval) Thread.Sleep(interval - elapsed);
            }
            client.Close();
            SetStatus("● Waiting for Android...", Color.DarkOrange);
        }

        void HandleInputClient(TcpClient client)
        {
            var stream = client.GetStream();
            var buf = new byte[9]; // 1 byte type + 4 bytes x + 4 bytes y (big-endian)
            while (running && client.Connected)
            {
                try
                {
                    int read = 0;
                    while (read < 9)
                    {
                        int n = stream.Read(buf, read, 9 - read);
                        if (n == 0) return;
                        read += n;
                    }
                    byte type = buf[0];
                    int x = ReadIntBE(buf, 1);
                    int y = ReadIntBE(buf, 5);
                    SetCursorPos(x, y);
                    if (type == 1) mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                    else if (type == 2) mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                }
                catch { break; }
            }
            client.Close();
        }

        // Big-endian helpers (to match Java's DataInputStream/DataOutputStream)
        static void WriteIntBE(Stream s, int val)
        {
            s.WriteByte((byte)((val >> 24) & 0xFF));
            s.WriteByte((byte)((val >> 16) & 0xFF));
            s.WriteByte((byte)((val >> 8)  & 0xFF));
            s.WriteByte((byte)( val        & 0xFF));
        }
        static int ReadIntBE(byte[] b, int offset)
        {
            return (b[offset] << 24) | (b[offset+1] << 16) | (b[offset+2] << 8) | b[offset+3];
        }

        ImageCodecInfo GetJpegCodec()
        {
            foreach (var c in ImageCodecInfo.GetImageEncoders())
                if (c.MimeType == "image/jpeg") return c;
            return null;
        }

        void SetStatus(string text, Color color)
        {
            if (statusLabel.InvokeRequired)
                statusLabel.Invoke(new Action(() => { statusLabel.Text = text; statusLabel.ForeColor = color; }));
            else { statusLabel.Text = text; statusLabel.ForeColor = color; }
        }

        void SetupUSB(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("cmd.exe",
                    $"/c adb forward tcp:{VIDEO_PORT} tcp:{VIDEO_PORT} && adb forward tcp:{INPUT_PORT} tcp:{INPUT_PORT} && pause")
                { UseShellExecute = true });
                MessageBox.Show(
                    $"ADB forwarding configured!\n\nPorts {VIDEO_PORT} (video) and {INPUT_PORT} (input) forwarded.\n\nIn the Android app, enter IP:  127.0.0.1",
                    "USB Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show(
                    "ADB not found.\n\nDownload Android Platform Tools from:\nhttps://developer.android.com/studio/releases/platform-tools\n\nExtract it and add to your PATH, then try again.",
                    "ADB Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
