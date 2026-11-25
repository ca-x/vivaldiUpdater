## vivaldi updater 
 vivaldi updater is an updater for  windows vivaldi browser with vivaldi++ 
<img width="870" height="814" alt="image" src="https://github.com/user-attachments/assets/1683a193-c477-4167-90a6-598a41b45161" />

<img width="841" height="793" alt="image" src="https://github.com/user-attachments/assets/0f09d456-8ce4-4bd9-a3d9-3ecf6efc464c" />

## Publishing a release

1. In `Properties/AssemblyInfo.cs`, bump `AssemblyVersion` and `AssemblyFileVersion` to the same four-part version, higher than the latest stable release. Commit the version bump.
2. Push the release commit to `main`. Then tag that commit with the matching version (for example, `git tag v1.0.3.10`) and push the tag separately (`git push origin v1.0.3.10`). The **Release VivaldiUpdater** workflow builds on Windows and rejects tags that do not match the executable's `AssemblyVersion`.

The workflow publishes a stable GitHub release marked **Latest**, with `Release.zip` as its only uploaded asset. The self-updater checks the latest release in `ca-x/vivaldiUpdater` and downloads the first asset, so do not add other assets. For a manual install, extract the whole ZIP before running `VivaldiUpdater.exe`.

If updating from v1.0.3.9 or earlier fails while the English interface is active, close Vivaldi Updater and extract the full `Release.zip` into its existing directory. Keep the existing `settings.json` and `App` folder.

## roadmap
 - [x] self update
 - [ ] auto install mod scripts
 - [x] vivaldi install and update
 - [x] vivaldi++ install and update
## refered projects
+ [ChromeUpdater](https://github.com/TkYu/ChromeUpdater)
+ [VivaldiCustomLauncher](https://github.com/Aldaviva/VivaldiCustomLauncher/)

## mod scripts
+ https://github.com/luetage/vivaldi_modding
+ https://github.com/benzBrake/VivaldiMods

## 修复说明
+ 添加了Vivaldi++下载的回退机制，当镜像地址下载失败时会自动回退到GitHub原始地址
