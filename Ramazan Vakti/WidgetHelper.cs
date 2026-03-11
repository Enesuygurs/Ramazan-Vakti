using System.Runtime.InteropServices;

namespace Ramazan_Vakti {
    public class WidgetHelper {
        private readonly Form _form;
        private bool _dragging;
        private Point _dragOffset;
        private int _lastSnapX = int.MinValue;
        private int _lastSnapY = int.MinValue;

        private const int SNAP_THRESHOLD = 10;
        private const int SNAP_GAP = 5;
        private const int WM_NCHITTEST = 0x84;
        private const int RESIZE_BORDER = 8;

        #region P/Invoke
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

        public WidgetHelper(Form form) {
            _form = form;
            _form.MouseDown += OnMouseDown;
            _form.MouseMove += OnMouseMove;
            _form.MouseUp += OnMouseUp;
        }

        #region Drag
        private void OnMouseDown(object? sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                _dragging = true;
                _dragOffset = e.Location;
                _lastSnapX = int.MinValue;
                _lastSnapY = int.MinValue;
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e) {
            if (_dragging && e.Button == MouseButtons.Left) {
                Point cursorScreen = Cursor.Position;
                int newX = cursorScreen.X - _dragOffset.X;
                int newY = cursorScreen.Y - _dragOffset.Y;
                var snapped = ApplySnap(newX, newY);
                _form.Location = new Point(snapped.X, snapped.Y);
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                _dragging = false;
            }
        }
        #endregion

        #region Snap
        private Point ApplySnap(int newX, int newY) {
            int w = _form.Width;
            int h = _form.Height;

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
            IntPtr thisHandle = _form.Handle;

            EnumWindows((hWnd, lParam) => {
                if (hWnd == thisHandle) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int style = GetWindowLong(hWnd, GWL_STYLE);
                if ((style & (int)WS_CHILD) != 0) return true;
                if ((style & (int)WS_CAPTION) != 0) return true;

                GetWindowRect(hWnd, out REKT r);
                int w = r.Right - r.Left;
                int h = r.Bottom - r.Top;

                if (w >= 50 && w <= 500 && h >= 50 && h <= 500) {
                    windows.Add(r);
                }

                return true;
            }, IntPtr.Zero);

            return windows;
        }
        #endregion

        #region WndProc Helper
        public bool HandleWndProc(ref Message m) {
            if (m.Msg == WM_NCHITTEST) {
                Point cursor = _form.PointToClient(Cursor.Position);
                int w = _form.Width, h = _form.Height;

                if (cursor.X < RESIZE_BORDER)
                    m.Result = (IntPtr)(cursor.Y < RESIZE_BORDER ? 13 : (cursor.Y > h - RESIZE_BORDER ? 16 : 10));
                else if (cursor.X > w - RESIZE_BORDER)
                    m.Result = (IntPtr)(cursor.Y < RESIZE_BORDER ? 14 : (cursor.Y > h - RESIZE_BORDER ? 17 : 11));
                else if (cursor.Y < RESIZE_BORDER)
                    m.Result = (IntPtr)12;
                else if (cursor.Y > h - RESIZE_BORDER)
                    m.Result = (IntPtr)15;
                else
                    return false;
                return true;
            }
            return false;
        }
        #endregion
    }
}
