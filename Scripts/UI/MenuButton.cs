using Godot;
using System;

public class MenuButton : CenterContainer
{
    [Export]
    public String TargetScene;

    [Signal]
    public delegate void PressedMenuButton(String targetScene);

    public void PressedButton() {
        EmitSignal(nameof(PressedMenuButton), TargetScene);
    }
}
