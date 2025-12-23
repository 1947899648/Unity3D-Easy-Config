using System;

namespace WPZ0325.EasyConfig
{
    /// <summary>
    /// 配置类必备接口
    /// </summary>
    /// <typeparam name="ConfigDataModel">配置数据模型</typeparam>
    public interface IEasyConfigBase<ConfigDataModel>
    {
        /// <summary>
        /// 获取默认配置数据对象
        /// </summary>
        /// <returns></returns>
        public ConfigDataModel GetDefaultConfigData();
    }
}