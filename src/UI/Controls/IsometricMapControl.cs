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
                            terrainColor = new XnaColor(76, 180, 76); // Soczysta zieleń trawy
                            borderColor = new XnaColor(40, 110, 40);
                        }
                        else if (tile.Building is CoalMine)
                        {
                            terrainColor = new XnaColor(45, 45, 45); // Antracyt / ciemnoszary
                            borderColor = new XnaColor(25, 25, 25);
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

            // 1. Zagroda (Pastwisko z krowami)
            DrawFenceAndCows(x, y);

            // 2. Główna stodoła (czerwony budynek dwuspadowy z białymi drzwiami X)
            DrawRedBarn(x, y);
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

        private void DrawFenceAndCows(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // Pastwisko (obszar ogrodzony) zajmujące część kafelka z przodu i z lewej
            float pfX = x - TileWidth * 0.15f;
            float pfY = y + TileHeight * 0.15f;

            // Drewniany płotek - pionowe słupki i poziome żerdzie
            XnaColor postColor = new XnaColor(120, 80, 50); // Drewno
            XnaColor railColor = new XnaColor(140, 95, 60);

            // Punkty narożne ogrodzenia (w rzucie izometrycznym)
            Vector3[] fenceCorners = new Vector3[]
            {
                new Vector3(x - TileWidth * 0.4f, y + TileHeight * 0.1f, 0), // Lewy
                new Vector3(x - TileWidth * 0.1f, y + TileHeight * 0.4f, 0), // Dolny
                new Vector3(x + TileWidth * 0.2f, y + TileHeight * 0.2f, 0)  // Prawy
            };

            // Rysowanie poziomych żerdzi (dwie żerdzie na każdym boku)
            for (int i = 0; i < fenceCorners.Length - 1; i++)
            {
                Vector3 p1 = fenceCorners[i];
                Vector3 p2 = fenceCorners[i+1];

                VertexPositionColor[] rails = new VertexPositionColor[4];
                // Dolna żerdź
                rails[0] = new VertexPositionColor(p1 + new Vector3(0, -2f, 0), railColor);
                rails[1] = new VertexPositionColor(p2 + new Vector3(0, -2f, 0), railColor);
                // Górna żerdź
                rails[2] = new VertexPositionColor(p1 + new Vector3(0, -5f, 0), railColor);
                rails[3] = new VertexPositionColor(p2 + new Vector3(0, -5f, 0), railColor);

                foreach (var pass in _basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, rails, 0, 2);
                }
            }

            // Rysowanie pionowych słupków na narożnikach i w połowie
            for (int i = 0; i < fenceCorners.Length; i++)
            {
                DrawFencePost(fenceCorners[i], postColor);
                if (i < fenceCorners.Length - 1)
                {
                    Vector3 mid = Vector3.Lerp(fenceCorners[i], fenceCorners[i+1], 0.5f);
                    DrawFencePost(mid, postColor);
                }
            }

            // Krowy na pastwisku (proste biało-czarne bloczki)
            DrawCow(x - TileWidth * 0.25f, y + TileHeight * 0.2f);
            DrawCow(x - TileWidth * 0.1f, y + TileHeight * 0.3f);
            DrawCow(x + TileWidth * 0.05f, y + TileHeight * 0.25f);
        }

        private void DrawFencePost(Vector3 basePos, XnaColor color)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            VertexPositionColor[] post = new VertexPositionColor[2];
            post[0] = new VertexPositionColor(basePos, color);
            post[1] = new VertexPositionColor(basePos + new Vector3(0, -7f, 0), color);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, post, 0, 1);
            }
        }

        private void DrawCow(float cx, float cy)
        {
            // Ciało krowy (białe)
            Draw3DBox(cx, cy, 3f, 1.5f, 2.5f, XnaColor.White, new XnaColor(220, 220, 220), XnaColor.White);

            // Czarne łaty (uproszczone jako małe prostokąty na górze)
            Draw3DBox(cx - 0.5f, cy, 1f, 1f, 2.6f, XnaColor.Black, XnaColor.Black, XnaColor.Black);
            Draw3DBox(cx + 0.5f, cy - 0.2f, 1f, 0.5f, 2.6f, XnaColor.Black, XnaColor.Black, XnaColor.Black);

            // Głowa krowy (czarno-biała)
            Draw3DBox(cx + 1f, cy + 0.5f, 1.5f, 1f, 3.5f, XnaColor.Black, XnaColor.White, XnaColor.Black);
        }

        private void DrawRedBarn(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            float bw = TileWidth * 0.45f; // Szerokość stodoły (podłużny budynek)
            float bh = TileHeight * 0.35f; // Głębokość stodoły
            float bHeight = 18f;           // Wysokość ścian stodoły
            float bx = x + TileWidth * 0.15f; // Przesunięcie stodoły do tyłu/prawo
            float by = y - TileHeight * 0.15f;

            // Klasyczna czerwień Barn Red z białymi krawędziami
            XnaColor barnRedLeft = new XnaColor(160, 40, 40);
            XnaColor barnRedRight = new XnaColor(190, 50, 50);
            XnaColor barnRedTop = new XnaColor(190, 50, 50); // Ukryte pod dachem

            // Ściany stodoły
            Draw3DBox(bx, by, bw, bh, bHeight, barnRedLeft, barnRedRight, barnRedTop);

            // Wierzchołki góry ścian stodoły (podstawa dachu)
            Vector3 tBottom = new Vector3(bx, by + bh / 2f - bHeight, 0);
            Vector3 tLeft = new Vector3(bx - bw / 2f, by - bHeight, 0);
            Vector3 tRight = new Vector3(bx + bw / 2f, by - bHeight, 0);
            Vector3 tTop = new Vector3(bx, by - bh / 2f - bHeight, 0);

            // Grzbiet dachu dwuspadowego (gradient od jasnego do ciemnego)
            float roofHeight = 10f;
            Vector3 peakFront = tBottom - new Vector3(0, roofHeight, 0);
            Vector3 peakBack = tTop - new Vector3(0, roofHeight, 0);

            // Kolory dachu (gradienty: jasna kalenica, ciemny okap)
            XnaColor roofRidgeLeft = new XnaColor(180, 160, 140);  // Jasny przy grzbiecie
            XnaColor roofEavesLeft = new XnaColor(120, 100, 80);   // Ciemniejszy przy okapie

            XnaColor roofRidgeRight = new XnaColor(210, 190, 170); // Jasny przy grzbiecie (oświetlony)
            XnaColor roofEavesRight = new XnaColor(150, 130, 110); // Ciemniejszy przy okapie

            XnaColor gableColor = barnRedLeft;

            VertexPositionColor[] roofVerts = new VertexPositionColor[18];

            // Połać lewa (tLeft -> peakFront -> peakBack)
            roofVerts[0] = new VertexPositionColor(tLeft, roofEavesLeft);
            roofVerts[1] = new VertexPositionColor(peakFront, roofRidgeLeft);
            roofVerts[2] = new VertexPositionColor(peakBack, roofRidgeLeft);

            roofVerts[3] = new VertexPositionColor(tLeft, roofEavesLeft);
            roofVerts[4] = new VertexPositionColor(peakBack, roofRidgeLeft);
            roofVerts[5] = new VertexPositionColor(Vector3.Lerp(tLeft, tTop, 1f), roofEavesLeft); // tTop dla lewej połaci? Nie, tLeft-tTop to okap
            // Korekta dla trójkątów: (tLeft, peakFront, peakBack) i (tLeft, peakBack, tTop - czekaj, tTop to tylny wierzchołek)
            // Połać lewa to czworokąt: tLeft, tTop (dolna krawędź), peakFront, peakBack (górna krawędź)
            // Trójkąt 1: tLeft, peakFront, tTop? Nie. tLeft, peakFront, peakBack.
            // Trójkąt 2: tLeft, peakBack, tTop.
            roofVerts[3] = new VertexPositionColor(tLeft, roofEavesLeft);
            roofVerts[4] = new VertexPositionColor(peakBack, roofRidgeLeft);
            roofVerts[5] = new VertexPositionColor(tTop, roofEavesLeft);


            // Połać prawa (tRight -> peakBack -> peakFront)
            // Czworokąt: tBottom, tRight, peakFront, peakBack
            roofVerts[6] = new VertexPositionColor(tBottom, roofEavesRight);
            roofVerts[7] = new VertexPositionColor(tRight, roofEavesRight);
            roofVerts[8] = new VertexPositionColor(peakFront, roofRidgeRight);

            roofVerts[9] = new VertexPositionColor(tRight, roofEavesRight);
            roofVerts[10] = new VertexPositionColor(peakBack, roofRidgeRight);
            roofVerts[11] = new VertexPositionColor(peakFront, roofRidgeRight);


            // Szczyt przedni (trójkąt nad przednią lewą ścianą: tLeft, tBottom, peakFront)
            roofVerts[12] = new VertexPositionColor(tLeft, gableColor);
            roofVerts[13] = new VertexPositionColor(tBottom, gableColor);
            roofVerts[14] = new VertexPositionColor(peakFront, gableColor);

            // Szczyt tylny (trójkąt nad tylną prawą ścianą: tTop, tRight, peakBack)
            roofVerts[15] = new VertexPositionColor(tTop, gableColor);
            roofVerts[16] = new VertexPositionColor(tRight, gableColor);
            roofVerts[17] = new VertexPositionColor(peakBack, gableColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, roofVerts, 0, 6);
            }

            // Białe wrota stodoły z charakterystycznym krzyżakiem X (ściana przednia-lewa, tLeft do tBottom)
            Vector3 bLeftGround = new Vector3(bx - bw / 2f, by, 0);
            Vector3 bBottomGround = new Vector3(bx, by + bh / 2f, 0);

            // Wylicz pozycje drzwi na lewej ścianie (widocznej od frontu)
            Vector3 dBL = Vector3.Lerp(bLeftGround, bBottomGround, 0.35f);
            Vector3 dBR = Vector3.Lerp(bLeftGround, bBottomGround, 0.65f);
            Vector3 dTL = dBL - new Vector3(0, 10f, 0);
            Vector3 dTR = dBR - new Vector3(0, 10f, 0);

            XnaColor doorColor = XnaColor.White;
            VertexPositionColor[] door = new VertexPositionColor[8];
            // Zewnętrzne krawędzie białych wrót
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

            // Białe wykończenia (trims) na rogach
            VertexPositionColor[] trims = new VertexPositionColor[6];
            trims[0] = new VertexPositionColor(bBottomGround, doorColor);
            trims[1] = new VertexPositionColor(tBottom, doorColor);

            trims[2] = new VertexPositionColor(bLeftGround, doorColor);
            trims[3] = new VertexPositionColor(tLeft, doorColor);

            Vector3 bRightGround = new Vector3(bx + bw / 2f, by, 0);
            trims[4] = new VertexPositionColor(bRightGround, doorColor);
            trims[5] = new VertexPositionColor(tRight, doorColor);

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, trims, 0, 3);
            }
        }

        private void DrawCoalMine3D(float x, float y)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // Główny budynek szybu (masywna wieża na planie kwadratu)
            float bw = TileWidth * 0.35f;
            float bh = TileHeight * 0.35f;
            float bHeight = 24f;

            // Cieniowane ściany (lewa ciemniejsza, prawa jaśniejsza)
            XnaColor buildingLeft = new XnaColor(40, 40, 45);   // Głęboki cień
            XnaColor buildingRight = new XnaColor(70, 70, 75);  // Doświetlona
            XnaColor buildingTop = new XnaColor(90, 90, 95);    // Najjaśniejsza

            Draw3DBox(x, y, bw, bh, bHeight, buildingLeft, buildingRight, buildingTop);

            // Rampa logistyczna z przodu (z żółto-czarnymi pasami)
            float rampW = TileWidth * 0.2f;
            float rampH = TileHeight * 0.15f;
            float rampHeight = 6f;
            float rampX = x + TileWidth * 0.15f;
            float rampY = y + TileHeight * 0.15f;

            Draw3DBox(rampX, rampY, rampW, rampH, rampHeight, new XnaColor(50, 50, 50), new XnaColor(80, 80, 80), new XnaColor(100, 100, 100));

            // Żółto-czarne pasy ostrzegawcze na rampie (krawędź przednia-prawa)
            DrawWarningStripes(rampX, rampY, rampW, rampH, rampHeight);

            // Wieża wyciągowa (stalowa kratownica na dachu)
            float tx = x;
            float ty = y - bHeight;
            float towerHeight = 22f;

            Vector3 groundFront = new Vector3(tx, ty + bh / 4f, 0);
            Vector3 groundLeft = new Vector3(tx - bw / 4f, ty, 0);
            Vector3 groundRight = new Vector3(tx + bw / 4f, ty, 0);
            Vector3 groundBack = new Vector3(tx, ty - bh / 4f, 0);

            Vector3 wTop = new Vector3(tx, ty - towerHeight, 0);

            XnaColor steelColor = new XnaColor(110, 115, 120);
            VertexPositionColor[] tower = new VertexPositionColor[16];

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

            // Obracające się koło linowe na szczycie
            float radius = 6f;
            float rotationSpeed = 4f;
            float angle = _waveTimer * rotationSpeed;

            XnaColor wheelColor = new XnaColor(180, 180, 180);
            VertexPositionColor[] wheel = new VertexPositionColor[12];

            for (int i = 0; i < 6; i++)
            {
                float rads = angle + (i * (float)Math.PI / 3f);
                float px = wTop.X + radius * (float)Math.Cos(rads);
                float py = wTop.Y + radius * (float)Math.Sin(rads);

                wheel[i * 2] = new VertexPositionColor(wTop, wheelColor);
                wheel[i * 2 + 1] = new VertexPositionColor(new Vector3(px, py, 0), wheelColor);
            }

            int segments = 12;
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

        private void DrawWarningStripes(float x, float y, float w, float h, float height)
        {
            if (GraphicsDevice == null || _basicEffect == null) return;

            // Krawędź przednia-prawa rampy (od dolnego-prawego rogu podstawy do przedniego rogu)
            // Zbudujemy uproszczoną ścieżkę z pasków
            Vector3 bBottom = new Vector3(x, y + h / 2f - height, 0);
            Vector3 bRight = new Vector3(x + w / 2f, y - height, 0);

            int stripeCount = 6;
            XnaColor yellow = new XnaColor(220, 180, 0);
            XnaColor black = new XnaColor(10, 10, 10);

            VertexPositionColor[] stripes = new VertexPositionColor[stripeCount * 2];

            for (int i = 0; i < stripeCount; i++)
            {
                float t1 = (float)i / stripeCount;
                float t2 = (float)(i + 0.5f) / stripeCount;

                Vector3 p1 = Vector3.Lerp(bBottom, bRight, t1);
                Vector3 p2 = Vector3.Lerp(bBottom, bRight, t2);

                XnaColor color = (i % 2 == 0) ? yellow : black;

                stripes[i * 2] = new VertexPositionColor(p1, color);
                stripes[i * 2 + 1] = new VertexPositionColor(p2, color);
            }

            foreach (var pass in _basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, stripes, 0, stripeCount);
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
