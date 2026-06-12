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
                    _tiles[x, y] = new Tile(x, y, TileType.Grass);
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
