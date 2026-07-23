using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WPZ0325.EasyConfig
{
    public class EasyConfigRuntimeEditor : MonoBehaviour
    {
        [Header("触发设置")]
        [SerializeField] private int _triggerTapCount = 8;
        [SerializeField] private float _triggerTimeWindow = 2.0f;

        [Header("按钮外观")]
        [SerializeField] private float _buttonSize = 40f;
        [SerializeField] private float _buttonOffsetX = 10f;
        [SerializeField] private float _buttonOffsetY = 10f;
        [Range(0f, 1f)]
        [SerializeField] private float _buttonAlpha = 0.4f;

        private List<Type> _configTypes = new List<Type>();
        private int _currentTabIndex = 0;
        private bool _isWindowVisible = false;
        private bool _windowRectInitialized = false;

        private int _tapCount = 0;
        private float _lastTapTime = -1f;

        private Dictionary<Type, object> _configDataCache = new Dictionary<Type, object>();
        private Dictionary<Type, object> _baselineData = new Dictionary<Type, object>();
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        private Dictionary<Type, Vector2> _scrollPositions = new Dictionary<Type, Vector2>();

        private Rect _windowRect;
        private const int MaxListItems = 50;
        private const int MaxNestDepth = 3;

        private void Awake()
        {
            DiscoverConfigTypes();
        }

        private void OnGUI()
        {
            if (!_windowRectInitialized)
            {
                float windowWidth = 540f;
                float windowHeight = 440f;
                _windowRect = new Rect(
                    (Screen.width - windowWidth) / 2f,
                    (Screen.height - windowHeight) / 3f,
                    windowWidth,
                    windowHeight
                );
                _windowRectInitialized = true;
            }

            DrawTriggerButton();

            if (_isWindowVisible)
            {
                _windowRect = GUI.Window(
                    GetInstanceID(),
                    _windowRect,
                    DrawConfigWindow,
                    "EasyConfig 配置编辑器"
                );
            }
        }

        #region 触发按钮

        private void DrawTriggerButton()
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _buttonAlpha);

            Rect buttonRect = new Rect(_buttonOffsetX, _buttonOffsetY, _buttonSize, _buttonSize);
            string progressText = _tapCount > 0
                ? $"⚙{_tapCount}/{_triggerTapCount}"
                : "⚙";

            if (GUI.Button(buttonRect, progressText))
            {
                float currentTime = Time.time;
                if (_lastTapTime < 0f || (currentTime - _lastTapTime) > _triggerTimeWindow)
                {
                    _tapCount = 1;
                }
                else
                {
                    _tapCount++;
                }

                _lastTapTime = currentTime;

                if (_tapCount >= _triggerTapCount)
                {
                    _isWindowVisible = !_isWindowVisible;
                    _tapCount = 0;
                    _lastTapTime = -1f;

                    if (_isWindowVisible && _configTypes.Count > 0)
                    {
                        EnsureDataLoaded(_configTypes[_currentTabIndex]);
                    }
                }
            }

            GUI.color = oldColor;

            if (_tapCount > 0 && _lastTapTime > 0f && (Time.time - _lastTapTime) > _triggerTimeWindow)
            {
                _tapCount = 0;
                _lastTapTime = -1f;
            }
        }

        #endregion

        #region 配置窗口

        private void DrawConfigWindow(int windowID)
        {
            DrawWindowTitleBar();
            DrawTabs();

            if (_configTypes.Count == 0)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("未发现任何配置类，请确保配置类实现了 IEasyConfigBase<T> 接口。");
                GUILayout.FlexibleSpace();
            }
            else
            {
                GUILayout.Space(5);
                DrawCurrentConfigFields();
                GUILayout.Space(5);
                DrawActionButtons();
            }
        }

        private void DrawWindowTitleBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(20)))
            {
                _isWindowVisible = false;
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }

        #endregion

        #region Tab 页签

        private void DrawTabs()
        {
            if (_configTypes.Count == 0) return;

            GUILayout.BeginHorizontal();
            for (int i = 0; i < _configTypes.Count; i++)
            {
                Type configType = _configTypes[i];
                GUI.backgroundColor = (i == _currentTabIndex) ? Color.cyan : Color.gray;
                if (GUILayout.Button(configType.Name, GUILayout.Height(25)))
                {
                    if (_currentTabIndex != i)
                    {
                        _currentTabIndex = i;
                        EnsureDataLoaded(configType);
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        #endregion

        #region 字段绘制

        private void DrawCurrentConfigFields()
        {
            Type configType = _configTypes[_currentTabIndex];
            object data = _configDataCache.ContainsKey(configType) ? _configDataCache[configType] : null;
            if (data == null) return;

            if (!_scrollPositions.ContainsKey(configType))
            {
                _scrollPositions[configType] = Vector2.zero;
            }

            Vector2 scrollPos = _scrollPositions[configType];
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            _scrollPositions[configType] = scrollPos;

            string foldoutPrefix = configType.Name;
            object baseline = _baselineData.ContainsKey(configType) ? _baselineData[configType] : null;
            DrawObjectFields(data, baseline, configType, foldoutPrefix, 0);

            GUILayout.EndScrollView();
        }

        private void DrawObjectFields(object obj, object baselineObj, Type type, string foldoutPrefix, int depth, bool isNew = false)
        {
            if (obj == null || depth >= MaxNestDepth) return;

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

            foreach (FieldInfo field in fields)
            {
                if (field.IsInitOnly || field.IsLiteral) continue;

                string fieldPath = foldoutPrefix + "." + field.Name;
                Type fieldType = field.FieldType;
                object fieldValue = field.GetValue(obj);
                object baselineFieldValue = baselineObj != null ? field.GetValue(baselineObj) : null;
                bool fieldIsNew = isNew || baselineObj == null;

                if (typeof(IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
                {
                    DrawListField(field, obj, fieldPath, fieldType, fieldValue, baselineFieldValue, depth, fieldIsNew);
                }
                else if (fieldType.IsClass && fieldType != typeof(string))
                {
                    DrawNestedObjectField(field, obj, fieldPath, fieldType, fieldValue, baselineFieldValue, depth, fieldIsNew);
                }
                else
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20 * depth);
                    DrawSimpleField(field, obj, fieldType, fieldValue, baselineFieldValue, fieldIsNew);
                    GUILayout.EndHorizontal();
                }
            }
        }

        private void DrawSimpleField(FieldInfo field, object obj, Type fieldType, object fieldValue, object baselineValue, bool isNew = false)
        {
            string label = field.Name;
            float labelWidth = Mathf.Max(120f, label.Length * 9f);

            bool isDifferent = isNew || (baselineValue != null && !object.Equals(fieldValue, baselineValue));
            Color oldColor = GUI.color;
            if (isDifferent)
            {
                GUI.color = Color.red;
            }

            if (fieldType == typeof(bool))
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));
                bool boolValue = fieldValue != null && (bool)fieldValue;
                bool newBoolValue = GUILayout.Toggle(boolValue, "");
                if (newBoolValue != boolValue)
                {
                    field.SetValue(obj, newBoolValue);
                }
            }
            else if (fieldType.IsEnum)
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));
                string[] enumNames = Enum.GetNames(fieldType);
                int currentIndex = fieldValue != null ? (int)fieldValue : 0;
                int columns = Mathf.Min(enumNames.Length, 4);
                int newIndex = GUILayout.SelectionGrid(currentIndex, enumNames, columns);
                if (newIndex != currentIndex)
                {
                    field.SetValue(obj, Enum.ToObject(fieldType, newIndex));
                }
            }
            else
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));
                string currentText = fieldValue != null ? fieldValue.ToString() : "";
                string newText = GUILayout.TextField(currentText);

                if (newText != currentText)
                {
                    if (fieldType == typeof(string))
                    {
                        field.SetValue(obj, newText);
                    }
                    else
                    {
                        try
                        {
                            object convertedValue = Convert.ChangeType(newText, fieldType);
                            field.SetValue(obj, convertedValue);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            GUI.color = oldColor;
        }

        private void DrawNestedObjectField(FieldInfo field, object obj, string fieldPath, Type fieldType, object fieldValue, object baselineFieldValue, int depth, bool isNew = false)
        {
            bool isFolded = GetFoldoutState(fieldPath);

            bool isDifferent = isNew;
            if (!isNew && baselineFieldValue != null && fieldValue != null)
            {
                string currentJson = JsonUtility.ToJson(fieldValue);
                string baselineJson = JsonUtility.ToJson(baselineFieldValue);
                isDifferent = currentJson != baselineJson;
            }
            Color oldColor = GUI.color;
            if (isDifferent)
            {
                GUI.color = Color.red;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20 * depth);

            string arrow = isFolded ? "▼" : "▶";
            if (GUILayout.Button(arrow + " " + field.Name, GUILayout.ExpandWidth(false)))
            {
                SetFoldoutState(fieldPath, !isFolded);
            }

            GUILayout.EndHorizontal();

            GUI.color = oldColor;

            if (isFolded)
            {
                if (fieldValue == null)
                {
                    fieldValue = Activator.CreateInstance(fieldType);
                    field.SetValue(obj, fieldValue);
                }

                DrawObjectFields(fieldValue, baselineFieldValue, fieldType, fieldPath, depth + 1, isNew);
            }
        }

        private void DrawListField(FieldInfo field, object obj, string fieldPath, Type fieldType, object fieldValue, object baselineFieldValue, int depth, bool isNew = false)
        {
            bool isFolded = GetFoldoutState(fieldPath);
            IList list = fieldValue as IList;
            if (list == null) return;

            IList baselineList = baselineFieldValue as IList;

            bool isDifferent = isNew;
            if (!isNew && baselineFieldValue != null && fieldValue != null)
            {
                string currentJson = JsonUtility.ToJson(fieldValue);
                string baselineJson = JsonUtility.ToJson(baselineFieldValue);
                isDifferent = currentJson != baselineJson;
            }
            Color oldColor = GUI.color;
            if (isDifferent)
            {
                GUI.color = Color.red;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Space(20 * depth);

            string arrow = isFolded ? "▼" : "▶";
            if (GUILayout.Button(arrow + " " + field.Name + " (" + list.Count + "项)", GUILayout.ExpandWidth(false)))
            {
                SetFoldoutState(fieldPath, !isFolded);
            }

            GUILayout.EndHorizontal();

            GUI.color = oldColor;

            if (!isFolded) return;

            Type elementType = fieldType.GetGenericArguments()[0];
            bool isElementSimpleType = IsSimpleType(elementType);

            int removeIndex = -1;

            for (int i = 0; i < list.Count; i++)
            {
                object elementValue = list[i];
                string elementPath = fieldPath + "[" + i + "]";

                object elementBaselineValue = null;
                if (baselineList != null && i < baselineList.Count)
                {
                    elementBaselineValue = baselineList[i];
                }

                bool isNewItem = isNew || baselineList == null || i >= baselineList.Count;

                GUILayout.BeginHorizontal();
                GUILayout.Space(20 * (depth + 1));

                GUILayout.Label("[" + i + "]", GUILayout.Width(35));

                if (isElementSimpleType)
                {
                    DrawSimpleListElement(list, i, elementType, elementValue, elementBaselineValue, isNewItem);
                }
                else
                {
                    if (isNewItem)
                    {
                        GUI.color = Color.red;
                    }

                    bool elementFolded = GetFoldoutState(elementPath);
                    string elementArrow = elementFolded ? "▼" : "▶";
                    string elementLabel = elementValue != null
                        ? elementType.Name
                        : elementType.Name + " (null)";

                    if (GUILayout.Button(elementArrow + " " + elementLabel, GUILayout.ExpandWidth(false)))
                    {
                        SetFoldoutState(elementPath, !elementFolded);
                    }

                    if (isNewItem)
                    {
                        GUI.color = oldColor;
                    }

                    GUILayout.EndHorizontal();

                    if (elementFolded && elementValue != null)
                    {
                        DrawObjectFields(elementValue, elementBaselineValue, elementType, elementPath, depth + 2, isNewItem);
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20 * (depth + 1) + 35);
                }

                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    removeIndex = i;
                }

                GUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                list.RemoveAt(removeIndex);
            }

            if (list.Count < MaxListItems)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20 * (depth + 1));
                GUILayout.Space(35);
                if (GUILayout.Button("+ 添加", GUILayout.Width(60)))
                {
                    object newItem = CreateDefaultElement(elementType);
                    list.Add(newItem);
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawSimpleListElement(IList list, int index, Type elementType, object elementValue, object baselineValue, bool isNewItem = false)
        {
            bool isDifferent = isNewItem || (baselineValue != null && !object.Equals(elementValue, baselineValue));
            Color oldColor = GUI.color;
            if (isDifferent)
            {
                GUI.color = Color.red;
            }

            if (elementType == typeof(bool))
            {
                bool boolValue = elementValue != null && (bool)elementValue;
                bool newBoolValue = GUILayout.Toggle(boolValue, "");
                if (newBoolValue != boolValue)
                {
                    list[index] = newBoolValue;
                }
            }
            else if (elementType.IsEnum)
            {
                string[] enumNames = Enum.GetNames(elementType);
                int currentIndex = elementValue != null ? (int)elementValue : 0;
                int columns = Mathf.Min(enumNames.Length, 4);
                int newIndex = GUILayout.SelectionGrid(currentIndex, enumNames, columns);
                if (newIndex != currentIndex)
                {
                    list[index] = Enum.ToObject(elementType, newIndex);
                }
            }
            else
            {
                string currentText = elementValue != null ? elementValue.ToString() : "";
                string newText = GUILayout.TextField(currentText);

                if (newText != currentText)
                {
                    if (elementType == typeof(string))
                    {
                        list[index] = newText;
                    }
                    else
                    {
                        try
                        {
                            object convertedValue = Convert.ChangeType(newText, elementType);
                            list[index] = convertedValue;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            GUI.color = oldColor;
        }

        #endregion

        #region 操作按钮

        private void DrawActionButtons()
        {
            Type configType = _configTypes[_currentTabIndex];

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("读取 (刷新)", GUILayout.Width(130), GUILayout.Height(30)))
            {
                RefreshCurrentData(configType);
            }

            GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
            if (GUILayout.Button("保存", GUILayout.Width(100), GUILayout.Height(30)))
            {
                SaveCurrentData(configType);
            }

            GUI.backgroundColor = new Color(1f, 0.9f, 0.4f);
            if (GUILayout.Button("重置默认值", GUILayout.Width(100), GUILayout.Height(30)))
            {
                ResetCurrentData(configType);
            }

            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        #endregion

        #region 数据操作

        private void EnsureDataLoaded(Type configType)
        {
            if (!_configDataCache.ContainsKey(configType))
            {
                RefreshCurrentData(configType);
            }
        }

        private void RefreshCurrentData(Type configType)
        {
            Type handlerType = typeof(EasyConfigHandler<>).MakeGenericType(configType);
            MethodInfo dataMethod = handlerType.GetMethod("Data");
            if (dataMethod != null)
            {
                object data = dataMethod.Invoke(null, new object[] { true });
                _configDataCache[configType] = data;
                _baselineData[configType] = DeepCopyConfig(configType, data);
            }
        }

        private void SaveCurrentData(Type configType)
        {
            if (!_configDataCache.ContainsKey(configType)) return;

            object data = _configDataCache[configType];
            Type handlerType = typeof(EasyConfigHandler<>).MakeGenericType(configType);
            MethodInfo saveMethod = handlerType.GetMethod("Save");
            if (saveMethod != null)
            {
                saveMethod.Invoke(null, new object[] { data, true });
                _baselineData[configType] = DeepCopyConfig(configType, data);
            }
        }

        private void ResetCurrentData(Type configType)
        {
            object tempInstance = Activator.CreateInstance(configType);
            MethodInfo getDefaultMethod = configType.GetMethod("GetDefaultConfigData");
            if (getDefaultMethod != null)
            {
                object defaultData = getDefaultMethod.Invoke(tempInstance, null);
                _configDataCache[configType] = defaultData;
            }
        }

        #endregion

        #region 工具方法

        private void DiscoverConfigTypes()
        {
            _configTypes.Clear();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition) continue;

                    Type[] interfaces = type.GetInterfaces();
                    foreach (Type iface in interfaces)
                    {
                        if (iface.IsGenericType &&
                            iface.GetGenericTypeDefinition() == typeof(IEasyConfigBase<>))
                        {
                            _configTypes.Add(type);
                            break;
                        }
                    }
                }
            }

            _configTypes.Sort((Type a, Type b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        }

        private bool IsSimpleType(Type type)
        {
            if (type == typeof(bool)) return true;
            if (type == typeof(string)) return true;
            if (type.IsEnum) return true;

            TypeCode typeCode = Type.GetTypeCode(type);
            return typeCode >= TypeCode.SByte && typeCode <= TypeCode.Decimal;
        }

        private bool GetFoldoutState(string key)
        {
            bool state = false;
            _foldoutStates.TryGetValue(key, out state);
            return state;
        }

        private void SetFoldoutState(string key, bool state)
        {
            _foldoutStates[key] = state;
        }

        private object CreateDefaultElement(Type elementType)
        {
            if (elementType == typeof(string))
            {
                return "";
            }

            if (elementType.IsValueType)
            {
                return Activator.CreateInstance(elementType);
            }

            return Activator.CreateInstance(elementType);
        }

        private object DeepCopyConfig(Type configType, object source)
        {
            Type jsonToolType = typeof(EasyConfigJsonTool);
            MethodInfo objectToJson = jsonToolType.GetMethod("ObjectToJson");
            MethodInfo jsonToObject = jsonToolType.GetMethod("JsonToObject");

            MethodInfo genericObjToJson = objectToJson.MakeGenericMethod(configType);
            MethodInfo genericJsonToObj = jsonToObject.MakeGenericMethod(configType);

            string json = (string)genericObjToJson.Invoke(null, new object[] { source, false });
            object copy = genericJsonToObj.Invoke(null, new object[] { json });
            return copy;
        }

        #endregion
    }
}
