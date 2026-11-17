# 图标来源

- `pin.svg` / `pin-filled.svg` 与对应 PNG：沿用体验优化 MOD 的同款资源。
- `toggle-on.svg` / `toggle-off.svg`：xicons 的 `@sicons/material@0.13.0`，
  `ToggleOnRound` / `ToggleOffRound`，源自 Google Material Design Icons。
  上游 Apache-2.0 许可证保留在 [LICENSE-material.txt](LICENSE-material.txt)。
  来源：https://cdn.jsdelivr.net/npm/@sicons/material@0.13.0/ToggleOnRound.svg
  与 https://cdn.jsdelivr.net/npm/@sicons/material@0.13.0/ToggleOffRound.svg 。

Unity 2017 原生 UI 不直接解析 SVG；保留原始 SVG，并用构建前的 resvg-py 将 `currentColor` 设为白色，
以128×128透明 PNG嵌入 DLL。运行时以64 UI显示，并分别着绿/灰色；圆点位置同时区分状态。
文字仍使用游戏 `OptionValue.On` / `OptionValue.Off` 本地化，不只依靠颜色表达开关。
