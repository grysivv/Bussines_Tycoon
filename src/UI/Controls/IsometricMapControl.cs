using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Point = System.Drawing.Point;
using XnaPoint = Microsoft.Xna.Framework.Point;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Conglomerate
{
    public class IsometricMapControl : MonoGameControl
    {
        // Stałe wymiarów kafli izometrycznych
        private const float TileWidth = 80f;
        private const float TileHeight = 40f;

        // Stan kamery i widoku
        private float _cameraX = 0f;
        private float _cameraY = 0f;
        private float _zoom = 1.0f;

        // Obsługa myszy (przesuwanie mapy)
        private bool _isDragging = false;
        private Point _dragStart = Point.Empty;
        private float _cameraStartX = 0f;
        private float _cameraStartY = 0f;

        // Stan zaznaczenia
        private XnaPoint? _hoveredTile = null;

        // Referencje do silnika gry
        private Map? _map;
        private GameManager? _gameManager;

        // Zasoby graficzne i efekty MonoGame
        private BasicEffect? _basicEffect;
        private System.Windows.Forms.Timer? _renderTimer;
        private float _waveTimer = 0f;

        // Interakcja
        private bool _buildMode = false;
        public bool GetBuildMode() => _buildMode;
        public void SetBuildMode(bool value) { _buildMode = value; }
        public event Action<XnaPoint>? OnTileSelected;
        public event Action<XnaPoint?>? OnTileHovered;

        public IsometricMapControl()
        {
            // Włączenie obsługi kółka myszy w WinForms
            this.MouseWheel += OnMouseWheelEvent;
        }

        public void Initialize(Map map, GameManager gameManager)
        {
            _map = map;
            _gameManager = gameManager;

            // Ustaw kamerę na środku mapy
            CenterCamera();
        }

        public void CenterCamera()
        {
            if (_map != null)
            {
                // Wyznacz środek mapy w przestrzeni świata
                float centerX = (_map.Width - _map.Height) * (TileWidth / 4f);
                float centerY = (_map.Width + _map.Height) * (TileHeight / 4f);
                _cameraX = centerX;
                _cameraY = centerY;
                _zoom = 1.0f;
            }
        }

        protected override void OnGraphicsDeviceInitialized()
        {
            _basicEffect = new BasicEffect(GraphicsDevice);
            _basicEffect.VertexColorEnabled = true;

            // Pętla odświeżania ~60 FPS do animacji wody i płynnego renderowania
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 16; // ~60 Hz
            _renderTimer.Tick += (s, e) =>
            {
                _waveTimer += 0.05f; // Zwiększenie czasu fali dla wody
                this.Invalidate();   // Wywołaj ponowne rysowanie kontrolki
            };
            _renderTimer.Start();
        }

        // --- MATEMATYKA IZOMETRYCZNA ---

        public Vector2 TileToWorld(int x, int y)
        {
            float worldX = (x - y) * (TileWidth / 2f);
            float worldY = (x + y) * (TileHeight / 2f);
            return new Vector2(worldX, worldY);
        }

        public XnaPoint WorldToTile(Vector2 worldPos)
        {
            float xFloat = (worldPos.X / (TileWidth / 2f) + worldPos.Y / (TileHeight / 2f)) / 2f;
            float yFloat = (worldPos.Y / (TileHeight / 2f) - worldPos.X / (TileWidth / 2f)) / 2f;
            return new XnaPoint((int)Math.Floor(xFloat + 0.5f), (int)Math.Floor(yFloat + 0.5f));
        }

        public XnaPoint ScreenToTile(Point screenPt)
        {
            // Przekształcenie punktu ekranowego na przestrzeń świata
            float worldX = (screenPt.X - Width / 2f) / _zoom + _cameraX;
            float worldY = (screenPt.Y - Height / 2f) / _zoom + _cameraY;
            return WorldToTile(new Vector2(worldX, worldY));
        }

        // --- OBSŁUGA ZDARZEŃ MYSZY ---

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                _dragStart = e.Location;
                _cameraStartX = _cameraX;
                _cameraStartY = _cameraY;
                _isDragging = false; // Jeszcze nie przeciągamy, dopóki mysz się nie przesunie
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                // Oblicz dystans przesunięcia od punktu kliknięcia
                float dragDistance = (float)Math.Sqrt(Math.Pow(e.X - _dragStart.X, 2) + Math.Pow(e.Y - _dragStart.Y, 2));

                // Aktywuj przeciąganie tylko jeśli przekroczono próg 10 pikseli (martwa strefa)
                if (!_isDragging && dragDistance > 10f)
                {
                    _isDragging = true;
                    this.Cursor = Cursors.NoMove2D;
                }

                if (_isDragging)
                {
                    float dx = (e.X - _dragStart.X) / _zoom;
                    float dy = (e.Y - _dragStart.Y) / _zoom;
                    _cameraX = _cameraStartX - dx;
                    _cameraY = _cameraStartY - dy;
                }
            }
            else
            {
                // Wyznaczenie hovered tile (tylko gdy nie przesuwamy kamery)
                var tile = ScreenToTile(e.Location);
                XnaPoint? newHovered = null;
                if (_map != null && tile.X >= 0 && tile.X < _map.Width && tile.Y >= 0 && tile.Y < _map.Height)
                {
                    newHovered = tile;
                }

                if (newHovered != _hoveredTile)
                {
                    _hoveredTile = newHovered;
                    OnTileHovered?.Invoke(_hoveredTile);
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (_isDragging)
            {
                _isDragging = false;
                this.Cursor = Cursors.Default;
            }
            else
            {
                // To było kliknięcie (brak przesunięcia) -> zaznacz kafel
                var tile = ScreenToTile(e.Location);
                if (_map != null && tile.X >= 0 && tile.X < _map.Width && tile.Y >= 0 && tile.Y < _map.Height)
                {
                    OnTileSelected?.Invoke(tile);
                }
            }
        }

        private void OnMouseWheelEvent(object? sender, MouseEventArgs e)
        {
            // Przybliżanie / Oddalanie
            float zoomDelta = e.Delta * 0.001f;
            _zoom += zoomDelta;
            _zoom = Math.Clamp(_zoom, 0.4f, 3.0f);
        }

        // --- RYSOWANIE GRY (MONOGAME) ---

        protected override void Draw()
        {
            if (GraphicsDevice == null || _basicEffect == null || _map == null)
            {
                return;
            }

            // Czyszczenie tła - elegancki ciemnoszary kolor
            GraphicsDevice.Clear(new XnaColor(34, 34, 34));

            // Konfiguracja macierzy widoku i projekcji 2D
            _basicEffect.World = Matrix.CreateTranslation(-_cameraX, -_cameraY, 0)
                                 * Matrix.CreateScale(_zoom)
                                 * Matrix.CreateTranslation(Width / 2f, Height / 2f, 0);
            
            _basicEffect.View = Matrix.Identity;
            _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, Width, Height, 0, 0, 1);

            // Rysowanie kafli i budynków w porządku warstw (od tyłu do przodu: Y+X)
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    Tile tile = _map.GetTile(x, y);
                    Vector2 worldPos = TileToWorld(x, y);

                    // 1. Wybór koloru terenu
                    XnaColor terrainColor = XnaColor.DarkGreen;
                    XnaColor borderColor = new XnaColor(40, 90, 40);

                    if (tile.Type == TileType.Water)
                    {
                        // Dynamiczna fala wodna (mikro-animacja)
                        float wave = (float)Math.Sin(_waveTimer + (x + y) * 0.5f) * 12f;
                        terrainColor = new XnaColor(30, 100 + (int)wave, 210);
                        borderColor = new XnaColor(20, 70, 150);
                    }
                    else if (tile.Type == TileType.Grass)
                    {
                        terrainColor = new XnaColor(76, 154, 76);
                    }
                    else if (tile.Type == TileType.Building)
                    {
                        if (tile.Building is Farm)
                        {
                            terrainColor = new XnaColor(190, 150, 100); // Podłoże pod farmą
                            borderColor = new XnaColor(120, 90, 60);
                        }
                        else if (tile.Building is CoalMine)
                        {
                            terrainColor = new XnaColor(90, 90, 90); // Podłoże pod kopalnią węgla
                            borderColor = new XnaColor(60, 60, 60);
                        }
                        else if (tile.Building is WarehouseBuilding wh)
                        {
                            if (wh.AllowedCategory == ResourceCategory.Food)
                            {
                                terrainColor = new XnaColor(130, 180, 220); // Podłoże pod magazynem żywności
                                borderColor = new XnaColor(90, 130, 160);
                            }
                            else
                            {
                                terrainColor = new XnaColor(150, 120, 100); // Podłoże pod magazynem kopalnianym
                                borderColor = new XnaColor(100, 80, 60);
                            }
                        }
                        else
                        {
                            terrainColor = new XnaColor(120, 120, 120); // Domyślne podłoże pod budynkiem
                            borderColor = new XnaColor(80, 80, 85);
                        }
                    }

                    // Wyrysuj kafel bazowy (teren)
                    DrawIsometricDiamond(worldPos.X, worldPos.Y, TileWidth, TileHeight, terrainColor);

                    // 2. Narysuj obramowanie dla czytelności siatki
                    DrawIsometricOutline(worldPos.X, worldPos.Y, TileWidth, TileHeight, borderColor);

                    // 3. Jeśli to budynek, narysuj budynek 3D
                    if (tile.Type == TileType.Building && tile.Building != null)
                    {
                        if (tile.Building is Farm)
                        {
                            DrawFarmBuilding3D(worldPos.X, worldPos.Y);
                        }
                        else if (tile.Building is CoalMine)
                        {
                            DrawCoalMine3D(worldPos.X, worldPos.Y);
                        }
                        else if (tile.Building is WarehouseBuilding wh)
                        {
                            DrawWarehouse3D(worldPos.X, worldPos.Y, wh);
                        }
                    }

                    // 4. Jeśli to aktualnie hovered kafel, wyrysuj nakładkę zaznaczenia
                    if (_hoveredTile.HasValue && _hoveredTile.Value.X == x && _hoveredTile.Value.Y == y)
                    {
                        XnaColor hoverColor = GetBuildMode() ? new XnaColor(255, 230, 50, 120) : new XnaColor(255, 255, 255, 80);
                        DrawIsometricDiamond(worldPos.X, worldPos.Y, TileWidth, TileHeight, hoverColor);
                        
                        // Złota ramka dla zaznaczonego pola
                        DrawIsometricOutline(worldPos.X, worldPos.Y, TileWidth, TileHeight, XnaColor.Gold, 2.0f);
                    }
                }
            }
        }

        // --- RYSOWANIE GEOMETRII ---

        private void DrawIsometricDiamond(float x, float y, float w, float h, XnaColor color)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            VertexPositionColor[] vertices = new VertexPositionColor[6];

            Vector3 top = new Vector3(x, y - h / 2f, 0);
            Vector3 right = new Vector3(x + w / 2f, y, 0);
            Vector3 bottom = new Vector3(x, y + h / 2f, 0);
            Vector3 left = new Vector3(x - w / 2f, y, 0);

            // Pierwszy trójkąt (góra, prawo, dół)
            vertices[0] = new VertexPositionColor(top, color);
            vertices[1] = new VertexPositionColor(right, color);
            vertices[2] = new VertexPositionColor(bottom, color);

            // Drugi trójkąt (góra, dół, lewo)
            vertices[3] = new VertexPositionColor(top, color);
            vertices[4] = new VertexPositionColor(bottom, color);
            vertices[5] = new VertexPositionColor(left, color);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 2);
            }
        }

        private void DrawIsometricOutline(float x, float y, float w, float h, XnaColor color, float thicknessMultiplier = 1.0f)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            VertexPositionColor[] vertices = new VertexPositionColor[5];

            Vector3 top = new Vector3(x, y - h / 2f, 0);
            Vector3 right = new Vector3(x + w / 2f, y, 0);
            Vector3 bottom = new Vector3(x, y + h / 2f, 0);
            Vector3 left = new Vector3(x - w / 2f, y, 0);

            vertices[0] = new VertexPositionColor(top, color);
            vertices[1] = new VertexPositionColor(right, color);
            vertices[2] = new VertexPositionColor(bottom, color);
            vertices[3] = new VertexPositionColor(left, color);
            vertices[4] = new VertexPositionColor(top, color); // zamknięcie pętli

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, 4);
            }
        }

        private void DrawFarmBuilding3D(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // 1. Zagroda (Płot drewniany i stóg siana)
            DrawFenceAndHayBale(x, y);

            // 2. Główna stodoła (czerwony budynek dwuspadowy z białymi drzwiami X)
            DrawRedBarn(x, y);

            // 3. Silos zbożowy (srebrna metalowa wieża)
            DrawSilo(x, y);

            // 4. Wiatrak farmerski (z obracającymi się łopatami)
            DrawWindmill(x, y);
        }

        private void Draw3DBox(float x, float y, float w, float h, float height, XnaColor leftColor, XnaColor rightColor, XnaColor topColor)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // Spód kostki na gruncie
            Vector3 bBottom = new Vector3(x, y + h / 2f, 0);
            Vector3 bLeft = new Vector3(x - w / 2f, y, 0);
            Vector3 bRight = new Vector3(x + w / 2f, y, 0);

            // Góra kostki (uniesiona o wysokość)
            Vector3 tBottom = new Vector3(x, y + h / 2f - height, 0);
            Vector3 tLeft = new Vector3(x - w / 2f, y - height, 0);
            Vector3 tRight = new Vector3(x + w / 2f, y - height, 0);
            Vector3 tTop = new Vector3(x, y - h / 2f - height, 0);

            VertexPositionColor[] vertices = new VertexPositionColor[18];

            // Ściana lewa
            vertices[0] = new VertexPositionColor(bLeft, leftColor);
            vertices[1] = new VertexPositionColor(bBottom, leftColor);
            vertices[2] = new VertexPositionColor(tBottom, leftColor);
            vertices[3] = new VertexPositionColor(bLeft, leftColor);
            vertices[4] = new VertexPositionColor(tBottom, leftColor);
            vertices[5] = new VertexPositionColor(tLeft, leftColor);

            // Ściana prawa
            vertices[6] = new VertexPositionColor(bBottom, rightColor);
            vertices[7] = new VertexPositionColor(bRight, rightColor);
            vertices[8] = new VertexPositionColor(tRight, rightColor);
            vertices[9] = new VertexPositionColor(bBottom, rightColor);
            vertices[10] = new VertexPositionColor(tRight, rightColor);
            vertices[11] = new VertexPositionColor(tBottom, rightColor);

            // Pokrywa górna
            vertices[12] = new VertexPositionColor(tLeft, topColor);
            vertices[13] = new VertexPositionColor(tBottom, topColor);
            vertices[14] = new VertexPositionColor(tRight, topColor);
            vertices[15] = new VertexPositionColor(tLeft, topColor);
            vertices[16] = new VertexPositionColor(tRight, topColor);
            vertices[17] = new VertexPositionColor(tTop, topColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vertices, 0, 6);
            }

            // Krawędzie / Obramowanie kostki dla ostrości low-poly
            XnaColor outlineColor = new XnaColor(40, 40, 40, 100);
            VertexPositionColor[] outline = new VertexPositionColor[10];
            outline[0] = new VertexPositionColor(tBottom, outlineColor);
            outline[1] = new VertexPositionColor(tRight, outlineColor);
            outline[2] = new VertexPositionColor(tTop, outlineColor);
            outline[3] = new VertexPositionColor(tLeft, outlineColor);
            outline[4] = new VertexPositionColor(tBottom, outlineColor);
            outline[5] = new VertexPositionColor(bBottom, outlineColor);
            outline[6] = new VertexPositionColor(bLeft, outlineColor);
            outline[7] = new VertexPositionColor(tLeft, outlineColor);
            outline[8] = new VertexPositionColor(bRight, outlineColor);
            outline[9] = new VertexPositionColor(tRight, outlineColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, outline, 0, 5);
            }
        }

        private void DrawFenceAndHayBale(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // Drewniany płotek zagrody wokół przedniej części farmy
            XnaColor fenceColor = new XnaColor(100, 65, 35);
            VertexPositionColor[] fence = new VertexPositionColor[4];
            fence[0] = new VertexPositionColor(new Vector3(x - TileWidth * 0.32f, y + TileHeight * 0.15f, 0), fenceColor);
            fence[1] = new VertexPositionColor(new Vector3(x - TileWidth * 0.1f, y + TileHeight * 0.36f, 0), fenceColor);
            fence[2] = new VertexPositionColor(new Vector3(x + TileWidth * 0.32f, y + TileHeight * 0.15f, 0), fenceColor);
            fence[3] = new VertexPositionColor(new Vector3(x + TileWidth * 0.1f, y - TileHeight * 0.05f, 0), fenceColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, fence, 0, 3);
            }

            // Mała beleczka siana (żółty prostopadłościan)
            float hx = x - TileWidth * 0.06f;
            float hy = y + TileHeight * 0.2f;
            Draw3DBox(hx, hy, 7f, 3.5f, 4.5f, new XnaColor(225, 190, 50), new XnaColor(190, 160, 40), new XnaColor(240, 210, 70));
        }

        private void DrawRedBarn(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            float bw = TileWidth * 0.42f; // Szerokość stodoły
            float bh = TileHeight * 0.42f; // Głębokość stodoły
            float bHeight = 16f;           // Wysokość ścian stodoły
            float bx = x + 3f;             // Przesunięcie stodoły na kafelku
            float by = y + 5f;

            // Ściany stodoły
            Draw3DBox(bx, by, bw, bh, bHeight, new XnaColor(135, 45, 45), new XnaColor(180, 55, 55), new XnaColor(180, 55, 55));

            // Wierzchołki góry ścian stodoły (podstawa dachu)
            Vector3 tBottom = new Vector3(bx, by + bh / 2f - bHeight, 0);
            Vector3 tLeft = new Vector3(bx - bw / 2f, by - bHeight, 0);
            Vector3 tRight = new Vector3(bx + bw / 2f, by - bHeight, 0);
            Vector3 tTop = new Vector3(bx, by - bh / 2f - bHeight, 0);

            // Grzbiet dachu dwuspadowego
            float roofHeight = 7f;
            Vector3 peakFront = tBottom - new Vector3(0, roofHeight, 0);
            Vector3 peakBack = tTop - new Vector3(0, roofHeight, 0);

            XnaColor roofLeftColor = new XnaColor(165, 70, 50);
            XnaColor roofRightColor = new XnaColor(210, 90, 70);
            XnaColor gableColor = new XnaColor(180, 55, 55);

            VertexPositionColor[] roofVerts = new VertexPositionColor[18];

            // Połać lewa (tLeft -> peakFront -> peakBack)
            roofVerts[0] = new VertexPositionColor(tLeft, roofLeftColor);
            roofVerts[1] = new VertexPositionColor(peakFront, roofLeftColor);
            roofVerts[2] = new VertexPositionColor(peakBack, roofLeftColor);

            // Połać prawa (tRight -> peakBack -> peakFront)
            roofVerts[3] = new VertexPositionColor(tRight, roofRightColor);
            roofVerts[4] = new VertexPositionColor(peakBack, roofRightColor);
            roofVerts[5] = new VertexPositionColor(peakFront, roofRightColor);

            // Szczyt przedni (trójkąt nad przednią ścianą)
            roofVerts[6] = new VertexPositionColor(tLeft, gableColor);
            roofVerts[7] = new VertexPositionColor(tBottom, gableColor);
            roofVerts[8] = new VertexPositionColor(peakFront, gableColor);

            roofVerts[9] = new VertexPositionColor(tBottom, gableColor);
            roofVerts[10] = new VertexPositionColor(tRight, gableColor);
            roofVerts[11] = new VertexPositionColor(peakFront, gableColor);

            // Szczyt tylny
            roofVerts[12] = new VertexPositionColor(tLeft, gableColor);
            roofVerts[13] = new VertexPositionColor(tTop, gableColor);
            roofVerts[14] = new VertexPositionColor(peakBack, gableColor);

            roofVerts[15] = new VertexPositionColor(tTop, gableColor);
            roofVerts[16] = new VertexPositionColor(tRight, gableColor);
            roofVerts[17] = new VertexPositionColor(peakBack, gableColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, roofVerts, 0, 6);
            }

            // Krawędzie dachu stodoły
            XnaColor roofOutlineColor = new XnaColor(40, 20, 20, 150);
            VertexPositionColor[] roofOutline = new VertexPositionColor[10];
            roofOutline[0] = new VertexPositionColor(tLeft, roofOutlineColor);
            roofOutline[1] = new VertexPositionColor(peakFront, roofOutlineColor);
            roofOutline[2] = new VertexPositionColor(tBottom, roofOutlineColor);
            roofOutline[3] = new VertexPositionColor(peakFront, roofOutlineColor);
            roofOutline[4] = new VertexPositionColor(tRight, roofOutlineColor);
            roofOutline[5] = new VertexPositionColor(peakBack, roofOutlineColor);
            roofOutline[6] = new VertexPositionColor(tTop, roofOutlineColor);
            roofOutline[7] = new VertexPositionColor(peakBack, roofOutlineColor);
            roofOutline[8] = new VertexPositionColor(tLeft, roofOutlineColor);
            roofOutline[9] = new VertexPositionColor(peakFront, roofOutlineColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, roofOutline, 0, 5);
            }

            // Białe wrota stodoły z charakterystycznym krzyżakiem X (ściana przednia-prawa)
            Vector3 bBottomGround = new Vector3(bx, by + bh / 2f, 0);
            Vector3 bRightGround = new Vector3(bx + bw / 2f, by, 0);

            Vector3 dBL = Vector3.Lerp(bBottomGround, bRightGround, 0.35f);
            Vector3 dBR = Vector3.Lerp(bBottomGround, bRightGround, 0.65f);
            Vector3 dTL = dBL - new Vector3(0, 8f, 0);
            Vector3 dTR = dBR - new Vector3(0, 8f, 0);

            XnaColor doorColor = XnaColor.White;
            VertexPositionColor[] door = new VertexPositionColor[8];
            door[0] = new VertexPositionColor(dBL, doorColor);
            door[1] = new VertexPositionColor(dTL, doorColor);

            door[2] = new VertexPositionColor(dTL, doorColor);
            door[3] = new VertexPositionColor(dTR, doorColor);

            door[4] = new VertexPositionColor(dTR, doorColor);
            door[5] = new VertexPositionColor(dBR, doorColor);

            // Poprzeczki X
            door[6] = new VertexPositionColor(dTL, doorColor);
            door[7] = new VertexPositionColor(dBR, doorColor);

            VertexPositionColor[] doorDiag = new VertexPositionColor[2];
            doorDiag[0] = new VertexPositionColor(dBL, doorColor);
            doorDiag[1] = new VertexPositionColor(dTR, doorColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, door, 0, 4);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, doorDiag, 0, 1);
            }
        }

        private void DrawSilo(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            float sx = x - TileWidth * 0.22f; // Silos po lewej-tylnej stronie
            float sy = y - TileHeight * 0.15f;
            float sw = 9f;
            float sh = 4.5f;
            float siloHeight = 26f;

            // Srebrne, cieniowane ściany silosu
            Draw3DBox(sx, sy, sw, sh, siloHeight, new XnaColor(120, 120, 125), new XnaColor(170, 170, 175), new XnaColor(190, 190, 195));

            // Wierzchołki góry silosu (podstawa stożka)
            Vector3 tBottom = new Vector3(sx, sy + sh / 2f - siloHeight, 0);
            Vector3 tLeft = new Vector3(sx - sw / 2f, sy - siloHeight, 0);
            Vector3 tRight = new Vector3(sx + sw / 2f, sy - siloHeight, 0);
            Vector3 tTop = new Vector3(sx, sy - sh / 2f - siloHeight, 0);

            // Szczyt stożkowego dachu silosu
            Vector3 peak = new Vector3(sx, sy - siloHeight - 5.5f, 0);

            XnaColor roofFront = new XnaColor(95, 105, 125);
            XnaColor roofLeft = new XnaColor(75, 85, 105);
            XnaColor roofBack = new XnaColor(60, 70, 90);
            XnaColor roofRight = new XnaColor(85, 95, 115);

            VertexPositionColor[] roof = new VertexPositionColor[12];

            // Trójkąt przedni
            roof[0] = new VertexPositionColor(tBottom, roofFront);
            roof[1] = new VertexPositionColor(tRight, roofFront);
            roof[2] = new VertexPositionColor(peak, roofFront);

            // Trójkąt lewy
            roof[3] = new VertexPositionColor(tLeft, roofLeft);
            roof[4] = new VertexPositionColor(tBottom, roofLeft);
            roof[5] = new VertexPositionColor(peak, roofLeft);

            // Trójkąt tylny
            roof[6] = new VertexPositionColor(tTop, roofBack);
            roof[7] = new VertexPositionColor(tLeft, roofBack);
            roof[8] = new VertexPositionColor(peak, roofBack);

            // Trójkąt prawy
            roof[9] = new VertexPositionColor(tRight, roofRight);
            roof[10] = new VertexPositionColor(tTop, roofRight);
            roof[11] = new VertexPositionColor(peak, roofRight);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, roof, 0, 4);
            }
        }

        private void DrawWindmill(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            float wx = x + TileWidth * 0.22f; // Wiatrak po prawej-tylnej stronie
            float wy = y - TileHeight * 0.15f;
            float wHeight = 36f;              // Wysokość wieży wiatraka

            // Podstawa wieży na ziemi (4 punkty)
            Vector3 groundFront = new Vector3(wx, wy + 3f, 0);
            Vector3 groundLeft = new Vector3(wx - 4.5f, wy, 0);
            Vector3 groundRight = new Vector3(wx + 4.5f, wy, 0);
            Vector3 groundBack = new Vector3(wx, wy - 3f, 0);

            // Szczyt wieży (punkt pojedynczy)
            Vector3 wTop = new Vector3(wx, wy - wHeight, 0);

            XnaColor towerColor = new XnaColor(150, 150, 150);
            VertexPositionColor[] tower = new VertexPositionColor[8];

            // 4 główne nogi kratownicy
            tower[0] = new VertexPositionColor(groundFront, towerColor);
            tower[1] = new VertexPositionColor(wTop, towerColor);

            tower[2] = new VertexPositionColor(groundLeft, towerColor);
            tower[3] = new VertexPositionColor(wTop, towerColor);

            tower[4] = new VertexPositionColor(groundRight, towerColor);
            tower[5] = new VertexPositionColor(wTop, towerColor);

            tower[6] = new VertexPositionColor(groundBack, towerColor);
            tower[7] = new VertexPositionColor(wTop, towerColor);

            // Poziome poprzeczki stabilizujące w połowie wysokości
            Vector3 midFront = Vector3.Lerp(groundFront, wTop, 0.5f);
            Vector3 midLeft = Vector3.Lerp(groundLeft, wTop, 0.5f);
            Vector3 midRight = Vector3.Lerp(groundRight, wTop, 0.5f);
            Vector3 midBack = Vector3.Lerp(groundBack, wTop, 0.5f);

            VertexPositionColor[] braces = new VertexPositionColor[8];
            braces[0] = new VertexPositionColor(midFront, towerColor);
            braces[1] = new VertexPositionColor(midLeft, towerColor);

            braces[2] = new VertexPositionColor(midLeft, towerColor);
            braces[3] = new VertexPositionColor(midBack, towerColor);

            braces[4] = new VertexPositionColor(midBack, towerColor);
            braces[5] = new VertexPositionColor(midRight, towerColor);

            braces[6] = new VertexPositionColor(midRight, towerColor);
            braces[7] = new VertexPositionColor(midFront, towerColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, tower, 0, 4);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, braces, 0, 4);
            }

            // Statecznik ogonowy (kierunkowy)
            Vector3 tailEnd = wTop + new Vector3(-7f, -2f, 0);
            VertexPositionColor[] tail = new VertexPositionColor[2];
            tail[0] = new VertexPositionColor(wTop, new XnaColor(110, 110, 110));
            tail[1] = new VertexPositionColor(tailEnd, new XnaColor(110, 110, 110));

            // Trójkątna czerwona lotka ogona
            VertexPositionColor[] vane = new VertexPositionColor[3];
            XnaColor vaneColor = new XnaColor(215, 80, 60);
            vane[0] = new VertexPositionColor(tailEnd, vaneColor);
            vane[1] = new VertexPositionColor(tailEnd + new Vector3(-2.5f, -3.5f, 0), vaneColor);
            vane[2] = new VertexPositionColor(tailEnd + new Vector3(-2.5f, 2f, 0), vaneColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, tail, 0, 1);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, vane, 0, 1);
            }

            // Dynamicznie obracające się łopaty wiatraka (4 łopaty)
            float radius = 9f;
            float rotationSpeed = 2.4f;
            float angle = _waveTimer * rotationSpeed;

            XnaColor bladeColor = new XnaColor(225, 225, 225);
            VertexPositionColor[] blades = new VertexPositionColor[8];

            for (int i = 0; i < 4; i++)
            {
                float rads = angle + (i * (float)Math.PI / 2f);
                float tx = wTop.X + radius * (float)Math.Cos(rads);
                float ty = wTop.Y + radius * (float)Math.Sin(rads);

                blades[i * 2] = new VertexPositionColor(wTop, bladeColor);
                blades[i * 2 + 1] = new VertexPositionColor(new Vector3(tx, ty, 0), bladeColor);
            }

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, blades, 0, 4);
            }

            // Czerwony kołpak śmigła (centralna kropka)
            VertexPositionColor[] spinner = new VertexPositionColor[3];
            spinner[0] = new VertexPositionColor(wTop + new Vector3(0, -1.2f, 0), XnaColor.Red);
            spinner[1] = new VertexPositionColor(wTop + new Vector3(1.2f, 0.8f, 0), XnaColor.Red);
            spinner[2] = new VertexPositionColor(wTop + new Vector3(-1.2f, 0.8f, 0), XnaColor.Red);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, spinner, 0, 1);
            }
        }

        private void DrawCoalMine3D(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // 1. Hałda węgla (Coal Pile) - czarny/ciemnoszary prostopadłościan
            float cx = x + TileWidth * 0.18f;
            float cy = y + TileHeight * 0.18f;
            Draw3DBox(cx, cy, 10f, 5f, 6f, new XnaColor(25, 25, 25), new XnaColor(35, 35, 35), new XnaColor(45, 45, 45));

            // 2. Maszynownia (Engine Shed) - niski ciemnoszary budynek z boku
            float ex = x - TileWidth * 0.18f;
            float ey = y - TileHeight * 0.15f;
            Draw3DBox(ex, ey, 14f, 7f, 10f, new XnaColor(50, 50, 55), new XnaColor(70, 70, 75), new XnaColor(80, 80, 85));

            // 3. Szyb wydobywczy / wieża (Mining Tower) - czteronogowa stalowa kratownica
            float tx = x;
            float ty = y - TileHeight * 0.05f;
            float towerHeight = 30f;

            Vector3 groundFront = new Vector3(tx, ty + 4f, 0);
            Vector3 groundLeft = new Vector3(tx - 6f, ty, 0);
            Vector3 groundRight = new Vector3(tx + 6f, ty, 0);
            Vector3 groundBack = new Vector3(tx, ty - 4f, 0);

            // Szczyt wieży
            Vector3 wTop = new Vector3(tx, ty - towerHeight, 0);

            XnaColor steelColor = new XnaColor(120, 125, 130);
            VertexPositionColor[] tower = new VertexPositionColor[16];

            // 4 nogi wieży
            tower[0] = new VertexPositionColor(groundFront, steelColor);
            tower[1] = new VertexPositionColor(wTop, steelColor);

            tower[2] = new VertexPositionColor(groundLeft, steelColor);
            tower[3] = new VertexPositionColor(wTop, steelColor);

            tower[4] = new VertexPositionColor(groundRight, steelColor);
            tower[5] = new VertexPositionColor(wTop, steelColor);

            tower[6] = new VertexPositionColor(groundBack, steelColor);
            tower[7] = new VertexPositionColor(wTop, steelColor);

            // Poprzeczki stabilizujące
            Vector3 midFront = Vector3.Lerp(groundFront, wTop, 0.5f);
            Vector3 midLeft = Vector3.Lerp(groundLeft, wTop, 0.5f);
            Vector3 midRight = Vector3.Lerp(groundRight, wTop, 0.5f);
            Vector3 midBack = Vector3.Lerp(groundBack, wTop, 0.5f);

            tower[8] = new VertexPositionColor(midFront, steelColor);
            tower[9] = new VertexPositionColor(midLeft, steelColor);

            tower[10] = new VertexPositionColor(midLeft, steelColor);
            tower[11] = new VertexPositionColor(midBack, steelColor);

            tower[12] = new VertexPositionColor(midBack, steelColor);
            tower[13] = new VertexPositionColor(midRight, steelColor);

            tower[14] = new VertexPositionColor(midRight, steelColor);
            tower[15] = new VertexPositionColor(midFront, steelColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, tower, 0, 8);
            }

            // 4. Obracające się koło linowe na szczycie wieży
            float radius = 5f;
            float rotationSpeed = 3f;
            float angle = _waveTimer * rotationSpeed;

            // Szprychy koła
            XnaColor wheelColor = new XnaColor(200, 205, 210);
            VertexPositionColor[] wheel = new VertexPositionColor[12];

            for (int i = 0; i < 6; i++)
            {
                float rads = angle + (i * (float)Math.PI / 3f);
                float px = wTop.X + radius * (float)Math.Cos(rads);
                float py = wTop.Y + radius * (float)Math.Sin(rads);

                wheel[i * 2] = new VertexPositionColor(wTop, wheelColor);
                wheel[i * 2 + 1] = new VertexPositionColor(new Vector3(px, py, 0), wheelColor);
            }

            // Obręcz koła
            int segments = 8;
            VertexPositionColor[] wheelRim = new VertexPositionColor[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float rads = angle + (i * 2f * (float)Math.PI / segments);
                float px = wTop.X + radius * (float)Math.Cos(rads);
                float py = wTop.Y + radius * (float)Math.Sin(rads);
                wheelRim[i] = new VertexPositionColor(new Vector3(px, py, 0), wheelColor);
            }

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, wheel, 0, 6);
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, wheelRim, 0, segments);
            }
        }

        private void DrawWarehouse3D(float x, float y, WarehouseBuilding wh)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // Ustal kolory w zależności od kategorii magazynu
            XnaColor leftColor, rightColor, topColor;
            if (wh.AllowedCategory == ResourceCategory.Food)
            {
                // Magazyn żywności: jasnoniebieski/chłodniczy design
                leftColor = new XnaColor(160, 200, 240);
                rightColor = new XnaColor(180, 220, 250);
                topColor = new XnaColor(210, 235, 255);
            }
            else
            {
                // Magazyn kopalniany: rdzawo-pomarańczowy, industrialny design
                leftColor = new XnaColor(180, 100, 40);
                rightColor = new XnaColor(210, 120, 50);
                topColor = new XnaColor(230, 140, 60);
            }

            // Główny budynek magazynu (duży hangar)
            Draw3DBox(x, y - TileHeight * 0.05f, 22f, 11f, 18f, leftColor, rightColor, topColor);

            // Dodatkowy element ozdobny na dachu/obok
            if (wh.AllowedCategory == ResourceCategory.Food)
            {
                // Srebrny agregat chłodniczy na dachu hangaru
                Draw3DBox(x, y - TileHeight * 0.05f - 18f, 6f, 3f, 4f, new XnaColor(200, 200, 200), new XnaColor(220, 220, 220), new XnaColor(240, 240, 240));
            }
            else
            {
                // Mała, ciemnoszara skrzynia pomocnicza na zewnątrz
                Draw3DBox(x + TileWidth * 0.22f, y + TileHeight * 0.15f, 6f, 3f, 5f, new XnaColor(80, 80, 80), new XnaColor(100, 100, 100), new XnaColor(120, 120, 120));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _renderTimer?.Stop();
                _renderTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
