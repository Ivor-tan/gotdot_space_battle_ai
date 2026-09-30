using Godot;

public partial class debugInfoPanel : CanvasLayer
{
	private Label _hpLabel;
	private Label _moveLabel;
	private Label _rotateLabel;
	private Label _attackLabel;
	private Label _damageLabel;
	private Label _countLabel;

	public override void _Ready()
	{
		_hpLabel = GetNode<Label>("Panel/VBox/HpLabel");
		_moveLabel = GetNode<Label>("Panel/VBox/MoveLabel");
		_rotateLabel = GetNode<Label>("Panel/VBox/RotateLabel");
		_attackLabel = GetNode<Label>("Panel/VBox/AttackLabel");
		_damageLabel = GetNode<Label>("Panel/VBox/DamageLabel");
		_countLabel = GetNode<Label>("Panel/VBox/CountLabel");
	}

	public override void _Process(double delta)
	{
		var pm = PlayerManager.Instance;
		if (pm == null || pm.Player == null) return;
		var s = pm.Player.CurrentPlayerStates;
		if (s == null) return;

		_hpLabel.Text = $"生命值：{s.Hp}";
		_moveLabel.Text = $"最大速度：{s.MaxSpeed:F1}  当前速度：{pm.Player.CurrentSpeed:F1}  加速度：{s.Acceleration:F1}  减速度：{s.Deceleration:F1}";
		_rotateLabel.Text = $"旋转速度：{s.RotateSpeed:F2}";
		_attackLabel.Text = $"攻击范围：{s.AttackRange:F1}  攻击间隔：{s.AttackInterval:F1}s";

		int damage = 0;
		foreach (var kvp in pm.PlayerBulletSpawns)
		{
			if (kvp.Value != null)
			{
				damage = kvp.Value.Damage;
				break;
			}
		}
		_damageLabel.Text = $"子弹伤害：{damage}";
		_countLabel.Text = $"飞船数量：{pm.CurrentPlayerCount}";
	}
}
