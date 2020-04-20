using Godot;
using System;

public class Win : Node
{
    [Export]
    public PackedScene nextLevel;

    public int Points;

    public override void _Ready()
    {
        (FindNode("PointsLabel") as Label).Text = String.Format("Score: {0}", Points);
        //FindNode("NextLevelButton").Connect("pressed", this, nameof(NextLevelButtonPressed));
    }

    [Signal]
    public delegate void NextLevel(PackedScene nextLevel);

    public void NextLevelButtonPressed() {
        EmitSignal(nameof(NextLevel), nextLevel);
    }

}
