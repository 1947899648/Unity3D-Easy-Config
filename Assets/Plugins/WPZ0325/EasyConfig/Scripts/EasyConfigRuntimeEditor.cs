using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WPZ0325.EasyConfig
{
    /// <summary>
    /// 运行时配置编辑器，通过IMGUI在运行时动态编辑所有实现IEasyConfigBase&lt;T&gt;的配置类
    /// 挂载到任意GameObject即可使用，左上角浮动按钮连续点击呼出配置窗口
    /// </summary>
    public class EasyConfigRuntimeEditor : MonoBehaviour
    {
        [Header("触发设置")]
        /// <summary>
        /// 呼出配置窗口所需的连续点击次数
        /// </summary>
        [SerializeField] private int _triggerTapCount = 8;
        /// <summary>
        /// 连续点击有效时间窗口（秒），超时计数归零
        /// </summary>
        [SerializeField] private float _triggerTimeWindow = 2.0f;

        [Header("按钮外观")]
        /// <summary>
        /// 浮动触发按钮的边长
        /// </summary>
        [SerializeField] private float _buttonSize = 40f;
        /// <summary>
        /// 浮动触发按钮距屏幕左边距
        /// </summary>
        [SerializeField] private float _buttonOffsetX = 10f;
        /// <summary>
        /// 浮动触发按钮距屏幕顶边距
        /// </summary>
        [SerializeField] private float _buttonOffsetY = 10f;
        /// <summary>
        /// 浮动触发按钮的透明度
        /// </summary>
        [Range(0f, 1f)]
        [SerializeField] private float _buttonAlpha = 0.4f;

        [Header("关闭行为")]
        /// <summary>
        /// 关闭配置窗口时是否放弃所有未保存修改（true=从磁盘刷新，false=保留草稿）
        /// </summary>
        [SerializeField] private bool _discardOnClose = false;

        /// <summary>
        /// 运行时自动扫描到的所有配置类类型列表
        /// </summary>
        private List<Type> _configTypes = new List<Type>();
        /// <summary>
        /// 当前选中的Tab页签索引
        /// </summary>
        private int _currentTabIndex = 0;
        /// <summary>
        /// 配置窗口是否可见
        /// </summary>
        private bool _isWindowVisible = false;
        /// <summary>
        /// 窗口位置是否已完成首次初始化
        /// </summary>
        private bool _windowRectInitialized = false;

        /// <summary>
        /// 当前连续点击计数
        /// </summary>
        private int _tapCount = 0;
        /// <summary>
        /// 最近一次点击时间（秒），-1表示未开始计数
        /// </summary>
        private float _lastTapTime = -1f;

        /// <summary>
        /// 各配置类当前编辑数据的缓存，Key为配置类Type
        /// </summary>
        private Dictionary<Type, object> _configDataCache = new Dictionary<Type, object>();
        /// <summary>
        /// 各配置类磁盘基线数据的深拷贝，用于与当前数据比较以判定红色标记
        /// </summary>
        private Dictionary<Type, object> _baselineData = new Dictionary<Type, object>();
        /// <summary>
        /// 折叠/展开状态缓存，Key为"{配置类名}.{字段路径}"格式
        /// </summary>
        private Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();
        /// <summary>
        /// 各配置类Tab的ScrollView滚动位置缓存
        /// </summary>
        private Dictionary<Type, Vector2> _scrollPositions = new Dictionary<Type, Vector2>();

        /// <summary>
        /// 配置窗口的矩形区域
        /// </summary>
        private Rect _windowRect;
        /// <summary>
        /// List类型字段最多展示的元素数量
        /// </summary>
        private const int MaxListItems = 50;
        /// <summary>
        /// 嵌套对象递归绘制最大深度
        /// </summary>
        private const int MaxNestDepth = 3;

        /// <summary>
        /// 启动时自动扫描所有已加载程序集中实现了IEasyConfigBase&lt;T&gt;的配置类
        /// </summary>
        private void Awake()
        {
            DiscoverConfigTypes();
        }

        /// <summary>
        /// 每帧绘制触发按钮；窗口可见时绘制可拖拽配置编辑窗口
        /// </summary>
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

        /// <summary>
        /// 绘制半透明浮动按钮，支持连续点击计数与超时重置，达到阈值后切换窗口显隐
        /// </summary>
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
                    bool wasVisible = _isWindowVisible;
                    _isWindowVisible = !_isWindowVisible;
                    _tapCount = 0;
                    _lastTapTime = -1f;

                    if (!_isWindowVisible && wasVisible)
                    {
                        OnWindowClosed();
                    }

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

        /// <summary>
        /// 配置窗口主绘制入口，包含标题栏、Tab页签、字段编辑区和操作按钮
        /// </summary>
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

        /// <summary>
        /// 窗口标题栏：右侧关闭按钮，整行可拖拽
        /// </summary>
        private void DrawWindowTitleBar()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(20)))
            {
                _isWindowVisible = false;
                OnWindowClosed();
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
        }

        #endregion

        #region Tab 页签

        /// <summary>
        /// 绘制配置类Tab页签栏，点击切换时自动加载对应数据
        /// </summary>
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

        /// <summary>
        /// 获取当前Tab配置数据与基线，在ScrollView中递归绘制所有字段
        /// </summary>
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

        /// <summary>
        /// 递归绘制对象的所有公开实例字段，根据类型分派到简单字段/嵌套对象/列表编辑器
        /// </summary>
        /// <param name="obj">当前数据对象</param>
        /// <param name="baselineObj">基线数据对象（磁盘快照），为null时不进行差异比较</param>
        /// <param name="type">对象运行时类型</param>
        /// <param name="foldoutPrefix">折叠状态Key前缀，用于区分不同层级的同名字段</param>
        /// <param name="depth">当前递归深度</param>
        /// <param name="isNew">是否为全新对象（新增项），为true时所有子字段强制红色标记</param>
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

        /// <summary>
        /// 绘制简单类型字段（bool/enum/数字/字符串），根据类型选择对应控件；与基线值不同时红色标记
        /// </summary>
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

        /// <summary>
        /// 绘制嵌套对象字段的折叠标题行，展开后递归绘制子字段；通过JsonUtility序列化比较内容判定红色
        /// </summary>
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

        /// <summary>
        /// 绘制泛型List字段的折叠标题行，展开后逐元素绘制编辑器；支持增删元素，最多展示MaxListItems项
        /// </summary>
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

        /// <summary>
        /// 绘制List中简单类型元素的编辑控件，与基线值不同时红色标记
        /// </summary>
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

        /// <summary>
        /// 绘制底部操作按钮：读取(从磁盘刷新)、保存、重置为默认值
        /// </summary>
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

        /// <summary>
        /// 确保指定配置类型的数据已加载到缓存中（懒加载）
        /// </summary>
        private void EnsureDataLoaded(Type configType)
        {
            if (!_configDataCache.ContainsKey(configType))
            {
                RefreshCurrentData(configType);
            }
        }

        /// <summary>
        /// 通过反射调用EasyConfigHandler&lt;T&gt;.Data()从磁盘强制刷新当前配置数据，并更新基线快照
        /// </summary>
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

        /// <summary>
        /// 通过反射调用EasyConfigHandler&lt;T&gt;.Save()将当前编辑数据持久化到磁盘，并更新基线快照
        /// </summary>
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

        /// <summary>
        /// 调用配置类的GetDefaultConfigData()获取代码默认值替换当前编辑数据，不更新基线（修改后字段将标红提醒未保存）
        /// </summary>
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

        /// <summary>
        /// 窗口关闭回调，若_discardOnClose为true则从磁盘刷新所有配置以放弃未保存修改
        /// </summary>
        private void OnWindowClosed()
        {
            if (!_discardOnClose) return;

            foreach (Type configType in _configTypes)
            {
                if (_configDataCache.ContainsKey(configType))
                {
                    RefreshCurrentData(configType);
                }
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 反射扫描当前AppDomain中所有已加载程序集，收集实现了IEasyConfigBase&lt;T&gt;的非抽象配置类，按名称排序
        /// </summary>
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

        /// <summary>
        /// 判断类型是否为可直接用简单控件编辑的基础类型（bool/string/enum/数值型）
        /// </summary>
        private bool IsSimpleType(Type type)
        {
            if (type == typeof(bool)) return true;
            if (type == typeof(string)) return true;
            if (type.IsEnum) return true;

            TypeCode typeCode = Type.GetTypeCode(type);
            return typeCode >= TypeCode.SByte && typeCode <= TypeCode.Decimal;
        }

        /// <summary>
        /// 获取指定字段路径的折叠/展开状态，默认为折叠(false)
        /// </summary>
        private bool GetFoldoutState(string key)
        {
            bool state = false;
            _foldoutStates.TryGetValue(key, out state);
            return state;
        }

        /// <summary>
        /// 设置指定字段路径的折叠/展开状态
        /// </summary>
        private void SetFoldoutState(string key, bool state)
        {
            _foldoutStates[key] = state;
        }

        /// <summary>
        /// 为List创建默认新元素：string返回空字符串，值类型返回零值，引用类型调用无参构造函数
        /// </summary>
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

        /// <summary>
        /// 通过EasyConfigJsonTool序列化再反序列化，创建配置对象的深拷贝，用作基线快照
        /// </summary>
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
