# 代理设置测试和故障排除指南

## 🔍 测试步骤

### 1. **基本功能测试**

#### 步骤1：设置代理
1. 打开vivaldiUpdater程序
2. 点击代理设置按钮（齿轮图标）
3. 勾选"启用代理"
4. 选择代理类型（HTTP或SOCKS5）
5. 输入代理主机地址（如：127.0.0.1）
6. 输入代理端口（如：8080）
7. 如需要，输入用户名和密码
8. 点击"确定"保存

#### 步骤2：验证保存
1. 检查程序目录下是否生成了 `proxy.json` 文件
2. 打开该文件查看内容是否正确

#### 步骤3：验证加载
1. 关闭代理设置窗口
2. 重新打开代理设置窗口
3. 检查所有设置是否正确显示

### 2. **调试信息检查**

程序现在会生成详细的调试信息：

#### 控制台输出
- 如果你从命令行运行程序，控制台会显示详细的调试信息
- 包括设置加载过程、UI控件状态等

#### 日志文件
- 程序会在运行目录生成 `proxy_debug.log` 文件
- 包含代理设置加载的详细过程
- 即使没有控制台窗口也能查看调试信息

## 🐛 故障排除

### 问题1：设置没有保存

#### 可能原因：
- 权限问题：程序无法写入配置文件
- 目录问题：程序无法找到正确的目录

#### 解决方案：
1. 以管理员身份运行程序
2. 检查程序目录是否有写入权限
3. 查看控制台输出或 `proxy_debug.log` 文件中的错误信息

### 问题2：设置保存了但没有加载

#### 可能原因：
- JSON格式错误
- UI控件初始化问题
- 配置文件路径问题

#### 解决方案：
1. 检查 `proxy.json` 文件格式是否正确
2. 查看 `proxy_debug.log` 文件中的详细信息
3. 检查是否有异常信息

### 问题3：UI控件显示不正确

#### 可能原因：
- 控件初始化时序问题
- 数据绑定问题

#### 解决方案：
1. 查看日志文件中的UI控件状态信息
2. 检查控件是否为null
3. 确认设置加载是否在窗口完全加载后执行

## 📋 调试信息解读

### 正常的调试输出应该包括：

```
=== ProxyConfigWindow Constructor Debug ===
ProxySettings loaded: UseProxy=True, Host='127.0.0.1', Port=8080
Config file path: D:\your\path\proxy.json
Config file exists: True
Config file content: {"UseProxy":true,"ProxyType":0,"ProxyHost":"127.0.0.1","ProxyPort":8080,"ProxyUsername":"","ProxyPassword":""}

=== LoadSettings Method Started ===
ProxySettings instance hash: 12345678
EnableProxyCheckBox is null: False
ProxyTypeComboBox is null: False
...
Current ProxySettings values:
  UseProxy: True
  ProxyType: Http
  ProxyHost: '127.0.0.1'
  ProxyPort: 8080
...
=== Setting UI Controls ===
Before: EnableProxyCheckBox.IsChecked = 
After: EnableProxyCheckBox.IsChecked = True
...
=== LoadSettings Completed Successfully ===
```

### 异常情况的标识：

- `ERROR: xxx is null!` - UI控件未正确初始化
- `Config file exists: False` - 配置文件未创建
- `ERROR in LoadSettings` - 加载过程中出现异常

## 🔧 手动测试配置文件

### 创建测试配置文件

在程序目录下手动创建 `proxy.json` 文件：

```json
{
  "UseProxy": true,
  "ProxyType": 0,
  "ProxyHost": "127.0.0.1",
  "ProxyPort": 8080,
  "ProxyUsername": "testuser",
  "ProxyPassword": "testpass"
}
```

其中：
- `ProxyType: 0` = HTTP
- `ProxyType: 1` = SOCKS5

### 验证配置加载

1. 保存上述JSON文件
2. 启动程序并打开代理设置
3. 检查UI是否正确显示配置

## 📞 报告问题

如果问题仍然存在，请提供以下信息：

1. **日志文件内容**：`proxy_debug.log` 的完整内容
2. **配置文件内容**：`proxy.json` 的内容（如果存在）
3. **操作步骤**：详细的操作步骤
4. **预期结果**：您期望看到什么
5. **实际结果**：实际发生了什么

## 🎯 已知修复

本次修复解决了以下问题：

1. **时序问题**：设置加载延迟到窗口完全加载后
2. **调试信息**：增加详细的调试输出和日志文件
3. **错误处理**：改进异常处理和恢复机制
4. **UI验证**：每个控件设置都有验证和调试信息

通过这些改进，应该能够更容易地识别和解决代理设置加载的问题。