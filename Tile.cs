namespace Conglomerate
{
    public enum TileType
    {
        Grass,
        Water,
        Building
    }

    public class Tile
    {
        public int X { get; }
        public int Y { get; }
        public TileType Type { get; set; }
        public Building? Building { get; set; }

        public Tile(int x, int y, TileType type = TileType.Grass)
        {
            X = x;
            Y = y;
            Type = type;
            Building = null;
        }
    }
}
