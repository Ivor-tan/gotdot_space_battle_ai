using System;
using System.Collections.Generic;
using Godot;

/// <summary>Displays eligible ships as reusable icon cards for the meta-upgrade tree.</summary>
public partial class ShipUpgradeShipSelector : PanelContainer
{
    private static readonly PackedScene ShipSelectorItemScene = GD.Load<PackedScene>(Assets.CodexListItem);

    [Export] public GridContainer ShipGrid;

    private Action<PlayerShipData> _onShipSelected;

    public void Configure(IReadOnlyList<PlayerShipData> ships, Action<PlayerShipData> onShipSelected)
    {
        _onShipSelected = onShipSelected;
        if (ShipGrid == null || !IsInstanceValid(ShipGrid))
        {
            LogUtil.Error("Ship upgrade selector grid is unavailable.");
            return;
        }

        foreach (Node child in ShipGrid.GetChildren())
        {
            child.QueueFree();
        }

        if (ShipSelectorItemScene == null || !IsInstanceValid(ShipSelectorItemScene))
        {
            LogUtil.Error("Ship upgrade selector item scene is unavailable.");
            return;
        }

        for (int index = 0; index < ships.Count; index++)
        {
            PlayerShipData shipData = ships[index];
            if (shipData == null || !IsInstanceValid(shipData))
            {
                continue;
            }

            CodexListItem shipItem = ShipSelectorItemScene.Instantiate<CodexListItem>();
            if (shipItem == null)
            {
                LogUtil.Error("Unable to instantiate ship upgrade selector item.");
                continue;
            }

            shipItem.configure(shipData.getDisplayName(), shipData.Icon, () => _onShipSelected?.Invoke(shipData));
            ShipGrid.AddChild(shipItem);
        }
    }
}
