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
        private WidgetHelper? _widgetHelper;
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
        #endregion

        public Form1() {
            InitializeComponent();
            SetWindowPos(this.Handle, (IntPtr)HWND_BOTTOM, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            _widgetHelper = new WidgetHelper(this);
            lblKalanZaman.Visible = false;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            // Apply saved transparency percentage (0-100)
            int pct = Properties.Settings.Default.TransparencyPercent;
            this.Opacity = Math.Clamp(pct / 100.0, 0.5, 1.0);

            // Restore compact/expanded state
            if (Properties.Settings.Default.IsCompact) {
                MaximumSize = new Size(220, 310);
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

        #region Change Widget Size
        private void lblChangeSize_Click(object sender, EventArgs e) {
            if (Height >= OriginalHeight) {
                Size = new Size(220, 140);
                lblChangeSize.Text = "⏷";
                Properties.Settings.Default.IsCompact = true;
            } else {
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
            if (_widgetHelper != null && _widgetHelper.HandleWndProc(ref m))
                return;
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
    }
}
