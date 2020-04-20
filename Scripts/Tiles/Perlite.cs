using System;
public class Perlite : Tile, IHydratable {
    public Perlite(int x, int y) : base(x, y) {
    }

    private int dripTimer = 0;

    public bool hasWater = false;
    public int Hydration {
        get {
            if (hasWater) return 1;
            return 0;
        }
    }

    public override int TileMapIndex => 7;

    public int Hydrate(int max) {
        if (max > 0 && hasWater == false) {
            hasWater = true;
            return 1;
        } else {
            return 0;
        }
    }

    public override bool Update(Level level) {
        if (hasWater && dripTimer <= 0) {
            if (level.AddWater(x, y + 1, 1) > 0) {
                dripTimer = 5;
                hasWater = false;
            }
        }

        if (dripTimer > 0) {
            --dripTimer;
        }


        return false;
    }
}