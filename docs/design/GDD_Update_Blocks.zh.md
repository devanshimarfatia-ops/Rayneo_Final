# GDD 更新块（第 1 个标签页，v0.8 → v0.9）——中文审阅版

> 中文 Google Doc（中文个人版）的实际措辞与英文不完全对应，粘贴时按中文文档原段落匹配；页首另删去“GDD 负责人／最终确认人：[ ]”，标记用“待确认（Qn）：”“待验证（Qn）：”。

依据：分支 `Yaxin-Mu/Prototype` 上已做好的原型（代码读取于 2026-11-21），以及 `Rayneo_Final/docs/DEVLOG.md`
里记录的决策（2026-11-07 条目：D8、配色、跟随身体的地图、54 张卡、锚点槽位、商店 / Profile、Start 界面）。
这一轮只让 GDD 和原型保持一致，不加新设计（主动玩法 / 点击玩法的提案是之后单独的一轮）。
粘贴用英文版 `GDD_Update_Blocks.md`。本文件只用于审阅：**原文**保留 GDD 里的英文原句，方便查找；**新文**是中文翻译。

使用规则：
- 每块：在指定章节找到**原文**（GDD 里的原句），替换为**新文**。
- "在……后面插入"的块是新增一段，不删除任何内容。
- 评论锚点：[a]（"@Devanshi Please check this first-time onboarding flow"）锚在标题 `2.1 Onboarding` 上（标题文字必须保持不变）。[b]（"Economic systems design in PRIORITY!!"）锚在 §5 以 `Economy values (first draft, November 1, 2026)` 开头的段落上（这一段不替换，本轮也不动它）。
- 只属于 MVP 的删减只写在 §9 / MVP Scope 标签页里，不去删 §1–8 的完整设计。
- 本版新增：每个待定标记都有编号（`TO CONFIRM (Q7)：...`），§11 待定事项列出全部。

---

## 页首

**原文：** `GDD Version: 0.8` → **新文：** `GDD Version: 0.9`
**原文：** `Updated: November 1, 2026` → **新文：** `Updated: November 21, 2026`
**原文：** `Game Name: [ ] ` → **新文：** `Game Name: [ ] (Q1)`
**原文：** `Typical session length: [ ]` → **新文：** `Typical session length: [ ] (Q2)`
**原文：** `Distribution and business model: [ ]` → **新文：** `Distribution and business model: [ ] (Q3)`

---

## 1.1 体验原则

**原文：** `TO CONFIRM: brief interaction, low distraction, visible progress, and reward browsing around breaks.`
**新文：** TO CONFIRM (Q4)：交互简短、干扰低、进度可见，在休息时浏览奖励。试玩反馈（2026 年 11 月）：开局太被动；开局需要多少主动玩法还没定。

---

## 1.2 参考（表格 "Spirit City: Lofi Sessions" 一行，"Our differentiation" 列）

**原文：** `Prioritize practical utility. The tool menu sits below the forward view and follows the glasses; the island sits to the right and requires a deliberate head turn.`
**新文：** 优先实用。工具栏在视野底部，跟随眼镜；小岛在右侧，需要特意转头才能看到。

---

## 2.1 新手引导（列表项；标题保持不变，评论 [a]）

**原文：** `Tool menu: locate the menu below the forward view.`
**新文：** 工具栏：在视野底部的工具栏上滑动。

**原文：** `View the island: turn right; the character briefly searches and discovers the tutorial chest.`
**新文：** 查看小岛：向右转（右边缘的箭头指路）；角色短暂搜索，发现教学宝箱。

**原文：** `Open the blind pack: receive five cards.`
**新文：** 打开盲包：在奖励窗口里点击盲包，获得五张卡。

### 在以 `Skip: available throughout.` 开头的列表项后面插入

**在这一段后面插入**一段新文字：
呈现方式：先显示一张介绍卡，然后每一步在底部区域显示一个带框的提示，标着 "TUTORIAL k/n"，要用的工具栏图标上方有闪烁箭头；最后一张结束卡收尾。每一步在玩家做出所教的操作后推进。跳过：没有打开面板时双击，弹出 "Skip tutorial?"；点击确认。当前步骤会存档，关掉 app 后从同一步继续。

---

## 2.2 重新进入与日常使用

**原文：** `Re-entry: select Start to restore saved data and settle offline earnings; Pomodoro resets to its unstarted state.`
**新文：** 重新进入：每次启动都显示 Start 界面（Start 按钮和金币计数）。真正退出后（第 6 节）先弹离线收益窗口：离开时长、获得的金币和找到的宝箱；点击继续，Start 界面上的金币计数会滚动到新余额。全新安装或 Reset 后直接进入 Start 界面。点 Start 后开始新手引导（2.1）。番茄钟重置为未开始状态。

**原文：** `Settlement presentation: after a real exit (Section 6), one settle toast on Start shows coins earned while away, with 10 decimal places (e.g. 0.0277777778 for a short absence). Everywhere else coins show as whole numbers.`
**新文：** 结算展示：离线收益窗口显示离开期间获得的金币，保留小数点后 5 位（例如短暂离开时显示 0.02778）；日志里保留 10 位。其他地方金币都显示整数。

**原文：** `TO CONFIRM: reward notifications and whether to provide session summaries.`
**新文：** TO CONFIRM (Q5)：奖励通知，以及是否提供会话总结。

---

## 3.1 空间布局与锚点

**原文：** `Layout: the tool menu follows the glasses below the forward view; zone tasks appear at the upper left; the island is placed to the right, outside straight-ahead view.`
**新文：**
- 布局（头部锁定的 HUD，按 RayNeo 分区）：左上角是时钟和金币计数，两者并排、同一级（同一行、同样字号）；下面依次是每小时金币和当前分区的任务；顶部中间是提示（toast）；底部是工具栏；工具栏上方是宝箱搜索状态和进度条。小岛是右侧的一张平面藏宝图，在正前方视野之外（第 4 节）。
- 小岛位置：地图跟随身体，不跟随头部。它在身体朝向右侧 55°、略低于视线约 8°、距离 2.9 m 处。向右看它时它不会动；面向新方向 1.5 s 后，它会移到新方向右侧 55° 处。地图不在视野里且有宝箱等待时，右边缘有闪烁箭头提示 "look right"。设置里有 Recenter map。

**原文：** `Anchors: up to four per user, each with its own tasks. Entering a zone shows its tasks; all tasks remain accessible in To-do. Anchors do not gate income or exploration; Pomodoro is independent of anchors (Section 3.3).`
**新文：**
- 锚点：每位用户最多四个，每个有自己的任务。进入分区时显示该分区的任务；所有任务都能在 To-do 里访问。锚点不限制收益或探索；番茄钟与锚点无关（第 3.3 节）。
- 锚点槽位：每个锚点是一个带编号、有名字的槽位。玩家在自己站的位置放置锚点（眼镜上没有命中检测；再次放置会替换旧的），从预设名里选名字（Work、Study、Office、Desk、Kitchen、Bedroom、Living room、Gym；离线可用），或通过系统 To-do 流程用语音命名（需要网络）。改名时会同步改写该槽位未完成待办的分区前缀（3.2）。分区判定：进入 1 m 内停留 2 s 算进入，离开超过 2 m 算退出。

**原文：** `TO CONFIRM: zone size, overlap priority, persistence, and island angle, distance, scale and repositioning.`
**新文：** TO CONFIRM (Q6)：分区重叠时的优先级，锚点跨会话的保存。分区大小和小岛位置是原型数值（见上），试玩后再调。

---

## 3.2 时钟与待办

**原文：** `Tools: clock, To-do and Pomodoro. Pomodoro interaction references Spirit City.`
**新文：** 工具：时钟、To-do 和番茄钟。时钟一直显示在 HUD 上，位于左上角金币计数旁边、与金币同级（第 3.1 节）：显示眼镜系统时间 HH:mm，每分钟更新，不可交互。工具栏有六个图标：Anchors、To-do、Collection、Profile、Pomodoro 和 Settings。滑动移动焦点，获得焦点的图标显示名称，点击打开。番茄钟的交互参考 Spirit City。

**原文：** `Task rewards: each completed To-do grants a fixed coin reward, independent of complexity. Amounts belong in the economy design.`
**新文：** 任务奖励：每完成一条待办获得固定金币，与复杂度无关，每条待办只发一次（重新打开后再完成不再发）。数额由经济设计决定。To-do 面板：当前分区的待办，加一行 "New to-do"；点击待办完成它，点击 "New to-do" 用语音新建。

**原文：** `TO CONFIRM: General To-do accessible everywhere without staying expanded; entry point. TO VERIFY: provider todoEdit on device (Section 8).`
**新文：**
- General 待办在所有分区都显示，并且是不在任何分区时唯一显示的待办。
- 已在设备上验证（2026-11-07）：provider 的 todoEdit 能从游戏里给待办改名；系统 To-do app 显示新文字。
- TO CONFIRM (Q7)：离线新建任务、空列表和错误处理。

**原文：** `TO CONFIRM: offline task entry, task selection, empty-list and error handling.`
**新文：** （删除这一行；已并入上面的 Q7）

---

## 3.3 番茄钟与休息提醒

**原文：** `TO CONFIRM: reminder after 90 minutes of continuous work; appearance, repeat interval, dismissal and timer reset conditions.`
**新文：** TO CONFIRM (Q8)：连续工作 90 分钟后的提醒；外观、重复间隔、关闭方式和计时重置条件。

---

## 3.4 输入与列表

**原文：** `TO CONFIRM: screen hierarchy, focus, wireframes, recovery, accessibility and notification presentation.`
**新文：**
- 面板：每个工具在 HUD 上方打开一个面板；有分区的面板用并排的三个框（Anchors：槽位 1–3；Collection：三种稀有度；Profile：Skills / Look / Pets）。滑动在框之间移动，点击打开，双击返回上一层，再双击关闭面板。模态窗口（奖励窗口、确认框）在关闭前独占输入。地图只在视野内（与头部朝向相差 20° 以内）时接收输入。
- TO CONFIRM (Q9)：恢复、无障碍和通知展示。

---

## 4. 小岛探索与收集

**原文：** `Collection display: cards not owned show as silhouettes with a ≥2 px outline (black alone is invisible on the glasses); owned cards show in full color. Reference: Kingdom Two Crowns' silhouette-to-detail unlock.`
**新文：** 图鉴显示：总览里每种稀有度一个框，显示 "已获得 / 总数"，有卡包等待时显示一行 "Open pack (N)"；稀有度页面用网格显示该稀有度的卡，并有信息栏（名称、数量、NEW）。未获得的卡显示为带 ≥2 px 描边和 "?" 的剪影（眼镜上纯黑是看不见的）；获得的卡显示全彩；NEW 小点在卡第一次获得焦点前一直显示。参考《王国两位君主》从剪影到细节的解锁。

**原文：** `Chest progress bar: a horizontal bar below the island shows search progress toward the next chest; it stays full with "Chests full" when all three slots hold chests.`
**新文：** 宝箱进度条：小岛下方的横条显示距离下一个宝箱的搜索进度，带状态文字（"Treasure hunt 42%"）；三个槽位都有宝箱时进度条保持满格，显示 "Chests full - open one"；找到的宝箱获得焦点时，文字显示 "Tap to open"。

### 在以 `Chest rewards: three rewards appear together` 开头的段落后面插入

**在这一段后面插入**一段新文字：
奖励窗口：开宝箱时在 HUD 上方弹出窗口，奖励逐个出现。窗口里的盲包点击即开，五张卡紧接着出现。点击跳过动画、打开下一个卡包或关闭；双击关闭，没开的卡包留在 Collection 里等待。

**原文：** `First card set: 9 cards, 3 per rarity (3 Common / 3 Treasured / 3 Special), filling a 3×3 collection grid. 8 artworks exist; at least 1 more is needed.`
**新文：** 第一套卡组：54 张，每种稀有度 18 张（18 Common / 18 Treasured / 18 Special），每种稀有度显示为 6 × 3 网格。原型用代码绘制的占位像素画。

**原文：** `TO CONFIRM: artwork-to-rarity mapping, card inventory beyond the first set, long-term collection capacity (paged grid) and goals; cat memes are a Special-content candidate.`
**新文：** TO CONFIRM (Q10)：54 张卡的插画（来源、风格、对应的稀有度），第一套之后的卡牌库存和长期图鉴目标；猫猫梗是 Special 内容的候选。

**原文：** `Pets: a character customization bought in the shop. A pet always follows the character and counts as a skin.`
**新文：** 角色外观：服装（包括男装和女装）和服装颜色，在 Profile 工具里。一部分免费，其余用金币购买；未解锁的选项可以在购买前在角色身上预览。宠物：在 Profile 工具里购买的角色装扮。宠物在地图上始终跟随角色，算作皮肤。外观和宠物没有属性效果。

---

## 5. 金币、升级与登录奖励

**原文：** `Coin spending: upgrades, blind packs and shop cosmetics (skin colors, pets, island skins, decorations).`
**新文：** 金币用途：升级（Profile → Skills）、盲包（Collection 里的 "Buy a pack"；卡包立即打开）和商店装扮（Profile → Look / Pets 里的服装、服装颜色、宠物；小岛皮肤和装饰以后再做）。

---

## 6. 离线收益与存档

**原文：** `Real exit vs pause: only closing the app or a system kill counts as an exit; the settle toast appears on the next Start.`
**新文：** 把“下次 Start 时弹结算提示”改为“下次启动时先弹离线收益窗口”。

**原文：** `TO CONFIRM: anchor restoration (Section 8, AnchorZone test 3).`
**新文：** 待确认（Q6）：锚点恢复方案（见第 8 节 AnchorZone 测试 3）。（与 3.1 是同一个待定项）

---

## 7. 美术与音频

**原文：** `Visual style (MVP): 2D looping sprite animation for the island, character search and chests, to lower development cost and power use. 3D island and character may be considered later.`
**新文：**
- 视觉风格：黑底上的线稿和像素画，使用 RayNeo 配色：青色 #4DE2FF 为主色，绿色 #44FF66 为强调色，灰青 #7FA8B8 用于次要文字。稀有度颜色：银色（Common）、金色（Treasured）、紫色（Special）。红色只用于破坏性警告。小岛是一张平面线稿藏宝图，有行走的探险者、宝箱和虚线搜索区域；只做 2D 动画，以降低开发成本和耗电。以后可以考虑 3D 小岛和角色。
- UI 用代码搭建，配自己的像素画，遵循 RayNeo 设计规范（决策 D8，2026 年 11 月 7 日）。最早的低饱和羊皮纸配色在真机上被否决。

**原文：** `TO CONFIRM: typography, island skin designs (Zen garden/seasons), map unlock visuals/conditions and asset ownership/licensing/credits, including the 9th card artwork.`
**新文：** TO CONFIRM (Q11)：字体、小岛皮肤设计（禅意花园 / 四季）、地图解锁视觉 / 条件、素材归属 / 授权 / 署名。

**原文：** `TO CONFIRM: Lo-fi, SFX, triggers/mixing/mute, roles, budget and delivery; inclusion of external music, notification blocking, app integrations, calendar and daily summaries.`
**新文：** TO CONFIRM (Q12)：Lo-fi、音效、触发 / 混音 / 静音、分工、预算和交付；是否加入外部音乐、通知屏蔽、应用集成、日历和每日总结。

---

## 8. 技术记录与验证

**原文：** `Record scope: October 18–19 implementation records and constraints, not evidence of current acceptance. Prototype status remains TBD.`
**新文：** 记录范围：截至 2026 年 11 月 7 日的实现记录和设备发现。原型状态：完整的 MVP 循环已在眼镜上运行（分支 Yaxin-Mu/Prototype，11 月 7 日做了四轮真机测试）；还没合并到 main。

**原文：** `Stack: Unity 2022.3.36f1, Android, RayNeo OpenXR ARDK 1.1.2. Planned architecture: ModeManager events, IdleCore, OfflineProgress, SaveSystem (per-slot saves), an injectable game clock (IGameClock: real clock, test clock), InputAdapter and HUD.`
**新文：** 技术栈：Unity 2022.3.36f1、Android、RayNeo OpenXR ARDK 1.1.2（作为 package 内嵌在仓库里）。已实现架构：ModeManager 事件、IdleCore（纯 C#）、离线结算、SaveSystem（每个槽位一个 JSON，临时文件 + 备份）、可注入的游戏时钟（IGameClock）、InputAdapter（单一输入通道，处理器栈）和用代码搭建、带面板的 HUD。

### 在以 `Verified on device (2026-10-19)` 开头的段落后面插入

**在这一段后面插入**一段新文字：
已在设备上验证（2026-11-07）：todoEdit 能从游戏里使用（分区前缀，D3）。app 在 SLAM 模式下运行时 startTodo 也能用：语音界面打开后会自己返回，SLAM 继续追踪，锚点仍然有效。镜腿滑动一次会触发两次步进事件（通常间隔 < 40 ms，有时 > 120 ms）；游戏忽略 0.18 s 内的第二次步进。间隔小于 0.4 s 的快速点击会合并成双击或三击（ARDK 触控设置），所以游戏不能用快速连点。

**原文：** `TO VERIFY: anchor accuracy/drift/re-entry/restoration (AnchorZone tests 1–4; no relocalization API exists, so the R1 manual zone pick is the fallback), provider todoEdit for zone prefixes (D3), startTodo in SLAM mode and SLAM recovery afterwards, launcher bottom layout / QUIT / double-tap back.`
**新文：** TO VERIFY (Q13)：锚点精度 / 漂移 / 重新进入 / 恢复（AnchorZone 测试 1–4；没有重定位 API，所以用 R1 手动选分区作为预案）；从游戏里调用 deleteTodo（语音改锚点名时用到）；商店、Look 和 Pets 的真机测试。

**原文：** `TO VERIFY: viewing distance/scale/comfort and island performance (after W3). Continuous-6DoF power/heat is deferred (D2): the MVP keeps SLAM on and focuses on gameplay; re-evaluate before release.`
**新文：** TO VERIFY (Q14)：长时间使用时地图位置的观看舒适度，小岛性能。持续 6DoF 的耗电 / 发热暂缓（D2）：MVP 全程开 SLAM，专注玩法，上线前再评估。

**原文：** `TO CONFIRM: tracking-loss fallback, recalibration, use environment, permissions/network/privacy/analytics and integration plan.`
**新文：** TO CONFIRM (Q15)：追踪丢失时的预案、重新校准、使用环境、权限 / 网络 / 隐私 / 数据分析和集成计划。

**原文：** `TO CONFIRM: one-minute onboarding and 20-minute comfort targets; participants, procedure, usability/distraction/reward-satisfaction measures, thresholds, owner and results.`
**新文：** TO CONFIRM (Q16)：一分钟新手引导和 20 分钟舒适度目标；参与者、流程、可用性 / 干扰 / 奖励满意度指标、阈值和结果。

---

## 9. 制作范围与分工

**原文（标题）：** `9. Production Scope and Ownership` → **新文（标题）：** `9. 制作范围`

**原文：** `For shippable version: [ ]` → **新文：** `For shippable version: [ ] (Q17)`

**原文：** `MVP scope, decisions, work breakdown, milestones, acceptance, risks and fallbacks: MVP Scope tab. TO CONFIRM: screen count, asset inventory and dates.`
**新文：** MVP 的范围、决策、工作拆分、里程碑、验收、风险和预案：见 MVP Scope 标签页。TO CONFIRM (Q18)：素材清单、日期。

**原文：** `Recorded workflow: feature branches and PRs; one editor per Unity scene. Yaxin's work includes anchors, voice To-do and launcher; Devanshi's includes Canvas integration.`
**新文：** 记录的工作流程：功能分支加 PR；每个 Unity 场景同一时间只有一人编辑。UI 用代码搭建（D8），所以没有 Canvas 集成这一步；真机场景由编辑器工具生成。GDD 不分配任务：负责人、排期和进度都在团队的 Trello 看板上。

**原文：** `TO CONFIRM: final owners/reviewers, Canvas integration ownership, capacity, schedule and production tracker.`
**新文：** *（删除这一行；任务分配不属于 GDD）*

**原文：** `Decisions: unresolved items appear in their relevant sections in yellow. Update the rule when agreed; record owner, date, rationale and affected sections as needed.`
**新文：** 决策管理：待定项只列在所属章节并标黄；确定后直接更新规则。决策记录可填写日期、理由与影响章节。

---

## 10. 术语与版本

**原文：** `Version: v0.8, November 1, 2026. Applied MVP decisions: card reveal and collection silhouettes, zone-tagged To-dos, real exit vs pause, time source, Reset, 9-card first set.`
**新文：**
- 版本：v0.9，2026 年 11 月 21 日。与原型同步：工具栏和 HUD 布局、跟随身体的地图、锚点槽位和命名、Start 界面和离线收益窗口（5 位小数）、奖励窗口、54 张卡的第一套卡组、Profile（升级、外观、宠物）和购买卡包、RayNeo 配色、用代码搭建的 UI（D8）。待定事项现在有编号（第 11 节）。
- v0.8，2026 年 11 月 1 日：卡牌揭示与图鉴剪影、带分区标记的待办、真正退出与暂停、时间来源、Reset、9 张卡的第一套卡组。

---

## 11. 待定事项（新章节；插在第 10 节后面）

行只关闭，不删除。类型：C = TO CONFIRM，V = TO VERIFY，F = 空字段。

| ID | 事项 | 章节 | 类型 | 阻塞 | 状态 |
|---|---|---|---|---|---|
| Q1 | 游戏名 | 页首 | F | 商店页、标题界面 | Open |
| Q2 | 典型会话时长 | 页首 | F | 经济玩家画像、试玩计划 | Open |
| Q3 | 发行与商业模式 | 页首 | F | 可发布版本范围 | Open |
| Q4 | 体验原则；开局需要多少主动玩法（试玩反馈） | 1.1 | C | 前期经济、新手引导、下一轮设计 | Open |
| Q5 | 奖励通知、会话总结 | 2.2 | C | HUD 提示 | Open |
| Q6 | 锚点重叠优先级与跨会话保存 | 3.1 | C | R1 预案、重新进入 | Open |
| Q7 | 离线新建任务、空列表、错误处理 | 3.2 | C | To-do 面板 | Open |
| Q8 | 工作 90 分钟后的休息提醒 | 3.3 | C | 番茄钟（v2） | Open |
| Q9 | 恢复、无障碍、通知展示 | 3.4 | C | HUD | Open |
| Q10 | 54 张卡的插画、第一套之后的库存、长期目标 | 4 | C | 图鉴美术、经济卡池大小 | Open |
| Q11 | 字体、小岛皮肤、地图解锁、素材授权 | 7 | C | 商店皮肤、署名 | Open |
| Q12 | 音频与集成 | 7 | C | 音频 | Open |
| Q13 | 锚点精度 / 恢复；deleteTodo；商店、Look、Pets 真机测试 | 8 | V | 语音改名、R1、演示第 12 步 | Open |
| Q14 | 长时间使用的地图观看舒适度；小岛性能 | 8 | V | 地图位置 | Open |
| Q15 | 追踪丢失、重新校准、权限 / 隐私 / 数据分析 | 8 | C | 上线 | Open |
| Q16 | 试玩目标、指标、结果 | 8 | C | 用户研究 | Open |
| Q17 | 可发布版本范围 | 9 | F | MVP 之后的计划 | Open |
| Q18 | 素材清单与日期 | 9 | C | 里程碑 | Open |
| — | 结算展示（原 2.2） | 2.2 | C | — | Closed v0.9：离线收益窗口，5 位小数 |
| — | 分区大小，小岛角度 / 距离 / 缩放 / 重新定位（原 3.1） | 3.1 | C | — | Closed v0.9：已记录原型数值 |
| — | provider todoEdit；SLAM 下的 startTodo（原 3.2 / 8） | 3.2, 8 | V | — | Closed v0.9：2026-11-07 已在设备上验证 |
| — | 界面层级与焦点（原 3.4） | 3.4 | C | — | Closed v0.9：已记录面板 / 框 / 模态规则 |
| — | 第 9 张卡的插画（原 4 / 7） | 4, 7 | C | — | Closed v0.9：被 54 张卡组取代（Q10） |
| — | Canvas 集成负责人（原 9） | 9 | C | — | Closed v0.9：D8 用代码搭建 UI |
| — | 最终负责人 / 审阅人、人力、排期、进度跟踪（原 9） | 9 | C | — | Closed v0.9：GDD 不分配任务（Trello） |

---

## 检查（在本地文本上完成）
- 第 1 个标签页里每个标记都带 Q#（Q1–Q18），并有对应的 Open 行；已关闭的标记列为 Closed 行。
- 没有替换任何带评论的段落：标题 `2.1 Onboarding` 文字不变；§5 的 `Economy values ...` 段落未动。
- 完整设计里锚点仍是"每位用户最多四个"；MVP 保留 3 个（MVP Scope 标签页）。
- 番茄钟保留在完整设计里（MVP 里只做图标）。
- 时钟是 HUD 元素，在金币计数旁边（3.1、3.2），不是工具栏图标。
- GDD 里不再有任何任务分配：§9 标题去掉"分工"，删除负责人标记，待定事项表没有负责人列。
