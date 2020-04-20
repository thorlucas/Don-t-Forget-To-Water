using Godot;
using System;

public class MainMenu : Control
{
    public override void _Ready() {
        GetNode<TileMap>("TileMap").UpdateBitmaskRegion();
    }

    public void PressedMenuButton(String targetScene) {
        GetTree().ChangeScene(targetScene);
    }
}
