# API研究文档 / API Research Documentation

本文档详细记录了《逃离鸭科夫》游戏的Mod开发API，包括物品系统、动作系统、UI系统等。

---

## 📦 物品系统 (Item System)

### 1. ItemAssetsCollection - 物品资源集合

这是游戏的核心物品注册表，包含所有已注册的物品信息。

#### 访问所有物品信息

```csharp
// 方法1: 通过AllEntries属性（推荐）
var itemAssetsCollectionType = typeof(ItemAssetsCollection);
var allEntriesProperty = itemAssetsCollectionType.GetProperty(
    "AllEntries",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
);

if (allEntriesProperty != null)
{
    var allEntries = allEntriesProperty.GetValue(null);
    if (allEntries is System.Collections.IEnumerable enumerable)
    {
        foreach (var entry in enumerable)
        {
            // 获取TypeID（Key）
            var keyProperty = entry.GetType().GetProperty("Key");
            var key = keyProperty?.GetValue(entry); // TypeID (int)
            
            // 获取物品信息（Value）
            var valueProperty = entry.GetType().GetProperty("Value");
            var value = valueProperty?.GetValue(entry);
            
            // 获取物品名称
            var nameProperty = value?.GetType().GetProperty("ItemName") ?? 
                              value?.GetType().GetProperty("name") ??
                              value?.GetType().GetProperty("Name");
            string itemName = nameProperty?.GetValue(value)?.ToString() ?? "Unknown";
        }
    }
}
```

#### 通过TypeID实例化物品

```csharp
// 同步实例化（在Unity主线程）
var instantiateSyncMethod = typeof(ItemAssetsCollection).GetMethod(
    "InstantiateSync",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
);

if (instantiateSyncMethod != null)
{
    var item = instantiateSyncMethod.Invoke(null, new object[] { typeID }) as Item;
    if (item != null)
    {
        // 使用物品
        // ...
        
        // 记得销毁测试物品
        Destroy(item.gameObject);
    }
}
```

#### 可能的其他方法

```csharp
// 可能的异步实例化方法
// InstantiateAsync
// GetItemAsset
// GetItemPrefab
// TryGetItem
```

---

### 2. Item类 - 物品核心类

物品的核心类，继承自`ItemStatsSystem.Item`。

#### 基本属性

```csharp
Item item = ...;

// TypeID - 物品的唯一类型ID
int typeID = item.TypeID;

// 名称
string itemName = item.name; // Unity GameObject名称（可能包含"(Clone)"）
string displayName = item.name.Replace("(Clone)", "").Trim();

// 检查物品是否在玩家背包中
bool isInInventory = item.IsInPlayerCharacter();
```

#### 获取物品图标/贴图

```csharp
// 方法1: 直接通过Icon属性（如果存在）
var itemType = item.GetType();
var iconProp = itemType.GetProperty("Icon", 
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Instance | 
    System.Reflection.BindingFlags.NonPublic);

if (iconProp != null)
{
    Sprite icon = iconProp.GetValue(item) as Sprite;
    if (icon != null)
    {
        // 使用图标
        // icon.texture - 获取Texture2D
        // icon.sprite - 已经是Sprite对象
    }
}

// 方法2: 可能的其他属性名
// IconSprite
// ItemIcon
// Thumbnail
// Image
// DisplayIcon
```

#### 物品的其他可能属性

```csharp
// 可能存在的属性（需要反射探索）
var properties = item.GetType().GetProperties(
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Instance | 
    System.Reflection.BindingFlags.NonPublic);

foreach (var prop in properties)
{
    Debug.Log($"Property: {prop.Name} = {prop.GetValue(item)}");
}

// 常见可能的属性：
// - Description / 描述
// - ItemType / 物品类型
// - Rarity / 稀有度
// - Weight / 重量
// - Value / 价值
// - StackSize / 堆叠数量
// - SkillType / 技能类型（投掷物可能是"itemSkill"）
```

---

### 3. Inventory类 - 背包系统

玩家背包管理系统。

#### 获取背包中的物品

```csharp
var player = FindPlayerCharacter();
var inventory = player.GetComponent<Inventory>() ?? 
                player.GetComponentInChildren<Inventory>();

if (inventory != null)
{
    var inventoryType = inventory.GetType();
    
    // 方法1: GetItem(int slotIndex)
    var getItemMethod = inventoryType.GetMethod(
        "GetItem",
        System.Reflection.BindingFlags.Public | 
        System.Reflection.BindingFlags.Instance | 
        System.Reflection.BindingFlags.NonPublic
    );
    
    // 方法2: GetItemAt(int slotIndex) - 备选方法
    if (getItemMethod == null)
    {
        getItemMethod = inventoryType.GetMethod(
            "GetItemAt",
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic
        );
    }
    
    // 方法3: GetSlotItem(int slotIndex) - 备选方法
    if (getItemMethod == null)
    {
        getItemMethod = inventoryType.GetMethod(
            "GetSlotItem",
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic
        );
    }
    
    // 获取最大槽位数
    var maxSlotsProp = inventoryType.GetProperty("maxSlots");
    var slotCountProp = inventoryType.GetProperty("SlotCount");
    
    int maxSlots = 47; // 默认值（根据实际游戏调整）
    if (maxSlotsProp != null)
    {
        maxSlots = (int)maxSlotsProp.GetValue(inventory);
    }
    else if (slotCountProp != null)
    {
        maxSlots = (int)slotCountProp.GetValue(inventory);
    }
    
    // 遍历所有槽位
    for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
    {
        if (getItemMethod != null)
        {
            var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
            if (item != null)
            {
                Debug.Log($"Slot {slotIndex}: {item.name} (TypeID: {item.TypeID})");
            }
        }
    }
}
```

#### 可能的其他Inventory方法

```csharp
// 可能存在的其他方法（需要反射探索）
var methods = inventoryType.GetMethods(
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Instance | 
    System.Reflection.BindingFlags.NonPublic);

foreach (var method in methods)
{
    Debug.Log($"Method: {method.Name} ({method.GetParameters().Length} params)");
}

// 常见可能的方法：
// - AddItem(Item item)
// - RemoveItem(Item item)
// - RemoveItemAt(int slotIndex)
// - SwapItems(int slot1, int slot2)
// - GetItemCount(int typeID)
// - HasItem(int typeID)
// - FindEmptySlot()
// - GetItemsOfType(int typeID)
```

---

## 🎮 动作系统 (Action System)

### 1. CharacterMainControl - 角色主控制器

控制角色的所有动作，包括移动、装备切换、使用物品等。

#### 获取当前手持物品

```csharp
var player = FindPlayerCharacter();
var playerType = player.GetType();

// 方法1: CurrentHoldItemAgent.Item（推荐）
var currentHoldItemAgentProp = playerType.GetProperty("CurrentHoldItemAgent", 
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Instance | 
    System.Reflection.BindingFlags.NonPublic);

if (currentHoldItemAgentProp != null)
{
    var agent = currentHoldItemAgentProp.GetValue(player);
    if (agent != null)
    {
        var agentType = agent.GetType();
        var itemProp = agentType.GetProperty("Item", 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance | 
            System.Reflection.BindingFlags.NonPublic);
        
        if (itemProp != null)
        {
            Item currentItem = itemProp.GetValue(agent) as Item;
        }
    }
}

// 方法2: 备选属性
var holdItemProp = playerType.GetProperty("HoldItem") ??
                   playerType.GetProperty("CurrentItem") ??
                   playerType.GetProperty("EquippedItem");

if (holdItemProp != null)
{
    Item currentItem = holdItemProp.GetValue(player) as Item;
}
```

#### 切换装备槽位

```csharp
// 使用SwitchHoldAgentInSlot(slotHash)
// 这是推荐的切换装备方法，使用槽位的哈希值

var player = FindPlayerCharacter();
var playerType = player.GetType();

// 获取CharacterItem.Slots来查找槽位信息
var slotsProp = typeof(CharacterItem).GetProperty("Slots",
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Static);

if (slotsProp != null)
{
    var slots = slotsProp.GetValue(null);
    // slots 可能是一个集合或字典
    // 需要遍历查找对应的槽位
}

// 切换方法
var switchMethod = playerType.GetMethod("SwitchHoldAgentInSlot",
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Instance | 
    System.Reflection.BindingFlags.NonPublic);

if (switchMethod != null)
{
    int slotHash = ...; // 从CharacterItem.Slots获取
    switchMethod.Invoke(player, new object[] { slotHash });
}
```

#### 执行物品动作（如投掷）

```csharp
// 可能的投掷/使用物品方法
var playerType = player.GetType();

// 方法1: UseItem / Use
var useItemMethod = playerType.GetMethod("UseItem") ??
                    playerType.GetMethod("Use") ??
                    playerType.GetMethod("UseCurrentItem");

// 方法2: 通过技能系统
var skillSystemProp = playerType.GetProperty("SkillSystem") ??
                      playerType.GetProperty("ActionSystem");

// 方法3: 直接触发技能
// 投掷物可能有对应的技能类型（SkillType = "itemSkill"）
var itemType = item.GetType();
var skillTypeProp = itemType.GetProperty("SkillType");
string skillType = skillTypeProp?.GetValue(item) as string;

if (skillType == "itemSkill")
{
    // 触发技能/动作
}
```

#### 检测投掷动作

```csharp
// 方法1: 监控鼠标按键释放
bool wasMouseButton0Down = false;

void Update()
{
    bool isMouseButton0Down = Input.GetMouseButton(0);
    bool isMouseButton1Down = Input.GetMouseButton(1);
    
    Item currentItem = GetCurrentHoldItem(player);
    bool isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
    
    // 检测左键释放（且没有右键），表示投掷完成
    if (wasMouseButton0Down && !isMouseButton0Down && 
        isHoldingThrowable && !isMouseButton1Down)
    {
        Debug.Log("Throw completed!");
        OnThrowCompleted();
    }
    
    wasMouseButton0Down = isMouseButton0Down;
}

// 方法2: 监控物品数量变化
Dictionary<int, int> lastItemCounts = new Dictionary<int, int>();

void MonitorThrowableItems()
{
    var inventory = GetInventory();
    foreach (var slot in throwableSlots)
    {
        var item = GetItemFromSlot(inventory, slot);
        if (item != null)
        {
            int currentCount = GetItemCount(item);
            if (lastItemCounts.ContainsKey(slot))
            {
                int lastCount = lastItemCounts[slot];
                if (currentCount < lastCount)
                {
                    Debug.Log($"Item count decreased in slot {slot} - throw completed!");
                    OnThrowCompleted();
                }
            }
            lastItemCounts[slot] = currentCount;
        }
    }
}

// 方法3: 监控手持物品变化（从投掷物变为空手）
bool wasHoldingThrowable = false;

void Update()
{
    Item currentItem = GetCurrentHoldItem(player);
    bool isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
    bool isEmptyHand = currentItem == null;
    
    if (wasHoldingThrowable && isEmptyHand)
    {
        Debug.Log("Throw completed - empty hand detected!");
        OnThrowCompleted();
    }
    
    wasHoldingThrowable = isHoldingThrowable;
}
```

---

## 🖼️ UI系统 (UI System)

### 1. DialogueBubbles - 对话气泡系统

用于显示提示信息的气泡UI。

#### 显示气泡

```csharp
using Duckov.UI.DialogueBubbles;

// 获取玩家Transform（用于气泡位置）
Transform playerTransform = FindPlayerTransform();
if (playerTransform == null)
{
    playerTransform = Camera.main.transform; // 备用方案
}

// 显示气泡
DialogueBubbleManager.ShowBubble(
    playerTransform,
    "气泡文本内容",
    duration: 2f, // 持续时间（秒）
    positionOffset: Vector3.zero // 位置偏移
);

// 或者使用简化的API（如果存在）
// DialogueBubble.Show("文本", duration);
```

#### 气泡相关API探索

```csharp
// 可能的其他方法（需要反射探索）
var bubbleManagerType = typeof(DialogueBubbleManager);
var methods = bubbleManagerType.GetMethods(
    System.Reflection.BindingFlags.Public | 
    System.Reflection.BindingFlags.Static);

foreach (var method in methods)
{
    Debug.Log($"DialogueBubbleManager method: {method.Name}");
}

// 可能的方法：
// - ShowBubble(Transform, string, float)
// - ShowBubble(Transform, string, float, Vector3)
// - HideBubble()
// - ClearAllBubbles()
// - SetBubbleStyle(...)
```

### 2. 物品图标显示

#### 在UI中显示物品图标

```csharp
// Unity UI Image组件显示Sprite
using UnityEngine.UI;

Image iconImage = ...; // 你的UI Image组件
Sprite itemIcon = GetItemIcon(item);

if (itemIcon != null)
{
    iconImage.sprite = itemIcon;
    iconImage.enabled = true;
}
else
{
    // 使用默认图标或隐藏
    iconImage.enabled = false;
}

// 获取物品图标的方法
Sprite GetItemIcon(Item item)
{
    if (item == null) return null;
    
    var itemType = item.GetType();
    
    // 尝试多种可能的属性名
    string[] possibleIconProperties = {
        "Icon", "IconSprite", "ItemIcon", 
        "Thumbnail", "Image", "DisplayIcon"
    };
    
    foreach (var propName in possibleIconProperties)
    {
        var iconProp = itemType.GetProperty(propName,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
        
        if (iconProp != null)
        {
            var icon = iconProp.GetValue(item) as Sprite;
            if (icon != null)
            {
                return icon;
            }
        }
    }
    
    return null;
}
```

---

## 🔍 反射探索工具方法

### 探索类的所有成员

```csharp
void ExploreClass(Type type, object instance = null)
{
    Debug.Log($"=== Exploring {type.Name} ===");
    
    // 属性
    var properties = type.GetProperties(
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Static);
    
    foreach (var prop in properties)
    {
        try
        {
            object value = instance != null ? prop.GetValue(instance) : null;
            Debug.Log($"Property: {prop.Name} ({prop.PropertyType.Name}) = {value}");
        }
        catch { }
    }
    
    // 方法
    var methods = type.GetMethods(
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Static);
    
    foreach (var method in methods)
    {
        var parameters = method.GetParameters();
        string paramList = string.Join(", ", 
            parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Debug.Log($"Method: {method.Name}({paramList})");
    }
    
    // 字段
    var fields = type.GetFields(
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance |
        System.Reflection.BindingFlags.Static);
    
    foreach (var field in fields)
    {
        try
        {
            object value = instance != null ? field.GetValue(instance) : null;
            Debug.Log($"Field: {field.Name} ({field.FieldType.Name}) = {value}");
        }
        catch { }
    }
}

// 使用示例
void ExploreItemAssetsCollection()
{
    ExploreClass(typeof(ItemAssetsCollection));
}

void ExploreItem(Item item)
{
    ExploreClass(item.GetType(), item);
}
```

---

## 📚 常用TypeID列表（已知）

### 投掷物TypeID

```csharp
int[] throwableTypeIDs = {
    24,   // 手雷 / Grenade
    66,   // ?
    67,   // ?
    660,  // ?
    933,  // 烟雾弹 / Smoke Grenade
    941,  // 燃烧瓶 / Molotov
    942   // 闪光弹 / Flashbang
};
```

### 非投掷物TypeID（黑名单）

```csharp
int[] excludedTypeIDs = {
    12,   // 豆子罐头 / BeanCan
    25,   // 手电筒 / Flashlight
    1257  // 屎球 / ShitBall
};
```

---

## 🔗 相关DLL文件

游戏的主要API位于以下DLL中：

- `Assembly-CSharp.dll` - 游戏核心逻辑（包含ItemAssetsCollection、Item、Inventory等）
- `ItemStatsSystem.dll` - 物品统计系统
- `TeamSoda.Duckov.Core.dll` - 游戏核心框架
- `UnityEngine.UI.dll` - Unity UI系统
- `UnityEngine.CoreModule.dll` - Unity核心模块

---

## 💡 最佳实践

### 1. 使用反射时的错误处理

```csharp
try
{
    var property = type.GetProperty("PropertyName", bindingFlags);
    if (property != null)
    {
        var value = property.GetValue(instance);
        // 使用value
    }
    else
    {
        Debug.LogWarning("Property not found, trying alternative...");
        // 尝试其他方法
    }
}
catch (System.Exception e)
{
    Debug.LogError($"Error accessing property: {e.Message}");
    // 降级处理
}
```

### 2. 缓存反射结果

```csharp
// 不好的做法：每次调用都反射
void BadExample()
{
    var prop = item.GetType().GetProperty("Icon");
    // ...
}

// 好的做法：缓存类型和属性信息
private static Dictionary<Type, System.Reflection.PropertyInfo> iconPropertyCache = 
    new Dictionary<Type, System.Reflection.PropertyInfo>();

Sprite GetItemIconCached(Item item)
{
    if (item == null) return null;
    
    var itemType = item.GetType();
    
    if (!iconPropertyCache.ContainsKey(itemType))
    {
        iconPropertyCache[itemType] = itemType.GetProperty("Icon",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic);
    }
    
    var iconProp = iconPropertyCache[itemType];
    return iconProp?.GetValue(item) as Sprite;
}
```

### 3. 异步操作

```csharp
using Cysharp.Threading.Tasks;

async UniTask ScanItemsAsync()
{
    await UniTask.Yield(); // 等待一帧，避免阻塞主线程
    
    var items = GetAllItems();
    foreach (var item in items)
    {
        ProcessItem(item);
        await UniTask.Yield(); // 每处理一个物品就等待一帧
    }
}
```

---

## 🔮 未来探索方向

### 1. 完整的物品数据库

- [ ] 扫描所有TypeID，建立完整的物品数据库
- [ ] 记录每个物品的属性（名称、图标、描述、类型等）
- [ ] 创建物品分类系统（投掷物、武器、消耗品等）

### 2. 动作系统深入

- [ ] 探索完整的技能/动作API
- [ ] 了解如何程序化触发动作
- [ ] 监听动作事件（开始、完成、取消）

### 3. UI系统扩展

- [ ] 创建自定义Mod设置UI
- [ ] 实现物品选择轮盘UI
- [ ] 添加物品图标显示系统

### 4. 性能优化

- [ ] 实现反射结果缓存
- [ ] 优化物品扫描性能
- [ ] 减少不必要的反射调用

---

## 📝 注意事项

1. **反射性能**：反射调用比直接调用慢，应尽量减少使用频率
2. **API变更**：游戏更新可能导致API变化，需要测试兼容性
3. **空值检查**：始终检查反射返回的值是否为null
4. **异常处理**：反射操作容易抛出异常，需要适当的try-catch
5. **线程安全**：Unity API必须在主线程调用

---

**最后更新**: 2025-11-02  
**版本**: 1.0

