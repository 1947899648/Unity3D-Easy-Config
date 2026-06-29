# Unity3D-Easy-Config

> 一种快速的、轻量的、简单的本地序列化配置功能  
> A fast, lightweight, and simple local serialization configuration feature

---

## 核心用法 / Core Usage

- `EasyConfigHandler<YourConfigClass>.Data()`
- `EasyConfigHandler<YourConfigClass>.Save(newConfig)`

> 注意：内置缓存机制，故支持 Update() 等高频访问  
> Note: Built-in caching mechanism, therefore supports high-frequency access such as Update().

### 核心方法 / Core Methods

1. 泛型接口 / Generic Interface — `IEasyConfigBase<T>`
2. 泛型配置控制类 / Generic Controller — `EasyConfigHandler<T>`

---

## 使用步骤 / Usage Steps

### Step 1

导入插件 `WPZ0325.EasyConfig`，创建自定义配置类，比如 `XXXXConfig`

> Import the plugin `WPZ0325.EasyConfig` and create a custom configuration class, such as `XXXXConfig`

<p align="center">
  <img src="https://github.com/user-attachments/assets/5859302b-782f-476f-a8eb-fb70d2786a81" alt="s1" />
</p>

<p align="center"><sub>创建自定义配置类 / Create custom config class</sub></p>

### Step 2

根据需求自定义配置的数据结构

> Data structures customized according to requirements

<p align="center">
  <img src="https://github.com/user-attachments/assets/5be6ee36-743f-416b-968b-6e854faf3367" alt="s2" />
</p>

<p align="center"><sub>自定义配置数据结构 / Customize data structure</sub></p>

### Step 3

引入命名空间 `WPZ0325.EasyConfig`，给配置类实现接口 `IEasyConfigBase<XXXXConfig>`，并设定配置的默认值

> Introduce the namespace `WPZ0325.EasyConfig`, implement the `IEasyConfigBase<XXXXConfig>` interface for the configuration class, and set the default values for the configuration.

<p align="center">
  <img src="https://github.com/user-attachments/assets/fd47184d-4cb0-477e-892f-c98c50f68720" alt="s3" />
</p>

<p align="center"><sub>实现接口并设定默认值 / Implement interface & set defaults</sub></p>

### Step 4

在业务代码中直接使用 `EasyConfigHandler<XXXXConfig>` 来获取配置数据或更新配置数据，如下图：

> In the business code, directly use `EasyConfigHandler<XXXXConfig>` to obtain or update configuration data, as shown in the figure below:

<p align="center">
  <img src="https://github.com/user-attachments/assets/94f5660f-18e0-40c7-a19e-910c92729368" alt="s4" />
</p>

<p align="center"><sub>获取/更新配置数据 / Get & update config data</sub></p>

### Step 5

结束！  
> The end!

<p align="center">
  <img src="https://github.com/user-attachments/assets/309949ec-d944-45b6-8c82-09760a55a376" alt="s7" />
</p>

<p align="center"><sub>完成 / Done</sub></p>

---

## 补充说明 / Additional Notes

执行程序后 StreamingAssets 自动生成文件夹 `EasyConfigsRoot`

> After running the program, the `StreamingAssets` folder `EasyConfigsRoot` is automatically created.

该文件夹里面存放配置 Json 文件，用户可根据需要直接自定义编辑参数值即可

> This folder contains configuration JSON files. Users can directly customize and edit the parameter values as needed.

<p align="center">
  <img src="https://github.com/user-attachments/assets/36bc0cd8-93b8-4faf-890b-2d72bf01ec35" alt="s5" />
</p>

<p align="center"><sub>自动生成 EasyConfigsRoot 文件夹 / Auto-generated folder</sub></p>

<p align="center">
  <img src="https://github.com/user-attachments/assets/348389de-013b-4b5e-86c0-59a729e3d763" alt="s6" />
</p>

<p align="center"><sub>Json 文件内容示例 / JSON file content</sub></p>
