using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinSelectionColor
{
    public class EyedropperForm : Form
    {
        public event Action<Color> ColorSelected;

        private Bitmap _screenCapture;
        private Rectangle _virtualBounds;
        private Point _currentMousePos;
        private Color _currentColor;
        private int _gridRadius = 5; // 5 -> (5*2+1) = 11x11 grid
        private int _pixelBoxSize = 13; // size of each pixel on magnifier

        public EyedropperForm()
        {
            // Set up form properties for full screen borderless overlay
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;
            Cursor = Cursors.Cross;

            _virtualBounds = SystemInformation.VirtualScreen;
            Location = _virtualBounds.Location;
            Size = _virtualBounds.Size;

            CaptureScreen();
        }

        private void CaptureScreen()
        {
            try
            {
                _screenCapture = new Bitmap(_virtualBounds.Width, _virtualBounds.Height);
                using (Graphics g = Graphics.FromImage(_screenCapture))
                {
                    g.CopyFromScreen(_virtualBounds.Left, _virtualBounds.Top, 0, 0, _virtualBounds.Size, CopyPixelOperation.SourceCopy);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("CaptureScreen error: " + ex.Message);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _currentMousePos = e.Location;
            UpdateCurrentColor();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta > 0 && _gridRadius > 3)
            {
                _gridRadius--; // Zoom in (fewer pixels, larger)
                _pixelBoxSize = Math.Min(20, _pixelBoxSize + 2);
            }
            else if (e.Delta < 0 && _gridRadius < 8)
            {
                _gridRadius++; // Zoom out (more pixels)
                _pixelBoxSize = Math.Max(9, _pixelBoxSize - 2);
            }
            Invalidate();
        }

        private void UpdateCurrentColor()
        {
            if (_screenCapture != null)
            {
                int x = Math.Max(0, Math.Min(_screenCapture.Width - 1, _currentMousePos.X));
                int y = Math.Max(0, Math.Min(_screenCapture.Height - 1, _currentMousePos.Y));
                _currentColor = _screenCapture.GetPixel(x, y);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                SelectAndClose();
            }
            else if (e.Button == MouseButtons.Right)
            {
                CancelAndClose();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                CancelAndClose();
            }
            else if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                SelectAndClose();
            }
            else
            {
                // Fine-tuning with arrow keys
                int dx = 0, dy = 0;
                if (e.KeyCode == Keys.Left) dx = -1;
                else if (e.KeyCode == Keys.Right) dx = 1;
                else if (e.KeyCode == Keys.Up) dy = -1;
                else if (e.KeyCode == Keys.Down) dy = 1;

                if (dx != 0 || dy != 0)
                {
                    Point screenPt = PointToScreen(_currentMousePos);
                    Cursor.Position = new Point(screenPt.X + dx, screenPt.Y + dy);
                    _currentMousePos = new Point(_currentMousePos.X + dx, _currentMousePos.Y + dy);
                    UpdateCurrentColor();
                    Invalidate();
                }
            }
        }

        private void SelectAndClose()
        {
            if (ColorSelected != null)
            {
                ColorSelected(_currentColor);
            }
            Close();
        }

        private void CancelAndClose()
        {
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            // 1. Draw screen capture as backdrop
            if (_screenCapture != null)
            {
                g.DrawImageUnscaled(_screenCapture, 0, 0);
            }

            // 2. Draw Magnifier Loupe near cursor
            DrawLoupe(g);
        }

        private void DrawLoupe(Graphics g)
        {
            if (_screenCapture == null) return;

            int gridSize = _gridRadius * 2 + 1;
            int gridPixelWidth = gridSize * _pixelBoxSize;

            int cardWidth = Math.Max(180, gridPixelWidth + 24);
            int cardHeight = gridPixelWidth + 90;

            // Determine card position with offset and boundary clamping
            int offsetX = 24;
            int offsetY = 24;

            int cardX = _currentMousePos.X + offsetX;
            int cardY = _currentMousePos.Y + offsetY;

            // Flip if near right edge
            if (cardX + cardWidth > _virtualBounds.Width)
            {
                cardX = _currentMousePos.X - cardWidth - offsetX;
            }
            // Flip if near bottom edge
            if (cardY + cardHeight > _virtualBounds.Height)
            {
                cardY = _currentMousePos.Y - cardHeight - offsetY;
            }

            // Ensure within virtual bounds
            cardX = Math.Max(0, Math.Min(_virtualBounds.Width - cardWidth, cardX));
            cardY = Math.Max(0, Math.Min(_virtualBounds.Height - cardHeight, cardY));

            // Setup high quality rendering
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;

            // Draw Card Background & Shadow
            Rectangle cardRect = new Rectangle(cardX, cardY, cardWidth, cardHeight);
            using (GraphicsPath path = GetRoundedRect(cardRect, 10))
            {
                // Shadow
                using (GraphicsPath shadowPath = GetRoundedRect(new Rectangle(cardX + 2, cardY + 2, cardWidth, cardHeight), 10))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // Dark background
                using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(245, 30, 30, 32)))
                {
                    g.FillPath(bgBrush, path);
                }

                // Border
                using (Pen borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.5f))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Draw pixel grid inside card
            int gridStartX = cardX + (cardWidth - gridPixelWidth) / 2;
            int gridStartY = cardY + 12;

            int capX = _currentMousePos.X;
            int capY = _currentMousePos.Y;

            for (int gy = -_gridRadius; gy <= _gridRadius; gy++)
            {
                for (int gx = -_gridRadius; gx <= _gridRadius; gx++)
                {
                    int px = capX + gx;
                    int py = capY + gy;

                    Color pixelColor = Color.Black;
                    if (px >= 0 && px < _screenCapture.Width && py >= 0 && py < _screenCapture.Height)
                    {
                        pixelColor = _screenCapture.GetPixel(px, py);
                    }

                    int drawX = gridStartX + (gx + _gridRadius) * _pixelBoxSize;
                    int drawY = gridStartY + (gy + _gridRadius) * _pixelBoxSize;

                    using (SolidBrush pb = new SolidBrush(pixelColor))
                    {
                        g.FillRectangle(pb, drawX, drawY, _pixelBoxSize, _pixelBoxSize);
                    }
                }
            }

            // Draw grid lines
            using (Pen gridPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f))
            {
                for (int i = 0; i <= gridSize; i++)
                {
                    int x = gridStartX + i * _pixelBoxSize;
                    int y = gridStartY + i * _pixelBoxSize;
                    g.DrawLine(gridPen, x, gridStartY, x, gridStartY + gridPixelWidth);
                    g.DrawLine(gridPen, gridStartX, y, gridStartX + gridPixelWidth, y);
                }
            }

            // Highlight Center Pixel (Target)
            int centerDrawX = gridStartX + _gridRadius * _pixelBoxSize;
            int centerDrawY = gridStartY + _gridRadius * _pixelBoxSize;

            using (Pen outerPen = new Pen(Color.Black, 2f))
            using (Pen innerPen = new Pen(Color.White, 1f))
            {
                g.DrawRectangle(outerPen, centerDrawX - 1, centerDrawY - 1, _pixelBoxSize + 2, _pixelBoxSize + 2);
                g.DrawRectangle(innerPen, centerDrawX, centerDrawY, _pixelBoxSize, _pixelBoxSize);
            }

            // Crosshair guidelines pointing to center pixel
            using (Pen guidePen = new Pen(Color.FromArgb(160, 255, 255, 255), 1f))
            {
                guidePen.DashStyle = DashStyle.Dot;
                g.DrawLine(guidePen, gridStartX, centerDrawY + _pixelBoxSize / 2, centerDrawX - 2, centerDrawY + _pixelBoxSize / 2);
                g.DrawLine(guidePen, centerDrawX + _pixelBoxSize + 2, centerDrawY + _pixelBoxSize / 2, gridStartX + gridPixelWidth, centerDrawY + _pixelBoxSize / 2);
                g.DrawLine(guidePen, centerDrawX + _pixelBoxSize / 2, gridStartY, centerDrawX + _pixelBoxSize / 2, centerDrawY - 2);
                g.DrawLine(guidePen, centerDrawX + _pixelBoxSize / 2, centerDrawY + _pixelBoxSize + 2, centerDrawX + _pixelBoxSize / 2, gridStartY + gridPixelWidth);
            }

            // Info section below grid
            int infoY = gridStartY + gridPixelWidth + 10;

            // Color Swatch
            int swatchSize = 24;
            int swatchX = cardX + 14;
            Rectangle swatchRect = new Rectangle(swatchX, infoY + 2, swatchSize, swatchSize);
            using (SolidBrush sb = new SolidBrush(_currentColor))
            {
                g.FillRectangle(sb, swatchRect);
            }
            using (Pen sp = new Pen(Color.FromArgb(180, 255, 255, 255), 1f))
            {
                g.DrawRectangle(sp, swatchRect);
            }

            // HEX string
            string hexStr = string.Format("#{0:X2}{1:X2}{2:X2}", _currentColor.R, _currentColor.G, _currentColor.B);
            using (Font hexFont = new Font("Segoe UI", 11f, FontStyle.Bold))
            using (SolidBrush textBrush = new SolidBrush(Color.White))
            {
                g.DrawString(hexStr, hexFont, textBrush, swatchX + swatchSize + 10, infoY - 1);
            }

            // RGB string
            string rgbStr = string.Format("RGB({0}, {1}, {2})", _currentColor.R, _currentColor.G, _currentColor.B);
            using (Font rgbFont = new Font("Segoe UI", 8.5f, FontStyle.Regular))
            using (SolidBrush subBrush = new SolidBrush(Color.FromArgb(190, 190, 195)))
            {
                g.DrawString(rgbStr, rgbFont, subBrush, swatchX + swatchSize + 10, infoY + 16);
            }

            // Shortcut Hint text at bottom
            using (Font hintFont = new Font("Segoe UI", 7.5f, FontStyle.Regular))
            using (SolidBrush hintBrush = new SolidBrush(Color.FromArgb(140, 140, 145)))
            {
                string hint = "Клик / Space: выбор | Esc: отмена";
                SizeF hintSize = g.MeasureString(hint, hintFont);
                g.DrawString(hint, hintFont, hintBrush, cardX + (cardWidth - hintSize.Width) / 2, cardY + cardHeight - 20);
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle bounds, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            Rectangle arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_screenCapture != null)
                {
                    _screenCapture.Dispose();
                    _screenCapture = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
