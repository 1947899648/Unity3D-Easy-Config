using UnityEngine;

namespace WPZ0325.EasyConfig
{
    /// <summary>
    /// Json工具源自定义类，默认使用UnityEngine.JsonUtility
    /// </summary>
    public class EasyConfigJsonTool
    {
        public static T JsonToObject<T>(string json) where T : class, IEasyConfigBase<T>, new()
        {
            T obj = JsonUtility.FromJson<T>(json);
            return obj;
        }

        public static string ObjectToJson<T>(T obj, bool prettyPrint = true) where T : class, IEasyConfigBase<T>, new()
        {
            string json = JsonUtility.ToJson(obj, prettyPrint);
            return json;
        }
    }
}
