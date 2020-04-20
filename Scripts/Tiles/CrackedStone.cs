using System;
public class CrackedStone : Tile, IHydratable {
    private bool cracked = false;

    public CrackedStone(int x, int y) : base(x, y) {
    }

    public override int TileMapIndex => 6;

    public int Hydration => 0;

    public int Hydrate(int max) {
        if (max >= 1) {
            cracked = true;
            return 1;
        } else {
            return 0;
        }
    }

    public override bool Update(Level level) {
        if (cracked) {
            level.SetTile(x, y, new Air(x, y));
            return true;
        } else {
            return false;
        }
    }
}