# Vivaldi++ 下载回退机制修复摘要

## 问题描述
当 Vivaldi 浏览器更新时，旧版本的 Vivaldi App 目录被重命名为 AppBackup，但是 Vivaldi++ 插件并没有下载到新的 App 目录。重新打开软件也不行，可能是因为 fastgit_mirror_url 下载失败了。

## 问题原因
在原来的实现中，Vivaldi++ 插件下载只使用了镜像地址（Fastgit），当镜像地址不可用时，没有回退到 GitHub 原始地址，导致下载失败。

## 解决方案
1. 在 [ResumeableDownloader.cs](file:///d:/code/vivaldiUpdater/Helpers/ResumeableDownloader.cs) 中添加 `DownloadFileWithFallbackAsync` 方法，支持主URL和备用URL的下载机制
2. 在 [MainViewModel.cs](file:///d:/code/vivaldiUpdater/ViewModel/MainViewModel.cs) 中修改 Vivaldi++ 下载逻辑，根据 `UseMirrorAddress` 设置决定优先使用镜像地址还是 GitHub 原始地址，并在主地址失败时回退到备用地址

## 实现细节

### ResumeableDownloader 增强
- 添加 `DownloadFileWithFallbackAsync` 方法
- 当主URL下载失败时，自动尝试备用URL
- 如果备用URL也失败，抛出包含两个异常信息的 AggregateException

### MainViewModel 逻辑修改
- 在两个Vivaldi++下载位置（新版本下载和版本一致但文件缺失的情况）都使用回退机制
- 根据 `UseMirrorAddress` 设置决定优先使用哪个URL：
  - 如果 `UseMirrorAddress` 为 true，优先使用镜像地址，回退到GitHub原始地址
  - 如果 `UseMirrorAddress` 为 false，直接使用GitHub原始地址

## 测试
添加了单元测试来验证回退机制的正确性：
- 测试主URL可用时正常下载
- 测试主URL失败时正确回退到备用URL
- 测试主URL和备用URL都失败时正确抛出异常

## 用户体验改进
- 用户不再需要手动干预就能在镜像地址失败时自动使用GitHub原始地址
- 提高了Vivaldi++插件安装的可靠性
- 保持了与界面设置的一致性（UseMirrorAddress选项）