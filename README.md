# Unity3D-Easy-Config

一种快速的、轻量的、简单的本地序列化配置功能
A fast, lightweight, and simple local serialization configuration feature

核心用法：
Core usage:
【1】EasyConfigHandler<YourConfigClass>.Data()
【2】EasyConfigHandler<YourConfigClass>.Save(newConfig)

注意：内置缓存机制，故支持Update()等高频访问
Note: Built-in caching mechanism, therefore supports high-frequency access such as Update().

核心方法清单如下：
The list of core methods is as follows:

【1】 泛型接口（Generic Interface）：IEasyConfigBase<T>

【2】 泛型配置控制类（Generic Configuration Controller）：EasyConfigHandler<T>

使用步骤如下：
The steps to use are as follows:

【Step1】
导入插件WPZ0325.EasyConfig，创建自定义配置类，比如“XXXXConfig”
Import the plugin WPZ0325.EasyConfig and create a custom configuration class, such as 'XXXXConfig'
<img width="263" height="253" alt="s1" src="https://github.com/user-attachments/assets/5859302b-782f-476f-a8eb-fb70d2786a81" />

【Step2】
根据需求自定义配置的数据结构
Data structures customized according to requirements
<img width="586" height="413" alt="s2" src="https://github.com/user-attachments/assets/5be6ee36-743f-416b-968b-6e854faf3367" />

【Step3】
引入命名空间WPZ0325.EasyConfig，给配置类实现接口IEasyConfigBase<XXXXConfig>，并设定配置的默认值
Introduce the namespace WPZ0325.EasyConfig, implement the IEasyConfigBase<XXXXConfig> interface for the configuration class, and set the default values for the configuration.
<img width="642" height="701" alt="s3" src="https://github.com/user-attachments/assets/fd47184d-4cb0-477e-892f-c98c50f68720" />

【Step4】
在业务代码中直接使用EasyConfigHandler<XXXXConfig>来获取配置数据或更新配置数据，如下图：
In the business code, directly use EasyConfigHandler<XXXXConfig> to obtain or update configuration data, as shown in the figure below:
<img width="805" height="605" alt="s4_2" src="https://github.com/user-attachments/assets/94f5660f-18e0-40c7-a19e-910c92729368" />

【Step5】
结束！
The end!
<img width="418" height="324" alt="s7" src="https://github.com/user-attachments/assets/309949ec-d944-45b6-8c82-09760a55a376" />

补充说明：
Additional explanation:

执行程序后StreamingAssets自动生成文件夹EasyConfigsRoot
After running the program, the StreamingAssets folder EasyConfigsRoot is automatically created.

该文件夹里面存放配置Json文件，用户可根据需要直接自定义编辑参数值即可
This folder contains configuration JSON files. Users can directly customize and edit the parameter values as needed.
<img width="265" height="382" alt="s5" src="https://github.com/user-attachments/assets/36bc0cd8-93b8-4faf-890b-2d72bf01ec35" />

<img width="418" height="324" alt="s7" src="https://github.com/user-attachments/assets/aa802762-f8fe-4c59-9b41-630977ba7b57" />


