# GDD 更新块（第 1 个标签页，v0.7 → v0.8）——中文审阅版

依据：`MVP_Scope_Draft.md` 里已定的决策（D1–D7、D9、D10），截至 2026-11-01。
粘贴用英文版 `GDD_Update_Blocks.md`。本文件只用于审阅：**原文**保留 GDD 里的英文原句，方便查找；**新文**是中文翻译。
使用规则：
- 每块：在指定章节找到**原文**，替换为**新文**。
- 评论锚点：[a]（"@Devanshi Please check this first-time onboarding flow"）锚在标题 `2.1 Onboarding` 上，标题文字必须保持不变，下面的列表可以改。[b]（"Economic systems design in PRIORITY!!"）锚在 §5 的 `TO DESIGN: base income/exploration rate, ...` 段落上，这一段不替换，§5 只在它后面插入一段。
- 只属于 MVP 的删减（番茄钟只做图标、只做 Coin Rate、3 个锚点、不做登录奖励、演示模式）只写在 §9 / MVP Scope 标签页里，不去删 §1–8 的完整设计。

---

## 页首

**原文：** `GDD Version: 0.7` → **新文：** `GDD Version: 0.8`
**原文：** `Updated: October 28, 2026` → **新文：** `Updated: November 1, 2026`

---

## 1.2 参考（表格 "Kingdom Two Crowns" 一行，"Our differentiation" 列）

**原文：** `TO CONFIRM: adapt map visuals`
**新文：** 把"已解锁 / 未解锁"的对比用在图鉴上：未获得的卡显示为剪影，获得后显示全彩（见第 4 节）。TO CONFIRM：地图视觉。

---

## 2.1 新手引导（列表第 3 项）

**原文：** `Add a To-do: create a zone task and see it at the upper left.`
**新文：** 添加待办：用语音新建一条分区任务，出现在左上角。永远不强制用语音：没有网络时，改为选择一条系统里已有的待办。

---

## 2.2 重新进入与日常使用

**原文：** `TO CONFIRM: reward notifications, settlement presentation and whether to provide session summaries.`
**新文：**
- 结算展示：真正退出后（见第 6 节），下次 Start 时弹一次结算提示，显示离开期间获得的金币，保留小数点后 10 位（例如短暂离开时显示 0.0277777778）。其他地方金币都显示整数。
- TO CONFIRM：奖励通知，以及是否提供会话总结。

---

## 3.2 时钟与待办

**原文：** `TO CONFIRM: General To-do accessible everywhere without staying expanded; entry point and display rules inside/outside zones.`
**新文：**
- 分区标记：在游戏内某个分区里新建的待办，建好后马上改名为"(分区)-内容"（例如 "(Work)-写报告"），系统 To-do 里也会显示分区。游戏读取前缀，只在该分区显示这条待办，显示时去掉前缀。在游戏外（系统 To-do）添加的、或前缀被改坏的待办算 General，在所有分区都显示。分区信息存在系统待办本身，所以游戏 Reset 或重装都不会丢。
- TO CONFIRM：General 待办在各处都能访问但不常驻展开；入口位置。TO VERIFY：provider 的 todoEdit 在真机上能否使用（第 8 节）。

---

## 4. 小岛探索与收集

### 4a：替换第一条 TO CONFIRM

**原文：** `TO CONFIRM: online refresh timing/positions, discovery detection, opening controls, sampling algorithm, fade-in animation and rarity guarantees.`
**新文：**
- 卡牌揭示：开宝箱或卡包时卡牌直接显示，不做马赛克、模糊或淡入揭示。
- 图鉴显示：未获得的卡显示为带 ≥2 px 描边的剪影（眼镜上纯黑是看不见的）；获得后显示全彩。参考《王国两位君主》地图的已解锁 / 未解锁区域。
- TO CONFIRM：在线刷新时机 / 位置、发现判定、打开操作、抽取算法、稀有度保底。

### 4b：替换第二条 TO CONFIRM

**原文：** `TO CONFIRM: island count/themes, card inventory, collection layout/capacity and long-term goals; cat memes are a Special-content candidate.`
**新文：**
- 第一套卡组：9 张，每种稀有度 3 张（3 Common / 3 Treasured / 3 Special），正好填满 3×3 图鉴网格。现有 8 张插画，至少还需要 1 张。
- TO CONFIRM：小岛数量 / 主题、插画对应哪个稀有度、第一套之后的卡牌库存、长期图鉴容量（分页网格）和长期目标；猫猫梗是 Special 内容的候选。

---

## 5. 金币、升级与登录奖励（只插入）

不要替换 `TO DESIGN: base income/exploration rate, ...` 这一段（评论 [b]）。
**在这一段后面插入**一段新文字：
状态（2026-11-01）：上面列的各项都已有初稿，MVP 精简版（经济数值初稿第 8 节）也已按当前 MVP 范围更新（加回教学宝箱、10 分钟演示、1x / 60x / 1800x 演示模式、9 张卡且每种稀有度 3 张）。见「经济数值初稿」标签页和 Economy_Draft.xlsx。还没有试玩验证。

---

## 6. 离线进度与存档

**原文：** `TO CONFIRM: online/offline boundaries for pause/close/removal, save timing, clock-change and duplicate-settlement protection, reset/deletion/sync and anchor restoration.`
**新文：**
- 真正退出与暂停：只有关闭 app 或被系统杀掉才算退出，下次 Start 时弹结算提示。暂停（语音加待办、系统弹窗、熄屏、摘下眼镜）都算同一次会话：恢复时把暂停期间的收益静默补上（24 h 上限）。如果 app 在暂停期间被系统杀掉，下次 Start 按暂停那一刻的存档结算。
- 离开时长没有下限：离开多久都会结算。金币按小数存储。
- 存档时机：暂停时存，并且每 30 秒存一次；每次 Start 只结算一次。
- 时间来源：眼镜的系统时间；经过时间按 UTC 计算。系统时间被往回调时，经过时间按 0 算。所有游戏代码都通过同一个可注入的时钟读时间。
- Reset：设置里提供 Reset，需要二次确认；只清空本游戏的存档和锚点，不动系统待办。存档内部按槽位存；多存档槽界面以后再做。
- TO CONFIRM：同步、锚点恢复。

---

## 7. 美术与音频

**原文：** `TO CONFIRM: Visual style, typography, themes (Zen garden/seasons), map unlock visuals/conditions, card presentation and asset ownership/licensing/credits.`
**新文：**
- 卡牌展示：见第 4 节（直接揭示；未获得时显示剪影）。
- TO CONFIRM：字体、主题（禅意花园 / 四季）、地图解锁视觉 / 条件、素材归属 / 授权 / 署名，包括第 9 张卡的插画。

（下一行 `Visual style (MVP): 2D looping sprite animation ...` 已经写了视觉风格，所以 TO CONFIRM 里去掉 "Visual style"。）

---

## 8. 技术记录与验证

### 8a
**原文：** `Planned architecture: ModeManager events, IdleCore, OfflineProgress, SaveSystem, InputAdapter and HUD.`
**新文：** 计划架构：ModeManager 事件、IdleCore、OfflineProgress、SaveSystem（按槽位存档）、可注入的游戏时钟（IGameClock：真实时钟、测试时钟）、InputAdapter 和 HUD。

### 8b
**原文：** `TO VERIFY: anchor accuracy/drift/re-entry/restoration, To-do create/complete/delete sync, voice handoff, launcher controls and foreground/background accounting.`
**新文：** TO VERIFY：锚点精度 / 漂移 / 重新进入 / 恢复，待办新建 / 完成 / 删除的同步，用于分区前缀的 provider todoEdit，语音交接，启动器操作，前后台计时。

### 8c
**原文：** `TO VERIFY: viewing distance/scale/comfort, continuous-6DoF power/heat versus 0DoF fallback and island performance. Proposed budgets: models <5,000 faces, textures ≤512 px.`
**新文：** TO VERIFY：观看距离 / 缩放 / 舒适度，小岛性能。持续 6DoF 的耗电 / 发热暂缓：MVP 全程开 SLAM，专注玩法，上线前再评估。建议预算：模型 <5,000 面，贴图 ≤512 px。

---

## 9. 制作范围与分工

**原文：** `For demo showcase: [ ]`
**新文：** 演示版：即 MVP，见 MVP Scope 标签页（从全新安装开始的 10 分钟演示，包括新手引导和一次退出后重新进入，每位观众之间 Reset）。

**原文：** `TO CONFIRM: MVP/exclusions, island/card/screen counts, asset inventory, dates, milestones, acceptance, scope cuts, risks and fallbacks.`
**新文：** MVP 的范围、决策、工作拆分、里程碑、验收、风险和预案：见 MVP Scope 标签页。TO CONFIRM：界面数量、素材清单、日期。

---

## 10. 术语与版本

**原文：** `Version: v0.7, October 28, 2026. Consolidated systems, current rules, and uncertainty markings.`
**新文：** 版本：v0.8，2026 年 11 月 1 日。应用了 MVP 决策：卡牌揭示与图鉴剪影、带分区标记的待办、真正退出与暂停、时间来源、Reset、9 张卡的第一套卡组。

---

## 其他标签页（不在第 1 个标签页）

- **MVP Scope 标签页**：内容已过时（5 分钟演示、600x、预先准备的演示存档、像素到清晰的揭示）。整页替换为 `MVP_Scope_Draft.md`（9.1–9.7）。这一页没有评论。
- **Economy Draft 标签页**：第 8 节（MVP 子集）还砍掉了教学宝箱，用的还是 5 分钟 / 600x 的演示脚本和演示存档，需要经济那边的 session 更新，这里不处理。另外它的卡池是每种稀有度 5 / 3 / 1 张，和 D6（3 / 3 / 3）冲突。

## 冲突

- **§5 `TO DESIGN:` 段落（评论 [b]）**：不替换。因为已经有初稿，这段的措辞（"TO DESIGN"）现在部分过时，用后面插入的状态段落来说明。如果要改这一段本身（比如改成 "DRAFTED:"），评论 [b] 就要移动或重新创建，这一点留给 Yaxin 决定。
- **§2.1（评论 [a]）**：没有冲突。只改标题下面列表的第 3 项，标题文字保持 `2.1 Onboarding` 不变。Devanshi 看到的会是改后的文字。
