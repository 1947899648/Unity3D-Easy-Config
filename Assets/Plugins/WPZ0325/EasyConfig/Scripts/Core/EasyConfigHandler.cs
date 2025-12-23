using UnityEngine;
using System.IO;

namespace WPZ0325.EasyConfig
{
    /// <summary>
    /// 静态类配置句柄
    /// </summary>
    /// <typeparam name="ConfigDataModel">配置数据模型类，必须实现接口IEasyConfigBase且可New()</typeparam>
    public static class EasyConfigHandler<ConfigDataModel> where ConfigDataModel : class, IEasyConfigBase<ConfigDataModel>, new()
    {
        #region 私有
        /// <summary>
        /// 配置文件本地完整路径信息，默认在Application.streamingAssetsPath里EasyConfigsRoot文件夹中
        /// </summary>
        private static string _configFileFullPath
        {
            get
            {
                string rootPath = Path.Combine(Application.streamingAssetsPath, "EasyConfigsRoot");
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
                return Path.Combine(rootPath, $"EasyConfig_{typeof(ConfigDataModel).Name}.json");
            }
        }

        /// <summary>
        /// 涉及System.IO操作
        /// </summary>
        /// <returns></returns>
        private static ConfigDataModel ReadLocalConfig()
        {
            ConfigDataModel cache = null;
            if (File.Exists(_configFileFullPath))
            {
                using (StreamReader sr = new StreamReader(_configFileFullPath))
                {
                    try
                    {
                        cache = JsonUtility.FromJson<ConfigDataModel>(sr.ReadToEnd());
                    }
                    catch
                    {
                        File.Delete(_configFileFullPath);
                        cache = ReadLocalConfig();
                    }
                }
            }
            else
            {
                cache = new ConfigDataModel().GetDefaultConfigData();
                Save(cache);
            }
            return cache;
        }

        /// <summary>
        /// 配置文件数据对象本地缓存
        /// </summary>
        private static ConfigDataModel _configData = null;
        #endregion

        #region 公有
        /// <summary>
        /// 获取配置信息，支持高频访问
        /// </summary>
        /// <param name="isUpdateCache">是否刷新本地缓存,isUpdateCache为True时不建议高频访问</param>
        /// <returns>配置信息对象</returns>
        public static ConfigDataModel Data(bool isUpdateCache = false)
        {
            if (isUpdateCache || _configData == null)
            {
                _configData = ReadLocalConfig();
            }
            return _configData;
        }

        /// <summary>
        /// 保存配置信息至本地
        /// </summary>
        /// <param name="configData"></param>
        /// <param name="prettyPrint"></param>
        public static void Save(ConfigDataModel configData, bool prettyPrint = true)
        {
            using (FileStream fs = new FileStream(_configFileFullPath, FileMode.Create, FileAccess.ReadWrite))//权限是读和写
            {
                StreamWriter sw = new StreamWriter(fs);
                string content = JsonUtility.ToJson(configData, prettyPrint);
                sw.Write(content);
                sw.Close();
                fs.Close();
            }
        }
        #endregion
    }
}

