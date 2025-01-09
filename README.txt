# AutoConfiguration.Core README

## 简介

AutoConfiguration.Core 是一个旨在简化配置文件读取与实例创建的 .NET 库。它支持从 JSON 文件中自动读取配置，并根据配置创建相应的实例。此外，它还支持 `Required` 属性检测、`TypeConverter` 注册以及自动配置功能。

## 主要功能

1. **自动配置**：从指定的 JSON 配置文件中读取设置，并根据这些设置自动创建实例。
2. **Required 检测**：检查配置实例中的必需属性是否已正确设置。
3. **TypeConverter 注册**：支持自定义类型转换器的注册，以便在配置读取过程中进行类型转换。

## 使用指南

### 1. 安装 AutoConfiguration.Core

你可以通过 NuGet 包管理器安装 AutoConfiguration.Core：

```bash
dotnet add package AutoConfiguration.Core
```

### 2. 创建配置类

为你的配置创建一个类，并使用 `ConfigurationAttribute` 标记它。同时，为需要检测的必需属性添加 `RequiredAttribute`。

```csharp
using AutoConfiguration.Attribute;
 
[Configuration("MyAppSettings")]
public class MyAppSettings
{
    [Required]
    public string ApiEndpoint { get; set; }
 
    public int Timeout { get; set; } = 30; // 非必需属性，有默认值
}
```

### 3. 注册 TypeConverter（可选）

如果你的配置类中包含需要自定义转换的类型，可以创建并注册一个 `TypeConverter`, `AutoConfiguration`会自动注册`[TypeConverter]`标注的转换器。

```csharp
using System.ComponentModel;
 
[TypeConverter]
public class MyCustomTypeConverter : TypeConverter
{
    // 实现自定义转换逻辑...
}
```

### 4. 加载配置

在应用程序启动时，使用 `ConfigurationLoader.Load` 方法加载配置文件。

```csharp
csharp复制代码

ConfigurationLoader.Load("appsettings.json");
```

### 5. 获取配置实例

使用 `ConfigurationLoader.GetConfiguration<T>` 方法获取配置实例。

```csharp
csharp复制代码

var settings = ConfigurationLoader.GetConfiguration<MyAppSettings>();
```

## 异常处理

- 如果在加载配置之前尝试获取配置实例，将抛出 `NullReferenceException`。
- 如果配置实例中缺少必需属性，将抛出 `ArgumentNullException`。

## 注意事项

- 确保 JSON 配置文件的格式与你的配置类相匹配。
- 如果你的配置类包含复杂的嵌套结构或集合类型，请确保 JSON 文件中的结构与之对应。
- TypeConverter 的注册逻辑可能需要根据你的实际需求进行调整。

## 贡献

我们欢迎任何形式的贡献，包括代码、文档、测试案例和反馈。请通过提交 pull request 或在 issues 中留言来参与贡献。

## 许可证

AutoConfiguration.Core 是在 MIT 许可证下发布的。有关详细信息，请参阅 [LICENSE](https://yiyan.baidu.com/chat/LICENSE) 文件。