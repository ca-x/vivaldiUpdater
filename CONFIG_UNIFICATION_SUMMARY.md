# 配置文件统一管理改进

## 📋 改进概述

将原来分散的两个配置文件 (`settings.json` 和 `proxy.json`) 合并为统一的 `settings.json` 文件，简化配置管理并提升用户体验。

## 🔄 改进前后对比

### 改进前
```
程序目录/
├── settings.json    # 应用设置（语言、镜像地址等）
└── proxy.json       # 代理设置（代理类型、地址、端口等）
```

### 改进后
```
程序目录/
└── settings.json    # 统一配置文件（包含所有设置）
```

## 📄 新配置文件结构

### 完整配置示例
```json
{
  "Language": "auto",
  "UseMirrorAddress": true,
  "Proxy": {
    "UseProxy": true,
    "ProxyType": 0,
    "ProxyHost": "127.0.0.1",
    "ProxyPort": 8080,
    "ProxyUsername": "testuser",
    "ProxyPassword": "testpass"
  }
}
```

### 字段说明

#### 应用程序设置
- **Language**: 界面语言设置
  - `"auto"`: 自动检测系统语言
  - `"zh-cn"`: 中文
  - `"en-us"`: 英文
- **UseMirrorAddress**: 是否使用镜像下载地址 (`true`/`false`)

#### 代理设置 (Proxy)
- **UseProxy**: 是否启用代理 (`true`/`false`)
- **ProxyType**: 代理类型
  - `0`: HTTP代理
  - `1`: SOCKS5代理
- **ProxyHost**: 代理服务器地址
- **ProxyPort**: 代理端口号
- **ProxyUsername**: 代理用户名（可选）
- **ProxyPassword**: 代理密码（可选）

## 🔧 技术实现

### 核心类结构
```csharp
public class AppSettings
{
    // 应用程序设置
    public string Language { get; set; } = "auto";
    public bool UseMirrorAddress { get; set; } = true;
    
    // 代理设置
    public ProxyConfig Proxy { get; set; } = new ProxyConfig();
    
    // 静态方法
    public static AppSettings Load();
    public void Save();
    public void ApplyLanguage();
}

public class ProxyConfig
{
    public bool UseProxy { get; set; } = false;
    public ProxyType ProxyType { get; set; } = ProxyType.Http;
    public string ProxyHost { get; set; } = "";
    public int ProxyPort { get; set; } = 0;
    public string ProxyUsername { get; set; } = "";
    public string ProxyPassword { get; set; } = "";
}
```

### 向后兼容性

程序支持从旧配置文件自动迁移：

1. **检查统一配置**: 首先尝试加载 `settings.json`
2. **自动迁移**: 如果不存在，自动从旧的 `settings.json` 和 `proxy.json` 迁移数据
3. **保存新格式**: 迁移完成后保存为新的统一格式
4. **无缝升级**: 用户无需手动操作，程序自动完成升级

### 错误处理

- **反序列化安全**: 使用字典方式手动反序列化，避免枚举类型转换错误
- **空值处理**: 对所有配置项进行空值检查和默认值设置
- **异常恢复**: 配置加载失败时使用默认配置，确保程序正常运行

## ✅ 改进优势

### 1. **简化管理**
- 只需维护一个配置文件
- 减少文件丢失和配置不一致的风险
- 便于备份和恢复设置

### 2. **提升性能**
- 减少文件I/O操作次数
- 统一的加载和保存机制
- 更高效的配置同步

### 3. **增强可维护性**
- 统一的配置结构和访问方式
- 更清晰的代码组织
- 便于添加新的配置项

### 4. **改善用户体验**
- 自动配置迁移，无需用户干预
- 更可靠的设置保存和加载
- 减少配置相关的错误

## 🧪 使用示例

### 加载配置
```csharp
var settings = AppSettings.Load();
var language = settings.Language;
var useProxy = settings.Proxy.UseProxy;
var proxyHost = settings.Proxy.ProxyHost;
```

### 保存配置
```csharp
var settings = AppSettings.Load();
settings.Language = "zh-cn";
settings.Proxy.UseProxy = true;
settings.Proxy.ProxyHost = "127.0.0.1";
settings.Save();
```

### 访问代理设置
```csharp
var settings = AppSettings.Load();
var proxyConfig = settings.Proxy;

if (proxyConfig.UseProxy)
{
    // 配置代理...
}
```

## 📝 迁移说明

- ✅ **自动迁移**: 程序首次运行时自动迁移旧配置
- ✅ **保留设置**: 所有用户设置都会被保留
- ✅ **零干扰**: 用户无需任何手动操作
- ✅ **向后兼容**: 支持新旧配置格式并存

这次改进大大简化了配置管理，提升了代码的可维护性和用户体验！