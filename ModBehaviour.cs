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
        
        // List of throwable item slot numbers (3-9) in player's inventory
        private List<int> throwableItemSlots = new List<int>();
        // Current index in the throwable slots list
        private int currentThrowableIndex = -1;

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

            // ② G key: Cycle through throwable items in inventory
            if (Input.GetKeyDown(throwKey))
            {
                Debug.Log("[BTS] ========== G KEY PRESSED - VERSION 2.0 ==========");
                CycleToNextThrowable();
            }
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
        /// Scan player inventory slots (ALL slots) for throwable items and record slot numbers
        /// </summary>
        private void ScanPlayerInventoryForThrowables()
        {
            throwableItemSlots.Clear();
            
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
                    // Try to find Inventory in children
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
                );
                
                if (getItemMethod == null)
                {
                    Debug.LogWarning("[BTS] Inventory.GetItem method not found! Trying alternative methods...");
                    // Try alternative method names
                    getItemMethod = inventoryType.GetMethod(
                        "GetItemAt",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetSlotItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                }
                
                if (getItemMethod == null)
                {
                    Debug.LogError("[BTS] Could not find method to get item from inventory slot!");
                    return;
                }
                
                // Try to get max slots or slot count
                var maxSlotsProp = inventoryType.GetProperty("maxSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var slotCountProp = inventoryType.GetProperty("SlotCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var capacityProp = inventoryType.GetProperty("Capacity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var sizeProp = inventoryType.GetProperty("Size", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
                int maxSlots = 30; // Default to a larger number to scan more slots (backpack might have multiple rows)
                if (maxSlotsProp != null)
                {
                    var value = maxSlotsProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                    Debug.Log($"[BTS] Found maxSlots property: {maxSlots}");
                }
                else if (slotCountProp != null)
                {
                    var value = slotCountProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                    Debug.Log($"[BTS] Found SlotCount property: {maxSlots}");
                }
                else if (capacityProp != null)
                {
                    var value = capacityProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                    Debug.Log($"[BTS] Found Capacity property: {maxSlots}");
                }
                else if (sizeProp != null)
                {
                    var value = sizeProp.GetValue(inventory);
                    if (value is int) maxSlots = (int)value;
                    Debug.Log($"[BTS] Found Size property: {maxSlots}");
                }
                else
                {
                    Debug.LogWarning("[BTS] Could not find max slots property, defaulting to 30 slots");
                }
                
                Debug.Log($"[BTS] Scanning ALL inventory slots 0-{maxSlots - 1} for throwables...");
                
                // Scan ALL slots (not just 3-9)
                // This will find throwables in all rows of the backpack
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    try
                    {
                        var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                        if (item != null && IsThrowableItem(item))
                        {
                            throwableItemSlots.Add(slotIndex);
                            Debug.Log($"[BTS] Found throwable in slot {slotIndex}: {item.name} (TypeID: {item.TypeID})");
                        }
                    }
                    catch (System.Exception)
                    {
                        // Silently skip slots that cause errors (they might not exist or be out of range)
                        // This is normal for slots beyond the actual inventory size
                    }
                }
                
                // Sort slots for better UX (items will be cycled in order)
                throwableItemSlots.Sort();
                
                Debug.Log($"[BTS] Scanned inventory: Found {throwableItemSlots.Count} throwable item(s) in slots: [{string.Join(", ", throwableItemSlots)}]");
                
                // Reset index if current slot is no longer in list
                if (currentThrowableIndex >= throwableItemSlots.Count)
                {
                    currentThrowableIndex = -1;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error scanning inventory slots: {e.Message}\n{e.StackTrace}");
            }
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
                            
                            object slots = null;
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
                                if (slots is System.Collections.IList)
                                {
                                    var slotsList = slots as System.Collections.IList;
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
        /// Cycle to the next throwable slot and switch to it
        /// </summary>
        private void CycleToNextThrowable()
        {
            // Rescan inventory slots to get up-to-date list
            ScanPlayerInventoryForThrowables();
            
            if (throwableItemSlots.Count == 0)
            {
                Debug.LogWarning("[BTS] No throwable items found in inventory slots!");
                ShowDebugBubble("❌ 背包中没有投掷物");
                return;
            }
            
            // Move to next slot in the list (cycle back to start if at end)
            currentThrowableIndex = (currentThrowableIndex + 1) % throwableItemSlots.Count;
            
            int targetSlot = throwableItemSlots[currentThrowableIndex];
            
            Debug.Log($"[BTS] Switching to throwable slot {targetSlot} [{currentThrowableIndex + 1}/{throwableItemSlots.Count}]");
            
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogError("[BTS] Player character not found!");
                    return;
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
                    // Show success bubble
                    var target = player.transform ?? Camera.main?.transform;
                    if (target != null)
                    {
                        ShowDebugBubble($"💣 [{currentThrowableIndex + 1}/{throwableItemSlots.Count}] {itemName}");
                    }
                }
                else
                {
                    ShowDebugBubble("❌ 无法切换到槽位");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error switching throwable slot: {e.Message}\n{e.StackTrace}");
                ShowDebugBubble($"❌ 错误：{e.Message.Substring(0, Math.Min(30, e.Message.Length))}...");
            }
        }

        /// <summary>
        /// Check if an item is a throwable item using multiple detection methods
        /// </summary>
        private bool IsThrowableItem(Item item)
        {
            if (item == null) return false;
            
            // Exclude non-throwable items by TypeID
            int[] excludedTypeIDs = { 25 }; // Flashlight is not a throwable
            if (excludedTypeIDs.Contains(item.TypeID))
            {
                return false;
            }
            
            // Method 1: Check by known throwable TypeIDs (from logs: 66, 67, 660, 933, 941, 942, 24)
            int[] throwableTypeIDs = { 24, 66, 67, 660, 933, 941, 942 }; // DynamiteMultiple, FlashGrenade, Grenade, SmokeGrenade, ToxGrenade, FireGrenade, ElecGrenade
            if (throwableTypeIDs.Contains(item.TypeID))
            {
                return true;
            }
            
            // Method 2: Check by item name keywords (expanded list)
            var name = item.name?.ToLower() ?? "";
            var displayName = item.name ?? "";
            
            // Exclude flashlight explicitly
            if (name.Contains("flashlight"))
            {
                return false;
            }
            
            // Check for throwable keywords (expanded to include bombs, tubes, dynamite, etc.)
            string[] throwableKeywords = {
                "grenade", "手雷",
                "flash", "闪光",
                "smoke", "烟雾",
                "molotov", "燃烧瓶",
                "tox", "毒气",
                "elec", "电",
                "bomb", "炸弹", "管状", "集数", "罐装",
                "dynamite", "炸药", "集束",
                "tube", "canister", "can",
                "throwing", "投掷",
                "粪球", "feces", "dung",
                "explosive", "爆炸"
            };
            
            foreach (var keyword in throwableKeywords)
            {
                if (name.Contains(keyword.ToLower()) || displayName.Contains(keyword))
                {
                    return true;
                }
            }
            
            // Method 3: Check SkillType property (from log: "skillType is itemSkill")
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
            
            // Method 4: Try to check item properties/methods that indicate throwable capability
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
                            
                            Debug.Log($"[BTS] Slot {slotIndex}: {item.name} | TypeID: {item.TypeID} | Type: {itemType.Name} {throwableMark}");
                            
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
                                        propName.Contains("dynamite") || propName.Contains("skill") || propName.Contains("cast"))
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
                                var skillTypeProp = itemType.GetProperty("SkillType", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                                if (skillTypeProp != null)
                                {
                                    try
                                    {
                                        var skillType = skillTypeProp.GetValue(item);
                                        Debug.Log($"[BTS]   -> SkillType: {skillType}");
                                        if (skillType != null)
                                        {
                                            string skillTypeStr = skillType.ToString().ToLower();
                                            if (skillTypeStr.Contains("item") || skillTypeStr.Contains("throw"))
                                            {
                                                Debug.Log($"[BTS]   ⚠️ This item might be throwable based on SkillType!");
                                            }
                                        }
                                    }
                                    catch { }
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
            
            Debug.LogWarning("[BTS] Could not find player character! Using first CharacterMainControl found.");
            return allChars.Length > 0 ? allChars[0] : null;
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
