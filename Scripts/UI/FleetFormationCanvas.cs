using System;
using Godot;

public partial class FleetFormationCanvas : Control
{
    public event Action<int> SlotSelected;
    public event Action<int, int> ShipDragged;

    private const float SlotRadius = 34.0f;
    private const int PendingStarLevel = 1;
    private readonly Color _emptySlotColor = new("1C486A");
    private readonly Color _occupiedSlotColor = new("303A4C");
    private readonly Color _fusionSlotColor = new("2E7D5A");
    private readonly Color _selectedSlotColor = new("D79A35");
    private readonly StyleBoxFlat _canvasStyle = makeCanvasStyle();
    private readonly Vector2[] _hexagonPoints = new Vector2[6];
    private PlayerManager _playerManager;
    private PlayerShipData _pendingShip;
    private bool _selectOccupiedShipsOnly;
    private int _selectedSlotIndex = -1;
    private int _dragSourceIndex = -1;
    private bool _isDragging;
    private Vector2 _dragPosition;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MinimumSizeChanged += QueueRedraw;
    }

    public override void _ExitTree()
    {
        MinimumSizeChanged -= QueueRedraw;
    }

    public void configure(PlayerManager playerManager, PlayerShipData pendingShip = null, bool selectOccupiedShipsOnly = false)
    {
        _playerManager = playerManager;
        _pendingShip = pendingShip;
        _selectOccupiedShipsOnly = selectOccupiedShipsOnly;
        _selectedSlotIndex = -1;
        _dragSourceIndex = -1;
        QueueRedraw();
    }

    public void setSelectedSlot(int slotIndex)
    {
        _selectedSlotIndex = slotIndex;
        QueueRedraw();
    }

    public bool canFuseAtSlot(int slotIndex)
    {
        if (_pendingShip == null || !IsInstanceValid(_pendingShip))
        {
            return false;
        }

        PlayerController player = getPlayerAtSlot(slotIndex);
        return player != null && isMatchingPendingFusionTarget(player);
    }

    public override void _Draw()
    {
        if (_playerManager == null || !IsInstanceValid(_playerManager))
        {
            return;
        }

        DrawStyleBox(_canvasStyle, new Rect2(Vector2.Zero, Size));
        for (int index = 1; index <= _playerManager.MaxPlayerCount; index++)
        {
            Vector2 slotPosition = getSlotPosition(index);
            PlayerController player = getPlayerAtSlot(index);
            bool isEmpty = player == null;
            bool canFuse = !isEmpty && _pendingShip != null && IsInstanceValid(_pendingShip) &&
                isMatchingPendingFusionTarget(player);
            Color color = index == _selectedSlotIndex || index == _dragSourceIndex
                ? _selectedSlotColor
                : canFuse ? _fusionSlotColor : isEmpty ? _emptySlotColor : _occupiedSlotColor;
            drawHexagon(slotPosition, SlotRadius, color);

            Texture2D icon = getSlotIcon(index, player);
            if (icon != null && IsInstanceValid(icon))
            {
                DrawTextureRect(icon, new Rect2(slotPosition - new Vector2(18, 18), new Vector2(36, 36)), false);
            }

            string label = isEmpty ? index.ToString() : $"{index} ★{player.StarLevel}";
            DrawString(ThemeDB.FallbackFont, slotPosition + new Vector2(-18, 26), label,
                HorizontalAlignment.Center, 36.0f, 12, Colors.White);
        }

        if (_isDragging)
        {
            DrawCircle(_dragPosition, 24.0f, new Color(0.85f, 0.6f, 0.2f, 0.82f));
            Texture2D dragIcon = getDragIcon();
            if (dragIcon != null && IsInstanceValid(dragIcon))
            {
                DrawTextureRect(dragIcon, new Rect2(_dragPosition - new Vector2(16, 16), new Vector2(32, 32)), false);
            }
        }
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            _dragPosition = mouseButton.Position;
            if (mouseButton.Pressed)
            {
                int pressedSlotIndex = getSlotIndexAt(mouseButton.Position);
                if (_selectOccupiedShipsOnly)
                {
                    if (getPlayerAtSlot(pressedSlotIndex) != null)
                    {
                        SlotSelected?.Invoke(pressedSlotIndex);
                    }

                    return;
                }

                _dragSourceIndex = getPlayerAtSlot(pressedSlotIndex) != null ? pressedSlotIndex : -1;
                _isDragging = true;
                QueueRedraw();
                return;
            }

            if (!_isDragging)
            {
                return;
            }

            int targetSlotIndex = getSlotIndexAt(mouseButton.Position);
            int sourceSlotIndex = _dragSourceIndex;
            _isDragging = false;
            _dragSourceIndex = -1;
            QueueRedraw();
            if (targetSlotIndex < 1)
            {
                return;
            }

            if (sourceSlotIndex > 0)
            {
                if (sourceSlotIndex != targetSlotIndex)
                {
                    ShipDragged?.Invoke(sourceSlotIndex, targetSlotIndex);
                }
                return;
            }

            if (_pendingShip != null && IsInstanceValid(_pendingShip))
            {
                SlotSelected?.Invoke(targetSlotIndex);
            }
            return;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion && _isDragging)
        {
            _dragPosition = mouseMotion.Position;
            QueueRedraw();
        }
    }

    private PlayerController getPlayerAtSlot(int slotIndex)
    {
        if (_playerManager == null || slotIndex < 1 || !_playerManager.CurrentPlayerDic.ContainsKey(slotIndex))
        {
            return null;
        }

        PlayerController player = _playerManager.CurrentPlayerDic[slotIndex];
        return player != null && IsInstanceValid(player) ? player : null;
    }

    private Texture2D getSlotIcon(int slotIndex, PlayerController player)
    {
        if (slotIndex == _selectedSlotIndex && _pendingShip != null && IsInstanceValid(_pendingShip) &&
            _pendingShip.Icon != null && IsInstanceValid(_pendingShip.Icon))
        {
            return _pendingShip.Icon;
        }

        return player?.ShipData?.Icon;
    }

    private Texture2D getDragIcon()
    {
        PlayerController sourcePlayer = getPlayerAtSlot(_dragSourceIndex);
        return sourcePlayer?.ShipData?.Icon ?? _pendingShip?.Icon;
    }

    private bool isMatchingPendingFusionTarget(PlayerController player)
    {
        return _pendingShip != null && IsInstanceValid(_pendingShip) &&
            player.ShipData != null && IsInstanceValid(player.ShipData) &&
            player.ShipData.ShipTypeId == _pendingShip.ShipTypeId &&
            player.StarLevel == PendingStarLevel && player.StarLevel < 5;
    }

    private Vector2 getSlotPosition(int slotIndex)
    {
        int positionIndex = slotIndex - 1;
        if (positionIndex < 0 || positionIndex >= _playerManager.PositionList.Count)
        {
            return Size * 0.5f;
        }

        float maxFormationRadius = 1.0f;
        foreach (Vector2 position in _playerManager.PositionList)
        {
            maxFormationRadius = Mathf.Max(maxFormationRadius, position.Length());
        }

        float availableRadius = Mathf.Max(Mathf.Min(Size.X, Size.Y) * 0.5f - SlotRadius - 12.0f, 1.0f);
        float scale = availableRadius / maxFormationRadius;
        return Size * 0.5f + _playerManager.PositionList[positionIndex] * scale;
    }

    private int getSlotIndexAt(Vector2 point)
    {
        for (int index = 1; index <= _playerManager.MaxPlayerCount; index++)
        {
            if (point.DistanceTo(getSlotPosition(index)) <= SlotRadius)
            {
                return index;
            }
        }

        return -1;
    }

    private void drawHexagon(Vector2 center, float radius, Color color)
    {
        for (int pointIndex = 0; pointIndex < 6; pointIndex++)
        {
            float angle = Mathf.Pi / 6.0f + pointIndex * Mathf.Pi / 3.0f;
            _hexagonPoints[pointIndex] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        DrawColoredPolygon(_hexagonPoints, color);
        for (int pointIndex = 0; pointIndex < _hexagonPoints.Length; pointIndex++)
        {
            DrawLine(_hexagonPoints[pointIndex], _hexagonPoints[(pointIndex + 1) % _hexagonPoints.Length], Colors.White, 1.5f, true);
        }
    }

    private static StyleBoxFlat makeCanvasStyle()
    {
        StyleBoxFlat style = new()
        {
            BgColor = new Color("101827"),
            BorderColor = new Color("4A6A88")
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(10);
        return style;
    }
}
