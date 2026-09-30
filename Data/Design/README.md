# 设计资源

这些资源由 `.tan` 设计文档整理为 Godot 可编辑的 `Resource`，用于后续 Inspector 调参和运行时接入。

| 目录 | 数量 | 类型 | 资源组 |
| --- | ---: | --- | --- |
| `ShipDesigns` | 10 | `ShipDesignData` | `Assets/Status/Ship_Design_Data.tres` |
| `ShipUpgradeNodes` | 90 | `ShipUpgradeNodeData` | `Assets/Status/Ship_Upgrade_Nodes.tres` |
| `Enemies` | 16 | `EnemyDesignData` | `Assets/Status/Enemy_Design_Data.tres` |
| `Cards` | 100 | `CardBenefitData` | `Assets/Status/Card_Benefit_Data.tres` |

舰船设计资源引用 `Data/PlayerShips` 中的实际 `PlayerShipData`，并保存中心环、第一环、第二环效果及每条三层局外升级路线。敌人资源保存生命、护盾、碰撞半径、移速、攻击范围、前摇、冷却、有效命中、控制抗性、生成上限、威胁权重、掉落与 Boss 阶段说明。卡牌资源按 `WPN/SYS/TRT/FLT/SIG` 分类保存稀有度、作用范围、效果与联动描述。

`IsImplementedInRuntime = false` 表示该对象目前是设计数据，尚未绑定具体运行时效果；接入效果时应保留资源 ID，避免 UI、掉落和存档使用显示名称作为主键。
