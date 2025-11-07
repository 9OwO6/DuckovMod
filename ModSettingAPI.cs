using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Duckov.Modding;
using UnityEngine;

namespace BetterThrowingSystem
{
    /// <summary>
    /// ModSettingAPI wrapper - similar to RadialMenu's implementation
    /// Provides a clean interface to interact with ModSetting mod
    /// </summary>
    public static class ModSettingAPI
    {
        private const string ADD_DROP_DOWN_LIST = "AddDropDownList";
        private const string ADD_SLIDER = "AddSlider";
        private const string ADD_TOGGLE = "AddToggle";
        private const string ADD_KEYBINDING = "AddKeybinding";
        private const string GET_VALUE = "GetValue";
        private const string SET_VALUE = "SetValue";
        private const string REMOVE_UI = "RemoveUI";
        private const string REMOVE_MOD = "RemoveMod";
        private const string ADD_INPUT = "AddInput";
        private const string HAS_CONFIG = "HasConfig";
        private const string GET_SAVED_VALUE = "GetSavedValue";
        
        private static float Version = 0.2f;
        public const string MOD_NAME = "ModSetting";
        private const string TYPE_NAME = "ModSetting.ModBehaviour";
        private static Type? modBehaviour;
        private static ModInfo modInfo;
        
        public static bool IsInit { get; private set; }
        
        // Cache delegates to avoid repeated reflection
        private static Dictionary<string, Delegate> methodCache = new Dictionary<string, Delegate>();
        
        private static readonly string[] methodNames = new[] {
            ADD_DROP_DOWN_LIST,
            ADD_SLIDER,
            ADD_TOGGLE,
            ADD_KEYBINDING,
            GET_VALUE,
            SET_VALUE,
            REMOVE_UI,
            REMOVE_MOD,
            ADD_INPUT
        };
        
        public static bool Init(ModInfo modInfo)
        {
            if (IsInit)
            {
                Debug.Log("[BTS] ModSettingAPI already initialized, returning true");
                return true;
            }
            
            ModSettingAPI.modInfo = modInfo;
            Debug.Log($"[BTS] ModSettingAPI.Init called with modInfo - name: '{modInfo.name}', displayName: '{modInfo.displayName}'");
            
            modBehaviour = FindTypeInAssemblies(TYPE_NAME);
            if (modBehaviour == null)
            {
                Debug.LogWarning("[BTS] ModSettingAPI: ModSetting.ModBehaviour type not found");
                return false;
            }
            
            if (!VersionAvailable())
            {
                Debug.LogWarning("[BTS] ModSettingAPI: Version check failed");
                return false;
            }
            
            foreach (string methodName in methodNames)
            {
                MethodInfo[] methodInfos = modBehaviour.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name == methodName)
                    .ToArray();
                if (methodInfos.Length == 0)
                {
                    Debug.LogWarning($"[BTS] ModSettingAPI: {methodName} method not found");
                    return false;
                }
            }
            
            IsInit = true;
            Debug.Log($"[BTS] ModSettingAPI initialized successfully - IsInit: {IsInit}, modInfo.name: '{modInfo.name}', modInfo.displayName: '{modInfo.displayName}'");
            return true;
        }
        
        public static bool AddDropdownList(string key, string description,
            List<string> options, string defaultValue, Action<string>? onValueChange = null)
        {
            Debug.Log($"[BTS] ModSettingAPI.AddDropdownList called - key: '{key}', description: '{description}', defaultValue: '{defaultValue}', options count: {options?.Count ?? 0}");
            if (!Available(key))
            {
                Debug.LogWarning($"[BTS] ModSettingAPI.AddDropdownList: Available(key) returned false for '{key}'");
                return false;
            }
            Type delegateType = typeof(Action<ModInfo, string, string, List<string>, string, Action<string>>);
            Action<string> callback = onValueChange ?? (x => { });
            Debug.Log($"[BTS] ModSettingAPI.AddDropdownList: Calling InvokeMethod with modInfo.name='{modInfo.name}', modInfo.displayName='{modInfo.displayName}'");
            return InvokeMethod(ADD_DROP_DOWN_LIST,
                ADD_DROP_DOWN_LIST,
                new object[] { modInfo, key, description, options, defaultValue, callback },
                delegateType);
        }
        
        public static bool AddSlider(string key, string description,
            float defaultValue, Vector2 sliderRange, Action<float>? onValueChange = null, int decimalPlaces = 1, int characterLimit = 5)
        {
            if (!Available(key)) return false;
            Type[] paramTypes = {
                typeof(ModInfo), typeof(string), typeof(string),
                typeof(float), typeof(Vector2), typeof(Action<float>), typeof(int), typeof(int)
            };
            Type delegateType = typeof(Action<ModInfo, string, string, float, Vector2, Action<float>, int, int>);
            return InvokeMethod(ADD_SLIDER + "Float",
                ADD_SLIDER,
                new object[]
                    { modInfo, key, description, defaultValue, sliderRange, onValueChange ?? (x => { }), decimalPlaces, characterLimit },
                delegateType,
                paramTypes);
        }
        
        public static bool AddSlider(string key, string description,
            int defaultValue, int minValue, int maxValue, Action<int>? onValueChange = null, int characterLimit = 5)
        {
            if (!Available(key)) return false;
            Type[] paramTypes = {
                typeof(ModInfo), typeof(string), typeof(string),
                typeof(int), typeof(int), typeof(int), typeof(Action<int>), typeof(int)
            };
            Type delegateType = typeof(Action<ModInfo, string, string, int, int, int, Action<int>, int>);
            return InvokeMethod(ADD_SLIDER + "Int", ADD_SLIDER,
                new object[]
                    { modInfo, key, description, defaultValue, minValue, maxValue, onValueChange ?? (x => { }), characterLimit },
                delegateType,
                paramTypes);
        }
        
        public static bool AddToggle(string key, string description,
            bool enable, Action<bool>? onValueChange = null)
        {
            Debug.Log($"[BTS] ModSettingAPI.AddToggle called - key: '{key}', description: '{description}', enable: {enable}");
            if (!Available(key))
            {
                Debug.LogWarning($"[BTS] ModSettingAPI.AddToggle: Available(key) returned false for '{key}'");
                return false;
            }
            Type delegateType = typeof(Action<ModInfo, string, string, bool, Action<bool>>);
            Action<bool> callback = onValueChange ?? (x => { });
            Debug.Log($"[BTS] ModSettingAPI.AddToggle: Calling InvokeMethod with modInfo.name='{modInfo.name}', modInfo.displayName='{modInfo.displayName}'");
            return InvokeMethod(ADD_TOGGLE,
                ADD_TOGGLE,
                new object[] { modInfo, key, description, enable, callback },
                delegateType);
        }
        
        public static bool AddKeybinding(string key, string description,
            KeyCode keyCode, Action<KeyCode>? onValueChange = null)
        {
            Debug.Log($"[BTS] ModSettingAPI.AddKeybinding called - key: '{key}', description: '{description}', keyCode: {keyCode}");
            if (!Available(key))
            {
                Debug.LogWarning($"[BTS] ModSettingAPI.AddKeybinding: Available(key) returned false for '{key}'");
                return false;
            }
            Type delegateType = typeof(Action<ModInfo, string, string, KeyCode, Action<KeyCode>>);
            Action<KeyCode> callback = onValueChange ?? (x => { });
            Debug.Log($"[BTS] ModSettingAPI.AddKeybinding: Calling InvokeMethod with modInfo.name='{modInfo.name}', modInfo.displayName='{modInfo.displayName}'");
            return InvokeMethod(ADD_KEYBINDING,
                ADD_KEYBINDING,
                new object[] { modInfo, key, description, keyCode, callback },
                delegateType);
        }
        
        public static bool AddInput(string key, string description,
            string defaultValue, int characterLimit = 40, Action<string>? onValueChange = null)
        {
            if (!Available(key)) return false;
            return InvokeMethod(ADD_INPUT,
                ADD_INPUT,
                new object[] { modInfo, key, description, defaultValue, characterLimit, onValueChange ?? (x => { }) },
                typeof(Action<ModInfo, string, string, string, int, Action<string>>));
        }
        
        public static bool GetValue<T>(string key, Action<T>? callback = null)
        {
            if (!Available(key)) return false;
            MethodInfo? methodInfo = GetStaticPublicMethodInfo(GET_VALUE);
            if (methodInfo == null) return false;
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(typeof(T));
            genericMethod.Invoke(null, new object[] { modInfo, key, callback ?? (x => { }) });
            return true;
        }
        
        public static bool SetValue<T>(string key, T value, Action<bool>? callback = null)
        {
            if (!Available(key)) return false;
            MethodInfo? methodInfo = GetStaticPublicMethodInfo(SET_VALUE);
            if (methodInfo == null) return false;
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(typeof(T));
            genericMethod.Invoke(null, new object[] { modInfo, key, value, callback ?? (x => { }) });
            return true;
        }
        
        public static bool HasConfig()
        {
            if (!Available()) return false;
            MethodInfo? methodInfo = GetStaticPublicMethodInfo(HAS_CONFIG);
            if (methodInfo == null) return false;
            return (bool)(methodInfo.Invoke(null, new object[] { modInfo }) ?? false);
        }
        
        public static bool GetSavedValue<T>(string key, out T value)
        {
            value = default(T)!;
            if (!Available(key)) return false;
            MethodInfo? methodInfo = GetStaticPublicMethodInfo(GET_SAVED_VALUE);
            if (methodInfo == null) return false;
            MethodInfo genericMethod = methodInfo.MakeGenericMethod(typeof(T));
            // Prepare parameter array (note: out parameter needs special handling)
            object[] parameters = new object[] { modInfo, key, null };
            bool result = (bool)(genericMethod.Invoke(null, parameters) ?? false);
            // Get the value of the out parameter
            if (parameters[2] != null)
            {
                value = (T)parameters[2];
            }
            return result;
        }
        
        public static bool RemoveUI(string key, Action<bool>? callback = null)
        {
            if (!Available(key)) return false;
            return InvokeMethod(REMOVE_UI,
                REMOVE_UI,
                new object[] { modInfo, key, callback ?? (x => { }) },
                typeof(Action<ModInfo, string, Action<bool>>));
        }
        
        public static bool RemoveMod(Action<bool>? callback = null)
        {
            if (!Available()) return false;
            Type delegateType = typeof(Action<ModInfo, Action<bool>>);
            return InvokeMethod(REMOVE_MOD, REMOVE_MOD, new object[] { modInfo, callback ?? (x => { }) }, delegateType);
        }
        
        private static bool Available()
        {
            // Match RadialMenu's Available() check exactly: modInfo.displayName != null && modInfo.name != null
            // This allows empty strings (which is what we get when info is not fully set yet)
            bool result = IsInit && modInfo.displayName != null && modInfo.name != null;
            if (!result)
            {
                Debug.LogWarning($"[BTS] ModSettingAPI.Available() = false - IsInit: {IsInit}, displayName: '{modInfo.displayName}', name: '{modInfo.name}'");
            }
            return result;
        }
        
        private static bool Available(string key)
        {
            // Match RadialMenu's Available() check exactly: modInfo.displayName != null && modInfo.name != null
            // This allows empty strings (which is what we get when info is not fully set yet)
            bool result = IsInit && modInfo.displayName != null && modInfo.name != null && key != null;
            if (!result)
            {
                Debug.LogWarning($"[BTS] ModSettingAPI.Available(key='{key}') = false - IsInit: {IsInit}, displayName: '{modInfo.displayName}', name: '{modInfo.name}'");
            }
            return result;
        }
        
        private static bool VersionAvailable()
        {
            FieldInfo? versionField = modBehaviour?.GetField("Version", BindingFlags.Public | BindingFlags.Static);
            if (versionField != null && versionField.FieldType == typeof(float))
            {
                float modSettingVersion = (float)(versionField.GetValue(null) ?? 0f);
                if (!Mathf.Approximately(modSettingVersion, Version))
                {
                    Debug.LogWarning($"[BTS] ModSettingAPI: Warning - ModSetting version: {modSettingVersion} (API version: {Version})");
                    return false;
                }
                return true;
            }
            return false;
        }
        
        private static Type? FindTypeInAssemblies(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.FullName != null && assembly.FullName.Contains(MOD_NAME))
                {
                    Debug.Log($"[BTS] ModSettingAPI: Found {MOD_NAME} related assembly: {assembly.FullName}");
                }
                
                Type? type = assembly.GetType(typeName);
                if (type != null) return type;
            }
            
            Debug.LogWarning("[BTS] ModSettingAPI: Could not find ModSetting.ModBehaviour type");
            return null;
        }
        
        private static MethodInfo? GetStaticPublicMethodInfo(string methodName, Type[]? parameterTypes = null)
        {
            if (!IsInit || modBehaviour == null) return null;
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static;
            if (parameterTypes != null)
            {
                MethodInfo[] methodInfos = modBehaviour.GetMethods(bindingFlags).Where(m => m.Name == methodName).ToArray();
                return methodInfos.Where(methodInfo =>
                {
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if (parameters.Length != parameterTypes.Length) return false;
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        // Handle parameter type matching (including inheritance and interface implementation)
                        if (!IsParameterTypeMatch(parameters[i].ParameterType, parameterTypes[i]))
                            return false;
                    }
                    return true;
                }).FirstOrDefault();
            }
            else
            {
                MethodInfo? methodInfo = modBehaviour.GetMethod(methodName, bindingFlags);
                return methodInfo;
            }
        }
        
        private static bool IsParameterTypeMatch(Type parameterType, Type providedType)
        {
            // Exact match
            if (parameterType == providedType)
                return true;
            // Handle value types and nullable types
            if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) == providedType)
                return true;
            // Handle inheritance
            if (parameterType.IsAssignableFrom(providedType))
                return true;
            return false;
        }
        
        private static bool InvokeMethod(string cacheKey, string methodName, object[] parameters, Type delegateType, Type[]? paramTypes = null)
        {
            if (!methodCache.ContainsKey(cacheKey))
            {
                Debug.Log($"[BTS] ModSettingAPI: Looking for method '{methodName}' with paramTypes: {(paramTypes != null ? string.Join(", ", paramTypes.Select(t => t.Name)) : "null")}");
                MethodInfo? method = GetStaticPublicMethodInfo(methodName, paramTypes);
                if (method == null)
                {
                    Debug.LogWarning($"[BTS] ModSettingAPI: Method '{methodName}' not found");
                    return false;
                }
                Debug.Log($"[BTS] ModSettingAPI: Found method '{methodName}', creating delegate...");
                try
                {
                    // Create delegate
                    methodCache[cacheKey] = Delegate.CreateDelegate(delegateType, method);
                    Debug.Log($"[BTS] ModSettingAPI: Delegate created successfully for '{methodName}'");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[BTS] ModSettingAPI: Failed to create delegate for '{methodName}': {ex.Message}");
                    Debug.LogError($"[BTS] Stack trace: {ex.StackTrace}");
                    return false;
                }
            }
            try
            {
                Debug.Log($"[BTS] ModSettingAPI: Invoking '{methodName}' with {parameters.Length} parameters");
                methodCache[cacheKey].DynamicInvoke(parameters);
                Debug.Log($"[BTS] ModSettingAPI: Successfully invoked '{methodName}'");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BTS] ModSettingAPI: Failed to invoke {methodName}: {ex.Message}");
                Debug.LogError($"[BTS] Exception type: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    Debug.LogError($"[BTS] Inner exception: {ex.InnerException.Message}");
                }
                Debug.LogError($"[BTS] Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}

