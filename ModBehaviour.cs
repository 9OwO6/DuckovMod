using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using ItemStatsSystem;
using Duckov.Modding;
using Duckov.UI.DialogueBubbles;
using Cysharp.Threading.Tasks;
using System.Threading;
using Debug = UnityEngine.Debug; // Alias to resolve conflict with System.Diagnostics.Debug

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
        
        /// <summary>
        /// Check if throw key is pressed (supports mouse side buttons)
        /// Unity mouse buttons: 0=Left, 1=Right, 2=Middle, 3=Mouse4, 4=Mouse5
        /// </summary>
        private bool IsThrowKeyDown()
        {
            int keyCodeInt = (int)throwKey;
            string keyName = throwKey.ToString();
            
            // First, check if it's a mouse button by name
            if (keyName.Contains("Mouse") || keyName.Contains("Button"))
            {
                // Try to extract button number from name
                if (keyName.Contains("Mouse0") || keyName.Contains("LeftButton")) return Input.GetMouseButtonDown(0);
                if (keyName.Contains("Mouse1") || keyName.Contains("RightButton")) return Input.GetMouseButtonDown(1);
                if (keyName.Contains("Mouse2") || keyName.Contains("MiddleButton")) return Input.GetMouseButtonDown(2);
                if (keyName.Contains("Mouse3") || keyName.Contains("Mouse4") || keyName.Contains("Button3")) return Input.GetMouseButtonDown(3);
                if (keyName.Contains("Mouse5") || keyName.Contains("Button4")) return Input.GetMouseButtonDown(4);
                if (keyName.Contains("Mouse6") || keyName.Contains("Button5")) return Input.GetMouseButtonDown(5);
                if (keyName.Contains("Mouse7") || keyName.Contains("Button6")) return Input.GetMouseButtonDown(6);
            }
            
            // Check by KeyCode integer value range
            // Unity KeyCode values: Mouse0=323, Mouse1=324, Mouse2=325, Mouse3=326, Mouse4=327, Mouse5=328
            if (keyCodeInt >= 323 && keyCodeInt <= 330)
            {
                int buttonIndex = keyCodeInt - 323;
                if (buttonIndex >= 0 && buttonIndex <= 6)
                {
                    bool result = Input.GetMouseButtonDown(buttonIndex);
                    if (result) Debug.Log($"[BTS] 🖱️ Mouse button {buttonIndex} pressed (KeyCode: {keyCodeInt}, Name: {keyName})");
                    return result;
                }
            }
            
            // Also check direct mouse button indices (in case ModSetting passes a special value)
            // Check Mouse4 (button 3) and Mouse5 (button 4) specifically
            // But only if throwKey seems to be a mouse button
            if (keyCodeInt == 326 || keyCodeInt == 327 || keyCodeInt == 328) // Mouse3, Mouse4, Mouse5
            {
                int buttonIndex = keyCodeInt - 323;
                bool result = Input.GetMouseButtonDown(buttonIndex);
                if (result) Debug.Log($"[BTS] 🖱️ Mouse button {buttonIndex} pressed via KeyCode {keyCodeInt}");
                return result;
            }
            
            // Standard keyboard key check
            bool keyResult = Input.GetKeyDown(throwKey);
            if (keyResult && (keyName.Contains("Mouse") || keyCodeInt >= 323))
            {
                Debug.Log($"[BTS] ⚠️ Warning: KeyCode {keyCodeInt} ({keyName}) detected as keyboard key, but might be mouse button");
            }
            return keyResult;
        }
        
        /// <summary>
        /// Check if throw key is held (supports mouse side buttons)
        /// Unity mouse buttons: 0=Left, 1=Right, 2=Middle, 3=Mouse4, 4=Mouse5
        /// </summary>
        private bool IsThrowKeyHeld()
        {
            int keyCodeInt = (int)throwKey;
            string keyName = throwKey.ToString();
            
            // First, check if it's a mouse button by name
            if (keyName.Contains("Mouse") || keyName.Contains("Button"))
            {
                if (keyName.Contains("Mouse0") || keyName.Contains("LeftButton")) return Input.GetMouseButton(0);
                if (keyName.Contains("Mouse1") || keyName.Contains("RightButton")) return Input.GetMouseButton(1);
                if (keyName.Contains("Mouse2") || keyName.Contains("MiddleButton")) return Input.GetMouseButton(2);
                if (keyName.Contains("Mouse3") || keyName.Contains("Mouse4") || keyName.Contains("Button3")) return Input.GetMouseButton(3);
                if (keyName.Contains("Mouse5") || keyName.Contains("Button4")) return Input.GetMouseButton(4);
                if (keyName.Contains("Mouse6") || keyName.Contains("Button5")) return Input.GetMouseButton(5);
                if (keyName.Contains("Mouse7") || keyName.Contains("Button6")) return Input.GetMouseButton(6);
            }
            
            // Check by KeyCode integer value range
            if (keyCodeInt >= 323 && keyCodeInt <= 330)
            {
                int buttonIndex = keyCodeInt - 323;
                if (buttonIndex >= 0 && buttonIndex <= 6)
                {
                    return Input.GetMouseButton(buttonIndex);
                }
            }
            
            // Also check direct mouse button indices
            if (keyCodeInt == 326 || keyCodeInt == 327 || keyCodeInt == 328)
            {
                int buttonIndex = keyCodeInt - 323;
                return Input.GetMouseButton(buttonIndex);
            }
            
            // Standard keyboard key check
            return Input.GetKey(throwKey);
        }
        
        /// <summary>
        /// Check if throw key is released (supports mouse side buttons)
        /// Unity mouse buttons: 0=Left, 1=Right, 2=Middle, 3=Mouse4, 4=Mouse5
        /// </summary>
        private bool IsThrowKeyUp()
        {
            int keyCodeInt = (int)throwKey;
            string keyName = throwKey.ToString();
            
            // First, check if it's a mouse button by name
            if (keyName.Contains("Mouse") || keyName.Contains("Button"))
            {
                if (keyName.Contains("Mouse0") || keyName.Contains("LeftButton")) return Input.GetMouseButtonUp(0);
                if (keyName.Contains("Mouse1") || keyName.Contains("RightButton")) return Input.GetMouseButtonUp(1);
                if (keyName.Contains("Mouse2") || keyName.Contains("MiddleButton")) return Input.GetMouseButtonUp(2);
                if (keyName.Contains("Mouse3") || keyName.Contains("Mouse4") || keyName.Contains("Button3")) return Input.GetMouseButtonUp(3);
                if (keyName.Contains("Mouse5") || keyName.Contains("Button4")) return Input.GetMouseButtonUp(4);
                if (keyName.Contains("Mouse6") || keyName.Contains("Button5")) return Input.GetMouseButtonUp(5);
                if (keyName.Contains("Mouse7") || keyName.Contains("Button6")) return Input.GetMouseButtonUp(6);
            }
            
            // Check by KeyCode integer value range
            if (keyCodeInt >= 323 && keyCodeInt <= 330)
            {
                int buttonIndex = keyCodeInt - 323;
                if (buttonIndex >= 0 && buttonIndex <= 6)
                {
                    bool result = Input.GetMouseButtonUp(buttonIndex);
                    if (result) Debug.Log($"[BTS] 🖱️ Mouse button {buttonIndex} released (KeyCode: {keyCodeInt}, Name: {keyName})");
                    return result;
                }
            }
            
            // Also check direct mouse button indices
            if (keyCodeInt == 326 || keyCodeInt == 327 || keyCodeInt == 328)
            {
                int buttonIndex = keyCodeInt - 323;
                bool result = Input.GetMouseButtonUp(buttonIndex);
                if (result) Debug.Log($"[BTS] 🖱️ Mouse button {buttonIndex} released via KeyCode {keyCodeInt}");
                return result;
            }
            
            // Standard keyboard key check
            bool keyResult = Input.GetKeyUp(throwKey);
            if (keyResult) Debug.Log($"[BTS] ⌨️ Keyboard key released: {throwKey} (Int: {keyCodeInt}, Name: {keyName})");
            return keyResult;
        }
        
        // Language support
        private bool isChinese = false; // Whether game is running in Chinese
        
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
        
        // Long-press G selection mode (old scroll wheel mode)
        private bool isInSelectionMode = false;             // Whether we're in long-press selection mode (scroll wheel)
        private int selectionModeCurrentIndex = 0;          // Current selected throwable index in selection mode
        private float gKeyHoldTime = 0f;                    // Time G key has been held
        private const float G_KEY_LONG_PRESS_TIME = 0.3f;   // Time to hold G to enter selection mode (seconds)
        
        // Radial menu (wheel menu) system
        private bool useRadialMenu = true;                  // Enable radial menu instead of scroll wheel mode
        private bool isRadialMenuOpen = false;              // Whether radial menu is currently open
        
        // Performance mode settings (for users with low-end hardware)
        // Set to true for maximum performance (disables some features)
        private const bool PERFORMANCE_MODE = true;         // PERFORMANCE MODE: true = max performance, false = full features
        private const bool DISABLE_RADIAL_MENU = false;    // If true, completely disable radial menu (use old scroll mode)
        private const float PERFORMANCE_MONITOR_INTERVAL = 0.5f; // Monitor update interval in performance mode (0.5s)
        private const float PERFORMANCE_ITEM_CHECK_INTERVAL = 0.3f; // Item check interval in performance mode (0.3s)
        private GameObject? radialMenuCanvas = null;        // Canvas for radial menu UI
        private RectTransform? radialMenuContainer = null;  // Container for radial menu items
        private List<GameObject> radialMenuItems = new List<GameObject>(); // UI items in radial menu
        private int radialMenuSelectedIndex = -1;           // Currently selected item index (-1 = none)
        private const float RADIAL_MENU_RADIUS = 250f;     // Radius of radial menu in pixels (increased for better selection)
        private const float RADIAL_MENU_ITEM_SIZE = 80f;   // Size of each item icon in pixels
        private const float RADIAL_MENU_SELECTION_TOLERANCE = 150f; // Tolerance for selection (pixels from center)
        
        // For detecting throw completion (monitor item count change)
        private Dictionary<int, int> lastItemCounts = new Dictionary<int, int>(); // slot -> count
        
        // State tracking for throw detection
        private bool wasHoldingThrowable = false; // Track if we were holding a throwable in previous frame
        private bool isThrowingInProgress = false; // Track if throw animation is in progress
        private float throwStartTime = 0f; // Time when throw was detected to have started
        private const float MAX_THROW_DURATION = 2f; // Maximum throw duration (seconds) - fallback timeout
        private bool wasMouseButton0Down = false; // Track mouse left button state for throw detection
        
        // Performance optimization: Cache player and reflection methods
        private CharacterMainControl? cachedPlayer = null; // Cached player object
        private float lastPlayerCacheTime = 0f; // Last time player was cached
        private const float PLAYER_CACHE_REFRESH_INTERVAL = 2f; // Refresh player cache every 2 seconds (AGGRESSIVE OPTIMIZATION)
        private System.Reflection.MethodInfo? cachedGetItemMethod = null; // Cached GetItem method
        private Inventory? cachedInventory = null; // Cached inventory component
        private System.Reflection.MethodInfo? cachedGetCurrentHoldItemMethod = null; // Cached GetCurrentHoldItem method
        private System.Reflection.MethodInfo? cachedSwitchHoldAgentInSlotMethod = null; // Cached SwitchHoldAgentInSlot method for performance
        private float lastMonitorUpdateTime = 0f; // Last time MonitorThrowableItems was called
        private float GetMonitorUpdateInterval() => PERFORMANCE_MODE ? PERFORMANCE_MONITOR_INTERVAL : 0.3f; // Dynamic interval based on performance mode
        private float lastItemCheckTime = 0f; // Last time current item was checked
        private float GetItemCheckInterval() => PERFORMANCE_MODE ? PERFORMANCE_ITEM_CHECK_INTERVAL : 0.15f; // Dynamic interval based on performance mode
        private const bool ENABLE_MOUSE_TRACKING = false; // DISABLED: Mouse tracking causes severe FPS drops
        
        // PERFORMANCE: Cache IsThrowableItem results by TypeID to avoid repeated checks
        private Dictionary<int, bool> throwableItemCache = new Dictionary<int, bool>();
        private const bool ENABLE_IS_THROWABLE_DEBUG_LOGS = false; // Disable debug logs in IsThrowableItem for performance
        
        // PERFORMANCE: Cache last inventory scan result to avoid rescanning on every G press
        private float lastInventoryScanTime = 0f;
        private const float INVENTORY_SCAN_CACHE_DURATION = 0.5f; // Cache inventory scan for 0.5 seconds
        private bool inventoryScanCacheValid = false;
        
        // PERFORMANCE: Cache maxSlots to avoid repeated reflection calls
        private int? cachedMaxSlots = null;
        private System.Type? cachedInventoryType = null;
        
        // Performance profiling
        private const bool ENABLE_PERFORMANCE_PROFILING = true; // Enable detailed performance logging
        private const float PERFORMANCE_LOG_THRESHOLD_MS = 5f; // Log frames that take longer than this (ms)
        private Stopwatch frameStopwatch = new Stopwatch();
        private Dictionary<string, float> methodTimings = new Dictionary<string, float>();
        private int frameCount = 0;
        private const int PERFORMANCE_LOG_INTERVAL = 60; // Log performance summary every N frames
        private float lastPerformanceLogTime = 0f;
        private const float PERFORMANCE_LOG_SUMMARY_INTERVAL = 5f; // Log summary every 5 seconds
        
        // ModConfig Settings
        public enum ThrowMode { Equip, Throw } // "按G装备" or "按G投掷"
        
        // Configuration values (will be loaded from ModConfig later)
        private bool throwSoundEnabled = false; // 投掷音效开关 - 默认关闭
        private ThrowMode throwMode = ThrowMode.Equip; // 按G投掷/按G装备切换 - 默认"按G装备"
        // "当前快捷投掷物" - 用于"按G投掷"模式
        private int? currentQuickThrowableSlot = null; // 当前快捷投掷物的槽位
        private int? currentQuickThrowableTypeID = null; // 当前快捷投掷物的 TypeID
        private bool enableContinuousThrow = false; // 连续投掷开关 - 默认关闭（仅在按G装备模式下有效）
        private bool disableThrowPreparationTime = false; // 取消投掷物投掷准备时间（取消读条）- 默认关闭
        private Dictionary<int, bool> enabledThrowableTypeIDs = new Dictionary<int, bool>(); // 投掷物识别列表：TypeID -> 是否启用
        private bool enableWarmGrenades = false; // Impact detonation toggle - default off
        private readonly HashSet<int> warmGrenadeCandidateTypeIDs = new HashSet<int> { 67, 660, 66, 23, 24 }; // Candidate TypeIDs for warm grenade behavior
        private readonly HashSet<int> warmGrenadeDeferredZeroTypeIDs = new HashSet<int> { 23, 24, 66 }; // TypeIDs that require delayed fuse reduction to avoid premature explosion
        private readonly HashSet<int> warmGrenadeAppliedItemInstanceIDs = new HashSet<int>(); // Track which item instances already have warm settings applied
        private readonly Dictionary<int, List<WarmGrenadeObjectState>> warmGrenadeOriginalStates = new Dictionary<int, List<WarmGrenadeObjectState>>(); // Store original values for restoration
        private readonly Dictionary<int, WarmGrenadeDeferredInfo> warmGrenadeDeferredItems = new Dictionary<int, WarmGrenadeDeferredInfo>(); // Deferred fuse reduction tracking per item instance
        private readonly HashSet<int> warmGrenadeDetonatedInstanceIDs = new HashSet<int>(); // Track items already detonated via impact handler
        private static readonly string[] WarmGrenadeDiagnosticKeywords = { "fuse", "delay", "timer", "time", "deton", "explod", "impact" };
        private readonly HashSet<string> warmGrenadeDiagnosticsLoggedPhases = new HashSet<string>();
        private float lastWarmGrenadeCleanupTime = 0f;
        private const float WARM_GRENADE_CLEANUP_INTERVAL = 5f;
        private static readonly string[] WarmGrenadeBoolKeywords = { "impact", "collision", "collide", "contact", "instant", "immediate", "warm", "explode", "deton", "onhit" };
        private static readonly string[] WarmGrenadeZeroKeywords = { "fuse", "delay", "timer", "countdown", "deton", "explod", "ignite" };
        private static readonly string[] WarmGrenadeZeroExcludeKeywords = { "cooldown", "interval", "recovery", "reload", "regen" };
        private static readonly string[] WarmGrenadeExplosionMethodNames = { "Explode", "Detonate", "TriggerExplosion", "TriggerDetonation", "ExplodeNow", "ExplodeImmediate", "DoExplode", "OnExplode", "ExplodeInternal", "DetonateImmediate" };
        private static readonly Dictionary<int, string> throwableDisplayNames = new Dictionary<int, string>
        {
            { 67, "手雷 / Grenade" },
            { 660, "烟雾弹 / Smoke Grenade" },
            { 942, "电机手雷 / Electric Grenade" },
            { 941, "燃烧弹 / Fire Grenade" },
            { 933, "毒物弹 / Toxin Grenade" },
            { 66, "闪光手雷 / Flash Grenade" },
            { 23, "管状炸弹 / Dynamite" },
            { 24, "管状炸弹(多) / Dynamite Multiple" },
            { 1257, "粪球 / Shit Ball" }
        };
        private bool isRefreshingThrowableDropdown = false; // Prevent duplicate dropdown rebuilds
        private readonly HashSet<int> autoDetectedThrowableTypeIDs = new HashSet<int>();
        private static readonly string[] AutoDetectNameKeywords = { "grenade", "bomb", "flash", "smoke", "throw", "爆", "雷", "手雷", "炸弹", "烟雾", "闪光" };
        private static readonly string[] AutoDetectSkillKeywords = { "grenade", "bomb", "throw", "throwable" };
        private static readonly string[] AutoDetectComponentKeywords = { "grenade", "bomb", "explosion", "explosive", "throw", "impact", "projectile" };
        private static readonly string[] AutoDetectGrenadeKeywords = { "grenade", "bomb", "手雷", "炸弹", "雷" };
        private static readonly string[] AutoDetectDelayKeywords = { "delay", "fuse", "timer" };
        private static readonly BindingFlags AutoDetectBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        private class WarmGrenadeObjectState
        {
            public WeakReference<object> Target { get; }
            public List<(System.Reflection.PropertyInfo property, object? value)> PropertyValues { get; } = new List<(System.Reflection.PropertyInfo, object?)>();
            public List<(System.Reflection.FieldInfo field, object? value)> FieldValues { get; } = new List<(System.Reflection.FieldInfo, object?)>();
            
            public WarmGrenadeObjectState(object target)
            {
                Target = new WeakReference<object>(target);
            }
        }
        
        private class WarmGrenadeDeferredInfo
        {
            public Item Item { get; set; }
            public float StartTime { get; set; }

            public WarmGrenadeDeferredInfo(Item item, float startTime)
            {
                Item = item;
                StartTime = startTime;
            }
        }
        
        // ModConfig integration - settings will appear in game's Mod Settings tab

        private void Start()
        {
            Debug.Log("[BTS] =========================================");
            Debug.Log("[BTS] Mod loaded (Start called) - VERSION 2.3.0 (Impact Detonation + Performance Optimizations)");
            Debug.Log($"[BTS] Performance Mode: {(PERFORMANCE_MODE ? "ENABLED (Max Performance)" : "DISABLED (Full Features)")}");
            Debug.Log($"[BTS] Radial Menu: {(DISABLE_RADIAL_MENU ? "DISABLED" : "ENABLED")}");
            Debug.Log("[BTS] =========================================");
            
            // Initialize cached player early for better performance
            cachedPlayer = FindPlayerCharacter();
            if (cachedPlayer != null)
            {
                lastPlayerCacheTime = Time.time;
                cachedInventory = cachedPlayer.GetComponent<Inventory>() ?? cachedPlayer.GetComponentInChildren<Inventory>();
                if (cachedInventory != null)
                {
                    var inventoryType = cachedInventory.GetType();
                    cachedGetItemMethod = inventoryType.GetMethod(
                        "GetItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetItemAt",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                }
                Debug.Log("[BTS] Player cached for performance optimization");
            }
            
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

            // Detect game language
            DetectGameLanguage();
            
            // Initialize throwable recognition list (all enabled by default)
            InitializeThrowableRecognitionList();
            
            // Scan and print all registered items in ItemAssetsCollection
            // This helps find the correct TypeID for throwables
            ScanAllRegisteredItems();
            
        }
        
        /// <summary>
        /// Initialize throwable recognition list with all known throwable TypeIDs enabled by default
        /// </summary>
        private void InitializeThrowableRecognitionList()
        {
            // Ensure dictionary contains all known TypeIDs
            foreach (int typeID in throwableDisplayNames.Keys)
            {
                enabledThrowableTypeIDs[typeID] = true;
            }
            
            Debug.Log($"[BTS] Initialized throwable recognition list with {enabledThrowableTypeIDs.Count} items (all enabled by default)");
        }
        
        void OnEnable()
        {
            // Subscribe to ModManager events to detect when ModSetting is activated
            try
            {
                Duckov.Modding.ModManager.OnModActivated += OnModActivated;
                Duckov.Modding.ModManager.OnModWillBeDeactivated += OnModWillBeDeactivated;
                Debug.Log("[BTS] Subscribed to ModManager events");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to subscribe to ModManager events: {ex.Message}");
            }
            
            // Check ModSettingAPI availability (similar to RadialMenu's OnEnable)
            // RadialMenu checks: if (ModSettingAPI.IsInit) { RadialMenuModConfig.SetupModConfig(info, MOD_NAME); }
            Debug.Log($"[BTS] OnEnable: Checking ModSettingAPI - IsInit: {ModSettingAPI.IsInit}, info.name='{info.name}', info.displayName='{info.displayName}'");
            
            if (ModSettingAPI.IsInit)
            {
                Debug.Log("[BTS] OnEnable: ModSettingAPI is already initialized, attempting to register");
                TryInitializeModSetting();
            }
            else
            {
                Debug.Log("[BTS] OnEnable: ModSettingAPI not initialized yet, will wait for OnModActivated or OnAfterSetup");
            }
        }
        
        void OnDisable()
        {
            // Unsubscribe from ModManager events
            try
            {
                Duckov.Modding.ModManager.OnModActivated -= OnModActivated;
                Duckov.Modding.ModManager.OnModWillBeDeactivated -= OnModWillBeDeactivated;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to unsubscribe from ModManager events: {ex.Message}");
            }
            
            RestoreWarmGrenadeSettings();
            
            // Teardown ModSetting
            TeardownModSetting();
        }
        
        /// <summary>
        /// Called when another mod (e.g., ModSetting) is activated
        /// Similar to RadialMenu's OnModActivated method
        /// </summary>
        private void OnModActivated(Duckov.Modding.ModInfo modInfo, Duckov.Modding.ModBehaviour behaviour)
        {
            try
            {
                if (modInfo.Equals(default(Duckov.Modding.ModInfo)) || string.IsNullOrEmpty(modInfo.name))
                    return;

                Debug.Log($"[BTS] OnModActivated: Mod '{modInfo.name}' activated");
                
                // Check if ModSetting mod is activated
                if (modInfo.name == ModSettingAPI.MOD_NAME)
                {
                    Debug.Log("[BTS] Detected ModSetting mod activation, attempting to register settings");
                    // Wait a bit for our own info to be set (if it's not ready yet)
                    StartCoroutine(DelayedModSettingInit());
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] OnModActivated handler error: {ex.Message}");
                Debug.LogWarning($"[BTS] Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Delayed initialization after ModSetting mod is activated
        /// Waits for this.info to be set properly
        /// </summary>
        private System.Collections.IEnumerator DelayedModSettingInit()
        {
            // Wait a bit for our mod's info to be set
            yield return new WaitForSeconds(0.2f);
            
            // Try multiple times until info is set
            for (int i = 0; i < 10; i++)
            {
                if (!string.IsNullOrEmpty(info.name))
                {
                    Debug.Log($"[BTS] DelayedModSettingInit: ModInfo is now valid (name='{info.name}', displayName='{info.displayName}')");
                    TryInitializeModSetting();
                    yield break;
                }
                
                if (i == 0)
                {
                    Debug.Log($"[BTS] DelayedModSettingInit: Waiting for ModInfo to be set (current: name='{info.name}', displayName='{info.displayName}')...");
                }
                
                yield return new WaitForSeconds(0.2f);
            }
            
            Debug.LogWarning("[BTS] DelayedModSettingInit: ModInfo.name was never set, cannot initialize ModSetting");
        }
        
        /// <summary>
        /// Called when a mod is about to be deactivated
        /// Similar to RadialMenu's OnModWillBeDeactivated
        /// </summary>
        private void OnModWillBeDeactivated(Duckov.Modding.ModInfo modInfo, Duckov.Modding.ModBehaviour behaviour)
        {
            if (modInfo.name != ModSettingAPI.MOD_NAME)
                return;
            
            // Only teardown if ModSetting is actually initialized
            // This prevents teardown when ModSetting hasn't been set up yet
            if (!ModSettingAPI.IsInit)
            {
                Debug.Log("[BTS] ModSetting is being deactivated but it's not initialized, skipping teardown");
                return;
            }
            
            Debug.Log("[BTS] ModSetting mod is being deactivated, removing our settings");
            // When ModSetting is disabled, remove our settings
            TeardownModSetting();
        }
        
        /// <summary>
        /// Called after Setup (if ModSetting was enabled before this mod)
        /// Similar to RadialMenu's OnAfterSetup method
        /// RadialMenu uses: if (ModSettingAPI.Init(info)) { RadialMenuModConfig.SetupModConfig(info, MOD_NAME); }
        /// </summary>
        protected override void OnAfterSetup()
        {
            base.OnAfterSetup();
            Debug.Log($"[BTS] OnAfterSetup called - info.name='{info.name}', info.displayName='{info.displayName}'");
            
            // Check if info is valid (at this point, info should be set by the game)
            if (string.IsNullOrEmpty(info.name))
            {
                Debug.LogWarning("[BTS] OnAfterSetup: ModInfo.name is still empty, ModSetting initialization will be deferred");
                // Start a coroutine to retry later
                StartCoroutine(RetryModSettingInit());
                return;
            }
            
            Debug.Log("[BTS] OnAfterSetup: ModInfo is valid, attempting to initialize ModSetting");
            
            // Check if ModSettingAPI is available (similar to RadialMenu)
            // RadialMenu does: if (ModSettingAPI.Init(info)) { RadialMenuModConfig.SetupModConfig(info, MOD_NAME); }
            if (ModSettingAPI.Init(info))
            {
                Debug.Log("[BTS] OnAfterSetup: ModSettingAPI.Init(info) succeeded, setting up ModSetting");
                SetupModSetting();
                LoadModSettingValues();
                _modSettingSetup = true;
                Debug.Log("[BTS] OnAfterSetup: ModSetting setup completed successfully!");
            }
            else
            {
                Debug.Log("[BTS] OnAfterSetup: ModSettingAPI.Init(info) failed, ModSetting not available yet");
            }
        }
        
        /// <summary>
        /// Retry ModSetting initialization if info was not ready initially
        /// </summary>
        private System.Collections.IEnumerator RetryModSettingInit()
        {
            // Wait a bit for info to be set
            yield return new WaitForSeconds(0.5f);
            
            for (int i = 0; i < 5; i++)
            {
                if (!string.IsNullOrEmpty(info.name))
                {
                    Debug.Log($"[BTS] RetryModSettingInit: ModInfo is now valid (name='{info.name}'), attempting initialization");
                    if (ModSettingAPI.Init(info))
                    {
                        SetupModSetting();
                        LoadModSettingValues();
                        _modSettingSetup = true;
                        Debug.Log("[BTS] RetryModSettingInit: ModSetting setup completed successfully!");
                        yield break;
                    }
                }
                yield return new WaitForSeconds(0.3f);
            }
            
            Debug.LogWarning("[BTS] RetryModSettingInit: Failed to initialize ModSetting after retries");
        }
        
        /// <summary>
        /// Called before deactivation
        /// Similar to RadialMenu's OnBeforeDeactivate method
        /// </summary>
        protected override void OnBeforeDeactivate()
        {
            base.OnBeforeDeactivate();
            TeardownModSetting();
        }
        
        /// <summary>
        /// Load settings from ModConfig using OptionsManager_Mod.Load<T>
        /// According to documentation: Configuration values are read through OptionsManager_Mod.Load<T>(string key, T defaultV)
        /// </summary>
        private void LoadModConfigSettings()
        {
            try
            {
                // Find OptionsManager_Mod type
                System.Type? optionsManagerType = null;
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        optionsManagerType = assembly.GetType("OptionsManager_Mod");
                        if (optionsManagerType != null)
                        {
                            Debug.Log($"[BTS] Found OptionsManager_Mod in assembly: {assembly.GetName().Name}");
                            break;
                        }
                    }
                    catch { }
                }
                
                if (optionsManagerType == null)
                {
                    Debug.LogWarning("[BTS] OptionsManager_Mod not found, using default settings");
                    return;
                }
                
                // Get Load<T> method
                var loadMethod = optionsManagerType.GetMethod("Load", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (loadMethod == null)
                {
                    Debug.LogWarning("[BTS] OptionsManager_Mod.Load method not found");
                    return;
                }
                
                // Load settings using generic method
                var loadGeneric = loadMethod.MakeGenericMethod(typeof(bool));
                var throwSoundValue = loadGeneric.Invoke(null, new object[] { "BetterThrowingSystem.ThrowSoundEnabled", throwSoundEnabled });
                if (throwSoundValue != null)
                {
                    throwSoundEnabled = (bool)throwSoundValue;
                    Debug.Log($"[BTS] Loaded ThrowSoundEnabled from ModConfig: {throwSoundEnabled}");
                }
                
                // Note: Enum and other types would need similar handling
                // For now, we'll just load the basic settings
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to load settings from ModConfig: {ex.Message}");
            }
        }

        private void Update()
        {
            // PERFORMANCE PROFILING: Start frame timing
            frameStopwatch.Restart();
            float frameStartTime = Time.realtimeSinceStartup;
            
            // Track method timings
            Stopwatch methodStopwatch = new Stopwatch();
            
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

            // Debug: Log mouse button states for side button detection
            // Check all mouse buttons (0-6) to detect side buttons
            for (int i = 0; i <= 6; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    Debug.Log($"[BTS] 🖱️ Mouse button {i} detected (ButtonDown) - Current throwKey: {throwKey} (Int: {(int)throwKey}, Name: {throwKey.ToString()})");
                    // If this mouse button matches throwKey, log it
                    int keyCodeInt = (int)throwKey;
                    if (keyCodeInt >= 323 && keyCodeInt <= 330)
                    {
                        int buttonIndex = keyCodeInt - 323;
                        if (buttonIndex == i)
                        {
                            Debug.Log($"[BTS] 🖱️ ✓ Mouse button {i} matches throwKey! (KeyCode: {keyCodeInt})");
                        }
                    }
                }
            }

            // Process pending warm grenade deferred items (e.g., dynamite, flashbang)
            ProcessWarmGrenadeDeferredItems();
            
            // ② G key: Handle both quick press and long-press selection mode
            Stopwatch gKeyStopwatch = new Stopwatch();
            gKeyStopwatch.Restart();
            
            if (IsThrowKeyHeld())
            {
                // Throw key is held down
                gKeyHoldTime += Time.deltaTime;
                
                // Check if we should enter selection mode
                if (gKeyHoldTime >= G_KEY_LONG_PRESS_TIME)
                {
                    // PERFORMANCE: Check if radial menu is disabled
                    if (useRadialMenu && !DISABLE_RADIAL_MENU)
                    {
                        // Use radial menu system
                        if (!isRadialMenuOpen)
                        {
                            Stopwatch openMenuStopwatch = new Stopwatch();
                            openMenuStopwatch.Restart();
                            OpenRadialMenu();
                            openMenuStopwatch.Stop();
                            if (ENABLE_PERFORMANCE_PROFILING && openMenuStopwatch.ElapsedMilliseconds > 1)
                            {
                                RecordMethodTiming("OpenRadialMenu", openMenuStopwatch.ElapsedMilliseconds);
                                UnityEngine.Debug.Log($"[BTS] OpenRadialMenu took {openMenuStopwatch.ElapsedMilliseconds}ms");
                            }
                        }
                        else
                        {
                            // Handle mouse scroll wheel for changing selection (no rotation, just highlight)
                            // PERFORMANCE: Mouse tracking disabled - only use scroll wheel
                            // PERFORMANCE: Only check scroll input when menu is open
                            if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > 0.001f)
                            {
                                Stopwatch scrollStopwatch = new Stopwatch();
                                scrollStopwatch.Restart();
                                HandleRadialMenuScroll();
                                scrollStopwatch.Stop();
                                if (ENABLE_PERFORMANCE_PROFILING && scrollStopwatch.ElapsedMilliseconds > 1)
                                {
                                    RecordMethodTiming("HandleRadialMenuScroll", scrollStopwatch.ElapsedMilliseconds);
                                }
                            }
                            
                            // DISABLED: Mouse tracking causes severe FPS drops (100+ FPS loss reported)
                            // Users can use scroll wheel to select items instead
                            // if (ENABLE_MOUSE_TRACKING)
                            // {
                            //     UpdateRadialMenuSelection();
                            // }
                        }
                    }
                    else
                    {
                        // Use old scroll wheel mode
                        if (!isInSelectionMode)
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
                                ShowDebugBubble(isChinese ? "❌ 背包中没有投掷物" : "❌ No throwables in inventory");
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
                }
            }
            else if (IsThrowKeyUp())
            {
                // Throw key was released
                
                // IMPORTANT: Save gKeyHoldTime BEFORE resetting it, so we can check if it was a quick press or long press
                float holdTimeBeforeReset = gKeyHoldTime;
                bool wasLongPress = holdTimeBeforeReset >= G_KEY_LONG_PRESS_TIME;
                
                // Reset gKeyHoldTime now
                gKeyHoldTime = 0f;
                
                if (useRadialMenu && isRadialMenuOpen)
                {
                    // Long press: Close radial menu and throw selected item (in Throw mode) or equip it (in Equip mode)
                    CloseRadialMenuAndEquip();
                }
                else if (isInSelectionMode)
                {
                    // Long press (scroll wheel mode): Exit selection mode and throw selected item (in Throw mode) or equip it (in Equip mode)
                    ExitSelectionModeAndEquip();
                }
                else if (!wasLongPress && holdTimeBeforeReset > 0f)
                {
                    // Quick press - directly throw current quick throwable (Throw mode) or cycle to next (Equip mode)
                    Stopwatch quickGStopwatch = new Stopwatch();
                    quickGStopwatch.Restart();
                    
                    Debug.Log("[BTS] =========================================");
                    Debug.Log($"[BTS] ========== G KEY PRESSED (QUICK) - Mode: {throwMode} ==========");
                    Debug.Log($"[BTS] Hold time: {holdTimeBeforeReset}s");
                    Debug.Log("[BTS] =========================================");
                    
                    Stopwatch findPlayerStopwatch = new Stopwatch();
                    findPlayerStopwatch.Restart();
                    var playerForGKey = FindPlayerCharacter();
                    findPlayerStopwatch.Stop();
                    if (ENABLE_PERFORMANCE_PROFILING && findPlayerStopwatch.ElapsedMilliseconds > 1)
                    {
                        RecordMethodTiming("FindPlayerCharacter", findPlayerStopwatch.ElapsedMilliseconds);
                        UnityEngine.Debug.Log($"[BTS] ⚠️ FindPlayerCharacter took {findPlayerStopwatch.ElapsedMilliseconds}ms");
                    }
                    
                    if (playerForGKey == null)
                    {
                        Debug.LogError("[BTS] ❌ CRITICAL: Player not found! Cannot proceed.");
                        ShowDebugBubble(isChinese ? "❌ 错误：找不到玩家角色" : "❌ Error: Player character not found");
                        return;
                    }
                    
                    Debug.Log($"[BTS] Player found: {playerForGKey.gameObject.name}");
                    
                    // In Throw mode, always throw directly (whether quick press or long press selection)
                    if (throwMode == ThrowMode.Throw)
                    {
                        // Throw mode: directly throw current quick throwable to mouse position
                        Debug.Log("[BTS] 🎯 Throw Mode: Quick press - throwing current quick throwable to mouse position");
                        ThrowToMousePosition(playerForGKey);
                    }
                    else
                    {
                        // Equip mode: use normal cycle logic
                        // IMPORTANT: Save current weapon BEFORE switching to throwable
                        // This must happen BEFORE CycleToNextThrowable, which might change the current item
                        Stopwatch saveSlotStopwatch = new Stopwatch();
                        saveSlotStopwatch.Restart();
                        SaveCurrentEquippedSlot(playerForGKey);
                        saveSlotStopwatch.Stop();
                        if (ENABLE_PERFORMANCE_PROFILING && saveSlotStopwatch.ElapsedMilliseconds > 1)
                        {
                            RecordMethodTiming("SaveCurrentEquippedSlot", saveSlotStopwatch.ElapsedMilliseconds);
                        }
                        
                        Stopwatch cycleStopwatch = new Stopwatch();
                        cycleStopwatch.Restart();
                        CycleToNextThrowable();
                        cycleStopwatch.Stop();
                        if (ENABLE_PERFORMANCE_PROFILING && cycleStopwatch.ElapsedMilliseconds > 1)
                        {
                            RecordMethodTiming("CycleToNextThrowable", cycleStopwatch.ElapsedMilliseconds);
                            UnityEngine.Debug.Log($"[BTS] ⚠️ CycleToNextThrowable took {cycleStopwatch.ElapsedMilliseconds}ms");
                        }
                    }
                    
                    quickGStopwatch.Stop();
                    if (ENABLE_PERFORMANCE_PROFILING && quickGStopwatch.ElapsedMilliseconds > 5)
                    {
                        UnityEngine.Debug.Log($"[BTS] ⚠️ Quick G press total took {quickGStopwatch.ElapsedMilliseconds}ms");
                    }
                    
                    // Mark that last action was G key (for detecting continuous G presses)
                    lastActionWasGKey = true;
                    lastActionWasWeaponSwitch = false;
                }
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
            
            // PERFORMANCE: Aggressively reduce player lookup frequency
            float currentTime = Time.time;
            // CRITICAL PERFORMANCE FIX: Only refresh player cache if it's null or very old
            // This prevents FindObjectsOfType from being called every frame (causing 100+ FPS drops)
            bool shouldRefreshPlayer = cachedPlayer == null || 
                                      cachedPlayer.gameObject == null || 
                                      !cachedPlayer.gameObject.activeInHierarchy || 
                                      (currentTime - lastPlayerCacheTime > PLAYER_CACHE_REFRESH_INTERVAL);
            
            if (shouldRefreshPlayer)
            {
                Stopwatch refreshPlayerStopwatch = new Stopwatch();
                refreshPlayerStopwatch.Restart();
                cachedPlayer = FindPlayerCharacter();
                refreshPlayerStopwatch.Stop();
                if (ENABLE_PERFORMANCE_PROFILING && refreshPlayerStopwatch.ElapsedMilliseconds > 1)
                {
                    RecordMethodTiming("RefreshPlayerCache", refreshPlayerStopwatch.ElapsedMilliseconds);
                    if (refreshPlayerStopwatch.ElapsedMilliseconds > 5)
                    {
                        UnityEngine.Debug.Log($"[BTS] ⚠️ RefreshPlayerCache took {refreshPlayerStopwatch.ElapsedMilliseconds}ms");
                    }
                }
                lastPlayerCacheTime = currentTime;
                
                // Cache inventory and GetItem method when player is found
                if (cachedPlayer != null)
                {
                    cachedInventory = cachedPlayer.GetComponent<Inventory>() ?? cachedPlayer.GetComponentInChildren<Inventory>();
                    if (cachedInventory != null)
                    {
                        var inventoryType = cachedInventory.GetType();
                        cachedGetItemMethod = inventoryType.GetMethod(
                            "GetItem",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        ) ?? inventoryType.GetMethod(
                            "GetItemAt",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                        );
                    }
                    
                    // Cache GetCurrentHoldItem method
                    var playerType = cachedPlayer.GetType();
                    var currentHoldItemAgentProp = playerType.GetProperty("CurrentHoldItemAgent",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (currentHoldItemAgentProp != null)
                    {
                        var agentType = currentHoldItemAgentProp.PropertyType;
                        var itemProp = agentType.GetProperty("Item",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        // Note: We'll use the property directly in GetCurrentHoldItemCached
                    }
                }
            }
            
            var player = cachedPlayer;
            
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
                // PERFORMANCE: Aggressively reduce reflection calls
                // Use dynamic interval based on performance mode
                Item? currentItem = null;
                float itemCheckInterval = GetItemCheckInterval(); // Dynamic based on performance mode
                bool justChecked = false;
                if (currentTime - lastItemCheckTime >= itemCheckInterval)
                {
                    Stopwatch getItemStopwatch = new Stopwatch();
                    getItemStopwatch.Restart();
                    currentItem = GetCurrentHoldItem(player);
                    getItemStopwatch.Stop();
                    if (ENABLE_PERFORMANCE_PROFILING && getItemStopwatch.ElapsedMilliseconds > 1)
                    {
                        RecordMethodTiming("GetCurrentHoldItem", getItemStopwatch.ElapsedMilliseconds);
                        if (getItemStopwatch.ElapsedMilliseconds > 3)
                        {
                            UnityEngine.Debug.Log($"[BTS] ⚠️ GetCurrentHoldItem took {getItemStopwatch.ElapsedMilliseconds}ms");
                        }
                    }
                    lastItemCheckTime = currentTime;
                    justChecked = true;
                }
                
                // Determine state based on currentItem or cached state
                bool isHoldingThrowable;
                bool isEmptyHand;
                if (justChecked)
                {
                    // We just checked - use actual result
                    isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
                    isEmptyHand = currentItem == null;
                }
                else
                {
                    // Use cached state if we skipped the check
                    // This is safe because we check frequently enough (6.7 times per second)
                    isHoldingThrowable = wasHoldingThrowable;
                    isEmptyHand = !wasHoldingThrowable;
                }
                
                // Track when we start holding throwable
                if (isHoldingThrowable && !wasHoldingThrowable)
                {
                    Debug.Log($"[BTS] 📌 Started holding throwable: {currentItem?.name ?? "null"} (Slot: {lastEquippedThrowableSlot.Value})");
                    Debug.Log($"[BTS] Previous weapon info - Slot: {previousEquippedSlot}, Key: {previousEquippedKey}");
                }
                
                if (enableWarmGrenades && isHoldingThrowable)
                {
                    Item? warmItem = currentItem;
                    if (warmItem == null)
                    {
                        warmItem = GetCurrentHoldItem(player);
                    }
                    
                    if (warmItem != null)
                    {
                        TryApplyWarmGrenadeSettings(warmItem);
                    }
                }
                
                // PERFORMANCE: Only check mouse buttons when we're actually tracking throwable
                // Check every frame is acceptable here since we're already in tracking mode
                bool isMouseButton0Down = Input.GetMouseButton(0); // Left mouse button
                bool isMouseButton1Down = Input.GetMouseButton(1); // Right mouse button
                
                // Handle "Disable Throw Preparation Time" feature
                // When left mouse button is held down and we're holding a throwable, continuously try to skip preparation time
                // This is needed because the preparation time might be updated every frame
                // IMPORTANT: In Throw mode, this should always be enabled and work even without mouse button pressed
                if (disableThrowPreparationTime && isHoldingThrowable)
                {
                    // In Throw mode, skip preparation time even without mouse button pressed
                    // In Equip mode, only skip when mouse button is held
                    bool shouldSkip = (throwMode == ThrowMode.Throw) || isMouseButton0Down;
                    
                    if (shouldSkip)
                    {
                        // Continuously try to skip throw preparation time
                        // Get current item if not already cached
                        Item? itemForSkip = currentItem;
                        if (itemForSkip == null && justChecked == false)
                        {
                            // We need to get the item to skip preparation time
                            itemForSkip = GetCurrentHoldItem(player);
                        }
                        
                        if (itemForSkip != null && IsThrowableItem(itemForSkip))
                        {
                            // Only log on first press to avoid spam (or in Throw mode, log once)
                            if (throwMode == ThrowMode.Throw || !wasMouseButton0Down)
                            {
                                Debug.Log($"[BTS] 🚀 Starting continuous skip attempt for: {itemForSkip.name} (Mode: {throwMode})");
                            }
                            
                            TrySkipThrowPreparationTime(itemForSkip, player);
                        }
                    }
                }
                
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
                // PERFORMANCE: Only check mouse button state periodically, not every frame
                if (currentTime - lastItemCheckTime >= 0.1f) // Check every 0.1s instead of every frame
                {
                    wasMouseButton0Down = Input.GetMouseButton(0);
                }
            }
            
            // Monitor throwable items to detect throw completion (backup detection via item count)
            // Only update periodically to reduce performance impact
            if (currentTime - lastMonitorUpdateTime >= GetMonitorUpdateInterval())
            {
                methodStopwatch.Restart();
                MonitorThrowableItems();
                methodStopwatch.Stop();
                if (ENABLE_PERFORMANCE_PROFILING && methodStopwatch.ElapsedMilliseconds > 1)
                {
                    RecordMethodTiming("MonitorThrowableItems", methodStopwatch.ElapsedMilliseconds);
                }
                lastMonitorUpdateTime = currentTime;
            }
            
            if (enableWarmGrenades && (currentTime - lastWarmGrenadeCleanupTime) >= WARM_GRENADE_CLEANUP_INTERVAL)
            {
                CleanupWarmGrenadeStates();
                lastWarmGrenadeCleanupTime = currentTime;
            }
            
            // PERFORMANCE PROFILING: End frame timing
            frameStopwatch.Stop();
            float frameEndTime = Time.realtimeSinceStartup;
            float frameTimeMs = (frameEndTime - frameStartTime) * 1000f;
            frameCount++;
            
            // Log heavy frames
            if (ENABLE_PERFORMANCE_PROFILING)
            {
                if (frameTimeMs > PERFORMANCE_LOG_THRESHOLD_MS)
                {
                    UnityEngine.Debug.Log($"[BTS] ⚠️ Heavy frame detected: {frameTimeMs:F2}ms (Frame #{frameCount})");
                }
                
                // Log performance summary periodically
                if (currentTime - lastPerformanceLogTime >= PERFORMANCE_LOG_SUMMARY_INTERVAL)
                {
                    LogPerformanceSummary();
                    lastPerformanceLogTime = currentTime;
                }
            }
        }
        
        /// <summary>
        /// Record method execution time for performance analysis
        /// </summary>
        private void RecordMethodTiming(string methodName, long milliseconds)
        {
            if (!methodTimings.ContainsKey(methodName))
            {
                methodTimings[methodName] = 0f;
            }
            methodTimings[methodName] += milliseconds;
        }
        
        /// <summary>
        /// Log performance summary to help identify bottlenecks
        /// </summary>
        private void LogPerformanceSummary()
        {
            if (methodTimings.Count == 0) return;
            
            UnityEngine.Debug.Log($"[BTS] ========== Performance Summary (Last {PERFORMANCE_LOG_SUMMARY_INTERVAL}s, {frameCount} frames) ==========");
            
            var sortedMethods = methodTimings.OrderByDescending(kvp => kvp.Value).Take(10);
            foreach (var kvp in sortedMethods)
            {
                float avgTime = kvp.Value / frameCount;
                UnityEngine.Debug.Log($"[BTS] {kvp.Key}: {kvp.Value:F2}ms total, {avgTime:F3}ms avg per frame");
            }
            
            UnityEngine.Debug.Log($"[BTS] =============================================================");
            
            // Reset for next period
            methodTimings.Clear();
            frameCount = 0;
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
        /// PERFORMANCE: This method scans ALL inventory slots - monitor for performance issues
        /// </summary>
        private void ScanPlayerInventoryForThrowables()
        {
            // PERFORMANCE: Use cache if available and recent (within 0.5 seconds)
            float currentTime = Time.time;
            if (inventoryScanCacheValid && (currentTime - lastInventoryScanTime) < INVENTORY_SCAN_CACHE_DURATION)
            {
                // Cache is valid - skip rescanning
                return;
            }
            
            throwableSlotsByTypeID.Clear();
            throwableTypeIDsInOrder.Clear();
            lastItemCounts.Clear();
            
            try
            {
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogWarning("[BTS] Player not found, cannot scan inventory slots.");
                    inventoryScanCacheValid = false;
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
                    inventoryScanCacheValid = false;
                    return;
                }
                
                var inventoryType = inventory.GetType();
                
                // PERFORMANCE: Use cached GetItem method if available
                var getItemMethod = cachedGetItemMethod;
                if (getItemMethod == null)
                {
                    // Try to get GetItem method
                    getItemMethod = inventoryType.GetMethod(
                        "GetItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetItemAt",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetSlotItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                    cachedGetItemMethod = getItemMethod; // Cache for next time
                }
                
                if (getItemMethod == null)
                {
                    Debug.LogError("[BTS] Could not find method to get item from inventory slot!");
                    inventoryScanCacheValid = false;
                    return;
                }
                
                // PERFORMANCE: Use cached maxSlots to avoid repeated reflection
                int maxSlots = 47; // Default
                if (cachedInventoryType == inventoryType && cachedMaxSlots.HasValue)
                {
                    maxSlots = cachedMaxSlots.Value;
                }
                else
                {
                    // Try to get max slots
                    var maxSlotsProp = inventoryType.GetProperty("maxSlots", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var slotCountProp = inventoryType.GetProperty("SlotCount", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var capacityProp = inventoryType.GetProperty("Capacity", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    var sizeProp = inventoryType.GetProperty("Size", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    
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
                    
                    cachedMaxSlots = maxSlots;
                    cachedInventoryType = inventoryType;
                }
                
                // PERFORMANCE: Only log in debug mode
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.Log($"[BTS] Scanning ALL inventory slots 0-{maxSlots - 1} for throwables (grouped by TypeID)...");
                }
                
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
                            
                            if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                            {
                                Debug.Log($"[BTS] Found throwable in slot {slotIndex}: {item.name} (TypeID: {typeID})");
                            }
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
                
                // Mark cache as valid
                lastInventoryScanTime = currentTime;
                inventoryScanCacheValid = true;
                
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.Log($"[BTS] Scanned inventory: Found {totalCount} throwable item(s) in {throwableTypeIDsInOrder.Count} category/categories: [{string.Join(", ", throwableTypeIDsInOrder.Select(id => $"TypeID {id}({throwableSlotsByTypeID[id].Count} slots)"))}]");
                }
                
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
                var throwablesByTypeID = new Dictionary<int, (int slot, string name, Sprite icon)>();
                
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
                                // Get localized name from Item object (better source)
                                string itemName = GetLocalizedItemName(item);
                                Sprite icon = null;
                                
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
        private void ShowSelectionModeBubble((int slot, int typeID, string name, Sprite icon) throwable)
        {
            try
            {
                var player = FindPlayerCharacter();
                Transform? target = player?.transform ?? Camera.main?.transform;
                
                if (target == null) return;
                
                // Format bubble text (no icon indicator since emojis don't display properly)
                // throwable.name is already localized from GetAllThrowablesByCategory()
                string bubbleText = isChinese 
                    ? $"投掷物选择中：{throwable.name}"
                    : $"Selecting throwable: {throwable.name}";
                
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
                
                // Update current quick throwable
                currentQuickThrowableSlot = selected.slot;
                currentQuickThrowableTypeID = selected.typeID;
                Debug.Log($"[BTS] ✓ Updated current quick throwable: Slot {currentQuickThrowableSlot}, TypeID {currentQuickThrowableTypeID}");
                
                // Update category index
                if (throwableTypeIDsInOrder.Contains(selected.typeID))
                {
                    currentCategoryIndex = throwableTypeIDsInOrder.IndexOf(selected.typeID);
                }
                
                // In Throw mode, directly throw the selected item to mouse position
                // In Equip mode, just equip it
                if (throwMode == ThrowMode.Throw)
                {
                    // Throw mode: directly throw to mouse position
                    Debug.Log($"[BTS] 🎯 Throw Mode: Long press selection (scroll wheel) - throwing {selected.name} to mouse position");
                    ThrowToMousePosition(player);
                }
                else
                {
                    // Equip mode: just equip the selected throwable
                    if (SwitchToSlot(selected.slot))
                    {
                        lastEquippedThrowableSlot = selected.slot;
                        lastSelectedThrowableSlot = selected.slot;
                        lastSelectedThrowableTypeID = selected.typeID;
                        
                        // Show confirmation bubble (selected.name is already localized from GetAllThrowablesByCategory)
                        string message = isChinese 
                            ? $"✓ 已选择：{selected.name}"
                            : $"✓ Selected: {selected.name}";
                        ShowDebugBubble(message);
                        
                        Debug.Log($"[BTS] ✓ Successfully equipped selected throwable category: {selected.name}");
                    }
                    else
                    {
                        string message = isChinese 
                            ? $"❌ 无法装备：{selected.name}"
                            : $"❌ Cannot equip: {selected.name}";
                        ShowDebugBubble(message);
                        Debug.LogError($"[BTS] Failed to equip selected throwable: {selected.name}");
                    }
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
            // PERFORMANCE: This method now uses caching to avoid rescanning on rapid G presses
            Stopwatch scanInventoryStopwatch = new Stopwatch();
            scanInventoryStopwatch.Restart();
            ScanPlayerInventoryForThrowables();
            scanInventoryStopwatch.Stop();
            if (ENABLE_PERFORMANCE_PROFILING && scanInventoryStopwatch.ElapsedMilliseconds > 1)
            {
                RecordMethodTiming("ScanPlayerInventoryForThrowables", scanInventoryStopwatch.ElapsedMilliseconds);
                if (scanInventoryStopwatch.ElapsedMilliseconds > 10)
                {
                    UnityEngine.Debug.Log($"[BTS] ⚠️⚠️ ScanPlayerInventoryForThrowables took {scanInventoryStopwatch.ElapsedMilliseconds}ms - THIS IS A BOTTLENECK!");
                }
            }
            
            if (throwableTypeIDsInOrder.Count == 0)
            {
                Debug.LogWarning("[BTS] No throwable items found in inventory slots!");
                ShowDebugBubble(isChinese ? "❌ 背包中没有投掷物" : "❌ No throwables in inventory");
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
            
            // NEW LOGIC: Smart throwable selection based on completion and availability
            // Rule 1: If throw completed AND continuous G key (last action was G), check if last selected item still exists
            //   - If last selected item still exists in inventory, continue using it (until it runs out)
            //   - If last selected item no longer exists, move to next category
            // Rule 2: If throw NOT completed OR just completed and back to weapon, restore last selection if it exists
            // Rule 3: If last action was weapon switch, restore last selection (user canceled throw)
            // Rule 4: If no last selection, start from first category
            
            bool isContinuousG = lastActionWasGKey && !lastActionWasWeaponSwitch;
            bool shouldSwitchCategory = hasCompletedThrow && isContinuousG;
            
            // Check current equipped item to see if we're holding a weapon (not throwable)
            var currentItem = GetCurrentHoldItem(player);
            bool isCurrentlyHoldingWeapon = currentItem != null && !IsThrowableItem(currentItem);
            bool isCurrentlyHoldingNothing = currentItem == null;
            bool isBackToWeaponState = (isCurrentlyHoldingWeapon || isCurrentlyHoldingNothing) && hasCompletedThrow;
            
            Debug.Log($"[BTS] Selection logic - hasCompletedThrow: {hasCompletedThrow}, isContinuousG: {isContinuousG}, shouldSwitchCategory: {shouldSwitchCategory}, lastActionWasWeaponSwitch: {lastActionWasWeaponSwitch}");
            Debug.Log($"[BTS] Current state - HoldingWeapon: {isCurrentlyHoldingWeapon}, HoldingNothing: {isCurrentlyHoldingNothing}, BackToWeapon: {isBackToWeaponState}");
            
            // Check if last selected item still exists in inventory
            bool lastSelectedItemStillExists = false;
            if (lastSelectedThrowableSlot.HasValue && lastSelectedThrowableTypeID.HasValue)
            {
                int lastTypeID = lastSelectedThrowableTypeID.Value;
                int lastSlot = lastSelectedThrowableSlot.Value;
                
                if (throwableSlotsByTypeID.ContainsKey(lastTypeID) && 
                    throwableSlotsByTypeID[lastTypeID].Contains(lastSlot))
                {
                    lastSelectedItemStillExists = true;
                    Debug.Log($"[BTS] Last selected item (TypeID: {lastTypeID}, Slot: {lastSlot}) still exists in inventory");
                }
                else
                {
                    Debug.Log($"[BTS] Last selected item (TypeID: {lastTypeID}, Slot: {lastSlot}) no longer exists in inventory");
                }
            }
            
            // Special case: If we just completed throw and are back to weapon state, and last selected item still exists,
            // restore it (this handles "long press G select, then quick press G to use again" scenario)
            if (isBackToWeaponState && lastSelectedItemStillExists && lastSelectedThrowableSlot.HasValue && lastSelectedThrowableTypeID.HasValue)
            {
                // We're back to weapon after throw, and the last selected item still exists - restore it
                targetSlot = lastSelectedThrowableSlot.Value;
                targetTypeID = lastSelectedThrowableTypeID.Value;
                currentQuickThrowableSlot = targetSlot;
                currentQuickThrowableTypeID = targetTypeID;
                
                currentCategoryIndex = throwableTypeIDsInOrder.IndexOf(targetTypeID);
                if (currentCategoryIndex < 0) currentCategoryIndex = 0;
                
                categoryInfo = $"[继续使用]";
                Debug.Log($"[BTS] ✓ Back to weapon after throw, restoring last selected item: TypeID {targetTypeID}, slot {targetSlot}");
                
                // Reset hasCompletedThrow since we're about to equip a new throwable
                hasCompletedThrow = false;
            }
            else if (shouldSwitchCategory && !lastSelectedItemStillExists)
            {
                // Rule 1: Throw completed + continuous G = move to next category
                currentCategoryIndex = (currentCategoryIndex + 1) % throwableTypeIDsInOrder.Count;
                targetTypeID = throwableTypeIDsInOrder[currentCategoryIndex];
                var slotsForCategory = throwableSlotsByTypeID[targetTypeID];
                targetSlot = slotsForCategory[0];
                
                // Update memory
                lastSelectedThrowableSlot = targetSlot;
                lastSelectedThrowableTypeID = targetTypeID;
                currentQuickThrowableSlot = targetSlot;
                currentQuickThrowableTypeID = targetTypeID;
                
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
                currentQuickThrowableSlot = targetSlot;
                currentQuickThrowableTypeID = targetTypeID;
                
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
                    currentQuickThrowableSlot = targetSlot;
                    currentQuickThrowableTypeID = targetTypeID;
                    
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
                    currentQuickThrowableSlot = targetSlot;
                    currentQuickThrowableTypeID = targetTypeID;
                    
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
                currentQuickThrowableSlot = targetSlot;
                currentQuickThrowableTypeID = targetTypeID;
                
                categoryInfo = $"[类别 1/{throwableTypeIDsInOrder.Count}]";
                Debug.Log($"[BTS] First selection: TypeID {targetTypeID}, slot {targetSlot}");
            }
            
            // Safety check: ensure player is in a safe state
            if (!IsPlayerSafeToSwitch(player))
            {
                Debug.LogWarning("[BTS] Player is not in a safe state to switch items - operation cancelled");
                ShowDebugBubble(isChinese ? "⚠️ 当前状态无法切换" : "⚠️ Cannot switch in current state");
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
                            itemName = GetLocalizedItemName(item);
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
                    string message = isChinese 
                        ? $"{itemName}"
                        : $"{itemName}";
                    ShowDebugBubble(message);
                }
            }
            else
            {
                ShowDebugBubble(isChinese ? "❌ 无法切换到槽位" : "❌ Cannot switch to slot");
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
                // Use cached player and methods for better performance
                var player = cachedPlayer ?? FindPlayerCharacter();
                if (player == null) return;
                
                var inventory = cachedInventory ?? (player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>());
                if (inventory == null) return;
                
                // Use cached method if available
                var getItemMethod = cachedGetItemMethod;
                if (getItemMethod == null)
                {
                    var inventoryType = inventory.GetType();
                    getItemMethod = inventoryType.GetMethod(
                        "GetItem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    ) ?? inventoryType.GetMethod(
                        "GetItemAt",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                    );
                    cachedGetItemMethod = getItemMethod; // Cache for next time
                }
                
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
        /// Directly switch back to weapon using saved slot hash (simplified version for Throw mode)
        /// This is called after a fixed delay instead of monitoring
        /// </summary>
        private void SwitchBackToWeaponDirectly()
        {
            if (!previousEquippedSlotHash.HasValue)
            {
                Debug.LogWarning("[BTS] ⚠️ No previous weapon slot hash saved, cannot switch back");
                return;
            }
            
            try
            {
                // PERFORMANCE: Use cached player instead of searching
                var player = cachedPlayer ?? FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogError("[BTS] ❌ Cannot switch back: Player not found");
                    return;
                }
                
                int slotHash = previousEquippedSlotHash.Value;
                Debug.Log($"[BTS] ⚡ Switching back to weapon via SwitchHoldAgentInSlot (hash={slotHash}, key='{previousEquippedSlotKey}')");
                
                // PERFORMANCE: Cache method info to avoid repeated reflection calls
                if (cachedSwitchHoldAgentInSlotMethod == null)
                {
                    var playerType = player.GetType();
                    cachedSwitchHoldAgentInSlotMethod = playerType.GetMethod("SwitchHoldAgentInSlot",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                        null,
                        new System.Type[] { typeof(int) },
                        null);
                }
                
                if (cachedSwitchHoldAgentInSlotMethod != null)
                {
                    cachedSwitchHoldAgentInSlotMethod.Invoke(player, new object[] { slotHash });
                    lastEquippedThrowableSlot = null;
                    Debug.Log($"[BTS] ✓ Successfully switched back to weapon (hash={slotHash})");
                }
                else
                {
                    Debug.LogError("[BTS] ❌ SwitchHoldAgentInSlot method not found");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error in SwitchBackToWeaponDirectly: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Auto-switch back to weapon immediately after throw
        /// UPDATED: Use SwitchHoldAgentInSlot with equipment slot hash (not inventory slot!)
        /// NOTE: This method is kept for backward compatibility but is no longer used in Throw mode
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
        /// Try to skip throw preparation time (charge/readiness time) for throwable items
        /// This is called when left mouse button is pressed while holding a throwable
        /// </summary>
        // Track last skip attempt to avoid spam logging
        private float lastSkipAttemptTime = 0f;
        private const float SKIP_ATTEMPT_LOG_INTERVAL = 0.5f; // Log every 0.5 seconds
        
        /// <summary>
        /// Find SkillTypes enum type from all loaded assemblies
        /// </summary>
        private System.Type? FindSkillTypesType()
        {
            // Cache the type to avoid repeated searches
            if (cachedSkillTypesType != null)
            {
                return cachedSkillTypesType;
            }
            
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var skillTypesType = assembly.GetType("SkillTypes") 
                        ?? assembly.GetType("Duckov.SkillTypes")
                        ?? assembly.GetType("TeamSoda.Duckov.Core.SkillTypes");
                    if (skillTypesType != null)
                    {
                        Debug.Log($"[BTS] Found SkillTypes type '{skillTypesType.FullName}' in assembly: {assembly.GetName().Name}");
                        cachedSkillTypesType = skillTypesType;
                        return skillTypesType;
                    }
                }
                catch
                {
                    // Ignore assembly access errors
                }
            }
            
            Debug.LogWarning("[BTS] Could not find SkillTypes type in any loaded assembly");
            return null;
        }
        
        // Cache for SkillTypes type to avoid repeated searches
        private System.Type? cachedSkillTypesType = null;
        
        /// <summary>
        /// Initialize or update current quick throwable slot (used in Throw mode)
        /// Selects the first available throwable if no current quick throwable is set
        /// </summary>
        private void InitializeCurrentQuickThrowable()
        {
            // Only initialize if in Throw mode
            if (throwMode != ThrowMode.Throw)
            {
                return;
            }
            
            // Check if current quick throwable still exists in inventory
            if (currentQuickThrowableSlot.HasValue)
            {
                var player = FindPlayerCharacter();
                if (player != null)
                {
                    // Try to get item from slot using CharacterMainControl.GetSlot
                    var playerType = player.GetType();
                    var getSlotMethod = playerType.GetMethod("GetSlot",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (getSlotMethod != null)
                    {
                        var slotObj = getSlotMethod.Invoke(player, new object[] { currentQuickThrowableSlot.Value });
                        if (slotObj != null)
                        {
                            var slotType = slotObj.GetType();
                            var itemProperty = slotType.GetProperty("Item",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                            
                            if (itemProperty != null)
                            {
                                var item = itemProperty.GetValue(slotObj) as Item;
                                if (item != null && IsThrowableItem(item))
                                {
                                    // Current quick throwable still exists, keep it
                                    Debug.Log($"[BTS] Current quick throwable still valid: Slot {currentQuickThrowableSlot.Value}, TypeID {currentQuickThrowableTypeID}");
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            
            // Need to select a new quick throwable - get first available
            var throwables = GetAllThrowablesByCategory();
            if (throwables.Count == 0)
            {
                Debug.Log("[BTS] No throwables found in inventory for quick throwable");
                currentQuickThrowableSlot = null;
                currentQuickThrowableTypeID = null;
                return;
            }
            
            // Get first throwable from first category
            if (throwableTypeIDsInOrder.Count > 0)
            {
                var firstTypeID = throwableTypeIDsInOrder[0];
                if (throwableSlotsByTypeID.ContainsKey(firstTypeID) && throwableSlotsByTypeID[firstTypeID].Count > 0)
                {
                    currentQuickThrowableSlot = throwableSlotsByTypeID[firstTypeID][0];
                    currentQuickThrowableTypeID = firstTypeID;
                    Debug.Log($"[BTS] ✓ Initialized current quick throwable: Slot {currentQuickThrowableSlot}, TypeID {currentQuickThrowableTypeID}");
                }
            }
        }
        
        /// <summary>
        /// Throw to mouse position (Throw Mode)
        /// Uses "current quick throwable" if available, otherwise initializes it
        /// If already holding a throwable, directly throw it. Otherwise, quickly equip and throw.
        /// </summary>
        private void ThrowToMousePosition(CharacterMainControl player)
        {
            try
            {
                Debug.Log("[BTS] 🎯 ThrowToMousePosition: Starting throw sequence");
                
                // Step 1: Check if already holding a throwable
                var currentItem = GetCurrentHoldItem(player);
                bool isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
                
                if (isHoldingThrowable)
                {
                    // Already holding a throwable - directly trigger throw
                    Debug.Log($"[BTS] ✓ Already holding throwable: {currentItem.name}, directly triggering throw");
                    StartCoroutine(DirectThrowSequence(player, currentItem));
                    return;
                }
                
                // Step 2: Check if we have a current quick throwable, if not initialize it
                if (!currentQuickThrowableSlot.HasValue)
                {
                    InitializeCurrentQuickThrowable();
                }
                else
                {
                    // Verify the current quick throwable still exists
                    var playerType = player.GetType();
                    var getSlotMethod = playerType.GetMethod("GetSlot",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (getSlotMethod != null)
                    {
                        var slotObj = getSlotMethod.Invoke(player, new object[] { currentQuickThrowableSlot.Value });
                        if (slotObj != null)
                        {
                            var slotType = slotObj.GetType();
                            var itemProperty = slotType.GetProperty("Item",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                            
                            if (itemProperty != null)
                            {
                                var item = itemProperty.GetValue(slotObj) as Item;
                                if (item == null || !IsThrowableItem(item))
                                {
                                    // Current quick throwable no longer exists, reinitialize
                                    Debug.Log($"[BTS] Current quick throwable (Slot {currentQuickThrowableSlot.Value}) no longer exists, reinitializing...");
                                    InitializeCurrentQuickThrowable();
                                }
                            }
                        }
                    }
                }
                
                // Step 3: Check if we have a current quick throwable
                if (!currentQuickThrowableSlot.HasValue)
                {
                    Debug.Log("[BTS] ❌ No current quick throwable available");
                    ShowDebugBubble(isChinese ? "❌ 没有可用的快捷投掷物" : "❌ No quick throwable available");
                    return;
                }
                
                // Step 4: Verify the quick throwable still exists (skip verification for now, just use the slot)
                // The SwitchToSlot method will handle if the item doesn't exist
                
                Debug.Log($"[BTS] ✓ Using current quick throwable: Slot {currentQuickThrowableSlot.Value}, TypeID {currentQuickThrowableTypeID}");
                
                // Save current weapon slot
                SaveCurrentEquippedSlot(player);
                
                // Step 5: Equip the quick throwable and immediately throw
                if (!SwitchToSlot(currentQuickThrowableSlot.Value))
                {
                    Debug.Log("[BTS] ❌ Failed to equip quick throwable");
                    return;
                }
                
                // Start throw sequence immediately after equipping
                StartCoroutine(ThrowAfterEquip(player, currentQuickThrowableSlot.Value));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BTS] Error in ThrowToMousePosition: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Direct throw sequence - when already holding a throwable
        /// </summary>
        private System.Collections.IEnumerator DirectThrowSequence(CharacterMainControl player, Item throwableItem)
        {
            Debug.Log($"[BTS] 🎯 DirectThrowSequence: Starting immediate throw for {throwableItem.name}");
            
            // Save current weapon before throwing (if not already saved)
            if (!previousEquippedSlotHash.HasValue)
            {
                SaveCurrentEquippedSlot(player);
            }
            
            // Get mouse world position
            Vector3 mouseWorldPos = GetMouseWorldPosition(player);
            Debug.Log($"[BTS] 🎯 Mouse world position: {mouseWorldPos}");
            
            // Calculate throw point (considering max range)
            Vector3 throwPoint = CalculateThrowPoint(player, mouseWorldPos, throwableItem);
            Debug.Log($"[BTS] 🎯 Calculated throw point: {throwPoint}");
            
            // Start skill action immediately (if not already started)
            var playerType = player.GetType();
            var currentActionProp = playerType.GetProperty("CurrentAction",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            
            var currentAction = currentActionProp?.GetValue(player);
            if (currentAction == null)
            {
                // Need to start skill action - use StartSkillAim directly
                Debug.Log("[BTS] Starting skill action for immediate throw");
                StartItemSkillAction(player);
                yield return new WaitForSeconds(0.1f); // Wait for skill action to start
                
                // Check again if action started
                currentAction = currentActionProp?.GetValue(player);
                if (currentAction == null)
                {
                    Debug.LogWarning("[BTS] ⚠️ Skill action still not started after StartSkillAim");
                    // Try once more with a longer wait
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // Skip preparation time immediately (must be done after skill action starts)
            if (disableThrowPreparationTime && currentAction != null)
            {
                TrySkipThrowPreparationTime(throwableItem, player);
                yield return null; // Wait one frame for the skip to take effect
            }
            
            // Trigger throw immediately
            try
            {
                TriggerThrow(player, throwPoint);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BTS] Error in DirectThrowSequence: {ex.Message}\n{ex.StackTrace}");
            }
            
            // In Throw mode, wait a short time for throw to complete, then switch back to weapon
            if (throwMode == ThrowMode.Throw && previousEquippedSlotHash.HasValue)
            {
                Debug.Log("[BTS] 🎯 Throw mode: Waiting 0.05s after throw, then switching back to weapon");
                yield return new WaitForSeconds(0.05f);  // Wait 0.05 seconds for throw to complete
                SwitchBackToWeaponDirectly();
            }
        }
        
        /// <summary>
        /// Coroutine to throw after item is equipped (for Throw mode - should be very fast)
        /// </summary>
        private System.Collections.IEnumerator ThrowAfterEquip(CharacterMainControl player, int slot)
        {
            // Wait for item to be equipped (reduced wait time for faster throw)
            yield return new WaitForSeconds(0.1f);
            
            // Get current item
            var currentItem = GetCurrentHoldItem(player);
            if (currentItem == null || !IsThrowableItem(currentItem))
            {
                Debug.Log("[BTS] ❌ Item not equipped or not throwable, waiting longer...");
                // Wait a bit more and check again
                yield return new WaitForSeconds(0.1f);
                currentItem = GetCurrentHoldItem(player);
                if (currentItem == null || !IsThrowableItem(currentItem))
                {
                    Debug.Log("[BTS] ❌ Item still not equipped after wait");
                    yield break;
                }
            }
            
            Debug.Log($"[BTS] ✓ Item equipped: {currentItem.name}, starting immediate throw sequence");
            
            // Get mouse world position
            Vector3 mouseWorldPos = GetMouseWorldPosition(player);
            Debug.Log($"[BTS] 🎯 Mouse world position: {mouseWorldPos}");
            
            // Calculate throw point (considering max range)
            Vector3 throwPoint = CalculateThrowPoint(player, mouseWorldPos, currentItem);
            Debug.Log($"[BTS] 🎯 Calculated throw point: {throwPoint}");
            
            // Start skill action immediately using StartSkillAim
            Debug.Log("[BTS] Starting skill action using StartSkillAim");
            StartItemSkillAction(player);
            
            // Wait for skill action to start
            yield return new WaitForSeconds(0.1f);
            
            // Check if skill action started
            var playerType = player.GetType();
            var currentActionProp = playerType.GetProperty("CurrentAction",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            
            var currentAction = currentActionProp?.GetValue(player);
            if (currentAction == null)
            {
                Debug.LogWarning("[BTS] ⚠️ Skill action still not started, waiting longer...");
                yield return new WaitForSeconds(0.1f);
                currentAction = currentActionProp?.GetValue(player);
            }
            
            // Skip throw preparation time immediately (for Throw mode, this should always be enabled)
            if (disableThrowPreparationTime && currentAction != null)
            {
                TrySkipThrowPreparationTime(currentItem, player);
                yield return null; // Wait one frame for the skip to take effect
            }
            
            // Trigger throw immediately
            try
            {
                TriggerThrow(player, throwPoint);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BTS] Error in ThrowAfterEquip: {ex.Message}\n{ex.StackTrace}");
            }
            
            // In Throw mode, wait a short time for throw to complete, then switch back to weapon
            if (throwMode == ThrowMode.Throw && previousEquippedSlotHash.HasValue)
            {
                Debug.Log("[BTS] 🎯 Throw mode: Waiting 0.05s after throw, then switching back to weapon");
                yield return new WaitForSeconds(0.05f);  // Wait 0.05 seconds for throw to complete
                SwitchBackToWeaponDirectly();
            }
        }
        
        /// <summary>
        /// Monitor throw completion and switch back to weapon immediately after throw (for Throw mode)
        /// This is called when G key is released while holding a throwable, or right after TriggerThrow succeeds
        /// </summary>
        private System.Collections.IEnumerator MonitorThrowCompletionAndSwitchBackImmediately(CharacterMainControl player)
        {
            Debug.Log("[BTS] 🎯 Throw mode: Starting immediate monitoring for throw release (投掷物出手)...");
            
            // Check initial state
            var initialItem = GetCurrentHoldItem(player);
            bool initiallyHoldingThrowable = initialItem != null && IsThrowableItem(initialItem);
            
            if (!initiallyHoldingThrowable)
            {
                // Not holding a throwable - switch back immediately
                Debug.Log("[BTS] 🎯 Not holding throwable initially - switching back immediately");
                AutoSwitchBackToWeaponImmediately();
                yield break;
            }
            
            // No initial wait - start checking immediately for fastest response
            // Check if player is still holding a throwable
            float checkInterval = 0.01f; // Check very frequently (every 10ms) for faster detection
            float maxWaitTime = 0.5f; // Maximum 0.5s wait - reduced for faster timeout
            float elapsed = 0f;
            bool wasHoldingThrowable = initiallyHoldingThrowable;
            int consecutiveEmptyChecks = 0; // Count consecutive frames where item is gone
            const int REQUIRED_EMPTY_CHECKS = 1; // Only require 1 check to confirm throw (faster response)
            
            while (elapsed < maxWaitTime)
            {
                var currentItem = GetCurrentHoldItem(player);
                bool isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
                
                // Check if player is holding nothing (empty hand) - fastest detection
                if (currentItem == null && wasHoldingThrowable)
                {
                    // Empty hand detected - throw released immediately
                    Debug.Log("[BTS] 🎯 Throw released (投掷物出手) - switching back to weapon IMMEDIATELY (detected via empty hand)");
                    AutoSwitchBackToWeaponImmediately();
                    yield break;
                }
                
                // If we were holding a throwable and now we're not, the throw released (投掷物出手)
                if (wasHoldingThrowable && !isHoldingThrowable)
                {
                    // Throw released - switch back to weapon IMMEDIATELY (投掷物出手后立即切换)
                    Debug.Log("[BTS] 🎯 Throw released (投掷物出手) - switching back to weapon IMMEDIATELY (detected via item change)");
                    AutoSwitchBackToWeaponImmediately();
                    yield break;
                }
                
                wasHoldingThrowable = isHoldingThrowable;
                elapsed += checkInterval;
                yield return new WaitForSeconds(checkInterval);
            }
            
            // Timeout - if we still haven't detected release, check one more time and switch anyway
            var finalItem = GetCurrentHoldItem(player);
            bool stillHolding = finalItem != null && IsThrowableItem(finalItem);
            if (!stillHolding)
            {
                Debug.Log("[BTS] 🎯 Throw released (timeout check) - switching back to weapon");
            }
            else
            {
                Debug.LogWarning("[BTS] ⚠️ Timeout reached but still holding throwable - forcing switch back anyway");
            }
            AutoSwitchBackToWeaponImmediately();
        }
        
        /// <summary>
        /// Monitor throw completion and switch back to weapon (for Throw mode)
        /// IMPORTANT: Do NOT reset currentQuickThrowableSlot - keep the selected throwable for next use
        /// </summary>
        private System.Collections.IEnumerator MonitorThrowCompletionAndSwitchBack(CharacterMainControl player)
        {
            // Reduced initial wait for faster response
            yield return new WaitForSeconds(0.1f);
            
            // Check if player is still holding a throwable
            float checkInterval = 0.01f; // Check very frequently (every 10ms) for faster detection
            float maxWaitTime = 1.0f; // Reduced timeout for faster fallback
            float elapsed = 0f;
            bool wasHoldingThrowable = true;
            
            while (elapsed < maxWaitTime)
            {
                var currentItem = GetCurrentHoldItem(player);
                bool isHoldingThrowable = currentItem != null && IsThrowableItem(currentItem);
                
                // Check if player is holding nothing (empty hand) - fastest detection
                if (currentItem == null && wasHoldingThrowable)
                {
                    // Throw completed - switch back to weapon IMMEDIATELY
                    Debug.Log("[BTS] Throw completed - switching back to weapon IMMEDIATELY (detected via empty hand)");
                    Debug.Log($"[BTS] Keeping current quick throwable for next use: Slot {currentQuickThrowableSlot}, TypeID {currentQuickThrowableTypeID}");
                    AutoSwitchBackToWeaponImmediately();
                    yield break;
                }
                
                // If we were holding a throwable and now we're not, the throw completed
                if (wasHoldingThrowable && !isHoldingThrowable)
                {
                    // Throw completed - switch back to weapon IMMEDIATELY
                    Debug.Log("[BTS] Throw completed - switching back to weapon IMMEDIATELY (detected via item change)");
                    Debug.Log($"[BTS] Keeping current quick throwable for next use: Slot {currentQuickThrowableSlot}, TypeID {currentQuickThrowableTypeID}");
                    AutoSwitchBackToWeaponImmediately();
                    yield break;
                }
                
                wasHoldingThrowable = isHoldingThrowable;
                elapsed += checkInterval;
                yield return new WaitForSeconds(checkInterval);
            }
            
            // Timeout - try switching back anyway (maybe throw completed but detection failed)
            Debug.Log("[BTS] Throw completion timeout - switching back to weapon IMMEDIATELY");
            Debug.Log($"[BTS] Keeping current quick throwable for next use: Slot {currentQuickThrowableSlot}, TypeID {currentQuickThrowableTypeID}");
            
            // Force switch back immediately - no delay
            AutoSwitchBackToWeaponImmediately();
        }
        
        /// <summary>
        /// Get mouse world position from screen position
        /// </summary>
        private Vector3 GetMouseWorldPosition(CharacterMainControl player)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Fallback: use player position + forward direction
                return player.transform.position + player.transform.forward * 10f;
            }
            
            // Raycast from camera through mouse position
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Try to hit something (ground, wall, etc.)
            if (Physics.Raycast(ray, out hit, 1000f))
            {
                return hit.point;
            }
            
            // If no hit, calculate point at a reasonable distance
            float defaultDistance = 10f;
            return ray.origin + ray.direction * defaultDistance;
        }
        
        /// <summary>
        /// Calculate throw point considering max range
        /// If target is beyond max range, return point at max range in that direction
        /// </summary>
        private Vector3 CalculateThrowPoint(CharacterMainControl player, Vector3 targetPos, Item throwableItem)
        {
            Vector3 playerPos = player.transform.position;
            Vector3 direction = (targetPos - playerPos).normalized;
            float distance = Vector3.Distance(playerPos, targetPos);
            
            // Try to get max throw range from item
            float maxRange = 50f; // Default max range (meters)
            try
            {
                var itemType = throwableItem.GetType();
                var rangeProp = itemType.GetProperty("MaxRange", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic);
                
                if (rangeProp != null)
                {
                    var rangeValue = rangeProp.GetValue(throwableItem);
                    if (rangeValue != null)
                    {
                        maxRange = Convert.ToSingle(rangeValue);
                    }
                }
                
                // Also check SkillContext for range
                var skillContextProp = itemType.GetProperty("SkillContext",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (skillContextProp != null)
                {
                    var skillContext = skillContextProp.GetValue(throwableItem);
                    if (skillContext != null)
                    {
                        var contextType = skillContext.GetType();
                        var rangeProp2 = contextType.GetProperty("MaxRange",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);
                        
                        if (rangeProp2 != null)
                        {
                            var rangeValue = rangeProp2.GetValue(skillContext);
                            if (rangeValue != null)
                            {
                                maxRange = Convert.ToSingle(rangeValue);
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Could not get max range from item: {ex.Message}");
            }
            
            // If target is within range, use it; otherwise use max range point
            if (distance <= maxRange)
            {
                return targetPos;
            }
            else
            {
                Vector3 maxRangePoint = playerPos + direction * maxRange;
                Debug.Log($"[BTS] Target beyond max range ({maxRange}m), using max range point");
                return maxRangePoint;
            }
        }
        
        /// <summary>
        /// Trigger throw skill release
        /// Use CharacterMainControl.ReleaseSkill directly instead of going through CurrentAction
        /// </summary>
        private void TriggerThrow(CharacterMainControl player, Vector3 aimPoint)
        {
            try
            {
                // First, ensure preparation time is skipped
                var currentItem = GetCurrentHoldItem(player);
                if (currentItem != null && disableThrowPreparationTime)
                {
                    TrySkipThrowPreparationTime(currentItem, player);
                }
                
                var playerType = player.GetType();
                
                // Get CurrentAction property (will be used in both methods)
                var currentActionProp = playerType.GetProperty("CurrentAction",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                // Method 1: Try to use CharacterMainControl.ReleaseSkill directly (preferred method)
                var releaseSkillMethod = playerType.GetMethod("ReleaseSkill",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (releaseSkillMethod != null)
                {
                    var skillTypesType = FindSkillTypesType();
                    if (skillTypesType != null)
                    {
                        var itemSkillValue = System.Enum.Parse(skillTypesType, "itemSkill");
                        
                        Debug.Log("[BTS] Attempting to call CharacterMainControl.ReleaseSkill(SkillTypes.itemSkill)...");
                        
                        // Check CurrentAction before calling ReleaseSkill
                        var currentActionBefore = currentActionProp?.GetValue(player);
                        Debug.Log($"[BTS] CurrentAction before ReleaseSkill: {(currentActionBefore != null ? currentActionBefore.GetType().Name : "null")}");
                        
                        // Call ReleaseSkill directly on player
                        bool result = (bool)releaseSkillMethod.Invoke(player, new object[] { itemSkillValue });
                        Debug.Log($"[BTS] ReleaseSkill result: {result}");
                        
                        if (result)
                        {
                            Debug.Log("[BTS] ✓ Successfully triggered throw via CharacterMainControl.ReleaseSkill!");
                            // Note: Weapon switching is now handled in DirectThrowSequence/ThrowAfterEquip after a short delay
                            return;
                        }
                        else
                        {
                            Debug.LogWarning("[BTS] ⚠️ ReleaseSkill returned false - skill might not be ready or action not started");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[BTS] Could not find SkillTypes type for ReleaseSkill");
                    }
                }
                else
                {
                    Debug.LogWarning("[BTS] Could not find ReleaseSkill method on CharacterMainControl");
                }
                
                // Method 2: Fallback - try through CurrentAction
                if (currentActionProp != null)
                {
                    var currentAction = currentActionProp.GetValue(player);
                    if (currentAction != null)
                    {
                        var actionType = currentAction.GetType();
                        if (actionType.Name == "CA_Skill")
                        {
                            var actionReleaseMethod = actionType.GetMethod("ReleaseSkill",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                            
                            if (actionReleaseMethod != null)
                            {
                                var skillTypesType = FindSkillTypesType();
                                if (skillTypesType != null)
                                {
                                    var itemSkillValue = System.Enum.Parse(skillTypesType, "itemSkill");
                                    bool result = (bool)actionReleaseMethod.Invoke(currentAction, new object[] { itemSkillValue });
                                    if (result)
                                    {
                                        Debug.Log("[BTS] ✓ Successfully triggered throw via CA_Skill.ReleaseSkill!");
                                        // Note: Weapon switching is now handled in DirectThrowSequence/ThrowAfterEquip after a short delay
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Method 3: Last resort - simulate mouse button release
                Debug.LogWarning("[BTS] ⚠️ All ReleaseSkill methods failed - trying mouse release as fallback");
                SimulateMouseLeftButtonRelease(player);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BTS] Error in TriggerThrow: {ex.Message}\n{ex.StackTrace}");
                // Fallback: try mouse release
                try
                {
                    SimulateMouseLeftButtonRelease(player);
                }
                catch { }
            }
        }
        
        /// <summary>
        /// Simulate mouse left button release to trigger throw
        /// </summary>
        private void SimulateMouseLeftButtonRelease(CharacterMainControl player)
        {
            try
            {
                // Try to find methods that handle mouse button release
                var playerType = player.GetType();
                
                // Method 1: Try OnMouseButtonUp
                var onMouseButtonUpMethod = playerType.GetMethod("OnMouseButtonUp",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (onMouseButtonUpMethod != null)
                {
                    onMouseButtonUpMethod.Invoke(player, new object[] { 0 }); // 0 = left mouse button
                    Debug.Log("[BTS] ✓ Called OnMouseButtonUp method");
                }
                
                // Method 2: Try HandleInput with release flag
                var handleInputReleaseMethod = playerType.GetMethod("HandleInputRelease",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (handleInputReleaseMethod != null)
                {
                    handleInputReleaseMethod.Invoke(player, new object[] { 0 });
                    Debug.Log("[BTS] ✓ Called HandleInputRelease method");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Could not simulate mouse button release: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Start item skill action
        /// CRITICAL: Must set SetNextSkillType before calling StartSkillAim (based on CA_Skill.OnStart() requirements)
        /// Try to get CA_Skill from ActionSystem or create it if needed
        /// </summary>
        private void StartItemSkillAction(CharacterMainControl player)
        {
            try
            {
                var playerType = player.GetType();
                var skillTypesType = FindSkillTypesType();
                if (skillTypesType == null)
                {
                    Debug.LogWarning("[BTS] Could not find SkillTypes type");
                    return;
                }
                
                var itemSkillValue = System.Enum.Parse(skillTypesType, "itemSkill");
                
                // Get CurrentAction property
                var currentActionProp = playerType.GetProperty("CurrentAction",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                // Try to get CA_Skill from ActionSystem or similar
                object caSkill = null;
                
                // Method 1: Try to get from CurrentAction
                if (currentActionProp != null)
                {
                    caSkill = currentActionProp.GetValue(player);
                    if (caSkill != null && caSkill.GetType().Name != "CA_Skill")
                    {
                        caSkill = null; // Not CA_Skill, reset
                    }
                }
                
                // Method 2: Try to get from ActionSystem or component
                if (caSkill == null)
                {
                    var actionSystemProp = playerType.GetProperty("ActionSystem",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (actionSystemProp != null)
                    {
                        var actionSystem = actionSystemProp.GetValue(player);
                        if (actionSystem != null)
                        {
                            var actionSystemType = actionSystem.GetType();
                            var getActionMethod = actionSystemType.GetMethod("GetAction",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                            
                            if (getActionMethod != null)
                            {
                                // Try to get CA_Skill action
                                var caSkillType = System.Type.GetType("CA_Skill");
                                if (caSkillType != null)
                                {
                                    caSkill = getActionMethod.Invoke(actionSystem, new object[] { caSkillType });
                                }
                            }
                        }
                    }
                }
                
                // Step 1: Set NextSkillType on CA_Skill (if we have it)
                if (caSkill != null)
                {
                    var actionType = caSkill.GetType();
                    if (actionType.Name == "CA_Skill")
                    {
                        var setNextSkillTypeMethod = actionType.GetMethod("SetNextSkillType",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);
                        
                        if (setNextSkillTypeMethod != null)
                        {
                            Debug.Log("[BTS] Setting NextSkillType to itemSkill on CA_Skill...");
                            setNextSkillTypeMethod.Invoke(caSkill, new object[] { itemSkillValue });
                            Debug.Log("[BTS] ✓ SetNextSkillType called");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[BTS] Could not find CA_Skill instance - StartSkillAim should create it");
                }
                
                // Step 2: Call StartSkillAim (this should create/set CurrentAction to CA_Skill)
                var startSkillAimMethod = playerType.GetMethod("StartSkillAim",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (startSkillAimMethod != null)
                {
                    Debug.Log($"[BTS] Calling StartSkillAim with SkillTypes.itemSkill...");
                    var result = startSkillAimMethod.Invoke(player, new object[] { itemSkillValue });
                    Debug.Log($"[BTS] ✓ StartSkillAim called, result: {(result != null ? result.ToString() : "null")}");
                    
                    // Step 3: After StartSkillAim, try to set SetNextSkillType again (in case CA_Skill was just created)
                    if (currentActionProp != null)
                    {
                        var currentAction = currentActionProp.GetValue(player);
                        Debug.Log($"[BTS] CurrentAction after StartSkillAim: {(currentAction != null ? currentAction.GetType().Name : "null")}");
                        
                        if (currentAction != null && currentAction.GetType().Name == "CA_Skill")
                        {
                            var actionType = currentAction.GetType();
                            var setNextSkillTypeMethod = actionType.GetMethod("SetNextSkillType",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                            
                            if (setNextSkillTypeMethod != null)
                            {
                                Debug.Log("[BTS] Setting NextSkillType to itemSkill on newly created CA_Skill...");
                                setNextSkillTypeMethod.Invoke(currentAction, new object[] { itemSkillValue });
                                Debug.Log("[BTS] ✓ SetNextSkillType called on new CA_Skill");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[BTS] Could not find StartSkillAim method on CharacterMainControl");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Could not start skill action: {ex.Message}\n{ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Simulate mouse left button press to start skill action
        /// This is needed because the game requires mouse input to start item skill actions
        /// </summary>
        private void SimulateMouseLeftButtonPress(CharacterMainControl player)
        {
            try
            {
                // Try to find Input handling methods or use Unity's Input system
                // The game might use Input.GetMouseButtonDown internally, so we need to trigger it programmatically
                
                // Method 1: Try to find and call a method that handles mouse input
                var playerType = player.GetType();
                var handleInputMethod = playerType.GetMethod("HandleInput",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (handleInputMethod != null)
                {
                    handleInputMethod.Invoke(player, null);
                    Debug.Log("[BTS] ✓ Called HandleInput method");
                }
                
                // Method 2: Try to find OnMouseButtonDown or similar
                var onMouseButtonMethod = playerType.GetMethod("OnMouseButtonDown",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic);
                
                if (onMouseButtonMethod != null)
                {
                    onMouseButtonMethod.Invoke(player, new object[] { 0 }); // 0 = left mouse button
                    Debug.Log("[BTS] ✓ Called OnMouseButtonDown method");
                }
                
                // Method 3: Try to directly call StartSkillAim if available
                StartItemSkillAction(player);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Could not simulate mouse button press: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Coroutine to trigger throw after delay (waiting for skill action to start)
        /// </summary>
        private System.Collections.IEnumerator TriggerThrowAfterDelay(CharacterMainControl player, Vector3 aimPoint, float delay)
        {
            yield return new WaitForSeconds(delay);
            TriggerThrow(player, aimPoint);
        }
        
        private void TrySkipThrowPreparationTime(Item throwableItem, CharacterMainControl player)
        {
            if (throwableItem == null || player == null)
                return;
            
            try
            {
                // Only log occasionally to avoid spam
                bool shouldLog = Time.time - lastSkipAttemptTime >= SKIP_ATTEMPT_LOG_INTERVAL;
                if (shouldLog)
                {
                    Debug.Log($"[BTS] 🚀 Attempting to skip throw preparation time for: {throwableItem.name}");
                    lastSkipAttemptTime = Time.time;
                }
                
                var itemType = throwableItem.GetType();
                
                // Method 1: Try to find and set charge/preparation time properties
                string[] chargeTimePropertyNames = {
                    "ChargeTime", "PreparationTime", "ThrowDelay", "ChargeProgress",
                    "IsReady", "IsCharged", "Ready", "Charged"
                };
                
                foreach (var propName in chargeTimePropertyNames)
                {
                    var prop = itemType.GetProperty(propName, 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty);
                    
                    if (prop != null && prop.CanWrite)
                    {
                        var propType = prop.PropertyType;
                        try
                        {
                            if (propType == typeof(float) || propType == typeof(double))
                            {
                                // Set to 0 or minimum value
                                prop.SetValue(throwableItem, 0f);
                                Debug.Log($"[BTS] ✓ Set {propName} to 0 for {throwableItem.name}");
                            }
                            else if (propType == typeof(bool))
                            {
                                // Set to true (ready/charged)
                                prop.SetValue(throwableItem, true);
                                Debug.Log($"[BTS] ✓ Set {propName} to true for {throwableItem.name}");
                            }
                            else if (propType == typeof(int))
                            {
                                prop.SetValue(throwableItem, 0);
                                Debug.Log($"[BTS] ✓ Set {propName} to 0 for {throwableItem.name}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to set {propName}: {ex.Message}");
                        }
                    }
                }
                
                // Method 2: Try to find and call methods that complete/skip preparation
                string[] completeMethodNames = {
                    "CompleteCharge", "SkipPreparation", "SetReady", "ForceReady",
                    "FinishCharging", "InstantReady", "CancelCharge"
                };
                
                foreach (var methodName in completeMethodNames)
                {
                    var method = itemType.GetMethod(methodName, 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (method != null)
                    {
                        try
                        {
                            method.Invoke(throwableItem, null);
                            Debug.Log($"[BTS] ✓ Called {methodName}() for {throwableItem.name}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to call {methodName}(): {ex.Message}");
                        }
                    }
                }
                
                // Method 3: Try to find CurrentHoldItemAgent and modify its properties
                try
                {
                    var playerType = player.GetType();
                    var currentHoldItemAgentProp = playerType.GetProperty("CurrentHoldItemAgent", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (currentHoldItemAgentProp != null)
                    {
                        var agent = currentHoldItemAgentProp.GetValue(player);
                        if (agent != null)
                        {
                            var agentType = agent.GetType();
                            
                            // Try to find charge-related properties in the agent
                            var agentChargeTimeProp = agentType.GetProperty("ChargeTime", 
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                System.Reflection.BindingFlags.NonPublic);
                            
                            if (agentChargeTimeProp != null && agentChargeTimeProp.CanWrite)
                            {
                                agentChargeTimeProp.SetValue(agent, 0f);
                                Debug.Log($"[BTS] ✓ Set CurrentHoldItemAgent.ChargeTime to 0");
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BTS] Failed to modify CurrentHoldItemAgent: {ex.Message}");
                }
                
                // Method 4: Try to find Action/Skill system and modify charge time
                // This might be handled by a separate system component
                try
                {
                    var itemGameObject = throwableItem.gameObject;
                    if (itemGameObject != null)
                    {
                        // Look for components that might handle charging
                        var allComponents = itemGameObject.GetComponents<Component>();
                        foreach (var component in allComponents)
                        {
                            if (component == null) continue;
                            var compType = component.GetType();
                            var compTypeName = compType.Name;
                            
                            // Check if this component might be related to charging/throwing
                            if (compTypeName.Contains("Charge") || compTypeName.Contains("Throw") || 
                                compTypeName.Contains("Action") || compTypeName.Contains("Skill"))
                            {
                                // Try to find charge time property
                                var chargeProp = compType.GetProperty("ChargeTime", 
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                    System.Reflection.BindingFlags.NonPublic);
                                
                                if (chargeProp != null && chargeProp.CanWrite)
                                {
                                    chargeProp.SetValue(component, 0f);
                                    Debug.Log($"[BTS] ✓ Set {compTypeName}.ChargeTime to 0");
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BTS] Failed to check item components: {ex.Message}");
                }
                
                // Method 5: Try to find and modify CurrentAction on player (from character.md documentation)
                try
                {
                    var playerType = player.GetType();
                    
                    // Try CurrentAction property first (mentioned in character.md)
                    var currentActionProp = playerType.GetProperty("CurrentAction",
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (currentActionProp != null)
                    {
                        var currentAction = currentActionProp.GetValue(player);
                        if (currentAction != null)
                        {
                            var actionType = currentAction.GetType();
                            if (shouldLog) Debug.Log($"[BTS] Found CurrentAction on player: {actionType.Name}");
                            
                            // Log all properties and fields for debugging
                            if (shouldLog)
                            {
                                var debugProps = actionType.GetProperties(
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                    System.Reflection.BindingFlags.NonPublic);
                                Debug.Log($"[BTS] CurrentAction ({actionType.Name}) properties count: {debugProps.Length}");
                                foreach (var prop in debugProps)
                                {
                                    Debug.Log($"[BTS]   - Property: {prop.Name} ({prop.PropertyType.Name}), CanWrite: {prop.CanWrite}");
                                }
                                
                                var debugFields = actionType.GetFields(
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                                    System.Reflection.BindingFlags.Instance);
                                Debug.Log($"[BTS] CurrentAction ({actionType.Name}) fields count: {debugFields.Length}");
                                foreach (var field in debugFields)
                                {
                                    Debug.Log($"[BTS]   - Field: {field.Name} ({field.FieldType.Name})");
                                }
                            }
                            
                            // CRITICAL: CA_Skill uses actionTimer and skillReadyTime
                            // Try to modify actionTimer field (from CharacterActionBase) to skip preparation
                            var actionTimerField = actionType.GetField("actionTimer",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            if (actionTimerField != null && (actionTimerField.FieldType == typeof(float) || actionTimerField.FieldType == typeof(double)))
                            {
                                // Get CurrentRunningSkill to find skillReadyTime
                                var currentRunningSkillProp = actionType.GetProperty("CurrentRunningSkill",
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                    System.Reflection.BindingFlags.NonPublic);
                                
                                if (currentRunningSkillProp != null)
                                {
                                    var skill = currentRunningSkillProp.GetValue(currentAction);
                                    if (skill != null)
                                    {
                                        var skillType = skill.GetType();
                                        var skillContextProp = skillType.GetProperty("SkillContext",
                                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                            System.Reflection.BindingFlags.NonPublic);
                                        
                                        if (skillContextProp != null)
                                        {
                                            var skillContext = skillContextProp.GetValue(skill);
                                            if (skillContext != null)
                                            {
                                                var contextType = skillContext.GetType();
                                                var skillReadyTimeProp = contextType.GetProperty("skillReadyTime",
                                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                                    System.Reflection.BindingFlags.NonPublic);
                                                
                                                if (skillReadyTimeProp != null)
                                                {
                                                    var skillReadyTime = Convert.ToSingle(skillReadyTimeProp.GetValue(skillContext));
                                                    // Set actionTimer to be >= skillReadyTime to skip preparation
                                                    actionTimerField.SetValue(currentAction, skillReadyTime + 0.1f);
                                                    if (shouldLog) Debug.Log($"[BTS] ✓ Set CA_Skill.actionTimer to {skillReadyTime + 0.1f} (skillReadyTime: {skillReadyTime})");
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // Fallback: set actionTimer to a large value
                                if (actionTimerField.FieldType == typeof(float))
                                {
                                    actionTimerField.SetValue(currentAction, 999f);
                                    if (shouldLog) Debug.Log($"[BTS] ✓ Set CA_Skill.actionTimer to 999 (fallback)");
                                }
                            }
                            
                            // Try to find and modify progress/time related properties
                            var actionProps = actionType.GetProperties(
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.NonPublic);
                            
                            foreach (var prop in actionProps)
                            {
                                var propName = prop.Name;
                                if ((propName.Contains("Time") || propName.Contains("Progress") || 
                                     propName.Contains("Charge") || propName.Contains("Elapsed") ||
                                     propName.Contains("Remaining") || propName.Contains("Duration")) && 
                                    prop.CanWrite)
                                {
                                    try
                                    {
                                        var propType = prop.PropertyType;
                                        if (propType == typeof(float) || propType == typeof(double))
                                        {
                                            if (propName.Contains("Remaining") || propName.Contains("Elapsed") || 
                                                propName.Contains("Charge") || propName.Contains("Progress"))
                                            {
                                                prop.SetValue(currentAction, 0f);
                                                if (shouldLog) Debug.Log($"[BTS] ✓ Set CurrentAction.{propName} to 0");
                                            }
                                            else if (propName.Contains("Duration"))
                                            {
                                                // Set duration to a very small value
                                                prop.SetValue(currentAction, 0.001f);
                                                if (shouldLog) Debug.Log($"[BTS] ✓ Set CurrentAction.{propName} to 0.001");
                                            }
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (shouldLog) Debug.LogWarning($"[BTS] Failed to set CurrentAction.{propName}: {ex.Message}");
                                    }
                                }
                                else if ((propName.Contains("IsReady") || propName.Contains("IsComplete") || 
                                         propName.Contains("IsFinished")) && prop.CanWrite && prop.PropertyType == typeof(bool))
                                {
                                    try
                                    {
                                        prop.SetValue(currentAction, true);
                                        if (shouldLog) Debug.Log($"[BTS] ✓ Set CurrentAction.{propName} to true");
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (shouldLog) Debug.LogWarning($"[BTS] Failed to set CurrentAction.{propName}: {ex.Message}");
                                    }
                                }
                            }
                            
                            // Also try to find and modify fields
                            var actionFields = actionType.GetFields(
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);
                            
                            foreach (var field in actionFields)
                            {
                                var fieldName = field.Name;
                                if ((fieldName.Contains("Time") || fieldName.Contains("Progress") || 
                                     fieldName.Contains("Charge") || fieldName.Contains("Elapsed") ||
                                     fieldName.Contains("Remaining") || fieldName.Contains("Duration")) && 
                                    (field.FieldType == typeof(float) || field.FieldType == typeof(double)))
                                {
                                    try
                                    {
                                        if (fieldName.Contains("Remaining") || fieldName.Contains("Elapsed") || 
                                            fieldName.Contains("Charge") || fieldName.Contains("Progress"))
                                        {
                                            field.SetValue(currentAction, 0f);
                                            if (shouldLog) Debug.Log($"[BTS] ✓ Set CurrentAction.{fieldName} field to 0");
                                        }
                                        else if (fieldName.Contains("Duration"))
                                        {
                                            field.SetValue(currentAction, 0.001f);
                                            if (shouldLog) Debug.Log($"[BTS] ✓ Set CurrentAction.{fieldName} field to 0.001");
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (shouldLog) Debug.LogWarning($"[BTS] Failed to set CurrentAction.{fieldName} field: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    if (shouldLog) Debug.LogWarning($"[BTS] Failed to check CurrentAction: {ex.Message}");
                }
                
                // Method 5b: Try to find SkillSystem/ActionSystem on player and modify active skill charge time
                try
                {
                    var playerType = player.GetType();
                    
                    // Look for SkillSystem or ActionSystem
                    string[] systemPropertyNames = { "SkillSystem", "ActionSystem", "SkillController", "ActionController" };
                    foreach (var systemPropName in systemPropertyNames)
                    {
                        var systemProp = playerType.GetProperty(systemPropName, 
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                            System.Reflection.BindingFlags.NonPublic);
                        
                        if (systemProp != null)
                        {
                            var system = systemProp.GetValue(player);
                            if (system != null)
                            {
                                var systemType = system.GetType();
                                if (shouldLog)
                                    Debug.Log($"[BTS] Found {systemPropName} on player: {systemType.Name}");
                                
                                // Try to find active skill/action
                                string[] activeSkillProps = { "ActiveSkill", "CurrentSkill", "ActiveAction", "CurrentAction", "ExecutingSkill", "ExecutingAction" };
                                foreach (var activeSkillPropName in activeSkillProps)
                                {
                                    var activeSkillProp = systemType.GetProperty(activeSkillPropName, 
                                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                        System.Reflection.BindingFlags.NonPublic);
                                    
                                    if (activeSkillProp != null)
                                    {
                                        var activeSkill = activeSkillProp.GetValue(system);
                                        if (activeSkill != null)
                                        {
                                            var skillType = activeSkill.GetType();
                                            if (shouldLog)
                                                Debug.Log($"[BTS] Found {activeSkillPropName}: {skillType.Name}");
                                            
                                            // Try to find charge/progress/time properties
                                            string[] skillTimeProps = { 
                                                "ChargeTime", "Progress", "ProgressTime", "CastTime", 
                                                "CurrentProgress", "CurrentCharge", "ElapsedTime",
                                                "RemainingTime", "TimeRemaining", "IsReady", "IsComplete"
                                            };
                                            
                                            foreach (var timePropName in skillTimeProps)
                                            {
                                                var timeProp = skillType.GetProperty(timePropName, 
                                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                                    System.Reflection.BindingFlags.NonPublic);
                                                
                                                if (timeProp != null && timeProp.CanWrite)
                                                {
                                                    var propType = timeProp.PropertyType;
                                                    try
                                                    {
                                                        if (propType == typeof(float) || propType == typeof(double))
                                                        {
                                                            // Set to max or 0 depending on property
                                                            if (timePropName.Contains("Remaining") || timePropName.Contains("Progress") || timePropName.Contains("Charge"))
                                                            {
                                                                timeProp.SetValue(activeSkill, 0f);
                                                                Debug.Log($"[BTS] ✓ Set {activeSkillPropName}.{timePropName} to 0");
                                                            }
                                                            else if (timePropName.Contains("Progress") || timePropName.Contains("Current"))
                                                            {
                                                                // Try to get max value or set to 1.0
                                                                var maxProp = skillType.GetProperty("Max" + timePropName, 
                                                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                                                    System.Reflection.BindingFlags.NonPublic);
                                                                if (maxProp != null)
                                                                {
                                                                    var maxValue = maxProp.GetValue(activeSkill);
                                                                    timeProp.SetValue(activeSkill, maxValue);
                                                                    Debug.Log($"[BTS] ✓ Set {activeSkillPropName}.{timePropName} to max value");
                                                                }
                                                                else
                                                                {
                                                                    timeProp.SetValue(activeSkill, 1.0f);
                                                                    Debug.Log($"[BTS] ✓ Set {activeSkillPropName}.{timePropName} to 1.0");
                                                                }
                                                            }
                                                        }
                                                        else if (propType == typeof(bool))
                                                        {
                                                            if (timePropName.Contains("Ready") || timePropName.Contains("Complete"))
                                                            {
                                                                timeProp.SetValue(activeSkill, true);
                                                                Debug.Log($"[BTS] ✓ Set {activeSkillPropName}.{timePropName} to true");
                                                            }
                                                        }
                                                    }
                                                    catch (System.Exception ex)
                                                    {
                                                        Debug.LogWarning($"[BTS] Failed to set {activeSkillPropName}.{timePropName}: {ex.Message}");
                                                    }
                                                }
                                            }
                                            
                                            // Try to call complete/ready methods
                                            string[] completeMethods = { 
                                                "Complete", "Finish", "Ready", "SetReady", 
                                                "ForceComplete", "InstantComplete", "SkipCharge"
                                            };
                                            
                                            foreach (var methodName in completeMethods)
                                            {
                                                var method = skillType.GetMethod(methodName, 
                                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                                    System.Reflection.BindingFlags.NonPublic);
                                                
                                                if (method != null)
                                                {
                                                    try
                                                    {
                                                        method.Invoke(activeSkill, null);
                                                        Debug.Log($"[BTS] ✓ Called {activeSkillPropName}.{methodName}()");
                                                    }
                                                    catch (System.Exception ex)
                                                    {
                                                        Debug.LogWarning($"[BTS] Failed to call {activeSkillPropName}.{methodName}(): {ex.Message}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                
                                // Also try to find charge/progress properties directly on the system
                                string[] systemTimeProps = { 
                                    "ChargeTime", "Progress", "CurrentCharge", "CurrentProgress",
                                    "ActiveChargeTime", "ActiveProgress"
                                };
                                
                                foreach (var timePropName in systemTimeProps)
                                {
                                    var timeProp = systemType.GetProperty(timePropName, 
                                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                        System.Reflection.BindingFlags.NonPublic);
                                    
                                    if (timeProp != null && timeProp.CanWrite)
                                    {
                                        try
                                        {
                                            timeProp.SetValue(system, 0f);
                                            Debug.Log($"[BTS] ✓ Set {systemPropName}.{timePropName} to 0");
                                        }
                                        catch (System.Exception ex)
                                        {
                                            Debug.LogWarning($"[BTS] Failed to set {systemPropName}.{timePropName}: {ex.Message}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BTS] Failed to check SkillSystem/ActionSystem: {ex.Message}");
                }
                
                // Method 6: Try to find and modify Item's UsageUtilities component
                try
                {
                    // UsageUtilities is an ItemComponent that has UseTime (read-only property) and useTime (private field)
                    var itemComponents = throwableItem.GetComponents<ItemStatsSystem.ItemComponent>();
                    foreach (var component in itemComponents)
                    {
                        if (component == null) continue;
                        var compType = component.GetType();
                        var compTypeName = compType.Name;
                        
                        // Look for UsageUtilities component
                        if (compTypeName == "UsageUtilities" || compTypeName.Contains("Usage"))
                        {
                            // Try to find and modify the private useTime field
                            var useTimeField = compType.GetField("useTime", 
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            
                            if (useTimeField != null)
                            {
                                useTimeField.SetValue(component, 0f);
                                if (shouldLog)
                                    Debug.Log($"[BTS] ✓ Set UsageUtilities.useTime field to 0");
                            }
                            
                            // Also try to find any progress-related fields
                            var allFields = compType.GetFields(
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                            
                            foreach (var field in allFields)
                            {
                                var fieldName = field.Name;
                                if ((fieldName.Contains("Time") || fieldName.Contains("Progress") || 
                                     fieldName.Contains("Charge") || fieldName.Contains("Elapsed")) && 
                                    (field.FieldType == typeof(float) || field.FieldType == typeof(double)))
                                {
                                    try
                                    {
                                        field.SetValue(component, 0f);
                                        if (shouldLog)
                                            Debug.Log($"[BTS] ✓ Set UsageUtilities.{fieldName} field to 0");
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (shouldLog)
                                            Debug.LogWarning($"[BTS] Failed to set UsageUtilities.{fieldName}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    if (shouldLog)
                        Debug.LogWarning($"[BTS] Failed to modify UsageUtilities: {ex.Message}");
                }
                
                // Method 7: Try to find all properties and methods on CurrentHoldItemAgent recursively
                try
                {
                    var playerType = player.GetType();
                    var currentHoldItemAgentProp = playerType.GetProperty("CurrentHoldItemAgent", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic);
                    
                    if (currentHoldItemAgentProp != null)
                    {
                        var agent = currentHoldItemAgentProp.GetValue(player);
                        if (agent != null)
                        {
                            var agentType = agent.GetType();
                            
                            // Dump all properties for debugging
                            var allProps = agentType.GetProperties(
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | 
                                System.Reflection.BindingFlags.NonPublic);
                            
                            // Log type info only on first attempt to avoid spam
                            if (shouldLog)
                            {
                                Debug.Log($"[BTS] CurrentHoldItemAgent type: {agentType.Name}, properties count: {allProps.Length}");
                                // Log all property names for debugging
                                foreach (var prop in allProps)
                                {
                                    Debug.Log($"[BTS]   - Property: {prop.Name} ({prop.PropertyType.Name}), CanWrite: {prop.CanWrite}");
                                }
                            }
                            
                            foreach (var prop in allProps)
                            {
                                var propName = prop.Name;
                                // Look for anything that might be related to time/charge/progress
                                if ((propName.Contains("Time") || propName.Contains("Charge") || propName.Contains("Progress") || 
                                     propName.Contains("Ready") || propName.Contains("Complete") || propName.Contains("Elapsed")) && prop.CanWrite)
                                {
                                    try
                                    {
                                        var propType = prop.PropertyType;
                                        if (propType == typeof(float) || propType == typeof(double))
                                        {
                                            prop.SetValue(agent, 0f);
                                            if (shouldLog)
                                                Debug.Log($"[BTS] ✓ Set CurrentHoldItemAgent.{propName} to 0");
                                        }
                                        else if (propType == typeof(bool))
                                        {
                                            if (propName.Contains("Ready") || propName.Contains("Complete"))
                                            {
                                                prop.SetValue(agent, true);
                                                if (shouldLog)
                                                    Debug.Log($"[BTS] ✓ Set CurrentHoldItemAgent.{propName} to true");
                                            }
                                        }
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (shouldLog)
                                            Debug.LogWarning($"[BTS] Failed to set CurrentHoldItemAgent.{propName}: {ex.Message}");
                                    }
                                }
                            }
                            
                            // Also try to find and modify fields
                            var allFields = agentType.GetFields(
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | 
                                System.Reflection.BindingFlags.Instance);
                            
                            foreach (var field in allFields)
                            {
                                var fieldName = field.Name;
                                if ((fieldName.Contains("Time") || fieldName.Contains("Progress") || 
                                     fieldName.Contains("Charge") || fieldName.Contains("Elapsed")) && 
                                    (field.FieldType == typeof(float) || field.FieldType == typeof(double)))
                                {
                                    try
                                    {
                                        field.SetValue(agent, 0f);
                                        if (shouldLog)
                                            Debug.Log($"[BTS] ✓ Set CurrentHoldItemAgent.{fieldName} field to 0");
                                    }
                                    catch (System.Exception ex)
                                    {
                                        if (shouldLog)
                                            Debug.LogWarning($"[BTS] Failed to set CurrentHoldItemAgent.{fieldName} field: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    if (shouldLog)
                        Debug.LogWarning($"[BTS] Failed to dump CurrentHoldItemAgent properties: {ex.Message}");
                }
                
                // Method 8: Try to find UI progress bar and hide/complete it
                try
                {
                    // Look for UI elements that might show progress
                    var progressBarObjects = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Slider>();
                    foreach (var slider in progressBarObjects)
                    {
                        if (slider != null && slider.gameObject != null && slider.gameObject.activeInHierarchy)
                        {
                            var sliderName = slider.gameObject.name;
                            // Look for progress bar related to actions/skills
                            if (sliderName.Contains("Progress") || sliderName.Contains("Charge") || 
                                sliderName.Contains("Action") || sliderName.Contains("Skill") ||
                                sliderName.Contains("取消") || sliderName.Contains("Cancel"))
                            {
                                // Set slider to max value (complete)
                                slider.value = slider.maxValue;
                                if (shouldLog)
                                    Debug.Log($"[BTS] ✓ Set UI Slider '{sliderName}' to max value ({slider.maxValue})");
                            }
                        }
                    }
                    
                    // Also try to find Image components that might show progress
                    var images = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Image>();
                    foreach (var image in images)
                    {
                        if (image != null && image.gameObject != null && image.gameObject.activeInHierarchy)
                        {
                            var imageName = image.gameObject.name;
                            var parentName = image.transform.parent != null ? image.transform.parent.name : "";
                            // Look for fill images in progress bars
                            if ((imageName.Contains("Fill") || imageName.Contains("Progress")) && 
                                (parentName.Contains("Progress") || parentName.Contains("Charge") || 
                                 parentName.Contains("Action") || parentName.Contains("取消")))
                            {
                                // Set fill amount to 1.0 (complete)
                                image.fillAmount = 1.0f;
                                if (shouldLog)
                                    Debug.Log($"[BTS] ✓ Set UI Image '{imageName}' fillAmount to 1.0");
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    if (shouldLog)
                        Debug.LogWarning($"[BTS] Failed to find UI progress bars: {ex.Message}");
                }
                
                // Only log completion occasionally
                if (shouldLog)
                    Debug.Log($"[BTS] ✓ Finished attempting to skip throw preparation time for {throwableItem.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BTS] Error in TrySkipThrowPreparationTime: {ex.Message}");
                Debug.LogError($"[BTS] Stack trace: {ex.StackTrace}");
            }
        }
        
        private void TryApplyWarmGrenadeSettings(Item item)
        {
            if (!enableWarmGrenades || item == null)
            {
                return;
            }
            
            if (!warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
            {
                return;
            }
            
            int instanceId = item.GetInstanceID();
            if (warmGrenadeAppliedItemInstanceIDs.Contains(instanceId))
            {
                return;
            }
            
            CleanupWarmGrenadeStates();
            
            var stateList = new List<WarmGrenadeObjectState>();
            bool anyChange = false;
            bool requiresDeferredZero = warmGrenadeDeferredZeroTypeIDs.Contains(item.TypeID);
            bool isInPlayerInventory = IsItemInPlayerInventory(item);
            bool shouldZeroDelayNow = !requiresDeferredZero || !isInPlayerInventory;
            
            if (warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
            {
                LogWarmGrenadeDiagnostics(item, "BeforeApply");
            }

            try
            {
                anyChange |= ApplyWarmSettingsToObject(item, "Item", stateList, shouldZeroDelayNow);
                
                var itemType = item.GetType();
                var skillContextProp = itemType.GetProperty(
                    "SkillContext",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (skillContextProp != null)
                {
                    var skillContext = skillContextProp.GetValue(item);
                    if (skillContext != null)
                    {
                        anyChange |= ApplyWarmSettingsToObject(skillContext, "SkillContext", stateList, shouldZeroDelayNow);
                    }
                }
                
                var components = item.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }
                    
                    var compType = component.GetType();
                    if (compType == typeof(Transform) || compType == typeof(RectTransform))
                    {
                        continue;
                    }
                    
                    anyChange |= ApplyWarmSettingsToObject(component, compType.Name, stateList, shouldZeroDelayNow);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to apply warm grenade settings: {ex.Message}");
            }
            
            if (anyChange && stateList.Count > 0)
            {
                warmGrenadeOriginalStates[instanceId] = stateList;
                Debug.Log($"[BTS] Warm grenade impact detonation applied to {item.name} (TypeID {item.TypeID})");
            }
            
            warmGrenadeAppliedItemInstanceIDs.Add(instanceId);

            if (requiresDeferredZero && isInPlayerInventory)
            {
                warmGrenadeDeferredItems[instanceId] = new WarmGrenadeDeferredInfo(item, Time.time);
                if (warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
                {
                    LogWarmGrenadeDiagnostics(item, "DeferredPending");
                }
            }
            else
            {
                warmGrenadeDeferredItems.Remove(instanceId);
                AttachWarmGrenadeImpactRelays(item);
                if (warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
                {
                    LogWarmGrenadeDiagnostics(item, "AfterImmediate");
                }
            }
        }

        private bool IsItemInPlayerInventory(Item item)
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                return item.IsInPlayerCharacter();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to check IsInPlayerCharacter for {item.name}: {ex.Message}");
                return false;
            }
        }

        private void ProcessWarmGrenadeDeferredItems()
        {
            if (warmGrenadeDeferredItems.Count == 0)
            {
                return;
            }

            var toRemove = new List<int>();
            foreach (var kvp in warmGrenadeDeferredItems)
            {
                int instanceId = kvp.Key;
                var info = kvp.Value;
                var item = info.Item;

                if (item == null || !warmGrenadeAppliedItemInstanceIDs.Contains(instanceId))
                {
                    toRemove.Add(instanceId);
                    continue;
                }

                if (!enableWarmGrenades)
                {
                    toRemove.Add(instanceId);
                    continue;
                }

                bool leftInventory = !IsItemInPlayerInventory(item);
                bool timeoutReached = (Time.time - info.StartTime) >= 2f;
                if (!leftInventory && !timeoutReached)
                {
                    continue;
                }

                if (!warmGrenadeOriginalStates.TryGetValue(instanceId, out var stateList))
                {
                    stateList = new List<WarmGrenadeObjectState>();
                    warmGrenadeOriginalStates[instanceId] = stateList;
                }

                bool anyChange = false;

                try
                {
                    anyChange |= ApplyWarmSettingsToObject(item, "ItemDeferred", stateList, true);

                    var itemType = item.GetType();
                    var skillContextProp = itemType.GetProperty(
                        "SkillContext",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (skillContextProp != null)
                    {
                        var skillContext = skillContextProp.GetValue(item);
                        if (skillContext != null)
                        {
                            anyChange |= ApplyWarmSettingsToObject(skillContext, "SkillContextDeferred", stateList, true);
                        }
                    }

                    var components = item.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            continue;
                        }

                        var compType = component.GetType();
                        if (compType == typeof(Transform) || compType == typeof(RectTransform))
                        {
                            continue;
                        }

                        anyChange |= ApplyWarmSettingsToObject(component, compType.Name + "Deferred", stateList, true);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BTS] Deferred warm grenade fuse reduction failed: {ex.Message}");
                }

                if (anyChange)
                {
                    Debug.Log($"[BTS] Warm grenade deferred fuse reduction applied to {item.name} (TypeID {item.TypeID})");
                }

                AttachWarmGrenadeImpactRelays(item);
                if (warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
                {
                    LogWarmGrenadeDiagnostics(item, leftInventory ? "AfterDeferredZero" : "AfterDeferredTimeout");
                }

                toRemove.Add(instanceId);
            }

            foreach (var instanceId in toRemove)
            {
                warmGrenadeDeferredItems.Remove(instanceId);
            }
        }

        private void AttachWarmGrenadeImpactRelays(Item item)
        {
            if (item == null)
            {
                return;
            }

            try
            {
                var colliders = item.GetComponentsInChildren<Collider>(true);
                if (colliders == null || colliders.Length == 0)
                {
                    return;
                }

                foreach (var collider in colliders)
                {
                    if (collider == null)
                    {
                        continue;
                    }

                    var relay = collider.gameObject.GetComponent<WarmGrenadeImpactRelay>();
                    if (relay == null)
                    {
                        relay = collider.gameObject.AddComponent<WarmGrenadeImpactRelay>();
                    }
                    relay.Initialize(item, this);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to attach warm grenade impact relays: {ex.Message}");
            }
        }

        private bool AttemptWarmGrenadeImpactDetonation(Item item)
        {
            if (item == null)
            {
                return false;
            }

            int instanceId = item.GetInstanceID();
            if (warmGrenadeDetonatedInstanceIDs.Contains(instanceId))
            {
                return true;
            }

            bool exploded = ForceWarmGrenadeExplosion(item);
            if (!exploded)
            {
                var components = item.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }
                    if (ForceWarmGrenadeExplosion(component))
                    {
                        exploded = true;
                        break;
                    }
                }
            }

            if (!exploded)
            {
                var allComponents = item.GetComponentsInChildren<Component>(true);
                foreach (var component in allComponents)
                {
                    if (component == null)
                    {
                        continue;
                    }
                    if (ForceWarmGrenadeExplosion(component))
                    {
                        exploded = true;
                        break;
                    }
                }
            }

            if (exploded)
            {
                warmGrenadeDetonatedInstanceIDs.Add(instanceId);
                warmGrenadeAppliedItemInstanceIDs.Remove(instanceId);
                warmGrenadeOriginalStates.Remove(instanceId);
                if (warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
                {
                    LogWarmGrenadeDiagnostics(item, "ImpactDetonated");
                }
            }
            else if (warmGrenadeCandidateTypeIDs.Contains(item.TypeID))
            {
                LogWarmGrenadeDiagnostics(item, "ImpactAttemptFailed");
            }

            return exploded;
        }

        private bool ForceWarmGrenadeExplosion(object target)
        {
            if (target == null)
            {
                return false;
            }

            var type = target.GetType();
            foreach (var methodName in WarmGrenadeExplosionMethodNames)
            {
                try
                {
                    var method = type.GetMethod(methodName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);
                    if (method != null)
                    {
                        method.Invoke(target, null);
                        Debug.Log($"[BTS] Warm grenade forced explosion via {type.Name}.{method.Name}");
                        return true;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BTS] Failed to invoke {methodName} on {type.Name}: {ex.Message}");
                }
            }

            return false;
        }

        private void LogWarmGrenadeDiagnostics(Item item, string phase)
        {
            if (item == null)
            {
                return;
            }

            string key = $"{item.GetInstanceID()}:{phase}";
            if (!warmGrenadeDiagnosticsLoggedPhases.Add(key))
            {
                return;
            }

            try
            {
                Debug.Log($"[BTS][WarmDiag] Phase={phase}, Item={item.name}, TypeID={item.TypeID}");
                LogWarmObjectDiagnostics(item, phase, "ItemRoot");

                var components = item.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }
                    LogWarmObjectDiagnostics(component, phase, component.GetType().Name);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS][WarmDiag] Failed to log diagnostics for {item.name}: {ex.Message}");
            }
        }

        private void LogWarmObjectDiagnostics(object target, string phase, string context)
        {
            if (target == null)
            {
                return;
            }

            try
            {
                var type = target.GetType();
                var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

                foreach (var prop in type.GetProperties(bindingFlags))
                {
                    if (!prop.CanRead)
                    {
                        continue;
                    }

                    string nameLower = prop.Name.ToLowerInvariant();
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeDiagnosticKeywords))
                    {
                        continue;
                    }

                    object? value = null;
                    try
                    {
                        value = prop.GetValue(target);
                    }
                    catch
                    {
                        continue;
                    }

                    Debug.Log($"[BTS][WarmDiag] Phase={phase} Context={context} Property={prop.Name} Value={value ?? "null"}");
                }

                foreach (var field in type.GetFields(bindingFlags))
                {
                    string nameLower = field.Name.ToLowerInvariant();
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeDiagnosticKeywords))
                    {
                        continue;
                    }

                    object? value = null;
                    try
                    {
                        value = field.GetValue(target);
                    }
                    catch
                    {
                        continue;
                    }

                    Debug.Log($"[BTS][WarmDiag] Phase={phase} Context={context} Field={field.Name} Value={value ?? "null"}");
                }

                var methods = type.GetMethods(bindingFlags)
                    .Where(m => m.GetParameters().Length == 0 && ContainsAnyKeyword(m.Name.ToLowerInvariant(), WarmGrenadeDiagnosticKeywords))
                    .Select(m => m.Name)
                    .Distinct()
                    .Take(10);

                foreach (var methodName in methods)
                {
                    Debug.Log($"[BTS][WarmDiag] Phase={phase} Context={context} Method={type.Name}.{methodName}()");
                }
            }
            catch
            {
                // Ignore diagnostics errors
            }
        }

        private class WarmGrenadeImpactRelay : MonoBehaviour
        {
            private Item trackedItem;
            private ModBehaviour owner;
            private bool triggered;

            public void Initialize(Item item, ModBehaviour behaviour)
            {
                trackedItem = item;
                owner = behaviour;
                triggered = false;
            }

            private void OnCollisionEnter(Collision collision)
            {
                if (collision == null)
                {
                    return;
                }

                float impactSpeed = collision.relativeVelocity.magnitude;
                if (impactSpeed < 0.5f)
                {
                    return;
                }

                TryDetonate(impactSpeed);
            }

            private void TryDetonate(float impactSpeed)
            {
                if (triggered || trackedItem == null || owner == null)
                {
                    return;
                }

                if (!owner.enableWarmGrenades)
                {
                    return;
                }

                if (owner.IsItemInPlayerInventory(trackedItem))
                {
                    return;
                }

                if (owner.AttemptWarmGrenadeImpactDetonation(trackedItem))
                {
                    triggered = true;
                    Debug.Log($"[BTS] Warm grenade impact detonation triggered (speed={impactSpeed:0.00}) for {trackedItem.name} (TypeID {trackedItem.TypeID}) at position {trackedItem.transform.position}");
                }
                else if (impactSpeed >= 5f)
                {
                    Debug.Log($"[BTS] Warm grenade impact attempt failed (speed={impactSpeed:0.00}) for {trackedItem.name} (TypeID {trackedItem.TypeID})");
                }
            }
        }
        
        private bool ApplyWarmSettingsToObject(object target, string context, List<WarmGrenadeObjectState> stateList, bool zeroDelayValues)
        {
            if (target == null)
            {
                return false;
            }
            
            _ = context;
            
            var type = target.GetType();
            var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
            bool changed = false;
            var state = new WarmGrenadeObjectState(target);
            
            foreach (var prop in type.GetProperties(bindingFlags))
            {
                if (!prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }
                
                if (prop.GetIndexParameters().Length > 0)
                {
                    continue;
                }
                
                var propType = prop.PropertyType;
                string nameLower = prop.Name.ToLowerInvariant();
                
                if (propType == typeof(bool))
                {
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeBoolKeywords))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var valueObj = prop.GetValue(target);
                        if (valueObj is bool boolValue && !boolValue)
                        {
                            state.PropertyValues.Add((prop, valueObj));
                            prop.SetValue(target, true);
                            changed = true;
                        }
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
                else if ((propType == typeof(float) || propType == typeof(double)) && zeroDelayValues)
                {
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeZeroKeywords) || ContainsAnyKeyword(nameLower, WarmGrenadeZeroExcludeKeywords))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var valueObj = prop.GetValue(target);
                        if (valueObj == null)
                        {
                            continue;
                        }
                        
                        if (propType == typeof(float))
                        {
                            float floatValue = (float)valueObj;
                            if (floatValue > 0f)
                            {
                                state.PropertyValues.Add((prop, valueObj));
                                prop.SetValue(target, 0f);
                                changed = true;
                            }
                        }
                        else
                        {
                            double doubleValue = (double)valueObj;
                            if (doubleValue > 0d)
                            {
                                state.PropertyValues.Add((prop, valueObj));
                                prop.SetValue(target, 0d);
                                changed = true;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
                else if (propType == typeof(int) && zeroDelayValues)
                {
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeZeroKeywords) || ContainsAnyKeyword(nameLower, WarmGrenadeZeroExcludeKeywords))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var valueObj = prop.GetValue(target);
                        if (valueObj is int intValue && intValue > 0)
                        {
                            state.PropertyValues.Add((prop, valueObj));
                            prop.SetValue(target, 0);
                            changed = true;
                        }
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
            }
            
            foreach (var field in type.GetFields(bindingFlags))
            {
                if (field.IsInitOnly)
                {
                    continue;
                }
                
                var fieldType = field.FieldType;
                string nameLower = field.Name.ToLowerInvariant();
                
                if (fieldType == typeof(bool))
                {
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeBoolKeywords))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var valueObj = field.GetValue(target);
                        if (valueObj is bool boolValue && !boolValue)
                        {
                            state.FieldValues.Add((field, valueObj));
                            field.SetValue(target, true);
                            changed = true;
                        }
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
                else if ((fieldType == typeof(float) || fieldType == typeof(double)) && zeroDelayValues)
                {
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeZeroKeywords) || ContainsAnyKeyword(nameLower, WarmGrenadeZeroExcludeKeywords))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var valueObj = field.GetValue(target);
                        if (valueObj == null)
                        {
                            continue;
                        }
                        
                        if (fieldType == typeof(float))
                        {
                            float floatValue = (float)valueObj;
                            if (floatValue > 0f)
                            {
                                state.FieldValues.Add((field, valueObj));
                                field.SetValue(target, 0f);
                                changed = true;
                            }
                        }
                        else
                        {
                            double doubleValue = (double)valueObj;
                            if (doubleValue > 0d)
                            {
                                state.FieldValues.Add((field, valueObj));
                                field.SetValue(target, 0d);
                                changed = true;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
                else if (fieldType == typeof(int) && zeroDelayValues)
                {
                    if (!ContainsAnyKeyword(nameLower, WarmGrenadeZeroKeywords) || ContainsAnyKeyword(nameLower, WarmGrenadeZeroExcludeKeywords))
                    {
                        continue;
                    }
                    
                    try
                    {
                        var valueObj = field.GetValue(target);
                        if (valueObj is int intValue && intValue > 0)
                        {
                            state.FieldValues.Add((field, valueObj));
                            field.SetValue(target, 0);
                            changed = true;
                        }
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
            }
            
            if (changed)
            {
                stateList.Add(state);
            }
            
            return changed;
        }
        
        private static bool ContainsAnyKeyword(string source, string[] keywords)
        {
            if (string.IsNullOrEmpty(source) || keywords == null || keywords.Length == 0)
            {
                return false;
            }
            
            foreach (var keyword in keywords)
            {
                if (string.IsNullOrEmpty(keyword))
                {
                    continue;
                }
                
                if (source.Contains(keyword))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void RestoreWarmGrenadeSettings()
        {
            warmGrenadeDeferredItems.Clear();
            warmGrenadeDetonatedInstanceIDs.Clear();

            if (warmGrenadeOriginalStates.Count == 0)
            {
                warmGrenadeAppliedItemInstanceIDs.Clear();
                return;
            }
            
            var keys = warmGrenadeOriginalStates.Keys.ToList();
            foreach (var instanceId in keys)
            {
                RestoreWarmGrenadeSettingsForItem(instanceId);
            }
            
            warmGrenadeAppliedItemInstanceIDs.Clear();
        }
        
        private void RestoreWarmGrenadeSettingsForItem(int instanceId)
        {
            if (!warmGrenadeOriginalStates.TryGetValue(instanceId, out var stateList))
            {
                warmGrenadeAppliedItemInstanceIDs.Remove(instanceId);
                return;
            }
            
            foreach (var state in stateList)
            {
                if (!state.Target.TryGetTarget(out var target))
                {
                    continue;
                }
                
                foreach (var (property, value) in state.PropertyValues)
                {
                    try
                    {
                        property.SetValue(target, value);
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
                
                foreach (var (field, value) in state.FieldValues)
                {
                    try
                    {
                        field.SetValue(target, value);
                    }
                    catch
                    {
                        // Ignore reflection failures
                    }
                }
            }
            
            warmGrenadeOriginalStates.Remove(instanceId);
            warmGrenadeAppliedItemInstanceIDs.Remove(instanceId);
        }
        
        private void CleanupWarmGrenadeStates()
        {
            if (warmGrenadeOriginalStates.Count == 0)
            {
                return;
            }
            
            var toRemove = new List<int>();
            foreach (var kvp in warmGrenadeOriginalStates)
            {
                bool hasAliveTarget = false;
                
                foreach (var state in kvp.Value)
                {
                    if (state.Target.TryGetTarget(out _))
                    {
                        hasAliveTarget = true;
                        break;
                    }
                }
                
                if (!hasAliveTarget)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var instanceId in toRemove)
            {
                warmGrenadeOriginalStates.Remove(instanceId);
                warmGrenadeAppliedItemInstanceIDs.Remove(instanceId);
                warmGrenadeDeferredItems.Remove(instanceId);
                warmGrenadeDetonatedInstanceIDs.Remove(instanceId);
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
        /// In continuous throw mode, only switch back if no more throwables of the same type exist
        /// </summary>
        private System.Collections.IEnumerator SwitchBackAfterThrowDelay()
        {
            Debug.Log("[BTS] ⏳ Waiting for throw animation to complete...");
            Debug.Log($"[BTS] Will switch back to - Slot: {previousEquippedSlot}, Key: {previousEquippedKey}");
            
            // Wait a bit longer to ensure throw animation completes (0.3 seconds)
            yield return new WaitForSeconds(0.3f);
            
            // Get player reference once for reuse
            var player = FindPlayerCharacter();
            
            // Check if continuous throw mode is enabled and we're in Equip mode
            Debug.Log($"[BTS] SwitchBackAfterThrowDelay - enableContinuousThrow: {enableContinuousThrow}, throwMode: {throwMode}, lastSelectedThrowableTypeID: {lastSelectedThrowableTypeID.HasValue}");
            
            // Only check for continuous throw if:
            // 1. Continuous throw is enabled
            // 2. We're in Equip mode
            // 3. We have a last selected throwable
            if (enableContinuousThrow && throwMode == ThrowMode.Equip && lastSelectedThrowableTypeID.HasValue)
            {
                // Check if there are more throwables of the same type in inventory
                int? nextSlot = FindNextThrowableSlotOfType(lastSelectedThrowableTypeID.Value);
                
                if (nextSlot.HasValue)
                {
                    Debug.Log($"[BTS] 🔄 Continuous throw mode: More throwables of TypeID {lastSelectedThrowableTypeID.Value} found in slot {nextSlot.Value} - re-equipping");
                    // Re-equip the next throwable of the same type
                    if (player != null)
                    {
                        // Wait a bit for the throw animation to complete
                        yield return new WaitForSeconds(0.1f);
                        
                        // Try to equip the next throwable
                        if (SwitchToSlot(nextSlot.Value))
                        {
                            lastEquippedThrowableSlot = nextSlot.Value;
                            lastSelectedThrowableSlot = nextSlot.Value;
                            Debug.Log($"[BTS] ✓ Successfully re-equipped throwable from slot {nextSlot.Value} for continuous throwing");
                            yield break; // Don't switch back to weapon
                        }
                        else
                        {
                            Debug.LogWarning($"[BTS] ⚠ Failed to re-equip throwable from slot {nextSlot.Value}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"[BTS] 🔄 Continuous throw mode: No more throwables of TypeID {lastSelectedThrowableTypeID.Value} - switching back to weapon");
                }
            }
            
            Debug.Log("[BTS] ⚡ Now attempting to switch back to weapon...");
            AutoSwitchBackToWeaponImmediately();
            
            // If that failed, try again with longer delay (fallback)
            yield return new WaitForSeconds(0.5f);
            
            if (player != null)
            {
                var currentItem = GetCurrentHoldItem(player);
                // Check if we're still holding a throwable (should not happen if quick press cycle is disabled)
                if (currentItem != null && IsThrowableItem(currentItem))
                {
                    Debug.LogWarning($"[BTS] ⚠️ Still holding throwable after switch back attempt: {currentItem.name} - retrying...");
                    AutoSwitchBackToWeaponImmediately();
                }
                else if (currentItem == null && previousEquippedSlot.HasValue)
                {
                    // Still empty hand - try switching again
                    Debug.Log("[BTS] Still empty hand, retrying switch back...");
                    AutoSwitchBackToWeaponImmediately();
                }
            }
        }
        
        /// <summary>
        /// Find the next available slot containing a throwable of the specified TypeID
        /// Returns the slot index if found, null otherwise
        /// </summary>
        private int? FindNextThrowableSlotOfType(int typeID)
        {
            try
            {
                var player = FindPlayerCharacter();
                if (player == null) return null;
                
                var inventory = player.GetComponent<Inventory>() ?? player.GetComponentInChildren<Inventory>();
                if (inventory == null) return null;
                
                var inventoryType = inventory.GetType();
                var getItemMethod = inventoryType.GetMethod(
                    "GetItem",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                ) ?? inventoryType.GetMethod(
                    "GetItemAt",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
                );
                
                if (getItemMethod == null) return null;
                
                // Get max slots
                int maxSlots = 50;
                var maxSlotsProp = inventoryType.GetProperty("maxSlots",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var slotCountProp = inventoryType.GetProperty("SlotCount",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                
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
                
                // Scan all slots for throwables of the same TypeID
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    try
                    {
                        var item = getItemMethod.Invoke(inventory, new object[] { slotIndex }) as Item;
                        if (item != null && IsThrowableItem(item) && item.TypeID == typeID)
                        {
                            // Found a throwable of the same type
                            int count = GetItemCount(item);
                            if (count > 0)
                            {
                                Debug.Log($"[BTS] Found {count} more throwable(s) of TypeID {typeID} in slot {slotIndex}");
                                return slotIndex;
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid slots
                    }
                }
                
                Debug.Log($"[BTS] No more throwables of TypeID {typeID} found in inventory");
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Error checking for more throwables of TypeID {typeID}: {ex.Message}");
                return null;
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
        /// PERFORMANCE: Results are cached by TypeID to avoid repeated expensive checks
        /// </summary>
        private bool IsThrowableItem(Item item)
        {
            if (item == null) return false;
            
            // PERFORMANCE: Check cache first (TypeID-based caching)
            int typeID = item.TypeID;
            
            // Check if this TypeID is disabled in the recognition list FIRST (before any other checks)
            // This allows users to disable specific throwables even if they match other criteria
            if (enabledThrowableTypeIDs.ContainsKey(typeID) && !enabledThrowableTypeIDs[typeID])
            {
                // TypeID is disabled by user - return false immediately
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.Log($"[BTS] ❌ Item {item.name} (TypeID: {typeID}) is disabled in recognition list - skipping all checks");
                }
                throwableItemCache[typeID] = false;
                return false;
            }
            
            if (throwableItemCache.ContainsKey(typeID))
            {
                return throwableItemCache[typeID];
            }
            
            // Remove (Clone) suffix for better matching
            var rawName = item.name ?? "";
            var name = rawName.ToLower().Replace("(clone)", "").Trim();
            var displayName = rawName.Replace("(Clone)", "").Trim();
            
            if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
            {
                Debug.Log($"[BTS] IsThrowableItem check: {item.name} (TypeID: {item.TypeID})");
            }
            
            // STEP 0: Exclude known non-throwable items by TypeID (highest priority - blacklist)
            // Also check item type/class name to exclude categories (totems, clothing, injections)
            int[] excludedTypeIDs = { 
                12,  // BeanCan - 豆子罐头（不是投掷物）
                25,  // Flashlight - 手电筒（不是投掷物）
                740, // WhiteGown - 白色长袍（衣服）
                742, // GlassShiny - 可能是衣服或装饰品
                800  // Injection_MeleeDamage - 针剂（不是投掷物）
            };
            if (excludedTypeIDs.Contains(item.TypeID))
            {
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.Log($"[BTS] Item {item.name} (TypeID: {item.TypeID}) excluded - in blacklist");
                }
                throwableItemCache[typeID] = false;
                return false;
            }
            
            // Check item type name to exclude categories (totem, clothing, injection)
            // This catches items by their class name, not just by item name
            try
            {
                var itemTypeName = item.GetType().Name.ToLower();
                if (itemTypeName.Contains("totem") || 
                    itemTypeName.Contains("clothing") || 
                    itemTypeName.Contains("clothes") || 
                    itemTypeName.Contains("uniform") ||
                    itemTypeName.Contains("gown") ||
                    itemTypeName.Contains("injection") ||
                    itemTypeName.Contains("syringe") ||
                    itemTypeName.Contains("medicine") ||
                    itemTypeName.Contains("med"))
                {
                    if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                    {
                        Debug.Log($"[BTS] Item {item.name} (TypeID: {item.TypeID}, Type: {item.GetType().Name}) excluded - item type category");
                    }
                    throwableItemCache[typeID] = false;
                    return false;
                }
            }
            catch (System.Exception)
            {
                // Ignore reflection errors
            }
            
            // Exclude by name patterns that are definitely not throwables
            // Enhanced exclusion list: totems, clothing, injections/medicine
            string[] excludedNamePatterns = {
                // Food & consumables
                "bean", "豆子", "罐头", "can", "candy", "糖果", "自制糖果",
                // Tools
                "flashlight", "手电",
                // Weapons
                "冲锋枪", "rifle", "gun", "weapon", "枪",
                // Toys
                "toy", "玩具", "cannon", "大炮", "玩具大炮",
                // Clothing & uniforms (all clothing items)
                "rubber", "橡胶", "工作服", "uniform", "suit", "clothing", "clothes", 
                "衣服", "服装", "gown", "dress", "shirt", "pants", "jacket", "coat", 
                "vest", "boots", "shoes", "hat", "cap", "helmet", "gloves", "手套",
                "armor", "护甲", "workwear", "工作装", "防护服",
                // Totems (all totems should be excluded)
                "totem", "图腾",
                // Injections & medicine (all medical items)
                "injection", "针剂", "syringe", "注射器", "medicine", "药品", 
                "med", "pill", "药丸", "drug", "药物", "heal", "治疗", "cure", "治愈"
            };
            foreach (var pattern in excludedNamePatterns)
            {
                if (name.Contains(pattern.ToLower()) && !IsThrowableException(item, pattern))
                {
                    if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                    {
                        Debug.Log($"[BTS] Item {item.name} excluded - matches excluded pattern: {pattern}");
                    }
                    throwableItemCache[typeID] = false;
                    return false;
                }
            }
            
            // STEP 1: Check by known throwable TypeIDs (most reliable - whitelist)
            // Only these specific throwables are recognized:
            // 手雷(Grenade), 烟雾弹(SmokeGrenade), 电机手雷(ElecGrenade), 燃烧弹(FireGrenade), 
            // 毒物弹(ToxGrenade), 闪光手雷(FlashGrenade), 管状炸弹(Dynamite/DynamiteMultiple), 
            // 集束管状炸弹, 粪球(ShitBall)
            int[] throwableTypeIDs = { 
                67,    // Grenade - 手雷
                660,   // SmokeGrenade - 烟雾弹
                942,   // ElecGrenade - 电机手雷
                941,   // FireGrenade - 燃烧弹
                933,   // ToxGrenade - 毒物弹
                66,    // FlashGrenade - 闪光手雷
                23,    // Dynamite - 管状炸弹（单个）
                24,    // DynamiteMultiple - 管状炸弹（多个）
                // TODO: Add TypeID for 集束管状炸弹 when identified
                1257   // ShitBall - 粪球
            };
            if (throwableTypeIDs.Contains(item.TypeID))
            {
                // Check if this TypeID is enabled in the recognition list
                if (enabledThrowableTypeIDs.ContainsKey(item.TypeID) && !enabledThrowableTypeIDs[item.TypeID])
                {
                    // TypeID is disabled by user
                    if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                    {
                        Debug.Log($"[BTS] ❌ Item {item.name} (TypeID: {item.TypeID}) is disabled in recognition list");
                    }
                    throwableItemCache[typeID] = false;
                    return false;
                }
                
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.Log($"[BTS] ✅ Item {item.name} (TypeID: {item.TypeID}) identified as throwable - TypeID whitelist");
                }
                throwableItemCache[typeID] = true;
                return true;
            }
            
            // STEP 1.5: Check by name patterns (whitelist only)
            // Only recognize these specific throwable names:
            string[] throwableNamePatterns = {
                "grenade", "手雷",
                "smoke", "烟雾弹",
                "elec", "电机手雷", "电机",
                "fire", "燃烧弹", "燃烧",
                "tox", "毒物弹", "毒物",
                "flash", "闪光手雷", "闪光",
                "dynamite", "管状炸弹", "管状",
                "shit", "粪球", "shitball"
                // Note: 集束管状炸弹 - add pattern when identified
            };
            bool matchesThrowableName = false;
            foreach (var pattern in throwableNamePatterns)
            {
                if (name.Contains(pattern.ToLower()))
                {
                    matchesThrowableName = true;
                    break;
                }
            }
            
            if (!matchesThrowableName)
            {
                // Not in whitelist, reject
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.Log($"[BTS] Item {item.name} (TypeID: {item.TypeID}) rejected - not in throwable name whitelist");
                }
                throwableItemCache[typeID] = false;
                return false;
            }
            
            // STEP 2: If name matches whitelist, check SkillType property to confirm
            // (This is now only for items that already passed name whitelist check)
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
                                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                                {
                                    Debug.Log($"[BTS] Item {item.name} identified as throwable via SkillType: {skillType}");
                                }
                                throwableItemCache[typeID] = true;
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
            
            // STEP 3: If name matches whitelist but SkillType check failed, check item properties/methods
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
                                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                                {
                                    Debug.Log($"[BTS] Item {item.name} identified as throwable via property {propName}");
                                }
                                throwableItemCache[typeID] = true;
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
                                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                                {
                                    Debug.Log($"[BTS] Item {item.name} identified as throwable via method {methodName}()");
                                }
                                throwableItemCache[typeID] = true;
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
            
            // If we reach here, item name matches whitelist but other checks failed
            // Reject it to be safe (strict whitelist mode)
            if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
            {
                Debug.Log($"[BTS] Item {item.name} (TypeID: {item.TypeID}) name matches whitelist but failed other checks - rejecting");
            }
            throwableItemCache[typeID] = false;
            return false;
        }
        
        /// <summary>
        /// Check if an item should be exempted from exclusion patterns (e.g., a throwable item that contains "can" in name)
        /// This is now only used for legacy exclusion pattern checking
        /// </summary>
        private bool IsThrowableException(Item item, string pattern)
        {
            // Check if it's in the throwable TypeID whitelist and enabled
            int[] throwableTypeIDs = { 24, 66, 67, 660, 933, 941, 942, 1257 };
            if (throwableTypeIDs.Contains(item.TypeID))
            {
                // Also check if it's enabled in recognition list
                if (enabledThrowableTypeIDs.ContainsKey(item.TypeID) && !enabledThrowableTypeIDs[item.TypeID])
                {
                    return false; // Disabled, so it's not an exception
                }
                return true;
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
        /// Detect game language (Chinese or English)
        /// </summary>
        private void DetectGameLanguage()
        {
            try
            {
                // Method 1: Check Application.systemLanguage
                SystemLanguage systemLang = Application.systemLanguage;
                if (systemLang == SystemLanguage.Chinese || 
                    systemLang == SystemLanguage.ChineseSimplified || 
                    systemLang == SystemLanguage.ChineseTraditional)
                {
                    isChinese = true;
                    Debug.Log("[BTS] Game language detected as Chinese (via SystemLanguage)");
                    return;
                }
                
                // Method 2: Try to find localization system in the game
                // Check for SodaLocalization or similar systems
                var localizationType = System.Type.GetType("SodaLocalization.Localization") ?? 
                                      System.Type.GetType("TeamSoda.MiniLocalizor.Localization");
                
                if (localizationType != null)
                {
                    var currentLangProp = localizationType.GetProperty("CurrentLanguage",
                        System.Reflection.BindingFlags.Public | 
                        System.Reflection.BindingFlags.Static | 
                        System.Reflection.BindingFlags.Instance);
                    
                    if (currentLangProp != null)
                    {
                        var currentLang = currentLangProp.GetValue(null);
                        string langStr = currentLang?.ToString() ?? "";
                        
                        if (langStr.Contains("Chinese", StringComparison.OrdinalIgnoreCase) ||
                            langStr.Contains("中文", StringComparison.OrdinalIgnoreCase) ||
                            langStr.Contains("zh", StringComparison.OrdinalIgnoreCase))
                        {
                            isChinese = true;
                            Debug.Log($"[BTS] Game language detected as Chinese (via Localization: {langStr})");
                            return;
                        }
                    }
                }
                
                // Method 3: Check PlayerPrefs or game settings
                string langPref = PlayerPrefs.GetString("Language", "");
                if (langPref.Contains("Chinese", StringComparison.OrdinalIgnoreCase) ||
                    langPref.Contains("中文", StringComparison.OrdinalIgnoreCase) ||
                    langPref.Contains("zh", StringComparison.OrdinalIgnoreCase))
                {
                    isChinese = true;
                    Debug.Log($"[BTS] Game language detected as Chinese (via PlayerPrefs: {langPref})");
                    return;
                }
                
                // Default to English if no Chinese detected
                isChinese = false;
                Debug.Log("[BTS] Game language detected as English (default)");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[BTS] Error detecting language: {e.Message}, defaulting to English");
                isChinese = false;
            }
        }
        
        /// <summary>
        /// Get localized item name (Chinese or English) from Item object
        /// </summary>
        private string GetLocalizedItemName(Item? item, string rawItemName = "")
        {
            // Try to get name from Item object first (better source)
            if (item != null)
            {
                try
                {
                    var itemType = item.GetType();
                    
                    // Try various properties that might contain the display name
                    string[] possibleProperties = {
                        "DisplayName", "LocalizedName", "ItemName", "Name", 
                        "GetDisplayName", "GetLocalizedName", "GetItemName"
                    };
                    
                    foreach (var propName in possibleProperties)
                    {
                        // Try property first
                        var prop = itemType.GetProperty(propName,
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic);
                        
                        if (prop != null)
                        {
                            try
                            {
                                var value = prop.GetValue(item);
                                if (value != null && !string.IsNullOrEmpty(value.ToString()))
                                {
                                    string name = value.ToString();
                                    if (!name.Contains("(Clone)"))
                                    {
                                        Debug.Log($"[BTS] Got item name from property {propName}: {name}");
                                        return name;
                                    }
                                }
                            }
                            catch { }
                        }
                        
                        // Try method
                        var method = itemType.GetMethod(propName,
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.NonPublic,
                            null,
                            new System.Type[0],
                            null);
                        
                        if (method != null && method.ReturnType == typeof(string))
                        {
                            try
                            {
                                var result = method.Invoke(item, null);
                                if (result != null && !string.IsNullOrEmpty(result.ToString()))
                                {
                                    string name = result.ToString();
                                    Debug.Log($"[BTS] Got item name from method {propName}(): {name}");
                                    return name;
                                }
                            }
                            catch { }
                        }
                    }
                    
                    // Fallback to item.name
                    if (!string.IsNullOrEmpty(item.name))
                    {
                        rawItemName = item.name;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[BTS] Error getting name from Item object: {e.Message}");
                }
            }
            
            // If no item provided or failed, use raw name
            if (string.IsNullOrEmpty(rawItemName))
            {
                return "Unknown";
            }
            
            // Remove (Clone) suffix
            string cleanName = rawItemName.Replace("(Clone)", "").Trim();
            
            // If Chinese, try to get Chinese name from game's localization system
            if (isChinese)
            {
                try
                {
                    // Method 1: Use localization system if available
                    var localizationType = System.Type.GetType("SodaLocalization.Localization") ?? 
                                          System.Type.GetType("TeamSoda.MiniLocalizor.Localization");
                    
                    if (localizationType != null)
                    {
                        var getStringMethod = localizationType.GetMethod("GetString",
                            System.Reflection.BindingFlags.Public | 
                            System.Reflection.BindingFlags.Static);
                        
                        if (getStringMethod != null)
                        {
                            try
                            {
                                // Try to get localized string for item name
                                var localized = getStringMethod.Invoke(null, new object[] { cleanName });
                                if (localized != null && !string.IsNullOrEmpty(localized.ToString()))
                                {
                                    return localized.ToString();
                                }
                            }
                            catch { }
                        }
                    }
                    
                    // Fallback: Return clean name (might already be Chinese)
                    // Many games use Chinese names directly in the item.name
                    return cleanName;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[BTS] Error getting localized name for {cleanName}: {e.Message}");
                    return cleanName;
                }
            }
            else
            {
                // English mode: Return English name
                return cleanName;
            }
        }
        
        /// <summary>
        /// Get localized item name from raw name string (backward compatibility)
        /// </summary>
        private string GetLocalizedItemName(string rawItemName)
        {
            return GetLocalizedItemName(null, rawItemName);
        }
        
        /// <summary>
        /// Open radial menu (wheel menu) for throwable selection
        /// </summary>
        private void OpenRadialMenu()
        {
            try
            {
                var throwablesList = GetAllThrowablesByCategory();
                if (throwablesList.Count == 0)
                {
                    ShowDebugBubble(isChinese ? "❌ 背包中没有投掷物" : "❌ No throwables in inventory");
                    return;
                }
                
                Debug.Log("[BTS] =========================================");
                Debug.Log("[BTS] ========== OPENING RADIAL MENU ==========");
                Debug.Log($"[BTS] Found {throwablesList.Count} throwable categories");
                Debug.Log("[BTS] =========================================");
                
                // Create canvas if it doesn't exist
                if (radialMenuCanvas == null)
                {
                    CreateRadialMenuCanvas();
                }
                
                if (radialMenuCanvas == null)
                {
                    Debug.LogError("[BTS] Failed to create radial menu canvas!");
                    return;
                }
                
                // Clear existing items
                ClearRadialMenuItems();
                
                // Ensure container is not rotated
                if (radialMenuContainer != null)
                {
                    radialMenuContainer.localRotation = Quaternion.identity;
                }
                
                // Create menu items for each throwable
                for (int i = 0; i < throwablesList.Count; i++)
                {
                    CreateRadialMenuItem(throwablesList[i], i, throwablesList.Count);
                }
                
                // Set 12 o'clock position (top) as default selection
                radialMenuSelectedIndex = 0; // First item is at 12 o'clock
                if (radialMenuItems.Count > 0)
                {
                    UpdateRadialMenuItemHighlight(0, true);
                }
                
                // Show canvas
                radialMenuCanvas.SetActive(true);
                isRadialMenuOpen = true;
                
                // Lock cursor to center (optional, can be disabled if player wants to look around)
                // Cursor.lockState = CursorLockMode.Locked;
                // Cursor.visible = true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error opening radial menu: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Create the radial menu canvas and container
        /// </summary>
        private void CreateRadialMenuCanvas()
        {
            try
            {
                // Create Canvas
                GameObject canvasObj = new GameObject("RadialMenuCanvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // High sorting order to appear on top
                
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Ensure EventSystem exists (needed for UI interactions)
                if (UnityEngine.EventSystems.EventSystem.current == null)
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
                
                // Create background (optional, can be a semi-transparent overlay)
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(canvasObj.transform, false);
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black
                
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                
                // Create container for radial menu items (centered on screen)
                GameObject containerObj = new GameObject("RadialMenuContainer");
                containerObj.transform.SetParent(canvasObj.transform, false);
                RectTransform containerRect = containerObj.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                containerRect.anchoredPosition = Vector2.zero;
                containerRect.sizeDelta = new Vector2(RADIAL_MENU_RADIUS * 2 + RADIAL_MENU_ITEM_SIZE, RADIAL_MENU_RADIUS * 2 + RADIAL_MENU_ITEM_SIZE);
                
                radialMenuCanvas = canvasObj;
                radialMenuContainer = containerRect;
                
                // Initially hide the canvas
                radialMenuCanvas.SetActive(false);
                
                Debug.Log("[BTS] Radial menu canvas created successfully");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error creating radial menu canvas: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Create a single radial menu item
        /// </summary>
        private void CreateRadialMenuItem((int slot, int typeID, string name, Sprite icon) throwable, int index, int totalCount)
        {
            try
            {
                if (radialMenuContainer == null) return;
                
                // Calculate angle for this item (always use base angle, rotation is applied to container)
                // Base angle: -90 to start at top (12 o'clock)
                float angleStep = 360f / totalCount;
                float baseAngle = index * angleStep - 90f; // -90 to start at top (12 o'clock)
                float angleRad = baseAngle * Mathf.Deg2Rad;
                
                // Calculate position using base angle (rotation will be applied to container)
                float x = Mathf.Cos(angleRad) * RADIAL_MENU_RADIUS;
                float y = Mathf.Sin(angleRad) * RADIAL_MENU_RADIUS;
                
                // Create item GameObject
                GameObject itemObj = new GameObject($"RadialMenuItem_{index}_{throwable.name}");
                itemObj.transform.SetParent(radialMenuContainer, false);
                
                RectTransform itemRect = itemObj.AddComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0.5f, 0.5f);
                itemRect.anchorMax = new Vector2(0.5f, 0.5f);
                itemRect.anchoredPosition = new Vector2(x, y);
                itemRect.sizeDelta = new Vector2(RADIAL_MENU_ITEM_SIZE, RADIAL_MENU_ITEM_SIZE);
                
                // Create background (circle)
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(itemObj.transform, false);
                Image bgImage = bgObj.AddComponent<Image>();
                bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                // Try to make it circular (would need a sprite, for now use square)
                RectTransform bgRect = bgObj.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                
                // Create icon image
                GameObject iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(itemObj.transform, false);
                Image iconImage = iconObj.AddComponent<Image>();
                
                if (throwable.icon != null)
                {
                    iconImage.sprite = throwable.icon;
                }
                else
                {
                    // Use default icon or create a simple colored square
                    iconImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }
                
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.2f, 0.2f);
                iconRect.anchorMax = new Vector2(0.8f, 0.8f);
                iconRect.sizeDelta = Vector2.zero;
                
                // Create text label (throwable.name is already localized from GetAllThrowablesByCategory())
                GameObject textObj = new GameObject("Label");
                textObj.transform.SetParent(itemObj.transform, false);
                Text text = textObj.AddComponent<Text>();
                text.text = throwable.name;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 14;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = Color.white;
                
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0f, -0.3f);
                textRect.anchorMax = new Vector2(1f, -0.1f);
                textRect.sizeDelta = Vector2.zero;
                
                // Store throwable data in the GameObject
                RadialMenuItemData data = itemObj.AddComponent<RadialMenuItemData>();
                data.slot = throwable.slot;
                data.typeID = throwable.typeID;
                data.name = throwable.name;
                data.index = index;
                
                radialMenuItems.Add(itemObj);
                
                Debug.Log($"[BTS] Created radial menu item {index}: {throwable.name} at angle {baseAngle}°");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error creating radial menu item: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Clear all radial menu items
        /// </summary>
        private void ClearRadialMenuItems()
        {
            foreach (var item in radialMenuItems)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item);
                }
            }
            radialMenuItems.Clear();
        }
        
        /// <summary>
        /// Handle mouse scroll wheel to change selection (highlight only, no rotation)
        /// PERFORMANCE OPTIMIZED: Removed expensive mouse position calculations
        /// </summary>
        private void HandleRadialMenuScroll()
        {
            if (radialMenuItems.Count == 0) return;
            
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            
            // More sensitive: respond to smaller scroll input
            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                // PERFORMANCE: Removed expensive RectTransformUtility.ScreenPointToLocalPointInRectangle call
                // Mouse tracking disabled - scroll wheel always works
                int currentIndex = radialMenuSelectedIndex >= 0 ? radialMenuSelectedIndex : 0;
                int newIndex = currentIndex;
                
                if (scrollDelta > 0f)
                {
                    // Scroll up - move to previous item (counter-clockwise, index decreases)
                    newIndex = currentIndex - 1;
                    if (newIndex < 0) newIndex = radialMenuItems.Count - 1;
                }
                else
                {
                    // Scroll down - move to next item (clockwise, index increases)
                    newIndex = currentIndex + 1;
                    if (newIndex >= radialMenuItems.Count) newIndex = 0;
                }
                
                // Update selection
                if (newIndex != currentIndex)
                {
                    // Deselect old item
                    if (currentIndex >= 0 && currentIndex < radialMenuItems.Count)
                    {
                        UpdateRadialMenuItemHighlight(currentIndex, false);
                    }
                    
                    // Select new item
                    radialMenuSelectedIndex = newIndex;
                    if (radialMenuSelectedIndex >= 0 && radialMenuSelectedIndex < radialMenuItems.Count)
                    {
                        UpdateRadialMenuItemHighlight(radialMenuSelectedIndex, true);
                    }
                    
                    Debug.Log($"[BTS] Radial menu scroll: {currentIndex} -> {newIndex}");
                }
            }
        }
        
        /// <summary>
        /// Update radial menu selection based on mouse position
        /// </summary>
        private void UpdateRadialMenuSelection()
        {
            if (radialMenuContainer == null || radialMenuItems.Count == 0) return;
            
            try
            {
                // Get mouse position in screen space
                Vector2 mousePos = Input.mousePosition;
                
                // Convert to canvas space
                Canvas? canvas = radialMenuContainer.GetComponentInParent<Canvas>();
                Camera? uiCamera = canvas?.worldCamera ?? (canvas?.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main);
                
                Vector2 localMousePos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    radialMenuContainer,
                    mousePos,
                    uiCamera,
                    out localMousePos
                );
                
                // Calculate angle from center (no rotation offset needed)
                float angle = Mathf.Atan2(localMousePos.y, localMousePos.x) * Mathf.Rad2Deg;
                angle += 90f; // Adjust to start at top
                if (angle < 0) angle += 360f;
                if (angle >= 360f) angle -= 360f;
                
                // Calculate distance from center
                float distance = Vector2.Distance(Vector2.zero, localMousePos);
                
                // Calculate angle step
                float angleStep = 360f / radialMenuItems.Count;
                
                // Check if mouse is within selection radius (increased tolerance for better selection)
                if (distance > RADIAL_MENU_RADIUS + RADIAL_MENU_SELECTION_TOLERANCE)
                {
                    // Mouse too far from center, keep current selection (don't change)
                    return;
                }
                
                // Calculate which item should be selected based on angle
                int selectedIndex = Mathf.FloorToInt((angle + angleStep * 0.5f) / angleStep) % radialMenuItems.Count;
                if (selectedIndex < 0) selectedIndex += radialMenuItems.Count;
                
                // Update selection
                if (selectedIndex != radialMenuSelectedIndex)
                {
                    // Deselect old item
                    if (radialMenuSelectedIndex >= 0 && radialMenuSelectedIndex < radialMenuItems.Count)
                    {
                        UpdateRadialMenuItemHighlight(radialMenuSelectedIndex, false);
                    }
                    
                    // Select new item
                    radialMenuSelectedIndex = selectedIndex;
                    if (radialMenuSelectedIndex >= 0 && radialMenuSelectedIndex < radialMenuItems.Count)
                    {
                        UpdateRadialMenuItemHighlight(radialMenuSelectedIndex, true);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error updating radial menu selection: {e.Message}\n{e.StackTrace}");
            }
        }
        
        /// <summary>
        /// Update highlight state of a radial menu item
        /// </summary>
        private void UpdateRadialMenuItemHighlight(int index, bool highlight)
        {
            if (index < 0 || index >= radialMenuItems.Count) return;
            
            try
            {
                GameObject itemObj = radialMenuItems[index];
                if (itemObj == null) return;
                
                // Find background image
                Transform bgTransform = itemObj.transform.Find("Background");
                if (bgTransform != null)
                {
                    Image bgImage = bgTransform.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.color = highlight 
                            ? new Color(0.4f, 0.6f, 0.9f, 0.9f) // Blue highlight
                            : new Color(0.2f, 0.2f, 0.2f, 0.8f); // Dark background
                    }
                }
                
                // Scale up slightly when selected
                RectTransform itemRect = itemObj.GetComponent<RectTransform>();
                if (itemRect != null)
                {
                    itemRect.localScale = highlight ? Vector3.one * 1.2f : Vector3.one;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error updating item highlight: {e.Message}");
            }
        }
        
        /// <summary>
        /// Close radial menu and equip selected item
        /// </summary>
        private void CloseRadialMenuAndEquip()
        {
            try
            {
                if (!isRadialMenuOpen) return;
                
                var player = FindPlayerCharacter();
                if (player == null)
                {
                    Debug.LogError("[BTS] Player not found, cannot equip selected throwable");
                    CloseRadialMenu();
                    return;
                }
                
                // Use current selection (or default to index 0 if invalid)
                int itemIndexToEquip = radialMenuSelectedIndex;
                if (itemIndexToEquip < 0 || itemIndexToEquip >= radialMenuItems.Count)
                {
                    itemIndexToEquip = 0;
                    Debug.Log($"[BTS] Invalid selection index, using default (index 0)");
                }
                
                if (itemIndexToEquip >= 0 && itemIndexToEquip < radialMenuItems.Count)
                {
                    var itemObj = radialMenuItems[itemIndexToEquip];
                    var itemData = itemObj?.GetComponent<RadialMenuItemData>();
                    
                    if (itemData != null)
                    {
                        Debug.Log($"[BTS] =========================================");
                        Debug.Log($"[BTS] ========== RADIAL MENU SELECTION ==========");
                        Debug.Log($"[BTS] Selected: {itemData.name} (Slot {itemData.slot}, TypeID {itemData.typeID})");
                        Debug.Log("[BTS] =========================================");
                        
                        // Save current weapon before switching (same as quick press G)
                        SaveCurrentEquippedSlot(player);
                        
                        // Update current quick throwable
                        currentQuickThrowableSlot = itemData.slot;
                        currentQuickThrowableTypeID = itemData.typeID;
                        Debug.Log($"[BTS] ✓ Updated current quick throwable via radial menu: Slot {currentQuickThrowableSlot}, TypeID {currentQuickThrowableTypeID}");
                        
                        // Update category index
                        if (throwableTypeIDsInOrder.Contains(itemData.typeID))
                        {
                            currentCategoryIndex = throwableTypeIDsInOrder.IndexOf(itemData.typeID);
                        }
                        
                        // In Throw mode, directly throw the selected item to mouse position
                        // In Equip mode, just equip it
                        if (throwMode == ThrowMode.Throw)
                        {
                            // Throw mode: directly throw to mouse position
                            Debug.Log($"[BTS] 🎯 Throw Mode: Long press selection - throwing {itemData.name} to mouse position");
                            ThrowToMousePosition(player);
                        }
                        else
                        {
                            // Equip mode: just equip the selected throwable
                            if (SwitchToSlot(itemData.slot))
                            {
                                lastEquippedThrowableSlot = itemData.slot;
                                lastSelectedThrowableSlot = itemData.slot;
                                lastSelectedThrowableTypeID = itemData.typeID;
                                
                                // Update current quick throwable (used when quick press cycle is disabled)
                                currentQuickThrowableSlot = itemData.slot;
                                currentQuickThrowableTypeID = itemData.typeID;
                                
                                // Mark that we started throwing process (for throw detection)
                                // This ensures OnThrowCompleted() will be called and weapon will be switched back
                                hasCompletedThrow = false; // Reset flag - throw hasn't completed yet
                                isThrowingInProgress = false; // Will be set when we actually start holding throwable
                                wasHoldingThrowable = false; // Reset tracking
                                
                                // Show confirmation bubble (itemData.name is already localized when created)
                                string message = isChinese 
                                    ? $"✓ 已选择：{itemData.name}"
                                    : $"✓ Selected: {itemData.name}";
                                ShowDebugBubble(message);
                                
                                Debug.Log($"[BTS] ✓ Successfully equipped selected throwable via radial menu: {itemData.name}");
                                Debug.Log($"[BTS] Previous weapon saved - SlotHash: {previousEquippedSlotHash}, SlotKey: '{previousEquippedSlotKey}'");
                            }
                            else
                            {
                                string message = isChinese 
                                    ? $"❌ 无法装备：{itemData.name}"
                                    : $"❌ Cannot equip: {itemData.name}";
                                ShowDebugBubble(message);
                                Debug.LogError($"[BTS] Failed to equip selected throwable: {itemData.name}");
                            }
                        }
                        
                        lastActionWasGKey = true;
                        lastActionWasWeaponSwitch = false;
                    }
                }
                else
                {
                    // No item selected, just close menu
                    Debug.Log("[BTS] Radial menu closed without selection");
                }
                
                CloseRadialMenu();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BTS] Error closing radial menu and equipping: {e.Message}\n{e.StackTrace}");
                CloseRadialMenu();
            }
        }
        
        /// <summary>
        /// Close the radial menu
        /// </summary>
        private void CloseRadialMenu()
        {
            if (radialMenuCanvas != null)
            {
                radialMenuCanvas.SetActive(false);
            }
            isRadialMenuOpen = false;
            radialMenuSelectedIndex = -1;
        }
        
        /// <summary>
        /// Component to store radial menu item data
        /// </summary>
        private class RadialMenuItemData : MonoBehaviour
        {
            public int slot;
            public int typeID;
            public new string name = "";  // Use 'new' to hide inherited Object.name
            public int index;
        }
        
        /// <summary>
        /// Find the actual player CharacterMainControl (not NPCs)
        /// </summary>
        private CharacterMainControl? FindPlayerCharacter()
        {
            // PERFORMANCE: Use cached player if available and valid
            // CRITICAL FIX: This check MUST come first to prevent FindObjectsOfType from being called every frame
            // This was causing severe FPS drops on low-end systems (100+ FPS loss)
            if (cachedPlayer != null && cachedPlayer.gameObject != null && cachedPlayer.gameObject.activeInHierarchy)
            {
                // Only refresh if cache is very old (4 seconds)
                // This prevents FindObjectsOfType from being called every frame
                float timeSinceCache = Time.time - lastPlayerCacheTime;
                if (timeSinceCache < PLAYER_CACHE_REFRESH_INTERVAL * 2f)
                {
                    // Cache is valid - return immediately without any expensive operations
                    // This is the most important performance optimization
                    return cachedPlayer;
                }
            }
            
            // Method 1: Try to find by Camera.main (player usually has the main camera)
            if (Camera.main != null && Camera.main.transform != null)
            {
                // Check if camera is a child of a CharacterMainControl
                var characterFromCamera = Camera.main.transform.GetComponentInParent<CharacterMainControl>();
                if (characterFromCamera != null)
                {
                    if (IsPlayerCharacter(characterFromCamera))
                    {
                        // PERFORMANCE: Cache player and update cache time
                        cachedPlayer = characterFromCamera;
                        lastPlayerCacheTime = Time.time;
                        if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                        {
                            Debug.Log("[BTS] Found player via Camera.main parent");
                        }
                        return characterFromCamera;
                    }
                }
                
                // Or check if camera follows a CharacterMainControl (common pattern)
                // PERFORMANCE WARNING: FindObjectsOfType is expensive - only called when cache is invalid
                Stopwatch findObjectsStopwatch = new Stopwatch();
                findObjectsStopwatch.Restart();
                var allCharacters = FindObjectsOfType<CharacterMainControl>();
                findObjectsStopwatch.Stop();
                if (ENABLE_PERFORMANCE_PROFILING && findObjectsStopwatch.ElapsedMilliseconds > 5)
                {
                    UnityEngine.Debug.Log($"[BTS] ⚠️⚠️ FindObjectsOfType<CharacterMainControl> took {findObjectsStopwatch.ElapsedMilliseconds}ms - THIS IS A BOTTLENECK!");
                }
                
                foreach (var character in allCharacters)
                {
                    if (IsPlayerCharacter(character))
                    {
                        // Check if this character is near the camera
                        float distance = Vector3.Distance(Camera.main.transform.position, character.transform.position);
                        if (distance < 10f) // Player should be close to camera
                        {
                            // PERFORMANCE: Cache player and update cache time
                            cachedPlayer = character;
                            lastPlayerCacheTime = Time.time;
                            if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                            {
                                Debug.Log($"[BTS] Found player near camera (distance: {distance})");
                            }
                            return character;
                        }
                    }
                }
            }
            
            // Method 2: Find all CharacterMainControl and filter for player
            // PERFORMANCE WARNING: FindObjectsOfType is expensive
            Stopwatch findObjects2Stopwatch = new Stopwatch();
            findObjects2Stopwatch.Restart();
            var allChars = FindObjectsOfType<CharacterMainControl>();
            findObjects2Stopwatch.Stop();
            if (ENABLE_PERFORMANCE_PROFILING && findObjects2Stopwatch.ElapsedMilliseconds > 5)
            {
                UnityEngine.Debug.Log($"[BTS] ⚠️⚠️ FindObjectsOfType<CharacterMainControl> (Method 2) took {findObjects2Stopwatch.ElapsedMilliseconds}ms - THIS IS A BOTTLENECK!");
            }
            foreach (var character in allChars)
            {
                if (IsPlayerCharacter(character))
                {
                    // PERFORMANCE: Cache player and update cache time
                    cachedPlayer = character;
                    lastPlayerCacheTime = Time.time;
                    if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                    {
                        Debug.Log("[BTS] Found player via IsPlayerCharacter check");
                    }
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
                    // PERFORMANCE: Cache player and update cache time
                    cachedPlayer = character;
                    lastPlayerCacheTime = Time.time;
                    if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                    {
                        Debug.Log("[BTS] Found player via Player tag");
                    }
                    return character;
                }
            }
            
            // Fallback: Use first CharacterMainControl found (even if we can't verify it's the player)
            // This allows the mod to work even if player detection fails
            if (allChars.Length > 0)
            {
                // PERFORMANCE: Cache player and update cache time
                cachedPlayer = allChars[0];
                lastPlayerCacheTime = Time.time;
                if (ENABLE_IS_THROWABLE_DEBUG_LOGS)
                {
                    Debug.LogWarning($"[BTS] Could not verify player character! Using first CharacterMainControl found: {allChars[0].gameObject.name}");
                }
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
        
        // ============================================================================
        // ModConfig and Settings UI
        // ============================================================================
        
        // Track if ModSetting is already set up
        private static bool _modSettingSetup = false;
        
        /// <summary>
        /// Try to initialize ModSetting - similar to RadialMenu's approach
        /// Uses this.info from ModBehaviour base class
        /// </summary>
        private void TryInitializeModSetting()
        {
            if (_modSettingSetup)
            {
                Debug.Log("[BTS] ModSetting already set up, skipping");
                return;
            }
            
            // Check if info is valid (name and displayName should not be empty)
            if (info.Equals(default(Duckov.Modding.ModInfo)) || string.IsNullOrEmpty(info.name))
            {
                Debug.LogWarning("[BTS] ModInfo is not valid yet (name is empty), cannot initialize ModSetting");
                Debug.LogWarning($"[BTS] ModInfo details: name='{info.name}', displayName='{info.displayName}'");
                return;
            }
            
            Debug.Log($"[BTS] ModInfo is valid - name: '{info.name}', displayName: '{info.displayName}'");
            
            // Use this.info from ModBehaviour base class (just like RadialMenu does)
            // RadialMenu uses: ModSettingAPI.Init(info)
            if (ModSettingAPI.Init(info))
            {
                SetupModSetting();
                LoadModSettingValues();
                _modSettingSetup = true;
                Debug.Log("[BTS] ModSetting initialized and registered successfully!");
            }
            else
            {
                Debug.Log("[BTS] ModSettingAPI not available yet, will retry when ModSetting mod is activated");
            }
        }
        
        /// <summary>
        /// Setup ModSetting UI items - similar to RadialMenu's AddUIItems
        /// </summary>
        private void SetupModSetting()
        {
            Debug.Log("[BTS] Setting up ModSetting UI items...");
            
            // 1. Toggle for ThrowSoundEnabled
            bool toggle1Result = ModSettingAPI.AddToggle(
                "ThrowSoundEnabled",
                "投掷音效开关 / Throw Sound",
                throwSoundEnabled,
                value => {
                    throwSoundEnabled = value;
                    Debug.Log($"[BTS] ThrowSoundEnabled changed to: {value}");
                }
            );
            Debug.Log($"[BTS] AddToggle(ThrowSoundEnabled) result: {toggle1Result}");
            
            // 2. Dropdown for ThrowMode
            var throwModeOptions = new System.Collections.Generic.List<string> { "按G装备", "按G投掷" };
            string currentThrowMode = throwMode == ThrowMode.Equip ? "按G装备" : "按G投掷";
            bool dropdown1Result = ModSettingAPI.AddDropdownList(
                "ThrowMode",
                "按G键模式 / G Key Mode",
                throwModeOptions,
                currentThrowMode,
                value => {
                    bool newIsThrow = value == "按G投掷";
                    throwMode = newIsThrow ? ThrowMode.Throw : ThrowMode.Equip;
                    Debug.Log($"[BTS] ThrowMode changed to: {value}");
                    
                    // When switching to Throw mode, automatically enable disableThrowPreparationTime
                    // and disable the toggle (user cannot turn it off in Throw mode)
                    if (newIsThrow)
                    {
                        disableThrowPreparationTime = true;
                        // Note: The toggle will be disabled in UI, but we keep it enabled internally
                        Debug.Log("[BTS] Throw mode enabled - automatically enabling disableThrowPreparationTime");
                        
                        // Initialize current quick throwable when switching to Throw mode
                        InitializeCurrentQuickThrowable();
                    }
                    else
                    {
                        // Clear current quick throwable when switching to Equip mode
                        currentQuickThrowableSlot = null;
                        currentQuickThrowableTypeID = null;
                    }
                }
            );
            Debug.Log($"[BTS] AddDropdownList(ThrowMode) result: {dropdown1Result}");
            
            // 3. Toggle for DisableThrowPreparationTime
            bool toggle3Result = ModSettingAPI.AddToggle(
                "DisableThrowPreparationTime",
                "取消投掷物投掷准备时间 / Disable Throw Preparation Time",
                disableThrowPreparationTime,
                value => {
                    // If user tries to disable this in Throw mode, switch back to Equip mode
                    if (throwMode == ThrowMode.Throw && !value)
                    {
                        Debug.Log("[BTS] Cannot disable throw preparation time in Throw mode - switching to Equip mode");
                        throwMode = ThrowMode.Equip;
                        // Update the dropdown in ModSetting
                        ModSettingAPI.SetValue("ThrowMode", "按G装备");
                        ShowDebugBubble(isChinese ? "已切换回按G装备模式" : "Switched back to Equip mode");
                    }
                    else
                    {
                        disableThrowPreparationTime = value;
                        Debug.Log($"[BTS] DisableThrowPreparationTime changed to: {value}");
                    }
                }
            );
            Debug.Log($"[BTS] AddToggle(DisableThrowPreparationTime) result: {toggle3Result}");
            
            // 4. Toggle for EnableContinuousThrow (only effective in Equip mode)
            bool toggle4Result = ModSettingAPI.AddToggle(
                "EnableContinuousThrow",
                "连续投掷开关 / Continuous Throw (Equip Mode Only)",
                enableContinuousThrow,
                value => {
                    enableContinuousThrow = value;
                    Debug.Log($"[BTS] EnableContinuousThrow changed to: {value}");
                    if (value && throwMode == ThrowMode.Throw)
                    {
                        Debug.Log("[BTS] Warning: Continuous throw is only effective in Equip mode. Current mode is Throw mode.");
                    }
                }
            );
            Debug.Log($"[BTS] AddToggle(EnableContinuousThrow) result: {toggle4Result}");
            
            // 5. Toggle for warm grenade behavior
            bool warmToggleResult = ModSettingAPI.AddToggle(
                "EnableWarmGrenade",
                isChinese ? "温雷开关 / Impact Detonation" : "Impact Detonation / 温雷开关",
                enableWarmGrenades,
                value => {
                    if (enableWarmGrenades != value)
                    {
                        if (!value)
                        {
                            RestoreWarmGrenadeSettings();
                        }
                        else
                        {
                            warmGrenadeAppliedItemInstanceIDs.Clear();
                            CleanupWarmGrenadeStates();
                        }
                        
                        lastWarmGrenadeCleanupTime = Time.time;
                    }
                    
                    enableWarmGrenades = value;
                    Debug.Log($"[BTS] EnableWarmGrenade changed to: {value}");
                }
            );
            Debug.Log($"[BTS] AddToggle(EnableWarmGrenade) result: {warmToggleResult}");
            
            // 6. Dropdown for Throwable Recognition List (multi-select via dropdown)
            // Use a dropdown menu where each option toggles the corresponding throwable
            AddThrowableRecognitionDropdown();
            
            // 7. Keybinding for Throw Key
            bool keybindingResult = ModSettingAPI.AddKeybinding(
                "ThrowKey",
                "投掷物按键 / Throw Key",
                throwKey,
                value => {
                    throwKey = value;
                    Debug.Log($"[BTS] ThrowKey changed to: {value}");
                }
            );
            Debug.Log($"[BTS] AddKeybinding(ThrowKey) result: {keybindingResult}");
            
            Debug.Log($"[BTS] ModSetting UI items added - Toggle1: {toggle1Result}, Dropdown1: {dropdown1Result}, Toggle3: {toggle3Result}, Toggle4: {toggle4Result}, Toggle5: {warmToggleResult}, Keybinding: {keybindingResult}");
        }
        
        /// <summary>
        /// Add a dropdown menu for Throwable Recognition List (multi-select via dropdown)
        /// Each option in the dropdown toggles the corresponding throwable's recognition state
        /// </summary>
        private void AddThrowableRecognitionDropdown()
        {
            // Ensure recognition list is initialized (in case SetupModSettingUI is called before Start)
            if (enabledThrowableTypeIDs.Count == 0)
            {
                Debug.Log("[BTS] Throwable recognition list is empty, initializing now...");
                InitializeThrowableRecognitionList();
            }
            
            Debug.Log($"[BTS] Adding throwable recognition dropdown for {enabledThrowableTypeIDs.Count} items");
            
            // Build dropdown options list
            // Each option will be in format: "✓ 手雷 / Grenade" (if enabled) or "  手雷 / Grenade" (if disabled)
            var dropdownOptions = new List<string>();
            var typeIDToOptionIndex = new Dictionary<int, int>();
            int optionIndex = 0;
            
            foreach (var kvp in enabledThrowableTypeIDs.OrderBy(x => x.Key))
            {
                int typeID = kvp.Key;
                bool isEnabled = kvp.Value;
                string baseName = throwableDisplayNames.TryGetValue(typeID, out var displayName)
                    ? displayName
                    : $"TypeID {typeID}";
                
                // Add checkmark if enabled, space if disabled
                string optionText = isEnabled ? $"✓ {baseName}" : $"  {baseName}";
                dropdownOptions.Add(optionText);
                typeIDToOptionIndex[typeID] = optionIndex;
                optionIndex++;
            }
            
            // Get current display value (showing selected items)
            string currentDisplayValue = GetThrowableRecognitionDisplayValue();
            
            // Add dropdown
            bool dropdownResult = ModSettingAPI.AddDropdownList(
                "ThrowableRecognitionList",
                isChinese ? "投掷物识别列表 / Throwable Recognition List" : "Throwable Recognition List / 投掷物识别列表",
                dropdownOptions,
                currentDisplayValue,
                value => {
                    // Find which throwable was selected by matching the option text
                    foreach (var kvp in typeIDToOptionIndex)
                    {
                        int typeID = kvp.Key;
                        string baseName = throwableDisplayNames.TryGetValue(typeID, out var displayName)
                            ? displayName
                            : $"TypeID {typeID}";
                        
                        // Check if this option matches the selected value (with or without checkmark)
                        if (value == $"✓ {baseName}" || value == $"  {baseName}")
                        {
                            // Toggle this throwable's recognition state
                            bool newValue = !enabledThrowableTypeIDs[typeID];
                            enabledThrowableTypeIDs[typeID] = newValue;
                            string settingKey = $"ThrowableRecognition_{typeID}";
                            ModSettingAPI.SetValue(settingKey, newValue);
                            
                            // Clear cache for this TypeID
                            if (throwableItemCache.ContainsKey(typeID))
                            {
                                throwableItemCache.Remove(typeID);
                            }
                            
                            Debug.Log($"[BTS] Throwable recognition toggled - TypeID {typeID} ({baseName}): {(newValue ? "Enabled" : "Disabled")}");
                            
                            // Update dropdown display value
                            string newDisplayValue = GetThrowableRecognitionDisplayValue();
                            ModSettingAPI.SetValue("ThrowableRecognitionList", newDisplayValue);
                            
                            // Recreate dropdown to update options with new checkmarks
                            // Use a coroutine to delay recreation slightly to avoid UI flicker
                            StartCoroutine(RecreateThrowableRecognitionDropdown());
                            
                            break;
                        }
                    }
                }
            );
            
            Debug.Log($"[BTS] AddDropdownList(ThrowableRecognitionList) result: {dropdownResult}");
        }
        
        /// <summary>
        /// Recreate the throwable recognition dropdown with updated options
        /// </summary>
        private System.Collections.IEnumerator RecreateThrowableRecognitionDropdown()
        {
            if (isRefreshingThrowableDropdown)
            {
                yield break;
            }

            isRefreshingThrowableDropdown = true;

            // Wait a frame to ensure UI updates are complete
            yield return null;

            bool removeRequested = ModSettingAPI.RemoveUI("ThrowableRecognitionList", success =>
            {
                StartCoroutine(AddThrowableRecognitionDropdownDelayed());
            });

            if (!removeRequested)
            {
                yield return AddThrowableRecognitionDropdownDelayed();
            }
        }

        private System.Collections.IEnumerator AddThrowableRecognitionDropdownDelayed()
        {
            yield return null;
            AddThrowableRecognitionDropdown();
            isRefreshingThrowableDropdown = false;
        }
        
        /// <summary>
        /// Get the display value for throwable recognition dropdown
        /// Shows selected items with checkmarks
        /// </summary>
        private string GetThrowableRecognitionDisplayValue()
        {
            var enabledItems = new List<string>();
            foreach (var kvp in enabledThrowableTypeIDs.OrderBy(x => x.Key))
            {
                if (kvp.Value)
                {
                    string name = throwableDisplayNames.TryGetValue(kvp.Key, out var displayName)
                        ? displayName
                        : $"TypeID {kvp.Key}";
                    enabledItems.Add(name);
                }
            }
            
            if (enabledItems.Count == 0)
            {
                return isChinese ? "无选中项" : "None selected";
            }
            else if (enabledItems.Count == enabledThrowableTypeIDs.Count)
            {
                return isChinese ? "全部选中" : "All selected";
            }
            else
            {
                // Show first few items
                string result = string.Join(", ", enabledItems.Take(3));
                if (enabledItems.Count > 3)
                {
                    result += isChinese ? $" 等{enabledItems.Count}项" : $" +{enabledItems.Count - 3} more";
                }
                return result;
            }
        }
        
        /// <summary>
        /// Load throwable recognition settings from ModSetting
        /// Since we're using a dropdown now, we load from individual toggle states for backward compatibility
        /// </summary>
        private void LoadThrowableRecognitionSettings()
        {
            if (!ModSettingAPI.IsInit) return;

            // Ensure all known throwables are tracked
            foreach (int typeID in throwableDisplayNames.Keys)
            {
                if (!enabledThrowableTypeIDs.ContainsKey(typeID))
                {
                    enabledThrowableTypeIDs[typeID] = true;
                }
            }

            foreach (var typeID in enabledThrowableTypeIDs.Keys.ToList())
            {
                string key = $"ThrowableRecognition_{typeID}";
                if (ModSettingAPI.GetSavedValue<bool>(key, out bool savedValue))
                {
                    enabledThrowableTypeIDs[typeID] = savedValue;
                    if (throwableItemCache.ContainsKey(typeID))
                    {
                        throwableItemCache.Remove(typeID);
                    }
                    Debug.Log($"[BTS] ✓ Loaded throwable recognition setting - TypeID {typeID}: {(savedValue ? "Enabled" : "Disabled")}");
                }
                else
                {
                    enabledThrowableTypeIDs[typeID] = true;
                    ModSettingAPI.SetValue(key, true);
                    Debug.Log($"[BTS] ✓ Initialized throwable recognition setting - TypeID {typeID}: Enabled by default");
                }
            }

            string displayValue = GetThrowableRecognitionDisplayValue();
            ModSettingAPI.SetValue("ThrowableRecognitionList", displayValue);
            StartCoroutine(RecreateThrowableRecognitionDropdown());
        }
        
        /// <summary>
        /// Load saved values from ModSetting - similar to RadialMenu's LoadConfigFromModSetting
        /// </summary>
        private void LoadModSettingValues()
        {
            if (!ModSettingAPI.IsInit) return;
            
            ModSettingAPI.GetValue<bool>("ThrowSoundEnabled", value => {
                throwSoundEnabled = value;
                Debug.Log($"[BTS] ✓ Loaded ThrowSoundEnabled from ModSetting: {value}");
            });
            
            ModSettingAPI.GetValue<string>("ThrowMode", value => {
                throwMode = value == "按G装备" ? ThrowMode.Equip : ThrowMode.Throw;
                Debug.Log($"[BTS] ✓ Loaded ThrowMode from ModSetting: {value}");
                // If in Throw mode, initialize current quick throwable
                if (throwMode == ThrowMode.Throw)
                {
                    InitializeCurrentQuickThrowable();
                }
            });
            
            ModSettingAPI.GetValue<bool>("DisableThrowPreparationTime", value => {
                disableThrowPreparationTime = value;
                Debug.Log($"[BTS] ✓ Loaded DisableThrowPreparationTime from ModSetting: {value}");
            });
            
            ModSettingAPI.GetValue<bool>("EnableContinuousThrow", value => {
                enableContinuousThrow = value;
                Debug.Log($"[BTS] ✓ Loaded EnableContinuousThrow from ModSetting: {value} (current value: {enableContinuousThrow})");
            });
            
            ModSettingAPI.GetValue<bool>("EnableWarmGrenade", value => {
                enableWarmGrenades = value;
                if (value)
                {
                    warmGrenadeAppliedItemInstanceIDs.Clear();
                    CleanupWarmGrenadeStates();
                }
                else
                {
                    RestoreWarmGrenadeSettings();
                }
                lastWarmGrenadeCleanupTime = Time.time;
                Debug.Log($"[BTS] ✓ Loaded EnableWarmGrenade from ModSetting: {value}");
            });
            
            // Load throwable recognition list settings
            LoadThrowableRecognitionSettings();
            
            ModSettingAPI.GetValue<KeyCode>("ThrowKey", value => {
                throwKey = value;
                int keyCodeInt = (int)value;
                string keyName = value.ToString();
                Debug.Log($"[BTS] ✓ Loaded ThrowKey from ModSetting: {value} (Int: {keyCodeInt}, Name: {keyName})");
                Debug.Log($"[BTS] 🔍 ThrowKey details - IsMouseButton: {keyName.Contains("Mouse") || keyName.Contains("Button")}, KeyCode range: {keyCodeInt}");
            });
            
            Debug.Log("[BTS] ✓ All settings loaded from ModSetting");
        }
        
        /// <summary>
        /// Teardown ModSetting - remove our settings
        /// </summary>
        private void TeardownModSetting()
        {
            if (!_modSettingSetup)
            {
                Debug.Log("[BTS] ModSetting teardown called but not set up, skipping");
                return;
            }
            
            try
            {
                Debug.Log("[BTS] Teardown ModSetting - removing our settings from ModSetting API");
                ModSettingAPI.RemoveMod();
                _modSettingSetup = false;
                Debug.Log("[BTS] ModSetting teardown completed");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Failed to teardown ModSetting: {ex.Message}");
                Debug.LogWarning($"[BTS] Stack trace: {ex.StackTrace}");
                // Reset flag even if teardown failed
                _modSettingSetup = false;
            }
        }
        
        /// <summary>
        /// OLD Initialize ModConfig integration - DEPRECATED, kept for reference
        /// Based on official ModConfig API: https://github.com/FrozenFish259/duckov_mod_config
        /// Returns true if registration was successful
        /// </summary>
        private bool InitializeModConfig_OLD()
        {
            try
            {
                Debug.Log("[BTS] Initializing ModConfig integration...");
                
                // Step 1: Find ModConfigAPI type by searching all loaded assemblies
                System.Type? modConfigApiType = null;
                System.Reflection.Assembly? modConfigAssembly = null;
                
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var assemblyName = assembly.GetName().Name;
                        
                        // Try to find ModConfigAPI (the official API class) or ModSetting API
                        modConfigApiType = assembly.GetType("ModConfig.ModConfigAPI") 
                            ?? assembly.GetType("ModConfigAPI")
                            ?? assembly.GetType("ModConfigApi")
                            ?? assembly.GetType("ModSetting.ModSettingAPI")
                            ?? assembly.GetType("ModSettingAPI")
                            ?? assembly.GetType("ModSetting.ModSetting");
                        
                        if (modConfigApiType != null)
                        {
                            modConfigAssembly = assembly;
                            Debug.Log($"[BTS] Found API type '{modConfigApiType.FullName}' in assembly: {assemblyName}");
                            break;
                        }
                    }
                    catch
                    {
                        // Ignore assembly access errors
                    }
                }
                
                if (modConfigApiType == null || modConfigAssembly == null)
                {
                    // Log detailed debug info on first attempt only
                    if (modConfigApiType == null)
                    {
                        Debug.LogWarning("[BTS] ModConfigAPI type not found in any loaded assembly!");
                        Debug.Log("[BTS] Searching for ModConfig-related assemblies...");
                        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                        {
                            try
                            {
                                var assemblyName = assembly.GetName().Name;
                        if (assemblyName != null && (assemblyName.Contains("ModConfig") || assemblyName.Contains("ModSetting")))
                        {
                            Debug.Log($"[BTS] Found potentially relevant assembly: {assemblyName}");
                            try
                            {
                                var types = assembly.GetTypes().Where(t => 
                                    t.Name.Contains("ModConfig") || 
                                    t.Name.Contains("ModSetting") ||
                                    t.Name.Contains("API") ||
                                    t.Name.Contains("Config")).Take(15);
                                foreach (var type in types)
                                {
                                    Debug.Log($"[BTS]   - Type: {type.FullName}");
                                    // Also list methods for API types
                                    if (type.Name.Contains("API") || type.Name.Contains("Config"))
                                    {
                                        var apiMethods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Take(5);
                                        foreach (var m in apiMethods)
                                        {
                                            var paramsStr = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name));
                                            Debug.Log($"[BTS]     -> {m.Name}({paramsStr})");
                                        }
                                    }
                                }
                            }
                            catch { }
                        }
                            }
                            catch { }
                        }
                    }
                    return false;
                }
                
                // Step 2: Initialize API (only for ModConfig, not ModSetting)
                bool isModSetting = modConfigApiType.FullName != null && modConfigApiType.FullName.Contains("ModSetting");
                
                if (!isModSetting)
                {
                    var initializeMethod = modConfigApiType.GetMethod("Initialize", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (initializeMethod != null)
                    {
                        try
                        {
                            var initResult = initializeMethod.Invoke(null, null);
                            Debug.Log($"[BTS] ModConfigAPI.Initialize() called, result: {initResult}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to call ModConfigAPI.Initialize(): {ex.Message}");
                        }
                    }
                }
                else
                {
                    Debug.Log("[BTS] Using ModSetting API (no Initialize needed)");
                }
                
                // Step 3: Check if API is available (skip for ModSetting as it doesn't have IsAvailable)
                // Reuse isModSetting from Step 2
                if (!isModSetting)
                {
                    // Only check IsAvailable for ModConfig
                    var isAvailableMethod = modConfigApiType.GetMethod("IsAvailable", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    bool isAvailable = false;
                    if (isAvailableMethod != null)
                    {
                        try
                        {
                            var result = isAvailableMethod.Invoke(null, null);
                            isAvailable = result != null && (bool)result;
                            Debug.Log($"[BTS] ModConfigAPI.IsAvailable() = {isAvailable}");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to check ModConfigAPI.IsAvailable(): {ex.Message}");
                        }
                    }
                    
                    if (!isAvailable)
                    {
                        Debug.LogWarning("[BTS] ModConfig is not available. Settings will not appear in Mod Settings tab.");
                        return false;
                    }
                }
                else
                {
                    Debug.Log("[BTS] Using ModSetting API (skipping IsAvailable check)");
                }
                
                // Step 4: Register configuration using the appropriate API
                Debug.Log("[BTS] API is available, registering configuration...");
                
                // Try different registration methods based on which API we're using
                // Reuse isModSetting from Step 2
                if (isModSetting)
                {
                    // ModSetting API uses Init() then AddToggle/AddDropdownList methods
                    // Step 4.1: Find ModInfo type (likely in Duckov.Modding namespace)
                    System.Type? modInfoType = null;
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            modInfoType = assembly.GetType("Duckov.Modding.ModInfo") 
                                ?? assembly.GetType("ModInfo");
                            if (modInfoType != null)
                            {
                                Debug.Log($"[BTS] Found ModInfo type in assembly: {assembly.GetName().Name}");
                                break;
                            }
                        }
                        catch { }
                    }
                    
                    if (modInfoType == null)
                    {
                        Debug.LogWarning("[BTS] ModInfo type not found, cannot initialize ModSetting");
                        return false;
                    }
                    
                    // Step 4.2: Get current mod's ModInfo
                    // ModBehaviour inherits from Duckov.Modding.ModBehaviour which should have ModInfo property
                    object? modInfo = null;
                    
                    // Try 1: Direct property access (if ModInfo is accessible)
                    try
                    {
                        Debug.Log("[BTS] Attempting to get ModInfo via property...");
                        // Use reflection to access ModInfo property from base class
                        var modInfoProperty = typeof(Duckov.Modding.ModBehaviour).GetProperty("ModInfo",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (modInfoProperty != null)
                        {
                            Debug.Log("[BTS] ModInfo property found, attempting to get value...");
                            modInfo = modInfoProperty.GetValue(this);
                            if (modInfo != null)
                            {
                                Debug.Log("[BTS] ✓ Got ModInfo from ModBehaviour.ModInfo property");
                            }
                            else
                            {
                                Debug.LogWarning("[BTS] ModInfo property exists but returned null");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[BTS] ModInfo property not found in ModBehaviour");
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[BTS] Failed to get ModInfo via property: {ex.Message}");
                        Debug.LogWarning($"[BTS] Exception stack: {ex.StackTrace}");
                    }
                    
                    // Try 2: Check if there's a field instead of property
                    if (modInfo == null)
                    {
                        try
                        {
                            Debug.Log("[BTS] Attempting to get ModInfo via field...");
                            var modInfoField = typeof(Duckov.Modding.ModBehaviour).GetField("ModInfo",
                                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            if (modInfoField != null)
                            {
                                Debug.Log("[BTS] ModInfo field found, attempting to get value...");
                                modInfo = modInfoField.GetValue(this);
                                if (modInfo != null)
                                {
                                    Debug.Log("[BTS] ✓ Got ModInfo from ModBehaviour.ModInfo field");
                                }
                                else
                                {
                                    Debug.LogWarning("[BTS] ModInfo field exists but returned null");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("[BTS] ModInfo field not found in ModBehaviour");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to get ModInfo via field: {ex.Message}");
                            Debug.LogWarning($"[BTS] Exception stack: {ex.StackTrace}");
                        }
                    }
                    
                    // Try 3: Try to find ModInfo from ModManager by searching all loaded mods
                    if (modInfo == null)
                    {
                        try
                        {
                            Debug.Log("[BTS] Attempting to get ModInfo from ModManager...");
                            // First, try to find ModManager type in the correct assembly
                            System.Type? modManagerType = null;
                            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                try
                                {
                                    modManagerType = assembly.GetType("Duckov.Modding.ModManager");
                                    if (modManagerType != null)
                                    {
                                        Debug.Log($"[BTS] Found ModManager in assembly: {assembly.GetName().Name}");
                                        break;
                                    }
                                }
                                catch { }
                            }
                            
                            if (modManagerType == null)
                            {
                                Debug.LogWarning("[BTS] ModManager type not found");
                            }
                            else
                            {
                                // Try to get all loaded mods
                                var getAllModsMethod = modManagerType.GetMethod("GetAllMods",
                                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                                if (getAllModsMethod != null)
                                {
                                    Debug.Log("[BTS] Found GetAllMods method, attempting to get all mods...");
                                    var allMods = getAllModsMethod.Invoke(null, null);
                                    if (allMods != null)
                                    {
                                        // Try to iterate through the collection
                                        var enumerable = allMods as System.Collections.IEnumerable;
                                        if (enumerable != null)
                                        {
                                            foreach (var mod in enumerable)
                                            {
                                                try
                                                {
                                                    var nameProp = mod.GetType().GetProperty("Name");
                                                    if (nameProp != null)
                                                    {
                                                        var name = nameProp.GetValue(mod)?.ToString();
                                                        Debug.Log($"[BTS] Found mod: {name}");
                                                        if (name != null && (name.Contains("BetterThrowing") || name.Contains("更好的投掷物")))
                                                        {
                                                            modInfo = mod;
                                                            Debug.Log($"[BTS] ✓ Found our ModInfo by searching all mods: {name}");
                                                            break;
                                                        }
                                                    }
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                }
                                
                                // Fallback: Try GetModInfo method
                                if (modInfo == null)
                                {
                                    var getModInfoMethod = modManagerType.GetMethod("GetModInfo",
                                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic,
                                        null,
                                        new System.Type[] { typeof(string) },
                                        null);
                                    if (getModInfoMethod != null)
                                    {
                                        Debug.Log("[BTS] Found GetModInfo method, trying 'BetterThrowingSystem'...");
                                        modInfo = getModInfoMethod.Invoke(null, new object[] { "BetterThrowingSystem" });
                                        if (modInfo != null)
                                        {
                                            Debug.Log("[BTS] ✓ Got ModInfo from ModManager.GetModInfo('BetterThrowingSystem')");
                                        }
                                        else
                                        {
                                            Debug.LogWarning("[BTS] GetModInfo('BetterThrowingSystem') returned null, trying display name...");
                                            modInfo = getModInfoMethod.Invoke(null, new object[] { "更好的投掷物系统" });
                                            if (modInfo != null)
                                            {
                                                Debug.Log("[BTS] ✓ Got ModInfo from ModManager.GetModInfo('更好的投掷物系统')");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to get ModInfo from ModManager: {ex.Message}");
                            Debug.LogWarning($"[BTS] Exception stack: {ex.StackTrace}");
                        }
                    }
                    
                    // Try 4: Create a basic ModInfo object as last resort
                    if (modInfo == null)
                    {
                        try
                        {
                            Debug.Log("[BTS] Attempting to create ModInfo object as last resort...");
                            // List all constructors to see what's available
                            var constructors = modInfoType.GetConstructors();
                            Debug.Log($"[BTS] Found {constructors.Length} constructors for ModInfo");
                            foreach (var ctor in constructors)
                            {
                                var paramTypes = string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.Name));
                                Debug.Log($"[BTS]   - Constructor({paramTypes})");
                            }
                            
                            // Try to create ModInfo with default constructor
                            var modInfoCtor = modInfoType.GetConstructor(new System.Type[] { });
                            if (modInfoCtor != null)
                            {
                                Debug.Log("[BTS] Found default constructor, attempting to create ModInfo...");
                                modInfo = modInfoCtor.Invoke(null);
                                Debug.Log("[BTS] ModInfo object created, attempting to set properties...");
                                
                                // Try to set name property if it exists
                                var nameProp = modInfoType.GetProperty("Name");
                                if (nameProp != null)
                                {
                                    Debug.Log($"[BTS] Name property found, CanWrite={nameProp.CanWrite}");
                                    if (nameProp.CanWrite)
                                    {
                                        nameProp.SetValue(modInfo, "更好的投掷物系统");
                                        Debug.Log("[BTS] Set Name property to '更好的投掷物系统'");
                                    }
                                }
                                
                                Debug.Log("[BTS] ✓ Created ModInfo object with default constructor");
                            }
                            else
                            {
                                Debug.LogWarning("[BTS] No default constructor found for ModInfo");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to create ModInfo: {ex.Message}");
                            Debug.LogWarning($"[BTS] Exception stack: {ex.StackTrace}");
                        }
                    }
                    
                    if (modInfo == null)
                    {
                        Debug.LogWarning("[BTS] Could not get or create ModInfo object. Cannot initialize ModSetting.");
                        return false;
                    }
                    
                    // Step 4.3: Call Init(ModInfo modInfo)
                    var initMethod = modConfigApiType.GetMethod("Init", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                        null,
                        new System.Type[] { modInfoType },
                        null);
                    
                    if (initMethod == null)
                    {
                        Debug.LogWarning("[BTS] Init method not found in ModSettingAPI");
                        return false;
                    }
                    
                    try
                    {
                        initMethod.Invoke(null, new object[] { modInfo });
                        Debug.Log("[BTS] ModSettingAPI.Init() called successfully");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[BTS] Failed to call ModSettingAPI.Init(): {ex.Message}");
                        Debug.LogWarning($"[BTS] Exception: {ex}");
                        return false;
                    }
                    
                    // Step 4.4: Add individual settings using AddToggle, AddDropdownList, etc.
                    try
                    {
                        // Add Toggle for ThrowSoundEnabled
                        var addToggleMethod = modConfigApiType.GetMethod("AddToggle",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                            null,
                            new System.Type[] { typeof(string), typeof(string), typeof(bool), typeof(System.Action<bool>) },
                            null);
                        
                        if (addToggleMethod != null)
                        {
                            System.Action<bool> throwSoundCallback = (value) => {
                                throwSoundEnabled = value;
                                Debug.Log($"[BTS] ThrowSoundEnabled changed to: {value}");
                            };
                            
                            addToggleMethod.Invoke(null, new object[] { 
                                "ThrowSoundEnabled", 
                                "投掷音效开关 / Throw Sound", 
                                throwSoundEnabled,
                                throwSoundCallback
                            });
                            Debug.Log("[BTS] Added ThrowSoundEnabled toggle");
                        }
                        
                        // Add Dropdown for ThrowMode
                        var addDropdownMethod = modConfigApiType.GetMethod("AddDropdownList",
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                            null,
                            new System.Type[] { typeof(string), typeof(string), typeof(System.Collections.Generic.List<string>), typeof(string), typeof(System.Action<string>) },
                            null);
                        
                        if (addDropdownMethod != null)
                        {
                            var throwModeOptions = new System.Collections.Generic.List<string> { "按G装备", "按G投掷" };
                            string currentThrowMode = throwMode == ThrowMode.Equip ? "按G装备" : "按G投掷";
                            
                            System.Action<string> throwModeCallback = (value) => {
                                throwMode = value == "按G装备" ? ThrowMode.Equip : ThrowMode.Throw;
                                Debug.Log($"[BTS] ThrowMode changed to: {value}");
                            };
                            
                            addDropdownMethod.Invoke(null, new object[] { 
                                "ThrowMode", 
                                "按G键模式 / G Key Mode", 
                                throwModeOptions,
                                currentThrowMode,
                                throwModeCallback
                            });
                            Debug.Log("[BTS] Added ThrowMode dropdown");
                        }
                        
                        Debug.Log("[BTS] ModSetting registration completed successfully!");
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[BTS] Failed to add ModSetting UI elements: {ex.Message}");
                        Debug.LogError($"[BTS] Exception: {ex}");
                        return false;
                    }
                }
                else
                {
                    // ModConfig API - try RegisterConfig first
                    var configObj = new BetterThrowingSystemConfig
                    {
                        ThrowSoundEnabled = throwSoundEnabled,
                        ThrowMode = throwMode
                    };
                    
                    var registerConfigMethod = modConfigApiType.GetMethod("RegisterConfig", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                        null,
                        new System.Type[] { typeof(object) },
                        null);
                    
                    if (registerConfigMethod != null)
                    {
                        try
                        {
                            registerConfigMethod.Invoke(null, new object[] { configObj });
                            Debug.Log("[BTS] ModConfig registered successfully via RegisterConfig(object)");
                            return true;
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"[BTS] Failed to register via RegisterConfig: {ex.Message}");
                        }
                    }
                    
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BTS] Failed to initialize ModConfig: {ex}");
                Debug.LogError($"[BTS] Exception details: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        
        /// <summary>
        /// Configuration class for ModConfig integration
        /// This class will be automatically displayed in the game's Mod Settings tab
        /// Note: Header and Tooltip attributes are used by ModConfig framework to display settings in UI
        /// </summary>
        [System.Serializable]
        public class BetterThrowingSystemConfig
        {
            // 投掷音效设置 / Throw Sound Settings
            public bool ThrowSoundEnabled = true;
            
            // 按G键模式 / G Key Mode (按G装备 = Equip on G, 按G投掷 = Throw on G)
            public ThrowMode ThrowMode = ThrowMode.Equip;
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
            if (IsThrowKeyDown())
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
            if (item == null)
            {
                return false;
            }

            int typeID = item.TypeID;

            if (enabledThrowableTypeIDs.TryGetValue(typeID, out bool enabled))
            {
                return enabled;
            }

            if (TryAutoRegisterThrowable(item))
            {
                return enabledThrowableTypeIDs.TryGetValue(typeID, out bool autoEnabled) && autoEnabled;
            }

            return false;
        }

        private bool TryAutoRegisterThrowable(Item item)
        {
            if (item == null)
            {
                return false;
            }

            int typeID = item.TypeID;

            if (enabledThrowableTypeIDs.ContainsKey(typeID))
            {
                return enabledThrowableTypeIDs[typeID];
            }

            if (!LooksLikeThrowable(item, out string reason, out bool isGrenadeLike, out bool hasDelayField))
            {
                return false;
            }

            string displayName = GetItemDisplayName(item);
            throwableDisplayNames[typeID] = displayName;

            bool enabled = true;
            string settingKey = $"ThrowableRecognition_{typeID}";

            if (ModSettingAPI.IsInit && ModSettingAPI.GetSavedValue<bool>(settingKey, out bool savedValue))
            {
                enabled = savedValue;
            }
            else if (ModSettingAPI.IsInit)
            {
                ModSettingAPI.SetValue(settingKey, true);
            }

            enabledThrowableTypeIDs[typeID] = enabled;
            autoDetectedThrowableTypeIDs.Add(typeID);
            if (throwableItemCache.ContainsKey(typeID))
            {
                throwableItemCache.Remove(typeID);
            }

            Debug.Log($"[BTS] ✓ Auto-detected throwable TypeID {typeID} ({displayName}) via {reason} (Enabled={enabled})");

            if (isGrenadeLike)
            {
                if (warmGrenadeCandidateTypeIDs.Add(typeID))
                {
                    Debug.Log($"[BTS] ✓ Added TypeID {typeID} to warm grenade candidate list (auto-detected)");
                }

                if (hasDelayField && warmGrenadeDeferredZeroTypeIDs.Add(typeID))
                {
                    Debug.Log($"[BTS] ✓ Added TypeID {typeID} to warm grenade deferred zero list (auto-detected)");
                }
            }

            if (ModSettingAPI.IsInit)
            {
                StartCoroutine(RecreateThrowableRecognitionDropdown());
            }

            return enabled;
        }

        private bool LooksLikeThrowable(Item item, out string reason, out bool isGrenadeLike, out bool hasDelayField)
        {
            reason = string.Empty;
            isGrenadeLike = false;
            hasDelayField = false;

            try
            {
                var skillContextProp = item.GetType().GetProperty("SkillContext", AutoDetectBindingFlags);
                if (skillContextProp != null)
                {
                    var skillContext = skillContextProp.GetValue(item);
                    if (skillContext != null)
                    {
                        var skillType = skillContext.GetType();
                        string skillNameLower = skillType.Name.ToLowerInvariant();

                        if (AutoDetectSkillKeywords.Any(keyword => skillNameLower.Contains(keyword)))
                        {
                            reason = $"SkillContext {skillType.Name}";
                            isGrenadeLike = AutoDetectGrenadeKeywords.Any(keyword => skillNameLower.Contains(keyword));
                        }

                        foreach (var delayKeyword in AutoDetectDelayKeywords)
                        {
                            var delayField = skillType.GetField(delayKeyword, AutoDetectBindingFlags);
                            if (delayField != null)
                            {
                                hasDelayField = true;
                                if (string.IsNullOrEmpty(reason))
                                {
                                    reason = $"SkillContext {skillType.Name}.{delayField.Name}";
                                }
                                break;
                            }

                            var delayProperty = skillType.GetProperty(delayKeyword, AutoDetectBindingFlags);
                            if (delayProperty != null)
                            {
                                hasDelayField = true;
                                if (string.IsNullOrEmpty(reason))
                                {
                                    reason = $"SkillContext {skillType.Name}.{delayProperty.Name}";
                                }
                                break;
                            }
                        }

                        var collideField = skillType.GetField("delayFromCollide", AutoDetectBindingFlags) ?? skillType.GetField("impactDetonate", AutoDetectBindingFlags);
                        if (collideField != null && string.IsNullOrEmpty(reason))
                        {
                            reason = $"SkillContext {skillType.Name}.{collideField.Name}";
                            isGrenadeLike = true;
                        }

                        if (!string.IsNullOrEmpty(reason))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BTS] Auto-detect: failed to inspect SkillContext for {item.name}: {ex.Message}");
            }

            try
            {
                foreach (var field in item.GetType().GetFields(AutoDetectBindingFlags))
                {
                    string fieldNameLower = field.Name.ToLowerInvariant();
                    if (AutoDetectDelayKeywords.Any(keyword => fieldNameLower.Contains(keyword)))
                    {
                        hasDelayField = true;
                        if (string.IsNullOrEmpty(reason))
                        {
                            reason = $"Item field {field.Name}";
                        }
                        isGrenadeLike = true;
                        return true;
                    }
                }

                foreach (var property in item.GetType().GetProperties(AutoDetectBindingFlags))
                {
                    string propertyNameLower = property.Name.ToLowerInvariant();
                    if (AutoDetectDelayKeywords.Any(keyword => propertyNameLower.Contains(keyword)))
                    {
                        hasDelayField = true;
                        if (string.IsNullOrEmpty(reason))
                        {
                            reason = $"Item property {property.Name}";
                        }
                        isGrenadeLike = true;
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                var components = item.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        continue;
                    }

                    string compNameLower = component.GetType().Name.ToLowerInvariant();
                    if (AutoDetectComponentKeywords.Any(keyword => compNameLower.Contains(keyword)))
                    {
                        reason = $"Component {component.GetType().Name}";
                        if (AutoDetectGrenadeKeywords.Any(keyword => compNameLower.Contains(keyword)))
                        {
                            isGrenadeLike = true;
                        }
                        return true;
                    }
                }
            }
            catch { }

            string typeNameLower = item.GetType().Name.ToLowerInvariant();
            if (AutoDetectComponentKeywords.Any(keyword => typeNameLower.Contains(keyword)))
            {
                reason = $"Type {item.GetType().Name}";
                if (AutoDetectGrenadeKeywords.Any(keyword => typeNameLower.Contains(keyword)))
                {
                    isGrenadeLike = true;
                }
                return true;
            }

            string itemNameLower = (item.name ?? string.Empty).ToLowerInvariant();
            if (AutoDetectNameKeywords.Any(keyword => itemNameLower.Contains(keyword)))
            {
                reason = "Name keyword match";
                if (AutoDetectGrenadeKeywords.Any(keyword => itemNameLower.Contains(keyword)))
                {
                    isGrenadeLike = true;
                }
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private string GetItemDisplayName(Item item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            string displayName = string.Empty;

            try
            {
                var displayNameProp = item.GetType().GetProperty("DisplayName", AutoDetectBindingFlags);
                if (displayNameProp != null)
                {
                    var value = displayNameProp.GetValue(item);
                    if (value != null)
                    {
                        displayName = value.ToString();
                    }
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = item.name ?? $"TypeID {item.TypeID}";
            }

            displayName = displayName.Replace("(Clone)", string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = $"TypeID {item.TypeID}";
            }

            return displayName;
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
            // Clean up radial menu UI when mod is unloaded
            if (radialMenuCanvas != null)
            {
                UnityEngine.Object.Destroy(radialMenuCanvas);
                radialMenuCanvas = null;
            }
            radialMenuItems.Clear();
            // Cleanup when mod is unloaded
            if (isThrowingMode)
            {
                UnequipThrowingItem().Forget();
            }
        }
    }
}
*/
