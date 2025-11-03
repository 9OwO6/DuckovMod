using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ItemStatsSystem;
using Duckov.Modding;
using Duckov.UI.DialogueBubbles;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace BetterThrowingSystem
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // Debug key to dump all items
        private KeyCode debugKey = KeyCode.F10;
        // Debug key to scan all items in inventory with details
        private KeyCode scanItemsKey = KeyCode.F11;
        // Throwable item key
        private KeyCode throwKey = KeyCode.G;
        // Player transform for dialogue bubbles
        private Transform? playerTransform;
        
        // List of throwable item slot numbers grouped by TypeID (category-based switching)
        private Dictionary<int, List<int>> throwableSlotsByTypeID = new Dictionary<int, List<int>>();
        // List of throwable TypeIDs in order (for category switching)
        private List<int> throwableTypeIDsInOrder = new List<int>();
        // Current category index (TypeID-based)
        private int currentCategoryIndex = -1;
        
        // Memory system for throwable selection
        private int? lastSelectedThrowableSlot = null;      // Last selected throwable slot
        private int? lastSelectedThrowableTypeID = null;    // Last selected throwable TypeID
        private int? previousEquippedSlot = null;           // Weapon slot before equipping throwable (deprecated, use slotHash)
        private KeyCode? previousEquippedKey = null;        // Weapon key before equipping throwable (1/2/V/etc.) (deprecated, use slotHash)
        private int? previousEquippedSlotHash = null;       // Equipment slot hash before equipping throwable (for SwitchHoldAgentInSlot)
        private string? previousEquippedSlotKey = null;     // Equipment slot key ("1", "2", "V", etc.)
        private bool hasCompletedThrow = false;             // Whether throw action was completed
        private int? lastEquippedThrowableSlot = null;      // Currently equipped throwable slot (if any)
        private bool lastActionWasGKey = false;             // Whether last action was pressing G key (for detecting continuous G presses)
        private bool lastActionWasWeaponSwitch = false;     // Whether last action was switching weapon (1/2/V key)
        
        // Long-press G selection mode
        private bool isInSelectionMode = false;             // Whether we're in long-press selection mode
        private int selectionModeCurrentIndex = 0;          // Current selected throwable index in selection mode
        private float gKeyHoldTime = 0f;                    // Time G key has been held
        private const float G_KEY_LONG_PRESS_TIME = 0.3f;   // Time to hold G to enter selection mode (seconds)
        
        // For detecting throw completion (monitor item count change)
        private Dictionary<int, int> lastItemCounts = new Dictionary<int, int>(); // slot -> count
        
        // State tracking for throw detection
        private bool wasHoldingThrowable = false; // Track if we were holding a throwable in previous frame
        private bool isThrowingInProgress = false; // Track if throw animation is in progress
        private float throwStartTime = 0f; // Time when throw was detected to have started
        private const float MAX_THROW_DURATION = 2f; // Maximum throw duration (seconds) - fallback timeout
        private bool wasMouseButton0Down = false; // Track mouse left button state for throw detection

        private void Start()
        {
            Debug.Log("[BTS] =========================================");
            Debug.Log("[BTS] Mod loaded (Start called) - VERSION 2.0");
            Debug.Log("[BTS] =========================================");
            
            // Try to find player transform for dialogue bubbles
            playerTransform = FindPlayerTransform();
            if (playerTransform != null)
            {
                Debug.Log("[BTS] Found player transform for DialogueBubbles.");
            }
            else
            {
                Debug.LogWarning("[BTS] Player transform not found, will use Camera.main.transform as fallback.");
            }

            // Scan and print all registered items in ItemAssetsCollection
            // This helps find the correct TypeID for throwables
            ScanAllRegisteredItems();
        }

        private void Update()
        {
            // ① First verify input is working - dump all items
            if (Input.GetKeyDown(debugKey))
            {
                Debug.Log("[BTS] F10 pressed.");
                DumpPlayerItems();
            }
            
            // Debug: Press F9 to scan Inventory slots and find slot switching methods
            if (Input.GetKeyDown(KeyCode.F9))
            {
                Debug.Log("[BTS] F9 pressed - Scanning Inventory slots and methods...");
                ScanInventorySlots();
            }
            
            // Debug: Press F8 to scan all Update/Input handling methods that might process number keys
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("[BTS] F8 pressed - Scanning for number key input handlers...");
                ScanNumberKeyHandlers();
            }
            
            // Debug: Press F11 to scan all items in inventory with details (helps identify throwable items)
            if (Input.GetKeyDown(scanItemsKey))
            {
                Debug.Log("[BTS] F11 pressed - Scanning all inventory items with details...");
                ScanAllInventoryItemsWithDetails();
            }

            // ② G key: Handle both quick press and long-press selection mode
            if (Input.GetKey(throwKey))
            {
                // G key is held down
                gKeyHoldTime += Time.deltaTime;
                
                // Check if we should enter selection mode
                if (!isInSelectionMode && gKeyHoldTime >= G_KEY_LONG_PRESS_TIME)
                {
                    // Enter selection mode
                    isInSelectionMode = true;
                    Debug.Log("[BTS] =========================================");
                    Debug.Log("[BTS] ========== ENTERING SELECTION MODE ==========");
                    Debug.Log("[BTS] =========================================");
                    
                    // Initialize selection index (start with first throwable category)
                    var throwablesList = GetAllThrowablesByCategory();
                    if (throwablesList.Count > 0)
                    {
                        selectionModeCurrentIndex = 0;
                        ShowSelectionModeBubble(throwablesList[selectionModeCurrentIndex]);
                    }
                    else
                    {
                        ShowDebugBubble("❌ 背包中没有投掷物");
                        isInSelectionMode = false;
                        gKeyHoldTime = 0f;
                    }
                }
                
                // If in selection mode, handle mouse scroll wheel
                if (isInSelectionMode)
                {
                    HandleSelectionModeScroll();
                }
            }
            else if (Input.GetKeyUp(throwKey))
            {
                // G key was released
                if (isInSelectionMode)
                {
                    // Exit selection mode and equip selected item
                    ExitSelectionModeAndEquip();
                }
                else if (gKeyHoldTime < G_KEY_LONG_PRESS_TIME && gKeyHoldTime > 0f)
                {
                    // Quick press - use normal cycle logic
                    Debug.Log("[BTS] =========================================");
                    Debug.Log("[BTS] ========== G KEY PRESSED (QUICK) ==========");
                    Debug.Log("[BTS] =========================================");
                    
                    var playerForGKey = FindPlayerCharacter();
                    if (playerForGKey == null)
                    {
                        Debug.LogError("[BTS] ❌ CRITICAL: Player not found! Cannot proceed.");
                        ShowDebugBubble("❌ 错误：找不到玩家角色");
                        gKeyHoldTime = 0f;
                        return;
                    }
                    
                    Debug.Log($"[BTS] Player found: {playerForGKey.gameObject.name}");
                    
                    // IMPORTANT: Save current weapon BEFORE switching to throwable
                    // This must happen BEFORE CycleToNextThrowable, which might change the current item
                    SaveCurrentEquippedSlot(playerForGKey);
                    
                    CycleToNextThrowable();
                    
                    // Mark that last action was G key (for detecting continuous G presses)
                    lastActionWasGKey = true;
                    lastActionWasWeaponSwitch = false;
                }
                
                // Reset hold time
                gKeyHoldTime = 0f;
            }
            else
            {
                // G key not pressed - reset hold time
                if (!isInSelectionMode)
                {
                    gKeyHoldTime = 0f;
                }
            }
            
            // Detect weapon switching (1/2/V keys) - this breaks "continuous G" sequence
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.V))
            {
                lastActionWasWeaponSwitch = true;
                lastActionWasGKey = false;
                
                // Exit selection mode if active
                if (isInSelectionMode)
                {
                    isInSelectionMode = false;
                    gKeyHoldTime = 0f;
                    Debug.Log("[BTS] Selection mode cancelled due to weapon switch");
                }
            }
            
            // Monitor throw completion through multiple methods:
            // 1. Item count change (primary - most reliable) - handled in MonitorThrowableItems
            // 2. Empty hand state after holding throwable (secondary)
            // 3. Timeout fallback (if throw takes too long)
            var player = FindPlayerCharacter();
            
            // Only monitor if we have a valid player and are tracking a throwable
            if (player == null)
            {
                // Player not found - reset tracking
                wasHoldingThrowable = false;
                isThrowingInProgress = false;
                return;
            }
            
            if (lastEquippedThrowableSlot.HasValue)
            {
                var currentItem = GetCurrentHoldItem(player);
                bool isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
                bool isEmptyHand = currentItem == null;
                
                // Track when we start holding throwable
                if (isHoldingThrowable && !wasHoldingThrowable)
                {
                    Debug.Log($"[BTS] 📌 Started holding throwable: {currentItem?.name ?? "null"} (Slot: {lastEquippedThrowableSlot.Value})");
                    Debug.Log($"[BTS] Previous weapon info - Slot: {previousEquippedSlot}, Key: {previousEquippedKey}");
                }
                
                // Mouse left button release detection (for throw completion)
                bool isMouseButton0Down = Input.GetMouseButton(0); // Left mouse button
                bool isMouseButton1Down = Input.GetMouseButton(1); // Right mouse button
                
                // Detect mouse left button release while holding throwable (and not right-clicking)
                if (wasMouseButton0Down && !isMouseButton0Down && isHoldingThrowable && !isMouseButton1Down && !hasCompletedThrow)
                {
                    Debug.Log("[BTS] ⚡⚡⚡ THROW COMPLETED (Mouse left button released)! Detected mouse release while holding throwable.");
                    OnThrowCompleted();
                }
                
                wasMouseButton0Down = isMouseButton0Down;
                
                // Secondary detection: empty hand after holding throwable (backup to count detection)
                if (wasHoldingThrowable && isEmptyHand && !hasCompletedThrow)
                {
                    Debug.Log("[BTS] ⚡⚡⚡ THROW COMPLETED (Empty hand detection)! Transition from throwable to empty hand.");
                    OnThrowCompleted();
                }
                
                wasHoldingThrowable = isHoldingThrowable;
                
                // Check throw timeout (fallback - if throw takes too long, assume it's done)
                if (isThrowingInProgress && Time.time - throwStartTime > MAX_THROW_DURATION)
                {
                    Debug.Log("[BTS] ⏱️ Throw timeout reached, assuming throw completed");
                    OnThrowCompleted();
                    isThrowingInProgress = false;
                }
            }
            else
            {
                // Not tracking throwable anymore - reset tracking states
                wasHoldingThrowable = false;
                isThrowingInProgress = false;
                // Reset mouse button tracking when not holding throwable
                wasMouseButton0Down = Input.GetMouseButton(0);
            }
            
            // Monitor throwable items to detect throw completion (backup detection via item count)
            MonitorThrowableItems();
        }
        

        /// <summary>
        /// Print all items in the scene to verify we can access ItemStatsSystem.Item
        /// </summary>
        private void DumpPlayerItems()
        {
            var allItems = FindObjectsOfType<Item>();
            Debug.Log($"[BTS] Scene has {allItems.Length} Item(s).");

            foreach (var it in allItems.Take(20))
            {
                Debug.Log($"[BTS] Item: {it.name} / typeId? {it.TypeID}");
            }
        }

        /// <summary>
        /// Scan player inventory slots (ALL slots) for throwable items and group by TypeID (category)
        /// </summary>
        private void ScanPlayerInventoryForThrowables()
        {
            throwableSlotsByTypeID.Clear();
            throwableTypeIDsInOrder.Clear();
            lastItemCounts.Clear();
            
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogWarning("[BTS] Player not found, cannot scan inventory slots.");
                    return;
                }
                
                // Get Inventory component
                var inventory = player.GetComponent<Inventory>();
                if (inventory == null)
                {
                    inventory = player.GetComponentInChildren<Inventory>();
                }
                
                if (inventory == null)
                {
                    Debug.LogWarning("[BTS] Inventory component not found on player!");
                    return;
                }
                
                var inventoryType = inventory.GetType();
                
                // Try to get GetItem method
                var getItemMethod = inventoryType.GetMethod(
                    "GetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetItemAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetSlotItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                if (getItemMethod == null)
                {
                    Debug.LogError("[BTS] Could not find method to get item from inventory slot!");
                    return;
                }
                
                // Try to get max slots
                var maxSlotsProp = inventoryType.GetProperty("maxSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var slotCountProp = inventoryType.GetProperty("SlotCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var capacityProp = inventoryType.GetProperty("Capacity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var sizeProp = inventoryType.GetProperty("Size", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                int maxSlots = 47; // Default
                if (maxSlotsProp != null)
                {
                    var value = maxSlotsProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                else if (slotCountProp != null)
                {
                    var value = slotCountProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                else if (capacityProp != null)
                {
                    var value = capacityProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                else if (sizeProp != null)
                {
                    var value = sizeProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                
                Debug.Log($"[BTS] Scanning ALL inventory slots 0-{maxSlots - 1} for throwables (grouped by TypeID)...");
                
                // Scan ALL slots and group by TypeID
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    try
                    {
                        var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                        if (item != null && IsThrowableItem(item))
                        {
                            int typeID = item.TypeID;
                            
                            // Group by TypeID
                            if (!throwableSlotsByTypeID.ContainsKey(typeID))
                            {
                                throwableSlotsByTypeID[typeID] = new List<int>();
                                throwableTypeIDsInOrder.Add(typeID);
                            }
                            throwableSlotsByTypeID[typeID].Add(slotIndex);
                            
                            // Store item count for throw detection
                            lastItemCounts[slotIndex] = GetItemCount(item);
                            
                            Debug.Log($"[BTS] Found throwable in slot {slotIndex}: {item.name} (TypeID: {typeID})");
                        }
                    }
                    catch (System.Exception)
                    {
                        // Skip invalid slots
                    }
                }
                
                // Sort slots within each category
                foreach (var typeID in throwableSlotsByTypeID.Keys.ToList())
                {
                    throwableSlotsByTypeID[typeID].Sort();
                }
                
                int totalCount = throwableSlotsByTypeID.Values.Sum(list => list.Count);
                Debug.Log($"[BTS] Scanned inventory: Found {totalCount} throwable item(s) in {throwableTypeIDsInOrder.Count} category/categories: [{string.Join(", ", throwableTypeIDsInOrder.Select(id => $"TypeID {id}({throwableSlotsByTypeID[id].Count} slots)"))}]");
                
                // Reset category index if current category is no longer available
                if (currentCategoryIndex >= throwableTypeIDsInOrder.Count)
                {
                    currentCategoryIndex = -1;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning inventory slots: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Get all throwables grouped by TypeID (category) for selection mode
        /// Each category appears only once, using the first slot found for that category
        /// </summary>
        private List<(int slot, int typeID, string name, Sprite icon)> GetAllThrowablesByCategory()
        {
            var result = new List<(int slot, int typeID, string name, Sprite icon)>();
            var seenTypeIDs = new HashSet<int>();
            
            try
            {
                var player = FindPlayerCharacter();
                if (player == null) return result;
                
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                if (inventory == null) return result;
                
                var inventoryType = inventory.GetType();
                var getItemMethod = inventoryType.GetMethod(
                    "GetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetItemAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                if (getItemMethod == null) return result;
                
                // First pass: collect all throwables by TypeID
                var throwablesByTypeID = new Dictionary<int, (int slot, string name, Sprite? icon)>();
                
                // Scan all slots (0-46) and group by TypeID
                for (int slotIndex = 0; slotIndex < 47; slotIndex++)
                {
                    try
                    {
                        var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                        if (item != null && IsThrowableItem(item))
                        {
                            int typeID = item.TypeID;
                            
                            // Only keep the first slot for each TypeID
                            if (!throwablesByTypeID.ContainsKey(typeID))
                            {
                                string itemName = item.name.Replace("(Clone)", "").Trim();
                                Sprite? icon = null;
                                
                                // Try to get icon
                                try
                                {
                                    var itemType = item.GetType();
                                    var iconProp = itemType.GetProperty("Icon", 
                                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                    if (iconProp != null)
                                    {
                                        icon = iconProp.GetValue(item) as Sprite;
                                    }
                                }
                                catch { }
                                
                                throwablesByTypeID[typeID] = (slotIndex, itemName, icon);
                            }
                        }
                    }
                    catch { }
                }
                
                // Convert to list, sorted by TypeID order (if available) or slot index
                if (throwableTypeIDsInOrder.Count > 0)
                {
                    // Use the category order from throwableTypeIDsInOrder
                    foreach (var typeID in throwableTypeIDsInOrder)
                    {
                        if (throwablesByTypeID.ContainsKey(typeID))
                        {
                            var data = throwablesByTypeID[typeID];
                            result.Add((data.slot, typeID, data.name, data.icon));
                        }
                    }
                    
                    // Add any remaining TypeIDs not in throwableTypeIDsInOrder
                    foreach (var kvp in throwablesByTypeID)
                    {
                        if (!throwableTypeIDsInOrder.Contains(kvp.Key))
                        {
                            var data = kvp.Value;
                            result.Add((data.slot, kvp.Key, data.name, data.icon));
                        }
                    }
                }
                else
                {
                    // No predefined order, just add all
                    foreach (var kvp in throwablesByTypeID.OrderBy(x => x.Value.slot))
                    {
                        var data = kvp.Value;
                        result.Add((data.slot, kvp.Key, data.name, data.icon));
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error getting throwables by category: {e.Message}");
            }
            
            Debug.Log($"[BTS] Selection mode: Found {result.Count} throwable categories");
            return result;
        }
        
        /// <summary>
        /// Handle mouse scroll wheel in selection mode (switches by category, not by slot)
        /// </summary>
        private void HandleSelectionModeScroll()
        {
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                var throwablesList = GetAllThrowablesByCategory();
                if (throwablesList.Count == 0) return;
                
                if (scrollDelta > 0f)
                {
                    // Scroll up - move to previous category (wrap to end)
                    selectionModeCurrentIndex--;
                    if (selectionModeCurrentIndex < 0)
                    {
                        selectionModeCurrentIndex = throwablesList.Count - 1;
                    }
                    var selected = throwablesList[selectionModeCurrentIndex];
                    Debug.Log($"[BTS] Selection mode: Scrolled UP to category '{selected.name}' (TypeID: {selected.typeID}, Slot: {selected.slot})");
                }
                else
                {
                    // Scroll down - move to next category (wrap to start)
                    selectionModeCurrentIndex++;
                    if (selectionModeCurrentIndex >= throwablesList.Count)
                    {
                        selectionModeCurrentIndex = 0;
                    }
                    var selected = throwablesList[selectionModeCurrentIndex];
                    Debug.Log($"[BTS] Selection mode: Scrolled DOWN to category '{selected.name}' (TypeID: {selected.typeID}, Slot: {selected.slot})");
                }
                
                // Show updated bubble
                ShowSelectionModeBubble(throwablesList[selectionModeCurrentIndex]);
            }
        }
        
        /// <summary>
        /// Show selection mode bubble with current throwable name and icon
        /// </summary>
        private void ShowSelectionModeBubble((int slot, int typeID, string name, Sprite? icon) throwable)
        {
            try
            {
                var player = FindPlayerCharacter();
                Transform? target = player?.transform ?? Camera.main?.transform;
                
                if (target == null) return;
                
                // Format bubble text with icon indicator (Unicode icon if available)
                string iconIndicator = throwable.icon != null ? "🎯" : "💣";
                string bubbleText = $"投掷物选择中，{iconIndicator} {throwable.name}";
                
                Debug.Log($"[BTS] Showing selection bubble: {bubbleText} (Icon: {(throwable.icon != null ? "Available" : "None")})");
                
                // Show bubble using reflection
                var showMethod = typeof(DialogueBubblesManager).GetMethod(
                    "Show",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                
                if (showMethod != null)
                {
                    // Call Show with parameters (shorter duration for selection mode, updates frequently)
                    var result = showMethod.Invoke(
                        null,
                        new object[] { bubbleText, target, 1f, false, false, -1f, 0.5f } // 0.5s duration
                    );
                    
                    // Try to call Forget() on the result if it has that method
                    if (result != null)
                    {
                        var forgetMethod = result.GetType().GetMethod("Forget");
                        if (forgetMethod != null)
                        {
                            forgetMethod.Invoke(result, null);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error showing selection bubble: {e.Message}");
            }
        }
        
        /// <summary>
        /// Exit selection mode and equip the selected throwable category
        /// </summary>
        private void ExitSelectionModeAndEquip()
        {
            if (!isInSelectionMode) return;
            
            try
            {
                var throwablesList = GetAllThrowablesByCategory();
                if (throwablesList.Count == 0 || selectionModeCurrentIndex < 0 || selectionModeCurrentIndex >= throwablesList.Count)
                {
                    Debug.LogWarning("[BTS] Invalid selection index, cannot equip");
                    isInSelectionMode = false;
                    gKeyHoldTime = 0f;
                    return;
                }
                
                var selected = throwablesList[selectionModeCurrentIndex];
                Debug.Log($"[BTS] =========================================");
                Debug.Log($"[BTS] ========== EXITING SELECTION MODE ==========");
                Debug.Log($"[BTS] Selected category: {selected.name} (Slot {selected.slot}, TypeID {selected.typeID})");
                Debug.Log("[BTS] =========================================");
                
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogError("[BTS] Player not found, cannot equip selected throwable");
                    isInSelectionMode = false;
                    gKeyHoldTime = 0f;
                    return;
                }
                
                // Save current weapon before switching
                SaveCurrentEquippedSlot(player);
                
                // Equip the selected throwable (first slot of the category)
                if (SwitchToSlot(selected.slot))
                {
                    lastEquippedThrowableSlot = selected.slot;
                    lastSelectedThrowableSlot = selected.slot;
                    lastSelectedThrowableTypeID = selected.typeID;
                    
                    // Update category index
                    if (throwableTypeIDsInOrder.Contains(selected.typeID))
                    {
                        currentCategoryIndex = throwableTypeIDsInOrder.IndexOf(selected.typeID);
                    }
                    
                    // Show confirmation bubble with icon indicator
                    string iconIndicator = selected.icon != null ? "🎯" : "💣";
                    ShowDebugBubble($"✓ 已选择：{iconIndicator} {selected.name}");
                    
                    Debug.Log($"[BTS] ✓ Successfully equipped selected throwable category: {selected.name}");
                }
                else
                {
                    ShowDebugBubble($"❌ 无法装备：{selected.name}");
                    Debug.LogError($"[BTS] Failed to equip selected throwable: {selected.name}");
                }
                
                // Exit selection mode
                isInSelectionMode = false;
                gKeyHoldTime = 0f;
                lastActionWasGKey = true;
                lastActionWasWeaponSwitch = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error exiting selection mode: {e.Message}\n{e.StackTrace}");
                isInSelectionMode = false;
                gKeyHoldTime = 0f;
            }
        }
        
        /// <summary>
        /// Get item count/stack size (for detecting throw completion)
        /// </summary>
        private int GetItemCount(Item item)
        {
            if (item == null) return 0;
            
            try
            {
                var itemType = item.GetType();
                
                // Try to get Count or StackSize property
                var countProp = itemType.GetProperty("Count", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?? itemType.GetProperty("StackSize", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?? itemType.GetProperty("Amount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (countProp != null)
                {
                    var value = countProp.GetValue(item);
                    if (value is int) return (int)value;
                    if (value != null && int.TryParse(value.ToString(), out int parsed)) return parsed;
                }
            }
            catch { }
            
            // Default: item exists = count 1
            return 1;
        }
        
        /// <summary>
        /// Debug: Scan all inventory slots and print their contents (F9 key)
        /// </summary>
        private void ScanInventorySlots()
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogWarning("[BTS] Player not found!");
                    return;
                }
                
                Debug.Log("[BTS] ========== Scanning Inventory Slots ==========");
                
                // Get Inventory component
                var inventory = player.GetComponent<Inventory>();
                if (inventory == null)
                {
                    inventory = player.GetComponentInChildren<Inventory>();
                }
                
                if (inventory == null)
                {
                    Debug.LogError("[BTS] Inventory component not found!");
                    return;
                }
                
                var inventoryType = inventory.GetType();
                Debug.Log($"[BTS] Inventory type: {inventoryType.Name}");
                
                // Find GetItem method
                var getItemMethod = inventoryType.GetMethod(
                    "GetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetItemAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetSlotItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                // Try to get max slots or slot count
                var maxSlotsProp = inventoryType.GetProperty("maxSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var slotCountProp = inventoryType.GetProperty("SlotCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                int maxSlots = 10; // Default
                if (maxSlotsProp != null)
                {
                    var value = maxSlotsProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                else if (slotCountProp != null)
                {
                    var value = slotCountProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                
                Debug.Log($"[BTS] Scanning slots 0-{maxSlots - 1} (focus on 3-9 for throwables)...");
                
                // Scan all slots (focus on 3-9)
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    if (getItemMethod != null)
                    {
                        try
                        {
                            var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                            if (item != null)
                            {
                                bool isThrowable = IsThrowableItem(item);
                                string marker = isThrowable ? "⭐ THROWABLE" : "";
                                string focus = (slotIndex >= 3 && slotIndex <= 9) ? ">>>" : "";
                                Debug.Log($"[BTS] {focus} Slot {slotIndex}: {item.name} (TypeID: {item.TypeID}) {marker}");
                            }
                            else if (slotIndex >= 3 && slotIndex <= 9)
                            {
                                Debug.Log($"[BTS] >>> Slot {slotIndex}: (empty)");
                            }
                        }
                        catch (System.Exception e)
                        {
                            if (slotIndex >= 3 && slotIndex <= 9)
                            {
                                Debug.LogWarning($"[BTS] Slot {slotIndex}: Error - {e.Message}");
                            }
                        }
                    }
                }
                
                // Also scan for slot switching methods
                Debug.Log("[BTS] ========== Scanning for Slot Switching Methods ==========");
                
                // Check CharacterMainControl
                var playerType = player.GetType();
                var playerMethods = playerType.GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                foreach (var method in playerMethods)
                {
                    var methodName = method.Name;
                    if ((methodName.Contains("Slot", StringComparison.OrdinalIgnoreCase) ||
                         methodName.Contains("Use", StringComparison.OrdinalIgnoreCase) ||
                         methodName.Contains("Equip", StringComparison.OrdinalIgnoreCase) ||
                         methodName.Contains("Select", StringComparison.OrdinalIgnoreCase)) &&
                        method.GetParameters().Length == 1 &&
                        method.GetParameters()[0].ParameterType == typeof(int))
                    {
                        var paramList = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Debug.Log($"[BTS] ⭐ PLAYER METHOD: {methodName}({paramList})");
                    }
                }
                
                // Check Inventory
                var invMethods = inventoryType.GetMethods(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                foreach (var method in invMethods)
                {
                    var methodName = method.Name;
                    if ((methodName.Contains("Slot", StringComparison.OrdinalIgnoreCase) ||
                         methodName.Contains("Use", StringComparison.OrdinalIgnoreCase) ||
                         methodName.Contains("Equip", StringComparison.OrdinalIgnoreCase) ||
                         methodName.Contains("Select", StringComparison.OrdinalIgnoreCase)) &&
                        method.GetParameters().Length == 1 &&
                        method.GetParameters()[0].ParameterType == typeof(int))
                    {
                        var paramList = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        Debug.Log($"[BTS] ⭐ INVENTORY METHOD: {methodName}({paramList})");
                    }
                }
                
                Debug.Log("[BTS] ========== Slot Scan Completed ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning inventory slots: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Debug: Scan for methods that handle number key input (F8 key)
        /// This helps find the correct method to simulate pressing number keys
        /// </summary>
        private void ScanNumberKeyHandlers()
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogWarning("[BTS] Player not found!");
                    return;
                }
                
                Debug.Log("[BTS] ========== Scanning for Number Key Input Handlers ==========");
                
                var playerType = player.GetType();
                var allMethods = playerType.GetMethods(
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Static
                );
                
                Debug.Log($"[BTS] Scanning {allMethods.Length} methods on {playerType.Name}...");
                
                // Look for methods that might handle number key input
                var keywords = new[] { "Key", "Input", "Number", "Slot", "Alpha", "Digit", "Update" };
                
                foreach (var method in allMethods)
                {
                    var methodName = method.Name;
                    bool matches = false;
                    
                    foreach (var keyword in keywords)
                    {
                        if (methodName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            matches = true;
                            break;
                        }
                    }
                    
                    if (matches)
                    {
                        var parameters = method.GetParameters();
                        var paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                        var returnType = method.ReturnType.Name;
                        Debug.Log($"[BTS] ⭐ METHOD: {returnType} {methodName}({paramList})");
                    }
                }
                
                // Also check all components
                Debug.Log("[BTS] ========== Scanning Player Components ==========");
                var components = player.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null || component == player) continue;
                    
                    var compType = component.GetType();
                    var compMethods = compType.GetMethods(
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic
                    );
                    
                    foreach (var method in compMethods)
                    {
                        var methodName = method.Name;
                        bool matches = false;
                        
                        foreach (var keyword in keywords)
                        {
                            if (methodName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                            {
                                matches = true;
                                break;
                            }
                        }
                        
                        if (matches)
                        {
                            var parameters = method.GetParameters();
                            var paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                            Debug.Log($"[BTS] ⭐ COMPONENT [{compType.Name}]: {methodName}({paramList})");
                        }
                    }
                }
                
                Debug.Log("[BTS] ========== Number Key Handler Scan Completed ==========");
                Debug.Log("[BTS] NOTE: Please press a number key (3-9) now and check the game's own log messages");
                Debug.Log("[BTS]       to see which methods get called when you press a number key.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning number key handlers: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Check if player is in a safe state to switch items
        /// </summary>
        private bool IsPlayerSafeToSwitch(CharacterMainControl player)
        {
            if (player == null || player.gameObject == null)
            {
                Debug.LogWarning("[BTS] Player is null - not safe to switch");
                return false;
            }
            
            try
            {
                var playerType = player.GetType();
                
                // Check if player is alive (has health/isDead property)
                var isDeadProp = playerType.GetProperty("IsDead", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (isDeadProp != null)
                {
                    var isDead = isDeadProp.GetValue(player);
                    if (isDead is bool && (bool)isDead)
                    {
                        Debug.Log("[BTS] Player is dead - cannot switch items");
                        return false;
                    }
                }
                
                // Check if player is in UI (has UI-related properties)
                // This is a placeholder - actual property names may vary
                var inUIMethod = playerType.GetMethod("IsInUI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (inUIMethod != null)
                {
                    var inUI = inUIMethod.Invoke(player, null);
                    if (inUI is bool && (bool)inUI)
                    {
                        Debug.Log("[BTS] Player is in UI - cannot switch items");
                        return false;
                    }
                }
                
                // Additional safety: check if player GameObject is active
                if (!player.gameObject.activeInHierarchy)
                {
                    Debug.Log("[BTS] Player GameObject is inactive - not safe to switch");
                    return false;
                }
                
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BTS] Error checking player safety: {e.Message}");
                // If we can't check, allow switching (better than blocking)
                return true;
            }
        }
        
        /// <summary>
        /// Switch to a specific inventory slot by simulating number key press
        /// This method tries multiple approaches to ensure it works correctly
        /// </summary>
        private bool SwitchToSlot(int slotNumber)
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogError("[BTS] Player not found!");
                    return false;
                }
                
                Debug.Log($"[BTS] Attempting to switch to slot {slotNumber}...");
                
                var playerType = player.GetType();
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                
                // Method 0: Try to simulate pressing the number key directly
                // This is the most reliable - it mimics what happens when player presses 3, 4, etc.
                // Convert slot number to KeyCode (3 -> Alpha3, 4 -> Alpha4, etc.)
                if (slotNumber >= 1 && slotNumber <= 9)
                {
                    KeyCode targetKey = KeyCode.Alpha1 + (slotNumber - 1); // Alpha1 for 1, Alpha3 for 3, etc.
                    Debug.Log($"[BTS] Method 0: Simulating number key press for slot {slotNumber} (KeyCode: {targetKey})...");
                    
                    // Try to find Update method or input handling logic that checks for number keys
                    // Since we can't directly inject Input events, we need to find what Update() does when number key is pressed
                    // The game likely has code like: if (Input.GetKeyDown(KeyCode.Alpha3)) { SwitchToSlot(3); }
                    // We'll try to find that SwitchToSlot or similar method
                    
                    var allMethods = playerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                    // Look for methods that might be called when number key is pressed
                    // Common patterns: OnSlotPressed, HandleSlotKey, ProcessSlotInput
                    string[] slotKeyMethods = {
                        "OnSlotKeyPressed",
                        "HandleSlotKey",
                        "ProcessSlotInput",
                        "OnNumberKeyDown",
                        "HandleNumberKey",
                        "SwitchSlot",
                        "SelectSlot"
                    };
                    
                    foreach (var methodName in slotKeyMethods)
                    {
                        foreach (var method in allMethods)
                        {
                            if (method.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                            {
                                var parameters = method.GetParameters();
                                // Try different parameter combinations
                                if (parameters.Length == 1)
                                {
                                    if (parameters[0].ParameterType == typeof(int))
                                    {
                                        try
                                        {
                                            Debug.Log($"[BTS] Calling {methodName}({slotNumber}) to simulate number key press");
                                            method.Invoke(player, new object[] { slotNumber });
                                            Debug.Log($"[BTS] ✓ Successfully called {methodName}({slotNumber})");
                                            return true;
                                        }
                                        catch (System.Exception e)
                                        {
                                            Debug.LogWarning($"[BTS] {methodName}(int) failed: {e.Message}");
                                        }
                                    }
                                    else if (parameters[0].ParameterType == typeof(KeyCode))
                                    {
                                        try
                                        {
                                            Debug.Log($"[BTS] Calling {methodName}({targetKey}) to simulate number key press");
                                            method.Invoke(player, new object[] { targetKey });
                                            Debug.Log($"[BTS] ✓ Successfully called {methodName}({targetKey})");
                                            return true;
                                        }
                                        catch (System.Exception e)
                                        {
                                            Debug.LogWarning($"[BTS] {methodName}(KeyCode) failed: {e.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Method 1: Try to find and call the input handler that processes number keys
                // This is the most reliable way - simulate what happens when player presses a number key
                // (inventory already declared above)
                
                // Try to find Update method or input handling methods that process number keys
                // Common pattern: OnSlotKeyPressed, HandleSlotInput, ProcessNumberKey, etc.
                string[] inputHandlerNames = {
                    "OnSlotKeyPressed",
                    "HandleSlotInput", 
                    "ProcessNumberKey",
                    "OnNumberKeyPressed",
                    "SwitchToSlotByKey",
                    "HandleInventoryKey"
                };
                
                foreach (var handlerName in inputHandlerNames)
                {
                    var handlerMethod = playerType.GetMethod(
                        handlerName,
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                    
                    if (handlerMethod != null)
                    {
                        var parameters = handlerMethod.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType == typeof(int))
                        {
                            try
                            {
                                Debug.Log($"[BTS] Found input handler: {handlerName}(int), calling with slot {slotNumber}");
                                handlerMethod.Invoke(player, new object[] { slotNumber });
                                Debug.Log($"[BTS] ✓ Called {handlerName}({slotNumber})");
                                return true;
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[BTS] {handlerName} failed: {e.Message}");
                            }
                        }
                        else if (parameters.Length == 0)
                        {
                            try
                            {
                                Debug.Log($"[BTS] Found input handler: {handlerName}(), calling");
                                handlerMethod.Invoke(player, null);
                                Debug.Log($"[BTS] ✓ Called {handlerName}()");
                                return true;
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[BTS] {handlerName}() failed: {e.Message}");
                            }
                        }
                    }
                }
                
                // Method 2: Try to get Slot hash from Inventory slots array
                int? slotHash = null;
                if (inventory != null)
                {
                    try
                    {
                        var inventoryType = inventory.GetType();
                        Debug.Log($"[BTS] Trying to get slot hash for slot {slotNumber} from Inventory ({inventoryType.Name})...");
                        
                        // Try multiple ways to access slots
                        // Method 2a: Try GetSlot method on Inventory
                        var inventoryGetSlotMethod = inventoryType.GetMethod(
                            "GetSlot",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                            null,
                            new System.Type[] { typeof(int) },
                            null
                        );
                        
                        if (inventoryGetSlotMethod != null)
                        {
                            try
                            {
                                Debug.Log($"[BTS] Trying Inventory.GetSlot({slotNumber})...");
                                var slotObj = inventoryGetSlotMethod.Invoke(inventory, new object[] { slotNumber });
                                if (slotObj != null)
                                {
                                    var slotType = slotObj.GetType();
                                    Debug.Log($"[BTS] Got Slot object from Inventory.GetSlot: {slotType.Name}");
                                    
                                    // Extract hash from Slot object
                                    var hashNames = new[] { "hash", "Hash", "slotHash", "SlotHash", "id", "Id", "slotId", "SlotId", "_hash", "_id" };
                                    foreach (var hashName in hashNames)
                                    {
                                        var hashProperty = slotType.GetProperty(hashName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                        var hashField = slotType.GetField(hashName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                        
                                        if (hashProperty != null)
                                        {
                                            var hashValue = hashProperty.GetValue(slotObj);
                                            if (hashValue is int)
                                            {
                                                slotHash = (int)hashValue;
                                                Debug.Log($"[BTS] ✓ Got slot hash from property {hashName}: {slotHash.Value}");
                                                break;
                                            }
                                        }
                                        else if (hashField != null)
                                        {
                                            var hashValue = hashField.GetValue(slotObj);
                                            if (hashValue is int)
                                            {
                                                slotHash = (int)hashValue;
                                                Debug.Log($"[BTS] ✓ Got slot hash from field {hashName}: {slotHash.Value}");
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"[BTS] Inventory.GetSlot({slotNumber}) returned null");
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"[BTS] Error calling Inventory.GetSlot: {e.Message}");
                            }
                        }
                        
                        // Method 2b: Try to access slots array/list directly
                        if (!slotHash.HasValue)
                        {
                            var slotsProperty = inventoryType.GetProperty("slots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            var slotsField = inventoryType.GetField("slots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            var _slotsProperty = inventoryType.GetProperty("_slots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            var _slotsField = inventoryType.GetField("_slots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            
                            object? slots = null;
                            if (slotsProperty != null)
                            {
                                slots = slotsProperty.GetValue(inventory);
                                Debug.Log("[BTS] Found slots property");
                            }
                            else if (slotsField != null)
                            {
                                slots = slotsField.GetValue(inventory);
                                Debug.Log("[BTS] Found slots field");
                            }
                            else if (_slotsProperty != null)
                            {
                                slots = _slotsProperty.GetValue(inventory);
                                Debug.Log("[BTS] Found _slots property");
                            }
                            else if (_slotsField != null)
                            {
                                slots = _slotsField.GetValue(inventory);
                                Debug.Log("[BTS] Found _slots field");
                            }
                            
                            if (slots != null)
                            {
                                if (slots is System.Collections.IList slotsList)
                                {
                                    Debug.Log($"[BTS] Found slots list with {slotsList.Count} slots");
                                    
                                    if (slotNumber >= 0 && slotNumber < slotsList.Count)
                                    {
                                        var slotObj = slotsList[slotNumber];
                                        if (slotObj != null)
                                        {
                                            var slotType = slotObj.GetType();
                                            Debug.Log($"[BTS] Got Slot object from array index {slotNumber}: {slotType.Name}");
                                            
                                            // Try to get hash - check all possible property/field names
                                            var hashNames = new[] { "hash", "Hash", "slotHash", "SlotHash", "id", "Id", "slotId", "SlotId", "_hash", "_id" };
                                            foreach (var hashName in hashNames)
                                            {
                                                var hashProperty = slotType.GetProperty(hashName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                                var hashField = slotType.GetField(hashName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                                
                                                if (hashProperty != null)
                                                {
                                                    var hashValue = hashProperty.GetValue(slotObj);
                                                    if (hashValue is int)
                                                    {
                                                        slotHash = (int)hashValue;
                                                        Debug.Log($"[BTS] ✓ Got slot hash from property {hashName}: {slotHash.Value}");
                                                        break;
                                                    }
                                                }
                                                else if (hashField != null)
                                                {
                                                    var hashValue = hashField.GetValue(slotObj);
                                                    if (hashValue is int)
                                                    {
                                                        slotHash = (int)hashValue;
                                                        Debug.Log($"[BTS] ✓ Got slot hash from field {hashName}: {slotHash.Value}");
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Debug.LogWarning($"[BTS] Slot at index {slotNumber} is null");
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"[BTS] Slots is not an IList, type: {slots.GetType().Name}");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("[BTS] Could not find slots property/field on Inventory");
                            }
                        }
                        
                        // Method 2c: Try CharacterMainControl.GetSlot (might use different parameter)
                        if (!slotHash.HasValue)
                        {
                            var getSlotMethod = playerType.GetMethod(
                                "GetSlot",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                                null,
                                new System.Type[] { typeof(int) },
                                null
                            );
                            
                            if (getSlotMethod != null)
                            {
                                try
                                {
                                    Debug.Log($"[BTS] Trying CharacterMainControl.GetSlot({slotNumber})...");
                                    var slotObj = getSlotMethod.Invoke(player, new object[] { slotNumber });
                                    if (slotObj != null)
                                    {
                                        var slotType = slotObj.GetType();
                                        Debug.Log($"[BTS] Got Slot object from CharacterMainControl.GetSlot: {slotType.Name}");
                                        
                                        var hashNames = new[] { "hash", "Hash", "slotHash", "SlotHash", "id", "Id", "slotId", "SlotId", "_hash", "_id" };
                                        foreach (var hashName in hashNames)
                                        {
                                            var hashProperty = slotType.GetProperty(hashName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                            var hashField = slotType.GetField(hashName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                            
                                            if (hashProperty != null)
                                            {
                                                var hashValue = hashProperty.GetValue(slotObj);
                                                if (hashValue is int)
                                                {
                                                    slotHash = (int)hashValue;
                                                    Debug.Log($"[BTS] ✓ Got slot hash from CharacterMainControl.GetSlot: {slotHash.Value}");
                                                    break;
                                                }
                                            }
                                            else if (hashField != null)
                                            {
                                                var hashValue = hashField.GetValue(slotObj);
                                                if (hashValue is int)
                                                {
                                                    slotHash = (int)hashValue;
                                                    Debug.Log($"[BTS] ✓ Got slot hash from CharacterMainControl.GetSlot: {slotHash.Value}");
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[BTS] CharacterMainControl.GetSlot({slotNumber}) returned null");
                                    }
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogWarning($"[BTS] Error calling CharacterMainControl.GetSlot: {e.Message}");
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[BTS] Error accessing slots array: {e.Message}\n{e.StackTrace}");
                    }
                }
                else
                {
                    Debug.LogWarning("[BTS] Inventory component is null!");
                }
                
                // Method 3: Try SwitchHoldAgentInSlot with hash or slot number
                var switchMethod = playerType.GetMethod(
                    "SwitchHoldAgentInSlot",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                if (switchMethod != null && slotHash.HasValue)
                {
                    try
                    {
                        Debug.Log($"[BTS] Calling SwitchHoldAgentInSlot({slotHash.Value}) with hash");
                        switchMethod.Invoke(player, new object[] { slotHash.Value });
                        Debug.Log($"[BTS] ✓ Called SwitchHoldAgentInSlot({slotHash.Value})");
                        return true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[BTS] SwitchHoldAgentInSlot with hash failed: {e.Message}");
                    }
                }
                
                // Method 4: Directly equip Item from slot (like the old version but safer)
                // This is the method that worked before but had bugs - we'll make it safer
                if (inventory != null)
                {
                    try
                    {
                        Debug.Log($"[BTS] Method 4: Trying to directly equip Item from slot {slotNumber}...");
                        
                        // Get Item from slot (we know this works from ScanPlayerInventoryForThrowables)
                        var inventoryType = inventory.GetType();
                        var getItemMethod = inventoryType.GetMethod(
                            "GetItem",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        ) ?? inventoryType.GetMethod(
                            "GetItemAt",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        ) ?? inventoryType.GetMethod(
                            "GetSlotItem",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        );
                        
                        if (getItemMethod != null)
                        {
                            var item = getItemMethod.Invoke(inventory, new object[] { slotNumber }) as Item;
                            if (item != null)
                            {
                                Debug.Log($"[BTS] Got Item from slot: {item.name} (TypeID: {item.TypeID})");
                                
                                // Verify item is in player's inventory (not a Clone from scene)
                                var itemType = item.GetType();
                                
                                // Check if item is in inventory using reflection
                                var isInPlayerCharMethod = itemType.GetMethod("IsInPlayerCharacter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                bool isInInventory = false;
                                if (isInPlayerCharMethod != null)
                                {
                                    try
                                    {
                                        var result = isInPlayerCharMethod.Invoke(item, null);
                                        if (result is bool) isInInventory = (bool)result;
                                    }
                                    catch { }
                                }
                                
                                // NOTE: IsInPlayerCharacter check removed - it was blocking valid items from being equipped
                                // Even items from Inventory slots can return false, so we'll trust that GetItem() returns valid items
                                Debug.Log($"[BTS] Item check: name={item.name}, IsInPlayerCharacter check skipped (may be false positive)");
                                
                                // Safety check: ensure player is safe to switch
                                if (!IsPlayerSafeToSwitch(player))
                                {
                                    Debug.LogWarning("[BTS] Player is not in a safe state to equip items!");
                                    return false;
                                }
                                
                                // Try to find and call ChangeHoldItem or similar method
                                // This is what the old version did, but we'll do it more safely
                                string[] equipMethodNames = {
                                    "ChangeHoldItem",
                                    "EquipItem",
                                    "SetHoldItem",
                                    "SetEquippedItem",
                                    "HoldItem"
                                };
                                
                                foreach (var methodName in equipMethodNames)
                                {
                                    var equipMethod = playerType.GetMethod(
                                        methodName,
                                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                                        null,
                                        new System.Type[] { typeof(Item) },
                                        null
                                    );
                                    
                                    if (equipMethod != null)
                                    {
                                        try
                                        {
                                            Debug.Log($"[BTS] Calling {methodName}(Item) with item from slot {slotNumber}...");
                                            Debug.Log($"[BTS] Item details: name={item.name}, TypeID={item.TypeID}, GameObject={item.gameObject.name}");
                                            equipMethod.Invoke(player, new object[] { item });
                                            Debug.Log($"[BTS] ✓ Successfully called {methodName} - Item should now be in hand!");
                                            return true;
                                        }
                                        catch (System.Exception e)
                                        {
                                            Debug.LogWarning($"[BTS] {methodName} failed: {e.Message}\n{e.StackTrace}");
                                        }
                                    }
                                }
                                
                                Debug.LogWarning("[BTS] Could not find method to directly equip Item object");
                            }
                            else
                            {
                                Debug.LogWarning($"[BTS] Slot {slotNumber} is empty or GetItem returned null");
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[BTS] Error in Method 4 (direct equip): {e.Message}\n{e.StackTrace}");
                    }
                }
                
                Debug.LogError($"[BTS] Could not switch to slot {slotNumber} - all methods failed!");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error switching to slot: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Cycle to the next throwable slot and switch to it (with memory system and category-based switching)
        /// </summary>
        private void CycleToNextThrowable()
        {
            var player = FindPlayerCharacter();
            if (player == null)
            {
                Debug.LogError("[BTS] Player character not found!");
                return;
            }
            
            // NOTE: SaveCurrentEquippedSlot is now called BEFORE CycleToNextThrowable in Update()
            // This ensures we save the weapon BEFORE any item changes
            
            // Rescan inventory slots to get up-to-date list (grouped by TypeID)
            ScanPlayerInventoryForThrowables();
            
            if (throwableTypeIDsInOrder.Count == 0)
            {
                Debug.LogWarning("[BTS] No throwable items found in inventory slots!");
                ShowDebugBubble("❌ 背包中没有投掷物");
                return;
            }
            
            // Determine which throwable to select
            int targetSlot;
            int targetTypeID;
            string categoryInfo;
            
            // Logic:
            // 1. If throw was completed, move to next category
            // 2. If current equipped item is the last selected throwable and not thrown, move to next category (continuous G press)
            // 3. Otherwise, restore last selection or select first category
            
            // Check if currently equipped item is the last selected throwable
            bool isCurrentlyEquipped = lastEquippedThrowableSlot.HasValue && 
                                      lastSelectedThrowableSlot.HasValue &&
                                      lastEquippedThrowableSlot.Value == lastSelectedThrowableSlot.Value;
            
            // NEW LOGIC: Smart throwable selection based on completion and continuity
            // Rule 1: If throw completed AND continuous G key (last action was G), move to next category
            // Rule 2: If throw NOT completed, always restore last selection (even if continuous G)
            // Rule 3: If last action was weapon switch, restore last selection (user canceled throw)
            // Rule 4: If no last selection, start from first category
            
            bool isContinuousG = lastActionWasGKey && !lastActionWasWeaponSwitch;
            bool shouldSwitchCategory = hasCompletedThrow && isContinuousG;
            
            Debug.Log($"[BTS] Selection logic - hasCompletedThrow: {hasCompletedThrow}, isContinuousG: {isContinuousG}, shouldSwitchCategory: {shouldSwitchCategory}, lastActionWasWeaponSwitch: {lastActionWasWeaponSwitch}");
            
            if (shouldSwitchCategory)
            {
                // Rule 1: Throw completed + continuous G = move to next category
                currentCategoryIndex = (currentCategoryIndex + 1) % throwableTypeIDsInOrder.Count;
                targetTypeID = throwableTypeIDsInOrder[currentCategoryIndex];
                var slotsForCategory = throwableSlotsByTypeID[targetTypeID];
                targetSlot = slotsForCategory[0];
                
                // Update memory
                lastSelectedThrowableSlot = targetSlot;
                lastSelectedThrowableTypeID = targetTypeID;
                
                categoryInfo = $"[类别 {currentCategoryIndex + 1}/{throwableTypeIDsInOrder.Count}]";
                Debug.Log($"[BTS] ✓ Continuous G after throw completed, moving to next category: TypeID {targetTypeID}, slot {targetSlot}");
                
                // Reset throw completion flag
                hasCompletedThrow = false;
            }
            else if (isCurrentlyEquipped && lastSelectedThrowableTypeID.HasValue && isContinuousG)
            {
                // Currently equipped is last selected throwable, continuous G press, but throw NOT completed
                // This means user is pressing G continuously but hasn't thrown yet
                // Move to next category only if continuous G (user wants to cycle)
                int lastTypeID = lastSelectedThrowableTypeID.Value;
                int currentCatIndex = throwableTypeIDsInOrder.IndexOf(lastTypeID);
                if (currentCatIndex < 0) currentCatIndex = 0;
                
                currentCategoryIndex = (currentCatIndex + 1) % throwableTypeIDsInOrder.Count;
                targetTypeID = throwableTypeIDsInOrder[currentCategoryIndex];
                var slotsForCategory = throwableSlotsByTypeID[targetTypeID];
                targetSlot = slotsForCategory[0];
                
                lastSelectedThrowableSlot = targetSlot;
                lastSelectedThrowableTypeID = targetTypeID;
                
                categoryInfo = $"[类别 {currentCategoryIndex + 1}/{throwableTypeIDsInOrder.Count}]";
                Debug.Log($"[BTS] ✓ Continuous G press (no throw completed), moving to next category: TypeID {targetTypeID}, slot {targetSlot}");
            }
            else if (lastSelectedThrowableSlot.HasValue && lastSelectedThrowableTypeID.HasValue)
            {
                // Rule 2 & 3: Restore last selection (throw not completed OR weapon was switched)
                int lastTypeID = lastSelectedThrowableTypeID.Value;
                int lastSlot = lastSelectedThrowableSlot.Value;
                
                if (throwableSlotsByTypeID.ContainsKey(lastTypeID) && 
                    throwableSlotsByTypeID[lastTypeID].Contains(lastSlot))
                {
                    // Restore last selection (user canceled throw or switched weapon)
                    targetSlot = lastSlot;
                    targetTypeID = lastTypeID;
                    
                    // Find category index
                    currentCategoryIndex = throwableTypeIDsInOrder.IndexOf(targetTypeID);
                    if (currentCategoryIndex < 0) currentCategoryIndex = 0;
                    
                    string reason = lastActionWasWeaponSwitch ? "武器切换后恢复" : "未完成投掷，恢复上次选择";
                    categoryInfo = $"[{reason}]";
                    Debug.Log($"[BTS] ✓ {reason}: TypeID {targetTypeID}, slot {targetSlot}");
                }
                else
                {
                    // Last selection no longer exists, start from first category
                    currentCategoryIndex = 0;
                    targetTypeID = throwableTypeIDsInOrder[currentCategoryIndex];
                    var slotsForCategory = throwableSlotsByTypeID[targetTypeID];
                    targetSlot = slotsForCategory[0];
                    
                    lastSelectedThrowableSlot = targetSlot;
                    lastSelectedThrowableTypeID = targetTypeID;
                    
                    categoryInfo = $"[类别 {currentCategoryIndex + 1}/{throwableTypeIDsInOrder.Count}]";
                    Debug.Log($"[BTS] Last selection unavailable, starting from first category: TypeID {targetTypeID}, slot {targetSlot}");
                }
            }
            else
            {
                // First time, select first category
                currentCategoryIndex = 0;
                targetTypeID = throwableTypeIDsInOrder[0];
                var slotsForCategory = throwableSlotsByTypeID[targetTypeID];
                targetSlot = slotsForCategory[0];
                
                lastSelectedThrowableSlot = targetSlot;
                lastSelectedThrowableTypeID = targetTypeID;
                
                categoryInfo = $"[类别 1/{throwableTypeIDsInOrder.Count}]";
                Debug.Log($"[BTS] First selection: TypeID {targetTypeID}, slot {targetSlot}");
            }
            
            // Safety check: ensure player is in a safe state
            if (!IsPlayerSafeToSwitch(player))
            {
                Debug.LogWarning("[BTS] Player is not in a safe state to switch items - operation cancelled");
                ShowDebugBubble("⚠️ 当前状态无法切换");
                return;
            }
            
            // Get item name for bubble display
            string itemName = $"Slot {targetSlot}";
            try
            {
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                if (inventory != null)
                {
                    var inventoryType = inventory.GetType();
                    var getItemMethod = inventoryType.GetMethod(
                        "GetItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetItemAt",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                    
                    if (getItemMethod != null)
                    {
                        var item = getItemMethod.Invoke(inventory, new object[] { targetSlot }) as Item;
                        if (item != null)
                        {
                            itemName = item.name.Replace("(Clone)", "").Trim();
                        }
                    }
                }
            }
            catch { }
            
            // Switch to the slot (simulates pressing number key)
            if (SwitchToSlot(targetSlot))
            {
                lastEquippedThrowableSlot = targetSlot;
                
                // Show success bubble
                var target = player.transform ?? Camera.main?.transform;
                if (target != null)
                {
                    ShowDebugBubble($"💣 {categoryInfo} {itemName}");
                }
            }
            else
            {
                ShowDebugBubble("❌ 无法切换到槽位");
            }
        }
        
        /// <summary>
        /// Save current equipped slot (weapon) before switching to throwable
        /// NEW APPROACH: Use CurrentHoldItemAgent to get current item, then find its slot via characterItem.Slots
        /// Based on duckovAPI documentation: CurrentHoldItemAgent.Item and characterItem.Slots
        /// </summary>
        private void SaveCurrentEquippedSlot(CharacterMainControl player)
        {
            try
            {
                Debug.Log("[BTS] ========== SaveCurrentEquippedSlot CALLED ==========");
                
                // NEW METHOD: Try to get current item from CurrentHoldItemAgent (recommended by duckovAPI)
                Item? currentItem = null;
                
                try
                {
                    var playerType = player.GetType();
                    var currentHoldItemAgentProp = playerType.GetProperty("CurrentHoldItemAgent", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
                    if (currentHoldItemAgentProp != null)
                    {
                        var agent = currentHoldItemAgentProp.GetValue(player);
                        if (agent != null)
                        {
                            var agentType = agent.GetType();
                            var itemProp = agentType.GetProperty("Item", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            if (itemProp != null)
                            {
                                currentItem = itemProp.GetValue(agent) as Item;
                                Debug.Log($"[BTS] Got current item from CurrentHoldItemAgent.Item: {(currentItem != null ? currentItem.name : "null")}");
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"[BTS] Error getting item from CurrentHoldItemAgent: {e.Message}");
                }
                
                // Fallback: Try GetCurrentHoldItem if CurrentHoldItemAgent failed
                if (currentItem == null)
                {
                    currentItem = GetCurrentHoldItem(player);
                    Debug.Log($"[BTS] Got current item from GetCurrentHoldItem (fallback): {(currentItem != null ? currentItem.name : "null")}");
                }
                
                bool isCurrentlyHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
                Debug.Log($"[BTS] Current item: {(currentItem != null ? currentItem.name : "null")}, IsThrowable: {isCurrentlyHoldingThrowable}");
                
                // Skip if we're already holding a throwable AND we already have a saved weapon (don't overwrite)
                if (isCurrentlyHoldingThrowable && previousEquippedSlotHash.HasValue)
                {
                    Debug.Log("[BTS] Currently holding throwable and previous weapon already saved, not overwriting");
                    Debug.Log($"[BTS] Previous weapon (already saved): SlotHash {previousEquippedSlotHash}, SlotKey '{previousEquippedSlotKey}', Slot {previousEquippedSlot}, Key {previousEquippedKey}");
                    return;
                }
                
                // METHOD 1: If we have currentItem, find which slot it's in via characterItem.Slots
                // This is the most reliable method according to duckovAPI
                if (currentItem != null && !IsThrowableItem(currentItem))
                {
                    try
                    {
                        var playerType = player.GetType();
                        var characterItemProp = playerType.GetProperty("CharacterItem", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        
                        if (characterItemProp != null)
                        {
                            var characterItem = characterItemProp.GetValue(player) as Item;
                            if (characterItem != null)
                            {
                                var itemType = characterItem.GetType();
                                var slotsProp = itemType.GetProperty("Slots", 
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                
                                if (slotsProp != null)
                                {
                                    var slots = slotsProp.GetValue(characterItem);
                                    if (slots != null)
                                    {
                                        var slotsType = slots.GetType();
                                        // Try to get slot by enumerating through all slots
                                        var getEnumeratorMethod = slotsType.GetMethod("GetEnumerator", 
                                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                        
                                        if (getEnumeratorMethod != null)
                                        {
                                            var enumerator = getEnumeratorMethod.Invoke(slots, null);
                                            var moveNextMethod = enumerator.GetType().GetMethod("MoveNext");
                                            var currentProperty = enumerator.GetType().GetProperty("Current");
                                            
                                            if (moveNextMethod != null && currentProperty != null)
                                            {
                                                while ((bool)moveNextMethod.Invoke(enumerator, null))
                                                {
                                                    var slot = currentProperty.GetValue(enumerator);
                                                    if (slot != null)
                                                    {
                                                        var slotType = slot.GetType();
                                                        var contentProp = slotType.GetProperty("Content", 
                                                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                                        var keyProp = slotType.GetProperty("Key", 
                                                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                                        
                                                        if (contentProp != null && keyProp != null)
                                                        {
                                                            var slotContent = contentProp.GetValue(slot) as Item;
                                                            var slotKey = keyProp.GetValue(slot) as string;
                                                            
                                                            if (slotContent == currentItem && slotKey != null)
                                                            {
                                                                // Found the slot containing current item!
                                                                // Save the slot hash for equipment slot switching (CRITICAL: This is equipment slot, not inventory slot!)
                                                                int slotHash = slotKey.GetHashCode();
                                                                previousEquippedSlotHash = slotHash;
                                                                previousEquippedSlotKey = slotKey;
                                                                
                                                                // Also save slot number for compatibility (if it's "1" or "2")
                                                                if (int.TryParse(slotKey, out int slotNum) && slotNum >= 1 && slotNum <= 2)
                                                                {
                                                                    previousEquippedSlot = slotNum;
                                                                    previousEquippedKey = KeyCode.Alpha1 + (slotNum - 1);
                                                                    Debug.Log($"[BTS] ✓✓✓ Saved EQUIPMENT weapon slot: Key='{slotKey}', Hash={slotHash}, SlotNum={slotNum} ({currentItem.name})");
                                                                }
                                                                else if (slotKey == "V" || slotKey == "0")
                                                                {
                                                                    previousEquippedSlot = null;
                                                                    previousEquippedKey = KeyCode.V;
                                                                    Debug.Log($"[BTS] ✓✓✓ Saved EQUIPMENT weapon slot: Key='{slotKey}', Hash={slotHash} ({currentItem.name})");
                                                                }
                                                                else
                                                                {
                                                                    previousEquippedSlot = null;
                                                                    previousEquippedKey = null;
                                                                    Debug.Log($"[BTS] ✓✓✓ Saved EQUIPMENT weapon slot: Key='{slotKey}', Hash={slotHash} ({currentItem.name})");
                                                                }
                                                                return;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"[BTS] Error finding slot via CharacterItem.Slots: {e.Message}");
                    }
                }
                
                // METHOD 2: Check inventory slots 1 and 2 for weapons (even if HoldItem is null)
                // This handles the case where player has weapon in slot 1/2 but isn't currently holding it
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                if (inventory != null)
                {
                    var inventoryType = inventory.GetType();
                    var getItemMethod = inventoryType.GetMethod(
                        "GetItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetItemAt",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                    
                    if (getItemMethod != null)
                    {
                        Debug.Log("[BTS] Method 2: Checking inventory slots 1 and 2 for weapons...");
                        
                        // Check slots 1 and 2 for any non-throwable weapon
                        int[] weaponSlotsOnly = { 1, 2 };
                        foreach (int slotIndex in weaponSlotsOnly)
                        {
                            try
                            {
                                var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                                if (item != null && !IsThrowableItem(item))
                                {
                                    // Found a weapon in slot 1 or 2!
                                    previousEquippedSlot = slotIndex;
                                    previousEquippedKey = KeyCode.Alpha1 + (slotIndex - 1);
                                    Debug.Log($"[BTS] ✓✓✓ Saved weapon slot (Method 2, found in inventory): {slotIndex} ({item.name}) (Key: {previousEquippedKey})");
                                    return;
                                }
                            }
                            catch (System.Exception e)
                            {
                                Debug.Log($"[BTS] Error checking slot {slotIndex}: {e.Message}");
                            }
                        }
                        
                        Debug.Log("[BTS] Method 2: No weapon found in slots 1 or 2");
                    }
                }
                
                // Final fallback: If still nothing saved, assume slot 1 (most common weapon slot)
                if (!previousEquippedSlotHash.HasValue)
                {
                    Debug.Log("[BTS] ⚠ No weapon found in equipment slots. Will use equipment slot '1' as fallback when switching back.");
                    // Don't save here - let AutoSwitchBackToWeaponImmediately handle fallback
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error saving current equipped slot: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Get current hold item from player
        /// NEW: Try CurrentHoldItemAgent.Item first (recommended by duckovAPI), then fallback to direct properties
        /// </summary>
        private Item? GetCurrentHoldItem(CharacterMainControl player)
        {
            if (player == null) return null;
            
            try
            {
                // Method 1: Try CurrentHoldItemAgent.Item (recommended by duckovAPI)
                var playerType = player.GetType();
                var currentHoldItemAgentProp = playerType.GetProperty("CurrentHoldItemAgent", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (currentHoldItemAgentProp != null)
                {
                    var agent = currentHoldItemAgentProp.GetValue(player);
                    if (agent != null)
                    {
                        var agentType = agent.GetType();
                        var itemProp = agentType.GetProperty("Item", 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (itemProp != null)
                        {
                            var item = itemProp.GetValue(agent) as Item;
                            if (item != null)
                            {
                                return item;
                            }
                        }
                    }
                }
                
                // Method 2: Fallback to direct properties
                var holdItemProp = playerType.GetProperty("HoldItem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?? playerType.GetProperty("CurrentItem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?? playerType.GetProperty("EquippedItem", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                if (holdItemProp != null)
                {
                    return holdItemProp.GetValue(player) as Item;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BTS] Error getting current hold item: {e.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Monitor throwable items to detect throw completion (check item count changes)
        /// </summary>
        private void MonitorThrowableItems()
        {
            if (!lastEquippedThrowableSlot.HasValue) return; // No throwable equipped
            
            try
            {
                var player = FindPlayerCharacter();
                if (player == null) return;
                
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                if (inventory == null) return;
                
                var inventoryType = inventory.GetType();
                var getItemMethod = inventoryType.GetMethod(
                    "GetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetItemAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                if (getItemMethod == null) return;
                
                int monitoredSlot = lastEquippedThrowableSlot.Value;
                
                // Get current item from slot
                var item = getItemMethod.Invoke(inventory, new object[] { monitoredSlot }) as Item;
                
                // Check if item still exists and get current count
                int currentCount = 0;
                if (item != null && IsThrowableItem(item))
                {
                    currentCount = GetItemCount(item);
                }
                // If item is null, count is 0 (item was removed/thrown)
                
                // Check if count decreased (throw completed) - PRIMARY DETECTION METHOD
                if (lastItemCounts.ContainsKey(monitoredSlot))
                {
                    int lastCount = lastItemCounts[monitoredSlot];
                    if (currentCount < lastCount)
                    {
                        // Item count decreased - throw completed!
                        Debug.Log($"[BTS] ⚡⚡⚡ THROW COMPLETED (Count change)! Slot {monitoredSlot}: {lastCount} -> {currentCount}");
                        lastItemCounts[monitoredSlot] = currentCount;
                        OnThrowCompleted();
                    }
                    else if (currentCount != lastCount)
                    {
                        // Count changed but increased (item added/stacked)
                        lastItemCounts[monitoredSlot] = currentCount;
                    }
                }
                else if (item != null)
                {
                    // First time monitoring this slot - initialize count
                    lastItemCounts[monitoredSlot] = currentCount;
                    Debug.Log($"[BTS] 📊 Started monitoring throwable slot {monitoredSlot}, initial count: {currentCount}");
                    isThrowingInProgress = false; // Reset throw tracking
                }
                else
                {
                    // Item was removed (fully consumed)
                    if (lastItemCounts.ContainsKey(monitoredSlot) && lastItemCounts[monitoredSlot] > 0)
                    {
                        Debug.Log($"[BTS] ⚡⚡⚡ THROW COMPLETED (Item removed)! Slot {monitoredSlot} item completely consumed");
                        OnThrowCompleted();
                    }
                    lastItemCounts[monitoredSlot] = 0;
                }
                
                // Note: Throw detection is primarily done by count change above
                // The count decrease detection in the main check above will trigger OnThrowCompleted()
                
                // Check if player manually switched to a non-throwable item (clear monitoring)
                // BUT: Don't clear if we just completed a throw (isThrowingInProgress or hasCompletedThrow)
                // This prevents false detection when auto-switching back to weapon after throw
                if ((item == null || !IsThrowableItem(item)) && !hasCompletedThrow && !isThrowingInProgress)
                {
                    // Player manually switched away from throwable (not after throw completion)
                    if (lastEquippedThrowableSlot == monitoredSlot)
                    {
                        Debug.Log($"[BTS] Player manually switched away from throwable slot {monitoredSlot}, clearing monitoring");
                        lastEquippedThrowableSlot = null;
                    }
                }
            }
            catch (System.Exception e)
            {
                // Silently ignore errors in monitoring
                Debug.LogWarning($"[BTS] Error monitoring throwable items: {e.Message}");
            }
        }
        
        /// <summary>
        /// Auto-switch back to weapon immediately after throw
        /// UPDATED: Use SwitchHoldAgentInSlot with equipment slot hash (not inventory slot!)
        /// </summary>
        private void AutoSwitchBackToWeaponImmediately()
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogError("[BTS] ❌ Cannot switch back: Player not found");
                    return;
                }
                
                Debug.Log($"[BTS] ⚡⚡⚡ AutoSwitchBackToWeaponImmediately CALLED");
                Debug.Log($"[BTS] Previous weapon - SlotHash: {previousEquippedSlotHash}, SlotKey: '{previousEquippedSlotKey}', Slot: {previousEquippedSlot}, Key: {previousEquippedKey}");
                
                // Method 1 (PRIORITY): Use SwitchHoldAgentInSlot with equipment slot hash
                // This is the correct way to switch equipment slots, not inventory slots!
                if (previousEquippedSlotHash.HasValue)
                {
                    int slotHash = previousEquippedSlotHash.Value;
                    Debug.Log($"[BTS] ⚡ Switching back to EQUIPMENT slot using SwitchHoldAgentInSlot(slotHash={slotHash}, key='{previousEquippedSlotKey}')");
                    
                    try
                    {
                        var playerType = player.GetType();
                        var switchMethod = playerType.GetMethod("SwitchHoldAgentInSlot",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                            null,
                            new System.Type[] { typeof(int) },
                            null);
                        
                        if (switchMethod != null)
                        {
                            switchMethod.Invoke(player, new object[] { slotHash });
                            lastEquippedThrowableSlot = null;
                            Debug.Log($"[BTS] ✓✓✓ Successfully switched back to EQUIPMENT slot via SwitchHoldAgentInSlot (hash={slotHash}, key='{previousEquippedSlotKey}')");
                            return;
                        }
                        else
                        {
                            Debug.LogWarning("[BTS] SwitchHoldAgentInSlot method not found, falling back to key simulation");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[BTS] Error calling SwitchHoldAgentInSlot: {e.Message}");
                    }
                }
                
                // Method 2: Try to simulate key press (fallback)
                if (previousEquippedKey.HasValue)
                {
                    Debug.Log($"[BTS] ⚡ Attempting to simulate key press: {previousEquippedKey.Value}");
                    if (SimulateKeyPress(previousEquippedKey.Value))
                    {
                        lastEquippedThrowableSlot = null;
                        Debug.Log($"[BTS] ✓ Successfully switched back via key press: {previousEquippedKey.Value}");
                        return;
                    }
                }
                
                // Method 3: Try SwitchToSlot with slot number (last resort, might switch inventory instead of equipment)
                if (previousEquippedSlot.HasValue)
                {
                    if (!IsPlayerSafeToSwitch(player))
                    {
                        Debug.LogWarning("[BTS] Player not safe to switch, will retry in next frame");
                        StartCoroutine(RetryAutoSwitchBack());
                        return;
                    }
                    
                    int weaponSlot = previousEquippedSlot.Value;
                    Debug.Log($"[BTS] ⚠ Fallback: Auto-switching using SwitchToSlot (slot {weaponSlot}) - WARNING: This might switch inventory slot, not equipment slot!");
                    
                    if (SwitchToSlot(weaponSlot))
                    {
                        lastEquippedThrowableSlot = null;
                        Debug.Log($"[BTS] ✓ Successfully switched back to weapon slot {weaponSlot} (fallback)");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[BTS] Failed to switch back to weapon slot {weaponSlot}, will retry");
                        StartCoroutine(RetryAutoSwitchBack());
                    }
                }
                else
                {
                    // Final fallback: Try equipment slot hash for "1" and "2"
                    Debug.LogWarning("[BTS] ⚠ No previous weapon slot hash saved, trying equipment slots '1' and '2'...");
                    
                    // Try slot "1" hash
                    int slot1Hash = "1".GetHashCode();
                    try
                    {
                        var playerType = player.GetType();
                        var switchMethod = playerType.GetMethod("SwitchHoldAgentInSlot",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                            null,
                            new System.Type[] { typeof(int) },
                            null);
                        
                        if (switchMethod != null)
                        {
                            switchMethod.Invoke(player, new object[] { slot1Hash });
                            lastEquippedThrowableSlot = null;
                            Debug.Log("[BTS] ✓ Successfully switched back via fallback (equipment slot '1')");
                            return;
                        }
                    }
                    catch { }
                    
                    // Try slot "2" hash
                    int slot2Hash = "2".GetHashCode();
                    try
                    {
                        var playerType = player.GetType();
                        var switchMethod = playerType.GetMethod("SwitchHoldAgentInSlot",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                            null,
                            new System.Type[] { typeof(int) },
                            null);
                        
                        if (switchMethod != null)
                        {
                            switchMethod.Invoke(player, new object[] { slot2Hash });
                            lastEquippedThrowableSlot = null;
                            Debug.Log("[BTS] ✓ Successfully switched back via fallback (equipment slot '2')");
                            return;
                        }
                    }
                    catch { }
                    
                    Debug.LogError("[BTS] ❌ All methods failed to switch back to weapon");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error in AutoSwitchBackToWeaponImmediately: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Simulate key press by finding and calling the input handler method
        /// </summary>
        private bool SimulateKeyPress(KeyCode keyCode)
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null) return false;
                
                var playerType = player.GetType();
                
                // Try to find Update or input handling methods that check for this key
                // Common pattern: if (Input.GetKeyDown(KeyCode.Alpha1)) { SwitchToSlot(1); }
                // We need to find the method that handles this key and call it directly
                
                // Look for methods that might handle key input
                var allMethods = playerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                foreach (var method in allMethods)
                {
                    // Try methods that accept KeyCode or int (slot number)
                    var parameters = method.GetParameters();
                    if (parameters.Length == 1)
                    {
                        if (parameters[0].ParameterType == typeof(KeyCode))
                        {
                            try
                            {
                                Debug.Log($"[BTS] Trying to call {method.Name}({keyCode})...");
                                method.Invoke(player, new object[] { keyCode });
                                Debug.Log($"[BTS] ✓ Called {method.Name}({keyCode})");
                                return true;
                            }
                            catch { }
                        }
                        else if (parameters[0].ParameterType == typeof(int))
                        {
                            // Map KeyCode to slot number
                            int slotNum = -1;
                            if (keyCode >= KeyCode.Alpha1 && keyCode <= KeyCode.Alpha9)
                            {
                                slotNum = (keyCode - KeyCode.Alpha1) + 1;
                            }
                            else if (keyCode == KeyCode.V)
                            {
                                slotNum = 0;
                            }
                            
                            if (slotNum >= 0)
                            {
                                try
                                {
                                    Debug.Log($"[BTS] Trying to call {method.Name}({slotNum}) for key {keyCode}...");
                                    method.Invoke(player, new object[] { slotNum });
                                    Debug.Log($"[BTS] ✓ Called {method.Name}({slotNum})");
                                    return true;
                                }
                                catch { }
                            }
                        }
                    }
                }
                
                Debug.LogWarning($"[BTS] Could not find method to simulate key press: {keyCode}");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BTS] Error simulating key press: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Called when throw is detected as completed
        /// </summary>
        private void OnThrowCompleted()
        {
            if (hasCompletedThrow) return; // Already processed
            
            Debug.Log("[BTS] ========== ON THROW COMPLETED CALLED ==========");
            Debug.Log($"[BTS] Previous weapon - Slot: {previousEquippedSlot}, Key: {previousEquippedKey}");
            Debug.Log($"[BTS] NOTE: hasCompletedThrow will be set to true. Next continuous G press will switch to next throwable category.");
            Debug.Log($"[BTS] If user switches weapon before pressing G again, next G will restore last selection.");
            
            hasCompletedThrow = true; // Set flag so next continuous G press will switch to next category
            isThrowingInProgress = false;
            
            // Wait a short delay then switch back to weapon (allow throw animation to finish)
            // This does NOT auto-switch to next throwable - user must press G again
            StartCoroutine(SwitchBackAfterThrowDelay());
        }
        
        /// <summary>
        /// Switch back to weapon after throw (with delay to allow throw animation)
        /// </summary>
        private System.Collections.IEnumerator SwitchBackAfterThrowDelay()
        {
            Debug.Log("[BTS] ⏳ Waiting for throw animation to complete...");
            Debug.Log($"[BTS] Will switch back to - Slot: {previousEquippedSlot}, Key: {previousEquippedKey}");
            
            // Wait a bit longer to ensure throw animation completes (0.3 seconds)
            yield return new WaitForSeconds(0.3f);
            
            Debug.Log("[BTS] ⚡ Now attempting to switch back to weapon...");
            AutoSwitchBackToWeaponImmediately();
            
            // If that failed, try again with longer delay (fallback)
            yield return new WaitForSeconds(0.5f);
            
            var player = FindPlayerCharacter();
            if (player != null)
            {
                var currentItem = GetCurrentHoldItem(player);
                if (currentItem == null && previousEquippedSlot.HasValue)
                {
                    // Still empty hand - try switching again
                    Debug.Log("[BTS] Still empty hand, retrying switch back...");
                    AutoSwitchBackToWeaponImmediately();
                }
            }
        }
        
        /// <summary>
        /// Retry auto-switch back if immediate switch failed (with minimal delay)
        /// </summary>
        private System.Collections.IEnumerator RetryAutoSwitchBack()
        {
            // Wait just a few frames for player state to stabilize
            yield return null; // Wait 1 frame
            yield return null; // Wait another frame
            
            if (!previousEquippedSlot.HasValue) yield break;
            
            try
            {
                var player = FindPlayerCharacter();
                if (player == null) yield break;
                
                if (!IsPlayerSafeToSwitch(player)) yield break;
                
                int weaponSlot = previousEquippedSlot.Value;
                if (SwitchToSlot(weaponSlot))
                {
                    lastEquippedThrowableSlot = null;
                    Debug.Log($"[BTS] ✓ Successfully switched back to weapon slot {weaponSlot} (retry)");
                }
            }
            catch { }
        }
        
        /// <summary>
        /// Auto-switch back to previous weapon after throw completion (detected via item count - backup method)
        /// </summary>
        private void AutoSwitchBackToWeaponAfterThrow()
        {
            // This is now a backup method for count-based detection
            // Primary detection is via mouse button release
            AutoSwitchBackToWeaponImmediately();
        }

        /// <summary>
        /// Check if an item is a throwable item using multiple detection methods
        /// </summary>
        private bool IsThrowableItem(Item item)
        {
            if (item == null) return false;
            
            // Remove (Clone) suffix for better matching
            var rawName = item.name ?? "";
            var name = rawName.ToLower().Replace("(clone)", "").Trim();
            var displayName = rawName.Replace("(Clone)", "").Trim();
            
            Debug.Log($"[BTS] IsThrowableItem check: {item.name} (TypeID: {item.TypeID})");
            
            // STEP 0: Exclude known non-throwable items by TypeID (highest priority - blacklist)
            int[] excludedTypeIDs = { 
                12,  // BeanCan - 豆子罐头（不是投掷物）
                25   // Flashlight - 手电筒（不是投掷物）
            };
            if (excludedTypeIDs.Contains(item.TypeID))
            {
                Debug.Log($"[BTS] Item {item.name} (TypeID: {item.TypeID}) excluded - in blacklist");
                return false;
            }
            
            // Exclude by name patterns that are definitely not throwables
            string[] excludedNamePatterns = {
                "bean", "豆子", "罐头", "can", "candy", "糖果", 
                "flashlight", "手电",
                "冲锋枪", "rifle", "gun", "weapon", "枪"
            };
            foreach (var pattern in excludedNamePatterns)
            {
                if (name.Contains(pattern.ToLower()) && !IsThrowableException(item, pattern))
                {
                    Debug.Log($"[BTS] Item {item.name} excluded - matches excluded pattern: {pattern}");
                    return false;
                }
            }
            
            // STEP 1: Check by known throwable TypeIDs (most reliable - whitelist)
            int[] throwableTypeIDs = { 
                24,    // DynamiteMultiple
                66,    // FlashGrenade
                67,    // Grenade
                660,   // SmokeGrenade
                933,   // ToxGrenade
                941,   // FireGrenade
                942,   // ElecGrenade
                1257   // ShitBall (粪球)
            };
            if (throwableTypeIDs.Contains(item.TypeID))
            {
                Debug.Log($"[BTS] ✅ Item {item.name} (TypeID: {item.TypeID}) identified as throwable - TypeID whitelist");
                return true;
            }
            
            // STEP 2: Check SkillType property (reliable indicator)
            try
            {
                var itemType = item.GetType();
                var skillTypeProp = itemType.GetProperty("SkillType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (skillTypeProp != null)
                {
                    try
                    {
                        var skillType = skillTypeProp.GetValue(item);
                        if (skillType != null)
                        {
                            string skillTypeStr = skillType.ToString().ToLower();
                            if (skillTypeStr.Contains("item") || skillTypeStr.Contains("throw"))
                            {
                                Debug.Log($"[BTS] Item {item.name} identified as throwable via SkillType: {skillType}");
                                return true;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch (System.Exception)
            {
                // Ignore reflection errors
            }
            
            // STEP 3: Check item properties/methods that indicate throwable capability
            try
            {
                var itemType = item.GetType();
                
                // Check for "IsThrowable", "CanThrow", "Throwable" properties
                string[] throwablePropertyNames = { "IsThrowable", "CanThrow", "Throwable", "IsThrowableItem" };
                foreach (var propName in throwablePropertyNames)
                {
                    var prop = itemType.GetProperty(propName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (prop != null)
                    {
                        try
                        {
                            var value = prop.GetValue(item);
                            if (value is bool && (bool)value)
                            {
                                Debug.Log($"[BTS] Item {item.name} identified as throwable via property {propName}");
                                return true;
                            }
                        }
                        catch { }
                    }
                }
                
                // Check for "IsThrowable", "CanThrow" methods
                string[] throwableMethodNames = { "IsThrowable", "CanThrow", "IsThrowableItem" };
                foreach (var methodName in throwableMethodNames)
                {
                    var method = itemType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null, new System.Type[0], null);
                    if (method != null && method.ReturnType == typeof(bool))
                    {
                        try
                        {
                            var result = method.Invoke(item, null);
                            if (result is bool && (bool)result)
                            {
                                Debug.Log($"[BTS] Item {item.name} identified as throwable via method {methodName}()");
                                return true;
                            }
                        }
                        catch { }
                    }
                }
            }
            catch (System.Exception)
            {
                // Ignore reflection errors
            }
            
            // STEP 4: Check by item name keywords (least reliable - use as fallback only)
            // Only use precise keywords that won't cause false positives
            string[] throwableKeywords = {
                "grenade", "手雷",
                "flash", "闪光",
                "smoke", "烟雾",
                "molotov", "燃烧瓶",
                "tox", "毒气",
                "elec", "电",
                "bomb", "炸弹", "管状", "集数",
                "dynamite", "炸药", "集束",
                "tube", "canister",  // Removed "can" to avoid BeanCan false positive
                "throwing", "投掷",
                "粪球", "feces", "dung", "shitball", "shit",  // Added "shitball" and "shit" for ShitBall
                "explosive", "爆炸"
            };
            
            foreach (var keyword in throwableKeywords)
            {
                if (name.Contains(keyword.ToLower()) || displayName.Contains(keyword))
                {
                    Debug.Log($"[BTS] Item {item.name} identified as throwable via keyword: {keyword}");
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if an item should be exempted from exclusion patterns (e.g., a throwable item that contains "can" in name)
        /// </summary>
        private bool IsThrowableException(Item item, string pattern)
        {
            // If the item has already passed TypeID or SkillType checks, don't exclude it
            // This allows items like "canister grenade" to be identified even if they contain "can"
            
            // Check if it's in the throwable TypeID whitelist (must match main whitelist)
            int[] throwableTypeIDs = { 24, 66, 67, 660, 933, 941, 942, 1257 }; // Include 1257 (ShitBall)
            if (throwableTypeIDs.Contains(item.TypeID))
            {
                return true;
            }
            
            // Check SkillType
            try
            {
                var itemType = item.GetType();
                var skillTypeProp = itemType.GetProperty("SkillType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (skillTypeProp != null)
                {
                    try
                    {
                        var skillType = skillTypeProp.GetValue(item);
                        if (skillType != null)
                        {
                            string skillTypeStr = skillType.ToString().ToLower();
                            if (skillTypeStr.Contains("item") || skillTypeStr.Contains("throw"))
                            {
                                return true;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return false;
        }
        
        /// <summary>
        /// Debug: Scan all items in inventory and print detailed information (F11 key)
        /// This helps identify throwable items by their properties
        /// </summary>
        private void ScanAllInventoryItemsWithDetails()
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogWarning("[BTS] Player not found!");
                    return;
                }
                
                Debug.Log("[BTS] ========== Scanning All Inventory Items with Details ==========");
                
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                if (inventory == null)
                {
                    Debug.LogError("[BTS] Inventory component not found!");
                    return;
                }
                
                var inventoryType = inventory.GetType();
                var getItemMethod = inventoryType.GetMethod(
                    "GetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetItemAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetSlotItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                if (getItemMethod == null)
                {
                    Debug.LogError("[BTS] Could not find method to get items from inventory!");
                    return;
                }
                
                // Get max slots
                var maxSlotsProp = inventoryType.GetProperty("maxSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var slotCountProp = inventoryType.GetProperty("SlotCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                int maxSlots = 30;
                if (maxSlotsProp != null)
                {
                    var value = maxSlotsProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                else if (slotCountProp != null)
                {
                    var value = slotCountProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                }
                
                int itemCount = 0;
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    try
                    {
                        var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                        if (item != null)
                        {
                            itemCount++;
                            var itemType = item.GetType();
                            bool isThrowable = IsThrowableItem(item);
                            string throwableMark = isThrowable ? "⭐ THROWABLE" : "";
                            
                            var itemName = item.name?.ToLower() ?? "";
                            Debug.Log($"[BTS] Slot {slotIndex}: {item.name} | TypeID: {item.TypeID} | Type: {itemType.Name} {throwableMark}");
                            
                            // Special debug for items that contain "粪球" or "feces" or "dung" in name
                            bool isFecesItem = itemName.Contains("粪球") || itemName.Contains("feces") || itemName.Contains("dung");
                            if (isFecesItem)
                            {
                                Debug.Log($"[BTS]   >>> FEces/Dung item detected! <<<");
                                Debug.Log($"[BTS]   Full name: {item.name}");
                                Debug.Log($"[BTS]   TypeID: {item.TypeID}");
                                Debug.Log($"[BTS]   IsThrowable check result: {isThrowable}");
                            }
                            
                            // Always check for throwable-related properties/methods for debugging
                            try
                            {
                                var props = itemType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                var methods = itemType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                
                                // Check properties
                                foreach (var prop in props)
                                {
                                    var propName = prop.Name.ToLower();
                                    if (propName.Contains("throw") || propName.Contains("bomb") || propName.Contains("explosive") || 
                                        propName.Contains("dynamite") || propName.Contains("skill") || propName.Contains("cast") ||
                                        isFecesItem)  // Always show all properties for feces items
                                    {
                                        try
                                        {
                                            var value = prop.GetValue(item);
                                            Debug.Log($"[BTS]   -> Property {prop.Name}: {value}");
                                        }
                                        catch { }
                                    }
                                }
                                
                                // Check methods
                                foreach (var method in methods)
                                {
                                    var methodName = method.Name.ToLower();
                                    if ((methodName.Contains("throw") || methodName.Contains("cast") || methodName.Contains("use")) && 
                                        method.GetParameters().Length == 0 && method.ReturnType == typeof(bool))
                                    {
                                        try
                                        {
                                            Debug.Log($"[BTS]   -> Method: {method.Name}() returns {method.ReturnType.Name}");
                                        }
                                        catch { }
                                    }
                                }
                                
                                // Also check for SkillType or ItemSkill related properties (from log: "skillType is itemSkill")
                                var skillTypePropCheck = itemType.GetProperty("SkillType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                if (skillTypePropCheck != null)
                                {
                                    try
                                    {
                                        var skillTypeValue = skillTypePropCheck.GetValue(item);
                                        Debug.Log($"[BTS]   -> SkillType: {skillTypeValue}");
                                        if (isFecesItem)
                                        {
                                            Debug.Log($"[BTS]   >>> SkillType for Feces item: {skillTypeValue} <<<");
                                        }
                                        if (skillTypeValue != null)
                                        {
                                            string skillTypeStr = skillTypeValue.ToString().ToLower();
                                            if (skillTypeStr.Contains("item") || skillTypeStr.Contains("throw"))
                                            {
                                                Debug.Log($"[BTS]   ⚠️ This item might be throwable based on SkillType!");
                                            }
                                        }
                                    }
                                    catch (System.Exception e)
                                    {
                                        if (isFecesItem)
                                        {
                                            Debug.Log($"[BTS]   >>> SkillType check failed: {e.Message} <<<");
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                    catch (System.Exception)
                    {
                        // Skip invalid slots
                    }
                }
                
                Debug.Log($"[BTS] ========== Scanned {itemCount} items in {maxSlots} slots ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning inventory items: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Show dialogue bubble when G key is pressed
        /// </summary>
        private void ShowDebugBubble(string text = "🧨 BetterThrowingSystem Triggered!")
        {
            Debug.Log("[BTS] Dialogue bubble requested!");

            // 1️⃣ Find player transform - try to get it fresh each time
            Transform? target = null;
            
            // First try to find actual player character (not NPC)
            var player = FindPlayerCharacter();
            if (player != null && player.transform != null)
            {
                target = player.transform;
                Debug.Log($"[BTS] Found player transform ({player.name}) for bubble.");
            }
            else if (Camera.main != null)
            {
                // If no player found, use camera as fallback
                target = Camera.main.transform;
                Debug.Log("[BTS] Using camera as bubble target.");
            }

            // 2️⃣ If still no target, create a dummy object in front of camera
            if (target == null)
            {
                GameObject dummy = new GameObject("BTSBubbleAnchor");
                if (Camera.main != null)
                {
                    dummy.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;
                    target = dummy.transform;
                    Debug.Log("[BTS] Created dummy anchor for bubble.");
                    
                    // Clean up dummy after a delay (10 seconds)
                    Destroy(dummy, 10f);
                }
                else
                {
                    Debug.LogError("[BTS] No valid transform or camera found for bubble!");
                    Destroy(dummy);
                    return;
                }
            }

            // 3️⃣ Show bubble using reflection (to avoid UniTask type mismatch)
            try
            {
                var showMethod = typeof(DialogueBubblesManager).GetMethod(
                    "Show",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );
                
                if (showMethod != null)
                {
                    // Call Show with parameters
                    var result = showMethod.Invoke(
                        null,
                        new object[] { text, target, 1f, false, false, -1f, 3f }
                    );
                    
                    // Try to call Forget() on the result if it has that method
                    if (result != null)
                    {
                        var forgetMethod = result.GetType().GetMethod("Forget");
                        if (forgetMethod != null)
                        {
                            forgetMethod.Invoke(result, null);
                            Debug.Log("[BTS] Bubble should now be visible.");
                        }
                        else
                        {
                            Debug.LogWarning("[BTS] Forget method not found on UniTask result.");
                        }
                    }
                }
                else
                {
                    Debug.LogError("[BTS] DialogueBubblesManager.Show method not found!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Failed to show dialogue bubble: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Find the actual player CharacterMainControl (not NPCs)
        /// </summary>
        private CharacterMainControl? FindPlayerCharacter()
        {
            // Method 1: Try to find by Camera.main (player usually has the main camera)
            if (Camera.main != null && Camera.main.transform != null)
            {
                // Check if camera is a child of a CharacterMainControl
                var characterFromCamera = Camera.main.transform.GetComponentInParent<CharacterMainControl>();
                if (characterFromCamera != null)
                {
                    if (IsPlayerCharacter(characterFromCamera))
                    {
                        Debug.Log("[BTS] Found player via Camera.main parent");
                        return characterFromCamera;
                    }
                }
                
                // Or check if camera follows a CharacterMainControl (common pattern)
                var allCharacters = FindObjectsOfType<CharacterMainControl>();
                foreach (var character in allCharacters)
                {
                    if (IsPlayerCharacter(character))
                    {
                        // Check if this character is near the camera
                        float distance = Vector3.Distance(Camera.main.transform.position, character.transform.position);
                        if (distance < 10f) // Player should be close to camera
                        {
                            Debug.Log($"[BTS] Found player near camera (distance: {distance})");
                            return character;
                        }
                    }
                }
            }
            
            // Method 2: Find all CharacterMainControl and filter for player
            var allChars = FindObjectsOfType<CharacterMainControl>();
            foreach (var character in allChars)
            {
                if (IsPlayerCharacter(character))
                {
                    Debug.Log("[BTS] Found player via IsPlayerCharacter check");
                    return character;
                }
            }
            
            // Method 3: Fallback - use GameObject.Find with "Player" tag
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                var character = playerObj.GetComponent<CharacterMainControl>();
                if (character != null)
                {
                    Debug.Log("[BTS] Found player via Player tag");
                    return character;
                }
            }
            
            // Fallback: Use first CharacterMainControl found (even if we can't verify it's the player)
            // This allows the mod to work even if player detection fails
            if (allChars.Length > 0)
            {
                Debug.LogWarning($"[BTS] Could not verify player character! Using first CharacterMainControl found: {allChars[0].gameObject.name}");
                return allChars[0];
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if a CharacterMainControl is the player (not an NPC)
        /// </summary>
        private bool IsPlayerCharacter(CharacterMainControl character)
        {
            if (character == null) return false;
            
            try
            {
                var charType = character.GetType();
                
                // Check for IsPlayer property
                var isPlayerProp = charType.GetProperty("IsPlayer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (isPlayerProp != null)
                {
                    var value = isPlayerProp.GetValue(character);
                    if (value is bool && (bool)value)
                    {
                        return true;
                    }
                }
                
                // Check for IsMainCharacter property
                var isMainProp = charType.GetProperty("IsMainCharacter", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (isMainProp != null)
                {
                    var value = isMainProp.GetValue(character);
                    if (value is bool && (bool)value)
                    {
                        return true;
                    }
                }
                
                // Check Tag
                if (character.gameObject.CompareTag("Player"))
                {
                    return true;
                }
                
                // Check if this character has the main camera as a child or is followed by it
                if (Camera.main != null)
                {
                    float distance = Vector3.Distance(Camera.main.transform.position, character.transform.position);
                    // Player is usually very close to camera (within 2-3 units typically)
                    if (distance < 5f)
                    {
                        // Additional check: player usually doesn't have AI components
                        var aiComponent = character.GetComponent<MonoBehaviour>();
                        if (aiComponent != null)
                        {
                            var aiTypeName = aiComponent.GetType().Name;
                            // NPCs often have AI-related components
                            if (aiTypeName.Contains("AI") || aiTypeName.Contains("NPC") || aiTypeName.Contains("Enemy"))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BTS] Error checking if character is player: {e.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Find player transform by searching for CharacterMainControl or PlayerController
        /// </summary>
        private Transform? FindPlayerTransform()
        {
            var player = FindPlayerCharacter();
            if (player != null)
            {
                return player.transform;
            }
            return null;
        }

        /// <summary>
        /// Find first throwable item in the scene
        /// </summary>
        private Item? FindFirstThrowable()
        {
            var allItems = FindObjectsOfType<Item>();
            return allItems.FirstOrDefault(IsThrowableItem);
        }

        // OLD METHOD REMOVED: ScanAndTryEquipMethods - no longer used
        // Now using slot-based switching instead of direct item manipulation

        /// <summary>
        /// Scan all methods and properties of CharacterMainControl to find equipment/inventory APIs
        /// </summary>
        private void ScanCharacterMethods()
        {
            try
            {
                var player = FindObjectOfType<CharacterMainControl>();
                if (player == null)
                {
                    Debug.LogWarning("[BTS] CharacterMainControl not found!");
                    return;
                }

                var playerType = player.GetType();
                Debug.Log($"[BTS] ========== Scanning {playerType.Name} ==========");
                
                // Get all public methods
                var methods = playerType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Debug.Log($"[BTS] Found {methods.Length} methods:");
                
                foreach (var method in methods)
                {
                    var methodName = method.Name;
                    var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    
                    // Look for keywords related to equipment, slot, inventory
                    if (methodName.Contains("Equip", StringComparison.OrdinalIgnoreCase) ||
                        methodName.Contains("Slot", StringComparison.OrdinalIgnoreCase) ||
                        methodName.Contains("Inventory", StringComparison.OrdinalIgnoreCase) ||
                        methodName.Contains("Item", StringComparison.OrdinalIgnoreCase) ||
                        methodName.Contains("Use", StringComparison.OrdinalIgnoreCase) ||
                        methodName.Contains("Select", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[BTS] ⭐ METHOD: {methodName}({parameters})");
                    }
                }
                
                // Get all public properties
                var properties = playerType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Debug.Log($"[BTS] Found {properties.Length} properties:");
                
                foreach (var prop in properties)
                {
                    var propName = prop.Name;
                    
                    if (propName.Contains("Equip", StringComparison.OrdinalIgnoreCase) ||
                        propName.Contains("Slot", StringComparison.OrdinalIgnoreCase) ||
                        propName.Contains("Inventory", StringComparison.OrdinalIgnoreCase) ||
                        propName.Contains("Item", StringComparison.OrdinalIgnoreCase) ||
                        propName.Contains("Equipment", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"[BTS] ⭐ PROPERTY: {propName} ({prop.PropertyType.Name})");
                    }
                }
                
                // Also check for Inventory component
                var inventory = player.GetComponent<Inventory>();
                if (inventory != null)
                {
                    var inventoryType = inventory.GetType();
                    Debug.Log($"[BTS] ========== Scanning {inventoryType.Name} ==========");
                    
                    var invMethods = inventoryType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    foreach (var method in invMethods)
                    {
                        var methodName = method.Name;
                        if (methodName.Contains("Equip", StringComparison.OrdinalIgnoreCase) ||
                            methodName.Contains("Slot", StringComparison.OrdinalIgnoreCase) ||
                            methodName.Contains("Use", StringComparison.OrdinalIgnoreCase) ||
                            methodName.Contains("Select", StringComparison.OrdinalIgnoreCase))
                        {
                            var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                            Debug.Log($"[BTS] ⭐ INVENTORY METHOD: {methodName}({parameters})");
                        }
                    }
                    
                    var invProps = inventoryType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    foreach (var prop in invProps)
                    {
                        if (prop.Name.Contains("Slot", StringComparison.OrdinalIgnoreCase) ||
                            prop.Name.Contains("Item", StringComparison.OrdinalIgnoreCase))
                        {
                            Debug.Log($"[BTS] ⭐ INVENTORY PROPERTY: {prop.Name} ({prop.PropertyType.Name})");
                        }
                    }
                }
                
                // Try to simulate pressing number key 3
                Debug.Log("[BTS] ========== Attempting to simulate pressing key 3 ==========");
                TrySimulateNumberKeyPress(3);
                
                Debug.Log("[BTS] ========== Scan completed ==========");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning character methods: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Try to simulate pressing a number key (3-9) to equip item from slot
        /// </summary>
        private void TrySimulateNumberKeyPress(int slotNumber)
        {
            try
            {
                var player = FindObjectOfType<CharacterMainControl>();
                if (player == null) return;
                
                var playerType = player.GetType();
                
                // Try common method names for slot selection
                string[] possibleMethodNames = {
                    $"UseSlot{slotNumber}",
                    $"EquipSlot{slotNumber}",
                    $"SelectSlot{slotNumber}",
                    "UseSlot",
                    "EquipSlot",
                    "SelectSlot",
                    "UseInventorySlot",
                    "EquipInventorySlot",
                    "OnSlotPressed"
                };
                
                foreach (var methodName in possibleMethodNames)
                {
                    var method = playerType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (method != null)
                    {
                        var paramCount = method.GetParameters().Length;
                        if (paramCount == 0)
                        {
                            method.Invoke(player, null);
                            Debug.Log($"[BTS] ✓ Called {methodName}()");
                        }
                        else if (paramCount == 1)
                        {
                            method.Invoke(player, new object[] { slotNumber });
                            Debug.Log($"[BTS] ✓ Called {methodName}({slotNumber})");
                        }
                    }
                }
                
                // Also try on Inventory component
                var inventory = player.GetComponent<Inventory>();
                if (inventory != null)
                {
                    var inventoryType = inventory.GetType();
                    foreach (var methodName in possibleMethodNames)
                    {
                        var method = inventoryType.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (method != null)
                        {
                            var paramCount = method.GetParameters().Length;
                            if (paramCount == 0)
                            {
                                method.Invoke(inventory, null);
                                Debug.Log($"[BTS] ✓ Called {methodName}() on Inventory");
                            }
                            else if (paramCount == 1)
                            {
                                method.Invoke(inventory, new object[] { slotNumber });
                                Debug.Log($"[BTS] ✓ Called {methodName}({slotNumber}) on Inventory");
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error simulating number key press: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Scan all registered items in ItemAssetsCollection and print their TypeIDs
        /// This helps identify the correct TypeID for throwables
        /// </summary>
        private void ScanAllRegisteredItems()
        {
            try
            {
                Debug.Log("[BTS] ====== Scanning all registered items in ItemAssetsCollection ======");
                
                var itemAssetsCollectionType = typeof(ItemAssetsCollection);
                
                // Try to get AllEntries property
                var allEntriesProperty = itemAssetsCollectionType.GetProperty(
                    "AllEntries",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                );

                if (allEntriesProperty != null)
                {
                    var allEntries = allEntriesProperty.GetValue(null);
                    if (allEntries != null)
                    {
                        // Try to enumerate the collection
                        var enumerable = allEntries as System.Collections.IEnumerable;
                        if (enumerable != null)
                        {
                            int count = 0;
                            foreach (var entry in enumerable)
                            {
                                if (entry != null)
                                {
                                    // Try to get Key and Value from dictionary entry
                                    var entryType = entry.GetType();
                                    var keyProperty = entryType.GetProperty("Key");
                                    var valueProperty = entryType.GetProperty("Value");
                                    
                                    if (keyProperty != null && valueProperty != null)
                                    {
                                        var key = keyProperty.GetValue(entry);
                                        var value = valueProperty.GetValue(entry);
                                        
                                        // Try to get item name from value
                                        string itemName = "Unknown";
                                        if (value != null)
                                        {
                                            var nameProperty = value.GetType().GetProperty("ItemName") ?? 
                                                              value.GetType().GetProperty("name") ??
                                                              value.GetType().GetProperty("Name");
                                            if (nameProperty != null)
                                            {
                                                itemName = nameProperty.GetValue(value)?.ToString() ?? "Unknown";
                                            }
                                            else
                                            {
                                                itemName = value.ToString() ?? "Unknown";
                                            }
                                        }
                                        
                                        Debug.Log($"[BTS] Registered Item: {itemName} | TypeID = {key}");
                                        count++;
                                        
                                        // Also check if this looks like a throwable
                                        if (itemName.Contains("grenade", StringComparison.OrdinalIgnoreCase) ||
                                            itemName.Contains("手雷", StringComparison.OrdinalIgnoreCase) ||
                                            itemName.Contains("molotov", StringComparison.OrdinalIgnoreCase) ||
                                            itemName.Contains("smoke", StringComparison.OrdinalIgnoreCase) ||
                                            itemName.Contains("flash", StringComparison.OrdinalIgnoreCase) ||
                                            itemName.Contains("bomb", StringComparison.OrdinalIgnoreCase))
                                        {
                                            Debug.Log($"[BTS] ⭐ FOUND THROWABLE: {itemName} | TypeID = {key}");
                                        }
                                    }
                                }
                            }
                            Debug.Log($"[BTS] ====== Total registered items: {count} ======");
                        }
                        else
                        {
                            Debug.LogWarning("[BTS] AllEntries is not enumerable.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[BTS] AllEntries property returned null.");
                    }
                }
                else
                {
                    Debug.LogWarning("[BTS] ItemAssetsCollection.AllEntries property not found. Trying alternative methods...");
                    
                    // Alternative: Try to find items by scanning known TypeID ranges
                    ScanItemsByRange();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning registered items: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Alternative method: Try to instantiate items in common ranges to find throwables
        /// </summary>
        private void ScanItemsByRange()
        {
            Debug.Log("[BTS] Trying to find items by scanning common TypeID ranges...");
            Debug.Log("[BTS] Note: This may take a moment and will only show items that can be instantiated.");
            
            // Common ranges where throwables might be
            int[] rangesToCheck = { 250, 254, 255, 256, 700, 740, 742, 800, 900, 2540, 2550, 2560 };
            
            foreach (int typeID in rangesToCheck)
            {
                try
                {
                    var instantiateSyncMethod = typeof(ItemAssetsCollection).GetMethod(
                        "InstantiateSync",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static
                    );
                    
                    if (instantiateSyncMethod != null)
                    {
                        var item = instantiateSyncMethod.Invoke(null, new object[] { typeID }) as Item;
                        if (item != null)
                        {
                            Debug.Log($"[BTS] ✓ TypeID {typeID}: {item.name}");
                            
                            // Check if it's a throwable
                            if (IsThrowableItem(item))
                            {
                                Debug.Log($"[BTS] ⭐ THROWABLE FOUND: TypeID {typeID} = {item.name}");
                            }
                            
                            // Clean up test item
                            Destroy(item.gameObject);
                        }
                    }
                }
                catch
                {
                    // Ignore errors for invalid TypeIDs
                }
            }
        }
    }
}

// ============================================================================
// FINAL VERSION DESIGN (Keep for later implementation)
// ============================================================================
// This section contains the original "ideal" design that will work once we
// know the actual game APIs. Keep it commented out until we have the real
// class names and methods from the game's DLL.
// ============================================================================

/*
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ItemStatsSystem;
using Duckov.Modding;

namespace BetterThrowingSystem
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        // Throwing items backpack - supports 5 slots, 1 equipped slot
        private Item[] throwingItemBackpack = new Item[5];
        private int equippedSlotIndex = 0; // Currently equipped throwing item slot
        
        // Key bindings
        private KeyCode throwKey = KeyCode.G;
        
        private bool isThrowingMode = false;
        private Item originalHeldItem = null;

        private void Start()
        {
            Debug.Log("[BetterThrowingSystem] Mod loaded successfully!");
            
            // Initialize throwing item backpack scanning
            ScanPlayerInventoryForThrowingItems().Forget();
        }

        private void Update()
        {
            // Check for G key press to switch to throwing mode
            if (Input.GetKeyDown(throwKey))
            {
                ToggleThrowingMode().Forget();
            }
        }

        /// <summary>
        /// Scan player inventory for throwable items and food items
        /// </summary>
        private async UniTask ScanPlayerInventoryForThrowingItems()
        {
            // Wait for player to be ready - check if player character exists
            int attempts = 0;
            while (GetPlayerCharacter() == null && attempts < 100)
            {
                await UniTask.Yield(); // Yield to next frame
                attempts++;
            }
            
            // Get player character
            var playerCharacter = GetPlayerCharacter();
            if (playerCharacter == null)
            {
                Debug.LogWarning("[BetterThrowingSystem] Player character not found after waiting!");
                return;
            }

            // Get player inventory
            var inventory = GetPlayerInventory(playerCharacter);
            if (inventory == null)
            {
                Debug.LogWarning("[BetterThrowingSystem] Player inventory not found!");
                return;
            }

            // Scan all items in inventory
            List<Item> throwableItems = new List<Item>();
            
            // Try to get items from inventory - method may vary based on actual API
            try
            {
                // Method 1: If inventory has slots property (adjust based on actual API)
                // Note: Inventory API may be different, adjust accordingly
                var maxSlots = inventory.GetType().GetProperty("maxSlots")?.GetValue(inventory);
                if (maxSlots != null && (int)maxSlots > 0)
                {
                    int slotCount = (int)maxSlots;
                    var getItemMethod = inventory.GetType().GetMethod("GetItem");
                    if (getItemMethod != null)
                    {
                        for (int i = 0; i < slotCount; i++)
                        {
                            var item = getItemMethod.Invoke(inventory, new object[] { i }) as Item;
                            if (item != null && IsThrowableItem(item))
                            {
                                throwableItems.Add(item);
                            }
                        }
                    }
                }
                
                // Method 2: Get all items using IsInPlayerCharacter
                // This scans through items that are in player character's inventory
                var allItems = FindObjectsOfType<Item>();
                foreach (var item in allItems)
                {
                    if (item.IsInPlayerCharacter() && IsThrowableItem(item))
                    {
                        if (!throwableItems.Contains(item))
                        {
                            throwableItems.Add(item);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BetterThrowingSystem] Error scanning inventory: {e.Message}");
            }

            Debug.Log($"[BetterThrowingSystem] Found {throwableItems.Count} throwable items in player inventory");
            
            // Populate throwing item backpack (max 5 items)
            int backpackIndex = 0;
            foreach (var item in throwableItems.Take(5))
            {
                if (backpackIndex < throwingItemBackpack.Length)
                {
                    throwingItemBackpack[backpackIndex] = item;
                    Debug.Log($"[BetterThrowingSystem] Added item to slot {backpackIndex}: {item.name}");
                    backpackIndex++;
                }
            }
        }

        /// <summary>
        /// Check if an item is a throwable item
        /// </summary>
        private bool IsThrowableItem(Item item)
        {
            if (item == null) return false;
            
            // Method 1: Check by TypeID if you know the throwable item TypeIDs
            // Uncomment and fill in the known TypeIDs for throwable items
            // int[] throwableTypeIDs = { 254, 255, 256 }; // Example IDs
            // if (throwableTypeIDs.Contains(item.TypeID))
            //     return true;
            
            // Method 2: Check by item name (keywords)
            string itemName = item.name?.ToLower() ?? "";
            string[] throwableKeywords = { "grenade", "molotov", "bomb", "flash", "smoke", "throwing", "手雷", "炸弹", "闪光", "烟雾" };
            
            if (throwableKeywords.Any(keyword => itemName.Contains(keyword)))
                return true;
            
            // Method 3: Check item properties/components if available
            // You may need to check for specific components or tags that indicate throwable items
            // For example: if (item.gameObject.HasComponent<ThrowableComponent>()) return true;
            
            return false;
        }

        /// <summary>
        /// Check if an item is food or medicine
        /// </summary>
        private bool IsFoodOrMedicine(Item item)
        {
            if (item == null) return false;
            
            string itemName = item.name?.ToLower() ?? "";
            string[] foodKeywords = { "food", "med", "bandage", "medicine", "drink", "eat", "consume" };
            
            return foodKeywords.Any(keyword => itemName.Contains(keyword));
        }

        /// <summary>
        /// Toggle throwing mode - switch to equipped throwing item
        /// </summary>
        private async UniTask ToggleThrowingMode()
        {
            if (throwingItemBackpack[equippedSlotIndex] == null)
            {
                Debug.Log("[BetterThrowingSystem] No throwing item equipped!");
                return;
            }

            var playerCharacter = GetPlayerCharacter();
            if (playerCharacter == null) return;

            if (!isThrowingMode)
            {
                // Enter throwing mode - equip the throwing item
                await EquipThrowingItem();
            }
            else
            {
                // Exit throwing mode - restore original item
                await UnequipThrowingItem();
            }
        }

        /// <summary>
        /// Equip the throwing item from equipped slot
        /// </summary>
        private async UniTask EquipThrowingItem()
        {
            var throwingItem = throwingItemBackpack[equippedSlotIndex];
            if (throwingItem == null)
            {
                Debug.Log("[BetterThrowingSystem] No throwing item in equipped slot!");
                return;
            }

            var playerCharacter = GetPlayerCharacter();
            if (playerCharacter == null) return;

            // Save current held item if any
            // Try to find what the player is currently holding
            // This may require accessing the character's equipment/hand slot through the actual API
            
            // Make sure the item is in player's inventory first
            if (!throwingItem.IsInPlayerCharacter())
            {
                // If item is not in player character, try to send it there
                ItemUtilities.SendToPlayerCharacter(throwingItem);
                await UniTask.Yield(); // Wait a bit for the item to be transferred
            }
            
            // Try to equip the item - this may require specific API calls
            // Option 1: Use SendToPlayerCharacter (if it equips items)
            ItemUtilities.SendToPlayerCharacter(throwingItem, false);
            
            // Option 2: You may need to use character's equipment system directly
            // For example: playerCharacter.EquipItem(throwingItem);
            
            isThrowingMode = true;
            Debug.Log($"[BetterThrowingSystem] Entered throwing mode with item: {throwingItem.name}");
        }

        /// <summary>
        /// Unequip throwing item and restore original
        /// </summary>
        private async UniTask UnequipThrowingItem()
        {
            var playerCharacter = GetPlayerCharacter();
            if (playerCharacter == null) return;

            // Restore original item if saved
            // This requires the actual equipment system API
            
            isThrowingMode = false;
            Debug.Log("[BetterThrowingSystem] Exited throwing mode");
            await UniTask.CompletedTask; // Fix CS1998 warning
        }

        /// <summary>
        /// Get player character component
        /// </summary>
        private CharacterMainControl GetPlayerCharacter()
        {
            // Find player character - adjust based on actual game structure
            return FindObjectOfType<CharacterMainControl>();
        }

        /// <summary>
        /// Get player inventory from character
        /// </summary>
        private Inventory GetPlayerInventory(CharacterMainControl character)
        {
            if (character == null) return null;
            
            // Access inventory - this may need adjustment based on actual API
            // Try to get inventory component from character or related GameObject
            return character.GetComponent<Inventory>() ?? 
                   character.gameObject.GetComponentInChildren<Inventory>();
        }

        /// <summary>
        /// Switch to a different slot in the throwing item backpack
        /// </summary>
        public void SwitchEquippedSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= throwingItemBackpack.Length)
                return;

            if (throwingItemBackpack[slotIndex] == null)
                return;

            equippedSlotIndex = slotIndex;
            
            // If currently in throwing mode, switch the equipped item
            if (isThrowingMode)
            {
                EquipThrowingItem().Forget();
            }
            
            Debug.Log($"[BetterThrowingSystem] Switched to slot {slotIndex}");
        }

        private void OnDestroy()
        {
            // Cleanup when mod is unloaded
            if (isThrowingMode)
            {
                UnequipThrowingItem().Forget();
            }
        }
    }
}
*/
