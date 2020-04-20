using System;
using Godot;

public class LevelManager : Node {
    private static int level = 10;
    private static int levelCount = 10;
    private static PackedScene currentPackedLevel; 

    public static PackedScene GetNextLevel() {
        if (level <= levelCount) {
            currentPackedLevel = GD.Load<PackedScene>(String.Format("res://Levels/Level{0}.tscn", level++));
            return currentPackedLevel;
        } else {
            currentPackedLevel = GD.Load<PackedScene>("res://Scenes/YouWin.tscn");
            return currentPackedLevel;
        }
    }

    public static PackedScene GetCurrentLevel() {
        return currentPackedLevel;
    }
}