using System;
using Godot;

public partial class CodexListItem : Button
{
    private const float TouchDragThreshold = 14.0f;

    [Export] public TextureRect ItemIcon;
    [Export] public Label NameLabel;

    private Action _onSelected;
    private Vector2 _pressPosition;
    private bool _isDragging;
    private bool _suppressNextPress;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        Pressed += selectItem;
    }

    public override void _ExitTree()
    {
        Pressed -= selectItem;
    }

    public void configure(string displayName, Texture2D icon, Action onSelected)
    {
        _onSelected = onSelected;
        NameLabel.Text = displayName;
        if (icon != null && IsInstanceValid(icon))
        {
            ItemIcon.Texture = icon;
        }

        ItemIcon.Show();
    }

    private void selectItem()
    {
        if (_suppressNextPress)
        {
            _suppressNextPress = false;
            return;
        }

        _onSelected?.Invoke();
    }

    public override void _GuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventScreenTouch touch:
                handleTouch(touch.Position, touch.Pressed);
                break;
            case InputEventScreenDrag drag:
                updateDragState(drag.Position);
                break;
            case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                handleTouch(mouseButton.Position, mouseButton.Pressed);
                break;
            case InputEventMouseMotion mouseMotion when Input.IsMouseButtonPressed(MouseButton.Left):
                updateDragState(mouseMotion.Position);
                break;
        }
    }

    private void handleTouch(Vector2 position, bool isPressed)
    {
        if (isPressed)
        {
            _pressPosition = position;
            _isDragging = false;
            return;
        }

        if (_isDragging)
        {
            _suppressNextPress = true;
        }
    }

    private void updateDragState(Vector2 position)
    {
        if (!_isDragging && position.DistanceSquaredTo(_pressPosition) >= TouchDragThreshold * TouchDragThreshold)
        {
            _isDragging = true;
        }
    }
}
