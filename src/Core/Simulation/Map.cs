using System;

namespace Conglomerate
{
    public class Map
    {
        public int Width { get; }
        public int Height { get; }
        private readonly Tile[,] _tiles;

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width, height];
            InitializeMap();
        }

        private void InitializeMap()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Uproszczona mapa - wszystkie kafle to zielona trawa
                    Tile tile = new Tile(x, y, TileType.Grass);
                    
                    // Capitalism Lab: Land Value (centrum drogie, peryferia tanie)
                    float centerX = Width / 2f;
                    float centerY = Height / 2f;
                    float maxDist = (float)Math.Sqrt(centerX * centerX + centerY * centerY);
                    float dist = (float)Math.Sqrt(Math.Pow(x - centerX, 2) + Math.Pow(y - centerY, 2));
                    
                    // Wartość od 0.5 (obrzeża) do 2.0 (centrum)
                    tile.LandValue = 2.0f - (dist / maxDist) * 1.5f;
                    
                    _tiles[x, y] = tile;
                }
            }
        }

        public Tile GetTile(int x, int y)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                return _tiles[x, y];
            }
            throw new ArgumentOutOfRangeException($"Współrzędne ({x},{y}) są poza granicami mapy.");
        }

        public bool BuildBuildingOnTile(int x, int y, Building building)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;

            var tile = _tiles[x, y];
            if (tile.Type == TileType.Grass && tile.Building == null)
            {
                tile.Type = TileType.Building;
                tile.Building = building;
                return true;
            }
            return false;
        }
    }
}
