# Rob the Merchant

Rob the Merchant is a Slay the Spire 2 gameplay mod that adds a high-risk shop robbery sequence.

## How It Works

In a shop, pressure the merchant by repeatedly trying to buy something you cannot afford or by opening the merchant. On the sixth pressure attempt, the game offers a choice to "Ask" the Merchant for a Discount.

Choosing to fight starts the game's merchant combat encounter. If you lose, the run ends like a normal combat loss. If you win, the current shop is yours: all visible goods become free, the merchant leaves, and the shelves do not restock.

In multiplayer, every player with the mod installed votes on the robbery prompt. Once everyone chooses to fight, the whole team enters the merchant combat together. Victory applies the same shop robbery result for the whole team.

After robbing the merchant once, future shops in the same run are abandoned. The merchant is gone, no goods are available, and you can only move on.

## Installation

1. Download `RobMerchant-v0.1.3-multiplayer-vote-fix.zip` from the release.
2. Extract it into your Slay the Spire 2 `mods` folder.
3. The final layout should look like:

```text
Slay the Spire 2/
  mods/
    RobMerchant/
      RobMerchant.dll
      RobMerchant.json
```

4. Start Slay the Spire 2 with mods enabled and enable **Rob the Merchant**.

## Notes

- Singleplayer and multiplayer are supported.
- In multiplayer, all players should use the same mod version.
- The mod changes shop gameplay and can make a run much more volatile.

## Credits

Thanks to the Slay the Spire 2 modding reference projects and community examples, especially ModTemplate-StS2 and BaseLib-StS2, for making the early modding surface easier to understand.

The shop-robbing idea is inspired by Arknights (明日方舟).

---

# 抢商店

Rob the Merchant 是一个 Slay the Spire 2 玩法 Mod，给商店加入了一个高风险的抢劫机制。

## 机制说明

在商店里，反复点击买不起的商品，或者点击商人，都算作向商人施压。累计到第六次时，游戏会弹出选项，询问你是否要“请”商人降价。

选择动手后，会进入游戏自带的商人战斗。如果输了，就像普通战斗失败一样结束本局。如果赢了，当前商店里的可见商品都会变成 0 金，商人会离开，货架不会补货。

多人模式下，所有安装了本 Mod 的玩家都会在抢商店弹窗中投票。所有人都选择动手后，全队一起进入商人战斗；胜利后的抢商店效果全队共享。

本局游戏中只要抢过一次商店，之后遇到的商店都会变成废弃商店：商人不在，没有商品可买，只能继续前进。

## 安装方式

1. 在 release 页面下载 `RobMerchant-v0.1.3-multiplayer-vote-fix.zip`。
2. 解压到 Slay the Spire 2 的 `mods` 文件夹。
3. 最终目录应该像这样：

```text
Slay the Spire 2/
  mods/
    RobMerchant/
      RobMerchant.dll
      RobMerchant.json
```

4. 启动带 Mod 的 Slay the Spire 2，并启用 **Rob the Merchant**。

## 注意事项

- 支持单人模式和多人模式。
- 多人模式下，所有玩家应使用相同 Mod 版本。
- 这个 Mod 会显著改变商店风险和收益。

## 致谢

感谢 Slay the Spire 2 社区的 Mod 参考项目和示例，尤其是 ModTemplate-StS2 和 BaseLib-StS2，它们帮助我们理解了早期 Mod 接入方式。

抢商店机制的灵感来自 Arknights（明日方舟）。
