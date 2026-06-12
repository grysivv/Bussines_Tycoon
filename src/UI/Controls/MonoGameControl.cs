using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Conglomerate
{
    public class MonoGameControl : Control
    {
        private GraphicsDevice? _graphicsDevice;
        private SpriteBatch? _spriteBatch;
        private readonly PresentationParameters _presentationParameters = new PresentationParameters();

        public GraphicsDevice? GraphicsDevice => _graphicsDevice;
        public SpriteBatch? SpriteBatch => _spriteBatch;

        public MonoGameControl()
        {
            // Optymalizacja stylów rysowania systemu Windows, aby uniknąć migotania (flickeringu)
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
            {
                InitializeGraphicsDevice();
            }
        }

        private void InitializeGraphicsDevice()
        {
            try
            {
                _presentationParameters.DeviceWindowHandle = this.Handle;
                _presentationParameters.IsFullScreen = false;
                _presentationParameters.BackBufferWidth = Math.Max(1, this.Width);
                _presentationParameters.BackBufferHeight = Math.Max(1, this.Height);
                _presentationParameters.BackBufferFormat = SurfaceFormat.Color;
                _presentationParameters.DepthStencilFormat = DepthFormat.Depth24Stencil8;
                _presentationParameters.MultiSampleCount = 4; // MSAA w celu wygładzenia krawędzi kafli izometrycznych

                _graphicsDevice = new GraphicsDevice(
                    GraphicsAdapter.DefaultAdapter,
                    GraphicsProfile.Reach,
                    _presentationParameters
                );

                _spriteBatch = new SpriteBatch(_graphicsDevice);
                
                OnGraphicsDeviceInitialized();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd podczas inicjalizacji urządzenia graficznego MonoGame: {ex.Message}\nUpewnij się, że masz zainstalowane biblioteki DirectX.", "Błąd Grafiki", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected virtual void OnGraphicsDeviceInitialized()
        {
            // Metoda do nadpisania w klasach pochodnych
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (_graphicsDevice != null)
            {
                int newWidth = Math.Max(1, this.Width);
                int newHeight = Math.Max(1, this.Height);

                _presentationParameters.BackBufferWidth = newWidth;
                _presentationParameters.BackBufferHeight = newHeight;

                _graphicsDevice.Reset(_presentationParameters);
                this.Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_graphicsDevice == null)
            {
                e.Graphics.Clear(System.Drawing.Color.DarkSlateGray);
                e.Graphics.DrawString("Inicjalizacja renderera MonoGame...", this.Font, System.Drawing.Brushes.White, 10, 10);
                return;
            }

            // Ustawienie obszaru widoku z zabezpieczeniem przed zerową wielkością (np. przy minimalizacji)
            int width = Math.Max(1, this.Width);
            int height = Math.Max(1, this.Height);
            _graphicsDevice.Viewport = new Viewport(0, 0, width, height);

            // Wywołanie renderowania do bufora wstecznego (Back Buffer)
            Draw();

            // Prezentacja bufora wstecznego na ekranie (Front Buffer okna WinForms)
            try
            {
                _graphicsDevice.Present();
            }
            catch (Exception)
            {
                // Ignorujemy wyjątki podczas nagłej zmiany rozmiaru okna
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spriteBatch?.Dispose();
                _graphicsDevice?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected virtual void Draw()
        {
            _graphicsDevice?.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);
        }
    }
}
