using System;
using Godot;

public class Sapling : Tile, IGoal {
    private static int MaxHydration = 3;
    private int hydration = 0;

    public Sapling(int x, int y) : base(x, y) {
    }

    public override int TileMapIndex => 5;
    public override Vector2 TileMapAutotileCoord => new Vector2(hydration, 0.0f);
    public override bool Transparent => true;
    public override bool Chiselable => false;

    public bool Complete => hydration >= MaxHydration;

    public override bool Update(Level level) {
        if (level.GetTile(x, y + 1) is IHydratable tile) {
            if (tile.Hydration > hydration) {
                hydration = Math.Min(tile.Hydration, 3);
                return true;
            } else {
                return false;
            }
        } else {
            level.SetTile(x, y, new Air(x, y));
            return true;
        }
    }
}