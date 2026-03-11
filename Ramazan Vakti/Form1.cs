using System.Diagnostics;
using System.Runtime.InteropServices;
using static System.Net.WebRequestMethods;

namespace Ramazan_Vakti {
    public partial class Form1 : Form {

        #region Fields
        private DateTime _lastCheckedDay = DateTime.Now.Date;
        private PrayerTimes _prayerTimes = new PrayerTimes();
        private Label? _activeLabel = null;
        private FormSettings? _settingsForm;
        private string? _fajr, _dhuhr, _asr, _maghrib, _isha, _tomorrowFajr;
        private const int OriginalWidth = 220;
        private const int OriginalHeight = 310;
        private System.Windows.Forms.Timer? _retryTimer;
        private bool _retryScheduled = false;
        private bool _loaded = false;
        private int _lastSnapX = int.MinValue;
        private int _lastSnapY = int.MinValue;
        private bool _dragging = false;
        private Point _dragOffset;
        #endregion

        #region Components
        // Pencereyi en alta almak için
        private const int HWND_BOTTOM = 1;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const int WM_NCHITTEST = 0x84;
        private const int RESIZE_BORDER = 8;
        private const int SNAP_THRESHOLD = 10;
        private const int SNAP_GAP = 5;
        private const int GWL_STYLE = -16;
        private const uint WS_CAPTION = 0x00C00000;
        private const uint WS_CHILD = 0x40000000;

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out REKT lpRect);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct REKT {
            public int Left, Top, Right, Bottom;
        }
        #endregion

        public Form1() {
            InitializeComponent();
            SetWindowPos(this.Handle, (IntPtr)HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            lblKalanZaman.Visible = false;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // Apply saved transparency percentage (0-100)
            int pct = Properties.Settings.Default.TransparencyPercent;
            this.Opacity = Math.Clamp(pct / 100.0, 0.5, 1.0);

            // Restore compact/expanded state
            if (Properties.Settings.Default.IsCompact) {
                MaximumSize = new Size(220, 140);
                Size = new Size(220, 140);
                lblChangeSize.Text = "⏷";
            }

            SetFormPosition();
            _loaded = true;

            await GetPrayerTimes();
        }

        private void ScheduleRetry() {
            if (_retryScheduled) return;
            _retryScheduled = true;
            _retryTimer?.Stop();
            _retryTimer?.Dispose();
            _retryTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            _retryTimer.Tick += async (s, e) => {
                _retryTimer!.Stop();
                _retryTimer.Dispose();
                _retryTimer = null;
                _retryScheduled = false;
                await GetPrayerTimes();
            };
            _retryTimer.Start();
        }

        #region Ramadan Timetable Operations 
        public async Task GetPrayerTimes() {
            try {
                string selectedCity = Properties.Settings.Default.SelectedCity ?? "İstanbul";
                lblCity.Text = selectedCity;

                var result = await _prayerTimes.GetPrayerTimesAsync(selectedCity);
                if (new[] { result.Fajr, result.Dhuhr, result.Asr, result.Maghrib, result.Isha, result.TomorrowFajr }.Any(string.IsNullOrEmpty)) {
                    MessageBox.Show("Prayer times are missing or incorrect!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                (_fajr, _dhuhr, _asr, _maghrib, _isha, _tomorrowFajr) = (result.Fajr, result.Dhuhr, result.Asr, result.Maghrib, result.Isha, result.TomorrowFajr);

                // Arayüzü güncelleme
                lblTime1.Text = $"İmsak: {_fajr}";
                lblTime2.Text = $"Öğle: {_dhuhr}";
                lblTime3.Text = $"İkindi: {_asr}";
                lblTime4.Text = $"Akşam: {_maghrib}";
                lblTime5.Text = $"Yatsı: {_isha}";
                lblRamadanDay.Text = $"{result.HijriDay}. Gün";

                lblKalanZaman.Visible = true;
                if (!timerRemainingTime.Enabled) timerRemainingTime.Enabled = true;
            } catch {
                // Network or API error – retry silently every 3 seconds
                ScheduleRetry();
            }
        }

        private async void timerRemainingTime_Tick(object sender, EventArgs e) {
            try {
                DateTime currentTime = DateTime.Now;
                DateTime today = currentTime.Date;

                // Eğer gün değiştiyse, namaz vakitlerini güncelle
                if (_lastCheckedDay != today) {
                    await GetPrayerTimes();
                    _lastCheckedDay = today;
                }
                if (string.IsNullOrWhiteSpace(_fajr) || string.IsNullOrWhiteSpace(_maghrib) || string.IsNullOrWhiteSpace(_tomorrowFajr)) return;

                // Tarihleri oluştur
                DateTime suhoorTime = today.Add(DateTime.Parse(_fajr).TimeOfDay);
                DateTime iftarTime = today.Add(DateTime.Parse(_maghrib).TimeOfDay);
                DateTime tomorrowSuhoorTime = today.AddDays(1).Add(DateTime.Parse(_tomorrowFajr).TimeOfDay);

                // Hangi Label aktif olacak?
                Label? activeLabelNew = null;
                TimeSpan remainingTime;

                if (currentTime < suhoorTime) {
                    remainingTime = suhoorTime - currentTime;
                    activeLabelNew = lblTime1;
                    CheckReminder(remainingTime, "Sahur Vakti Yaklaşıyor!", "Sahura 15 dakika kaldı.");
                } else if (currentTime < iftarTime) {
                    remainingTime = iftarTime - currentTime;
                    activeLabelNew = lblTime4;
                    CheckReminder(remainingTime, "İftar Vakti Yaklaşıyor!", "İftara 15 dakika kaldı.");
                } else {
                    remainingTime = tomorrowSuhoorTime - currentTime;
                    activeLabelNew = lblTime1;
                }

                lblKalanZaman.Text = $"Kalan Süre\n{remainingTime.Hours:D2}:{remainingTime.Minutes:D2}:{remainingTime.Seconds:D2}";

                // Label renk değişimi yönetimi
                if (_activeLabel != activeLabelNew) {
                    if (_activeLabel != null) {
                        _activeLabel.ForeColor = Color.WhiteSmoke;
                    }

                    if (activeLabelNew != null) {
                        activeLabelNew.ForeColor = Color.Red;
                        _activeLabel = activeLabelNew;
                    }
                }
            } catch {
                // Silently ignore tick errors; GetPrayerTimes will schedule a retry if needed
            }
        }

        #endregion

        #region Notifications
        private void CheckReminder(TimeSpan remainingTime, string title, string message) {
            if (Properties.Settings.Default.reminder && remainingTime.Hours == 0 && remainingTime.Minutes == 15 && remainingTime.Seconds == 0) {
                ShowNotification(title, message);
            }
        }

        private void ShowNotification(string title, string message) {
            reminderNotification.Visible = true;
            reminderNotification.BalloonTipTitle = title;
            reminderNotification.BalloonTipText = message;
            reminderNotification.ShowBalloonTip(3000);
        }
        #endregion

        #region Buttons
        private void btnSettings_Click(object sender, EventArgs e) {
            if (_settingsForm == null || _settingsForm.IsDisposed) {
                _settingsForm = new FormSettings();
                _settingsForm.CityChanged += OnCityChanged; // Tek seferlik ekleniyor
            }

            // Formu konumlandır
            PositionSettingsForm();

            // Formu göster
            _settingsForm.ShowDialog();
        }


        private async void OnCityChanged(object? sender, EventArgs e) {
            await GetPrayerTimes();
        }

        private void PositionSettingsForm() {
            int formX = this.Location.X;
            int formY = this.Location.Y;
            if (_settingsForm != null) {
                int settingsWidth = _settingsForm.Width; // Varsayılan bir genişlik
                _settingsForm.StartPosition = FormStartPosition.Manual;
                if (formX - settingsWidth - 2 >= 0) {
                    _settingsForm.Location = new Point(formX - settingsWidth - 2, formY);
                } else {
                    _settingsForm.Location = new Point(formX + this.Width + 2, formY);
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e) => Application.Exit();
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Application.Exit();
        #endregion
        
        #region Movable Form
        private void Form1_MouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                _dragging = true;
                _dragOffset = e.Location;
                _lastSnapX = int.MinValue;
                _lastSnapY = int.MinValue;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e) {
            if (_dragging && e.Button == MouseButtons.Left) {
                Point cursorScreen = Cursor.Position;
                int newX = cursorScreen.X - _dragOffset.X;
                int newY = cursorScreen.Y - _dragOffset.Y;
                var snapped = ApplySnap(newX, newY);
                this.Location = new Point(snapped.X, snapped.Y);
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                _dragging = false;
            }
        }
        #endregion

        #region Change Widget Size
        private void lblChangeSize_Click(object sender, EventArgs e) {
            if (Height >= OriginalHeight) {
                MaximumSize = new Size(220, 140);
                Size = new Size(220, 140);
                lblChangeSize.Text = "⏷";
                Properties.Settings.Default.IsCompact = true;
            } else {
                MinimumSize = new Size(220, 310);
                MaximumSize = new Size(225, 320);
                Size = new Size(220, 310);
                lblChangeSize.Text = "⏶";
                Properties.Settings.Default.IsCompact = false;
            }
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Startup Position & Resizable Form
        public void SetFormPosition() {
            this.StartPosition = FormStartPosition.Manual;

            int savedX = Properties.Settings.Default.LastX;
            int savedY = Properties.Settings.Default.LastY;

            if (savedX >= 0 && savedY >= 0 && IsPositionOnScreen(savedX, savedY)) {
                this.Location = new Point(savedX, savedY);
            } else {
                int pointX = (Screen.PrimaryScreen != null) ? Screen.PrimaryScreen.Bounds.Width - this.Width - 10 : 100;
                int pointY = 10;
                this.Location = new Point(pointX, pointY);
            }
        }

        private bool IsPositionOnScreen(int x, int y) {
            var rect = new Rectangle(x, y, this.Width, this.Height);
            foreach (var screen in Screen.AllScreens) {
                if (screen.WorkingArea.IntersectsWith(rect))
                    return true;
            }
            return false;
        }

        private void Form1_LocationChanged(object sender, EventArgs e) {
            if (_loaded && this.WindowState == FormWindowState.Normal) {
                Properties.Settings.Default.LastX = this.Location.X;
                Properties.Settings.Default.LastY = this.Location.Y;
                Properties.Settings.Default.Save();
            }
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == WM_NCHITTEST) {
                Point cursor = PointToClient(Cursor.Position);
                int w = Width, h = Height;

                if (cursor.X < RESIZE_BORDER) m.Result = (IntPtr)(cursor.Y < RESIZE_BORDER ? 13 : (cursor.Y > h - RESIZE_BORDER ? 16 : 10)); 
                else if (cursor.X > w - RESIZE_BORDER) m.Result = (IntPtr)(cursor.Y < RESIZE_BORDER ? 14 : (cursor.Y > h - RESIZE_BORDER ? 17 : 11));
                else if (cursor.Y < RESIZE_BORDER) m.Result = (IntPtr)12;
                else if (cursor.Y > h - RESIZE_BORDER) m.Result = (IntPtr)15;
                else base.WndProc(ref m);
                return;
            }
            base.WndProc(ref m);
        }

        private void Form1_Resize(object sender, EventArgs e) {
            if (OriginalWidth == 0 || OriginalHeight == 0 || Height < 310) return; // Bölme hatasını önlemek için kontrol
            // Form büyüklüğüne göre ölçeklendirme faktörü hesapla
            float widthScale = (float)this.ClientSize.Width / OriginalWidth;
            float heightScale = (float)this.ClientSize.Height / OriginalHeight;
            float scaleFactor = Math.Min(widthScale, heightScale); // Oranı minimuma göre ayarla

            int baseFontSize = 12; // Orijinal font boyutu
            int maxFontSize = 14;  // Maksimum font boyutu

            // Label fontlarını güncelle
            lblTime1.Font = new Font("Segoe UI Semibold", Math.Min(baseFontSize * scaleFactor, maxFontSize), FontStyle.Bold);
            lblTime2.Font = new Font("Segoe UI Semibold", Math.Min(baseFontSize * scaleFactor, maxFontSize), FontStyle.Bold);
            lblTime3.Font = new Font("Segoe UI Semibold", Math.Min(baseFontSize * scaleFactor, maxFontSize), FontStyle.Bold);
            lblTime4.Font = new Font("Segoe UI Semibold", Math.Min(baseFontSize * scaleFactor, maxFontSize), FontStyle.Bold);
            lblTime5.Font = new Font("Segoe UI Semibold", Math.Min(baseFontSize * scaleFactor, maxFontSize), FontStyle.Bold);

            // Kalan zaman label'ı biraz daha büyük başlasın
            lblKalanZaman.Font = new Font("Segoe UI Semibold", Math.Min(baseFontSize * scaleFactor, maxFontSize), FontStyle.Bold);
        }
        #endregion

        #region Snap to Widgets
        private Point ApplySnap(int newX, int newY) {
            int w = this.Width;
            int h = this.Height;

            bool skipSnapX = _lastSnapX != int.MinValue && Math.Abs(newX - _lastSnapX) > SNAP_THRESHOLD;
            bool skipSnapY = _lastSnapY != int.MinValue && Math.Abs(newY - _lastSnapY) > SNAP_THRESHOLD;

            if (skipSnapX) _lastSnapX = int.MinValue;
            if (skipSnapY) _lastSnapY = int.MinValue;

            var others = GetOtherWidgetWindows();

            bool snappedX = false, snappedY = false;
            int bestDx = int.MaxValue, bestDy = int.MaxValue;
            int snapX = newX, snapY = newY;
            REKT? snapSourceX = null, snapSourceY = null;

            int rLeft = newX, rTop = newY, rRight = newX + w, rBottom = newY + h;

            if (!skipSnapX || !skipSnapY) {
                foreach (var other in others) {
                    bool vertOverlap = rTop < other.Bottom && rBottom > other.Top;
                    bool horizOverlap = rLeft < other.Right && rRight > other.Left;

                    if (!skipSnapX && vertOverlap) {
                        int d = Math.Abs(rRight - (other.Left - SNAP_GAP));
                        if (d < SNAP_THRESHOLD && d < bestDx) { snapX = other.Left - SNAP_GAP - w; bestDx = d; snappedX = true; snapSourceX = other; }
                        d = Math.Abs(rLeft - (other.Right + SNAP_GAP));
                        if (d < SNAP_THRESHOLD && d < bestDx) { snapX = other.Right + SNAP_GAP; bestDx = d; snappedX = true; snapSourceX = other; }
                    }

                    if (!skipSnapY && horizOverlap) {
                        int d = Math.Abs(rBottom - (other.Top - SNAP_GAP));
                        if (d < SNAP_THRESHOLD && d < bestDy) { snapY = other.Top - SNAP_GAP - h; bestDy = d; snappedY = true; snapSourceY = other; }
                        d = Math.Abs(rTop - (other.Bottom + SNAP_GAP));
                        if (d < SNAP_THRESHOLD && d < bestDy) { snapY = other.Bottom + SNAP_GAP; bestDy = d; snappedY = true; snapSourceY = other; }
                    }
                }

                foreach (var screen in Screen.AllScreens) {
                    var wa = screen.WorkingArea;
                    if (!skipSnapX) {
                        int d = Math.Abs(rLeft - wa.Left);
                        if (d < SNAP_THRESHOLD && d < bestDx) { snapX = wa.Left; bestDx = d; snappedX = true; snapSourceX = null; }
                        d = Math.Abs(rRight - wa.Right);
                        if (d < SNAP_THRESHOLD && d < bestDx) { snapX = wa.Right - w; bestDx = d; snappedX = true; snapSourceX = null; }
                    }
                    if (!skipSnapY) {
                        int d = Math.Abs(rTop - wa.Top);
                        if (d < SNAP_THRESHOLD && d < bestDy) { snapY = wa.Top; bestDy = d; snappedY = true; snapSourceY = null; }
                        d = Math.Abs(rBottom - wa.Bottom);
                        if (d < SNAP_THRESHOLD && d < bestDy) { snapY = wa.Bottom - h; bestDy = d; snappedY = true; snapSourceY = null; }
                    }
                }
            }

            if (snappedX && snapSourceX.HasValue) {
                var o = snapSourceX.Value;
                int dTop = Math.Abs(rTop - o.Top);
                int dBot = Math.Abs(rBottom - o.Bottom);
                if (dTop < SNAP_THRESHOLD) { snapY = o.Top; snappedY = true; }
                else if (dBot < SNAP_THRESHOLD) { snapY = o.Bottom - h; snappedY = true; }
            }
            if (snappedY && snapSourceY.HasValue) {
                var o = snapSourceY.Value;
                int dLeft = Math.Abs(rLeft - o.Left);
                int dRight = Math.Abs(rRight - o.Right);
                if (dLeft < SNAP_THRESHOLD) { snapX = o.Left; snappedX = true; }
                else if (dRight < SNAP_THRESHOLD) { snapX = o.Right - w; snappedX = true; }
            }

            if (snappedX) _lastSnapX = snapX; else _lastSnapX = int.MinValue;
            if (snappedY) _lastSnapY = snapY; else _lastSnapY = int.MinValue;

            return new Point(snappedX ? snapX : newX, snappedY ? snapY : newY);
        }

        private List<REKT> GetOtherWidgetWindows() {
            var windows = new List<REKT>();
            IntPtr thisHandle = this.Handle;

            EnumWindows((hWnd, lParam) => {
                if (hWnd == thisHandle) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int style = GetWindowLong(hWnd, GWL_STYLE);
                if ((style & (int)WS_CHILD) != 0) return true;
                if ((style & (int)WS_CAPTION) != 0) return true;

                GetWindowRect(hWnd, out REKT r);
                int rw = r.Right - r.Left;
                int rh = r.Bottom - r.Top;

                if (rw >= 50 && rw <= 500 && rh >= 50 && rh <= 500) {
                    windows.Add(r);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }
        #endregion
    }
}
