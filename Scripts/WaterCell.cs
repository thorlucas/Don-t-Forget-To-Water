using System;

public enum FlowMomentumDirection {
    None,
    Left,
    Right
}

public class WaterCell {
    public static int MaxLevel = 8;

    public int x;
    public int y;

    public int Level;
    public bool NoCalc;
    public FlowMomentumDirection Direction;
    public int bodyIndex;

    public bool staticFull;

    public WaterCell(int x, int y) {
        this.x = x;
        this.y = y;
        bodyIndex = -1;
    }
}
