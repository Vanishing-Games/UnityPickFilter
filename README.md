# UnityPickFilter

Unity Editor 场景点击过滤工具。通过 ScriptableObject 配置规则，批量控制场景物体的 Pickability，解决大型场景中物体叠加导致难以点击选中的问题。

---

## 特性

- **规则驱动**：每条规则支持名称（正则）、Tag、Layer、拥有/不拥有 Component 等多维度匹配，条件之间为 AND 逻辑
- **ScriptableObject 配置**：规则存储在 SO 资产中，支持多个 SO 独立开关
- **优先级控制**：窗口列表上方的 SO 优先级更高（First-wins），冲突时在 Console 输出 Warning
- **作用范围**：SingleObject（仅匹配对象本身）或 Tree（对象及所有子物体）
- **自动应用**：Editor 启动、Scene 切换、Hierarchy 变化（300ms 防抖）、SO 保存时自动重新应用
- **Odin 增强**：默认依赖 Odin Inspector，提供组件类型可搜索下拉选择；无 Odin 时定义宏 `UNITY_PICK_FILTER_NO_ODIN` 降级为纯 Unity 实现

---

## 依赖

| 模式 | 要求 |
|---|---|
| 默认 | [Odin Inspector](https://odininspector.com/)（Asset Store） |
| 无 Odin | 定义脚本宏 `UNITY_PICK_FILTER_NO_ODIN`，仅使用 Unity 内置 API |

### 切换到无 Odin 模式

在 **Edit → Project Settings → Player → Scripting Define Symbols** 中添加：

```
UNITY_PICK_FILTER_NO_ODIN
```

---

## 安装

### 作为 git submodule

```bash
git submodule add <repo-url> Assets/Editor/UnityPickFilter
git submodule update --init
```

### 直接复制

将 `Assets/Editor/UnityPickFilter/` 目录复制到目标项目的 `Assets/Editor/` 下即可。

---

## 快速开始

1. 打开菜单 **Tools → RainRust → Pick Filter**
2. 点击 **Create New Rule Set** 创建一个规则集 SO，选择保存路径
3. 在 Inspector 中为该 SO 添加规则，配置匹配条件和动作（DisablePick / EnablePick）
4. 点击窗口中的 **Apply Now**，或保持 **Auto Apply** 开启让其自动生效

---

## 窗口说明

```
┌─────────────────────────────────────────────────────────────────┐
│  [Auto Apply: ON]                  [Apply Now]   [Reset All]    │
├─────────────────────────────────────────────────────────────────┤
│  Rule Sets  （上方 = 高优先级）                                  │
│  ≡  [✓]  UI过滤规则.asset                          [Select]    │
│  ≡  [✓]  背景层规则.asset                          [Select]    │
│  ≡  [ ]  Debug用白名单.asset  (disabled)           [Select]    │
├─────────────────────────────────────────────────────────────────┤
│  Add Existing SO: [ _________________ ]                         │
└─────────────────────────────────────────────────────────────────┘
```

| 控件 | 说明 |
|---|---|
| `≡` 拖拽手柄 | 调整 SO 优先级顺序 |
| 左侧 `[✓]` | 开启/关闭该 Rule Set SO |
| `[Select]` | 在 Project 窗口中定位该 SO |
| **Apply Now** | 立即应用所有规则（忽略 Auto Apply 开关） |
| **Reset All** | 重置场景内所有物体为可点击状态 |
| **Add Existing SO** | 将已有的 SO 资产加入列表 |

---

## 规则配置

在 `PickFilterRuleSO` 的 Inspector 中配置规则列表，每条规则包含：

| 字段 | 说明 |
|---|---|
| **Rule Name** | 规则名称，用于冲突 Warning 中的定位 |
| **Action** | `DisablePick`（禁止点击）或 `EnablePick`（允许点击） |
| **Scope** | `SingleObject`（仅该物体）或 `Tree`（该物体及全部子物体） |
| **Name (Regex)** | 按 GameObject 名称匹配，支持正则表达式，大小写不敏感 |
| **Tag** | 按 Tag 匹配 |
| **Layer** | 按 LayerMask 匹配（支持多层选择） |
| **Has Component** | 填写类型名（如 `SpriteRenderer`），匹配拥有该组件的物体 |
| **Not Has Component** | 匹配不含该组件的物体 |

条件之间为 **AND** 逻辑，所有启用的条件全部满足才匹配。全部条件均未启用时，该规则作为**通配符**匹配所有物体。

### 示例

**禁止点击所有 UI 层物体：**
```
Action: DisablePick  |  Scope: Tree
Layer: UI
```

**禁止点击名称以 "Tilemap" 开头的物体及其子物体：**
```
Action: DisablePick  |  Scope: Tree
Name (Regex): ^Tilemap
```

**仅允许点击带有 PlayerController 组件的物体：**
```
Action: DisablePick  |  Scope: Tree   ← 先用通配符禁用所有（空条件）
---
Action: EnablePick   |  Scope: SingleObject
Has Component: PlayerController
```

---

## 优先级与冲突

- 窗口中**靠上的 SO 优先级更高**
- 同一 SO 内**靠上的规则优先级更高**
- 当同一个物体被多个规则以**不同 Action** 匹配时，首条匹配生效，其余被跳过并在 Console 输出 Warning：

```
[PickFilter] Conflict on GameObject 'Button_Start' — First-match wins.
  Applied  : EnablePick  (SO: 'WhitelistRules', Rule: 'Allow Interactables')
  Skipped  : DisablePick (SO: 'GlobalFilter',   Rule: 'Disable UI Layer')
```

---

## 文件结构

```
Assets/Editor/UnityPickFilter/
├── UnityPickFilter.asmdef          # Editor-only 独立程序集
├── Core/
│   ├── PickFilterRule.cs           # 规则数据类（枚举 + Matches 逻辑）
│   ├── PickFilterRuleSO.cs         # Rule Set ScriptableObject
│   ├── PickFilterRuleSOEditor.cs   # Rule Set 的自定义 Inspector
│   ├── PickFilterSettings.cs       # 主配置 SO（有序列表 + autoApply）
│   └── PickFilterProcessor.cs      # [InitializeOnLoad] 应用引擎
└── Window/
    └── PickFilterWindow.cs         # EditorWindow 主窗口
```

项目侧配置（不在 submodule 内）：

```
Assets/Settings/PickFilter/
├── PickFilterSettings.asset        # 主配置（自动创建，项目唯一）
└── *.asset                         # 用户创建的 Rule Set SO
```

---

## 兼容性

- Unity 6000.x (Unity 6 LTS) 及以上
