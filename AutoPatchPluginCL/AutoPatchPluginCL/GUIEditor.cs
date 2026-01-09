using MetroFramework.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AutoPatch
{
    public class LayoutConfig
    {
        public List<ControlConfig> Controls { get; set; } = new List<ControlConfig>();
        public bool ShowFormTitle { get; set; } = true;
    }

    public class ControlConfig
    {
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public static class LayoutSerializer
    {
        public static LayoutConfig Load(string path)
        {
            if (!File.Exists(path)) return new LayoutConfig();
            var json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<LayoutConfig>(json) ?? new LayoutConfig();
        }

        public static void Save(string path, LayoutConfig config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(path, json);
        }

        public static void Apply(Control root, LayoutConfig config)
        {
            if (root == null || config?.Controls == null) return;

            var map = config.Controls
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

            foreach (var ctl in GetAllControls(root))
            {
                if (string.IsNullOrWhiteSpace(ctl.Name)) continue;
                if (!map.TryGetValue(ctl.Name, out var cc)) continue;
                ctl.SetBounds(cc.X, cc.Y, cc.Width, cc.Height);
            }

            if (root is MetroForm f)
            {
                f.DisplayHeader = config.ShowFormTitle;
            }
        }

        public static LayoutConfig Capture(Control root)
        {
            var config = new LayoutConfig();
            foreach (var ctl in GetAllControls(root))
            {
                if (string.IsNullOrWhiteSpace(ctl.Name)) continue;

                config.Controls.Add(new ControlConfig
                {
                    Name = ctl.Name,
                    X = ctl.Left,
                    Y = ctl.Top,
                    Width = ctl.Width,
                    Height = ctl.Height
                });
            }
            return config;
        }

        private static IEnumerable<Control> GetAllControls(Control root)
        {
            // Incluye root.Children recursivo (paneles, groupbox, tabs, etc.)
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var child in GetAllControls(c))
                    yield return child;
            }
        }
    }

    public sealed class LayoutEditor : IDisposable
    {
        private readonly Control _root;              // contenedor donde editas (Form o Panel principal)
        private readonly SelectionAdorner _adorner;  // overlay visual
        private readonly HashSet<Control> _hooked = new HashSet<Control>();
        private bool _enabled;
        private bool _blockClicks;

        // Estado de drag/resize
        private Control _selected;
        private bool _dragging;
        private bool _resizing;
        private Point _mouseDownRoot;         // mouse en coords del root
        private Rectangle _startBounds;       // bounds del control al inicio
        private const int MinW = 20;
        private const int MinH = 20;

        // Opcional: rejilla
        public bool SnapToGrid { get; set; } = false;
        public int GridSize { get; set; } = 5;

        public LayoutEditor(Control root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));

            _adorner = new SelectionAdorner
            {
                Visible = false
            };

            // IMPORTANTE: poner overlay encima
            _root.Controls.Add(_adorner);
            _adorner.BringToFront();

            _root.ControlAdded += Root_ControlAdded;
            _root.ControlRemoved += Root_ControlRemoved;
            _root.Resize += (s, e) => UpdateAdorner();

            HookAllExistingControls();
        }

        public void Enable(bool enabled)
        {
            _enabled = enabled;
            _adorner.Visible = enabled && _selected != null;
            _adorner.Enabled = enabled;
            _blockClicks = enabled;

            if (!enabled)
            {
                _dragging = false;
                _resizing = false;
            }
        }
        private static bool IsAutoSize(Control c)
        {
            var p = c.GetType().GetProperty("AutoSize");
            return p != null && p.PropertyType == typeof(bool) && (bool)p.GetValue(c);
        }

        public void SelectControl(Control ctl)
        {
            if (_selected == ctl) return;
            _selected = ctl;
            _adorner.Visible = _enabled && _selected != null;
            UpdateAdorner();
        }

        public Control Selected => _selected;

        public void Dispose()
        {
            Enable(false);

            _root.ControlAdded -= Root_ControlAdded;
            _root.ControlRemoved -= Root_ControlRemoved;

            foreach (var c in _hooked.ToList())
                UnhookControl(c);

            if (!_adorner.IsDisposed)
                _adorner.Dispose();
        }

        private void Root_ControlAdded(object sender, ControlEventArgs e)
        {
            HookControlRecursive(e.Control);
            _adorner.BringToFront();
        }

        private void Root_ControlRemoved(object sender, ControlEventArgs e)
        {
            UnhookControlRecursive(e.Control);
            if (_selected == e.Control) SelectControl(null);
        }

        private void HookAllExistingControls()
        {
            foreach (Control c in GetAllControls(_root))
                HookControl(c);
            _adorner.BringToFront();
        }

        private void HookControlRecursive(Control c)
        {
            HookControl(c);
            foreach (Control child in c.Controls)
                HookControlRecursive(child);
        }

        private void UnhookControlRecursive(Control c)
        {
            UnhookControl(c);
            foreach (Control child in c.Controls)
                UnhookControlRecursive(child);
        }

        private IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                // Ignora el propio adorner
                if (ReferenceEquals(c, _adorner)) continue;

                yield return c;
                foreach (var cc in GetAllControls(c))
                    yield return cc;
            }
        }

        private void HookControl(Control c)
        {
            if (c == null) return;
            if (ReferenceEquals(c, _adorner)) return;
            if (_hooked.Contains(c)) return;

            c.Dock = DockStyle.None;

            c.MouseDown += Control_MouseDown;
            c.MouseMove += Control_MouseMove;
            c.MouseUp += Control_MouseUp;
            c.MouseClick += Control_MouseClick;

            c.PreviewKeyDown += (s, e) =>
            {
                if (_blockClicks)
                    e.IsInputKey = true;
            };

            c.MouseDown += (s, e) =>
            {
                if (_blockClicks && e.Button == MouseButtons.Left)
                {
                    ((Control)s).Capture = false;
                }
            };

            c.LocationChanged += (s, e) => { if (_enabled && _selected == c) UpdateAdorner(); };
            c.SizeChanged += (s, e) => { if (_enabled && _selected == c) UpdateAdorner(); };

            _hooked.Add(c);
        }

        private void UnhookControl(Control c)
        {
            if (c == null) return;
            if (!_hooked.Contains(c)) return;

            c.MouseDown -= Control_MouseDown;
            c.MouseMove -= Control_MouseMove;
            c.MouseUp -= Control_MouseUp;
            c.MouseClick -= Control_MouseClick;

            _hooked.Remove(c);
        }

        private void Control_MouseClick(object sender, MouseEventArgs e)
        {
            if (!_enabled) return;
            if (sender is Control c)
                SelectControl(c);
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_enabled) return;
            if (e.Button != MouseButtons.Left) return;

            var c = sender as Control;
            if (c == null) return;

            SelectControl(c);

            var canResize = !IsAutoSize(c);
            var inResizeHandle = canResize && (e.X >= c.Width - 10) && (e.Y >= c.Height - 10);

            _dragging = !inResizeHandle;
            _resizing = inResizeHandle;

            _mouseDownRoot = _root.PointToClient(c.PointToScreen(e.Location));
            _startBounds = c.Bounds;
            c.Capture = true;
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_enabled) return;
            if (!_dragging && !_resizing) return;

            var c = sender as Control;
            if (c == null || c != _selected) return;

            var mouseNowRoot = _root.PointToClient(c.PointToScreen(e.Location));
            var dx = mouseNowRoot.X - _mouseDownRoot.X;
            var dy = mouseNowRoot.Y - _mouseDownRoot.Y;

            var newBounds = _startBounds;

            if (_dragging)
            {
                newBounds.X = _startBounds.X + dx;
                newBounds.Y = _startBounds.Y + dy;
            }
            else if (_resizing)
            {
                newBounds.Width = Math.Max(MinW, _startBounds.Width + dx);
                newBounds.Height = Math.Max(MinH, _startBounds.Height + dy);
            }

            var parent = c.Parent ?? _root;
            newBounds = ConstrainToParent(newBounds, parent);

            if (SnapToGrid)
                newBounds = SnapRect(newBounds, GridSize, _dragging, _resizing);

            c.Bounds = newBounds;
            UpdateAdorner();
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_enabled) return;

            var c = sender as Control;
            if (c != null) c.Capture = false;

            _dragging = false;
            _resizing = false;
        }

        private Rectangle ConstrainToParent(Rectangle r, Control parent)
        {
            var maxX = parent.ClientSize.Width - r.Width;
            var maxY = parent.ClientSize.Height - r.Height;

            r.X = Math.Max(0, Math.Min(r.X, maxX));
            r.Y = Math.Max(0, Math.Min(r.Y, maxY));

            r.Width = Math.Min(r.Width, parent.ClientSize.Width - r.X);
            r.Height = Math.Min(r.Height, parent.ClientSize.Height - r.Y);

            r.Width = Math.Max(MinW, r.Width);
            r.Height = Math.Max(MinH, r.Height);

            return r;
        }

        private Rectangle SnapRect(Rectangle r, int grid, bool dragging, bool resizing)
        {
            int Snap(int v) => (int)Math.Round(v / (double)grid) * grid;

            if (dragging)
            {
                r.X = Snap(r.X);
                r.Y = Snap(r.Y);
            }
            if (resizing)
            {
                r.Width = Math.Max(MinW, Snap(r.Width));
                r.Height = Math.Max(MinH, Snap(r.Height));
            }
            return r;
        }

        private void UpdateAdorner()
        {
            if (!_enabled || _selected == null)
            {
                _adorner.Visible = false;
                return;
            }

            _adorner.Visible = true;
            _adorner.AttachTo(_selected);
            _adorner.BringToFront();
            _adorner.Invalidate();
        }
        private sealed class SelectionAdorner : Control
        {
            private Control _target;
            private const int Pad = 2;
            private const int Handle = 10;
            private const int WM_NCHITTEST = 0x0084;
            private static readonly IntPtr HTTRANSPARENT = new IntPtr(-1);
            private const int InfoBar = 22;

            public SelectionAdorner()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint |
                         ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.UserPaint |
                         ControlStyles.SupportsTransparentBackColor, true);

                BackColor = Color.Transparent;
                TabStop = false;
                Enabled = false;
            }


            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_NCHITTEST)
                {
                    m.Result = HTTRANSPARENT;
                    return;
                }
                base.WndProc(ref m);
            }

            public void AttachTo(Control target)
            {
                _target = target;
                if (_target == null) return;

                var root = Parent;
                if (root == null) return;

                var topLeft = root.PointToClient(_target.Parent.PointToScreen(_target.Location));

                Bounds = new Rectangle(
                    topLeft.X - Pad,
                    topLeft.Y - Pad - InfoBar,
                    _target.Width + Pad * 2,
                    _target.Height + Pad * 2 + InfoBar
                );
            }

            public bool IsInResizeHandle(Point mouseRoot, Control target)
            {
                if (target == null || Parent == null) return false;

                var topLeft = Parent.PointToClient(target.Parent.PointToScreen(target.Location));
                var adornerRect = new Rectangle(
                    topLeft.X - Pad,
                    topLeft.Y - Pad,
                    target.Width + Pad * 2,
                    target.Height + Pad * 2
                );

                var handleRect = new Rectangle(
                    adornerRect.Right - Handle,
                    adornerRect.Bottom - Handle,
                    Handle,
                    Handle
                );

                return handleRect.Contains(mouseRoot);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (_target == null) return;

                var rect = new Rectangle(0, InfoBar, Width - 1, Height - InfoBar - 1);

                using (var pen = new Pen(Color.DodgerBlue, 2))
                    e.Graphics.DrawRectangle(pen, rect);

                var text = $"X:{_target.Left} Y:{_target.Top} W:{_target.Width} H:{_target.Height}";
                var textSize = TextRenderer.MeasureText(text, Font);
                var bgRect = new Rectangle(2, 2, Math.Min(Width - 4, textSize.Width + 10), Math.Min(InfoBar - 4, textSize.Height + 4));

                using (var bg = new SolidBrush(Color.FromArgb(160, Color.Black)))
                    e.Graphics.FillRectangle(bg, bgRect);

                TextRenderer.DrawText(e.Graphics, text, Font, new Point(6, 4), Color.White, TextFormatFlags.NoPadding);

                if (!IsAutoSize(_target))
                {
                    var hr = new Rectangle(rect.Right - Handle + 1, rect.Bottom - Handle + 1, Handle - 1, Handle - 1);
                    using (var b = new SolidBrush(Color.DodgerBlue))
                        e.Graphics.FillRectangle(b, hr);
                }
            }

        }
    }
}