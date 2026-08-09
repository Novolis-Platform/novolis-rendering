# Release

This repository publishes with the org CalVer scheme (`2026.1.*`) via `merge.yml` to GitHub Packages when packages are packable.

See [release-policy](https://github.com/Novolis-Platform/novolis-governance/blob/main/docs/release-policy.md).

Published docs: [https://novolis-platform.github.io/.github/novolis-rendering/](https://novolis-platform.github.io/.github/novolis-rendering/)

## Packages

- `Novolis.Rendering`
- `Novolis.Rendering.Abstractions`
- `Novolis.Rendering.Backends.Cpu`
- `Novolis.Rendering.Backends.Igpu`
- `Novolis.Rendering.Backends.TwoD.Silk`
- `Novolis.Rendering.Backends.Vulkan`
- `Novolis.Rendering.Compile`
- `Novolis.Rendering.DependencyInjection`
- `Novolis.Rendering.Materials`
- `Novolis.Rendering.PathTrace.Demos`
- `Novolis.Rendering.Presentation.Abstractions`
- `Novolis.Rendering.Presentation.Raylib`
- `Novolis.Rendering.Presentation.Silk`
- `Novolis.Rendering.Runtime`
- `Novolis.Rendering.Scene`
- `Novolis.Rendering.Testing`
- `Novolis.Rendering.TwoD`

## Consumers

Restore from nuget.org + `https://nuget.pkg.github.com/Novolis-Platform/index.json` only.

Local multi-repo iteration: open `d:\novolis\Novolis.Platform.slnx` (ProjectReference mode) — do not add a local feed.
