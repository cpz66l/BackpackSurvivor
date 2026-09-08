# VNext modular environment kit

Five hand-built hard-surface modules for the 24 × 24 m Quarantine / Recovery Yard sample. Source generated in Blender 5.2.1. The reference and implementation renders are distinct from Unity scene captures.

## Rebuild

From the project root in PowerShell:

```powershell
& 'E:/Blender/blender.exe' --background --python 'Tools/ArtPipeline/build_modular_kit.py'
python 'Tools/ArtPipeline/make_modular_contact_sheet.py'
```

Rebuilding rewrites only these five GLBs, their `.blend`, manifest, palette PNGs, individual PNG previews and contact sheet. Existing Unity `.meta` files are retained. One-off verification scripts and logs used during v0.4 art intake were removed from the cleaned project; retained evidence lives under `Docs/ArtDirection/`. Do not run concurrently with editing these assets in Blender.

## Coordinates and geometry

- Units: one unit = one metre; local object origin on the floor, Z = 0 in Blender.
- Blender Z up / −Y front becomes glTF +Y up / +Z front. Exported transforms are applied, root scale is `(1, 1, 1)`.
- Unity dimensions in the manifest are ordered width (X), height (Y), depth (Z). Small protrusions such as latches are included in the measured bounds.
- Each GLB is **one mesh / one primitive / one material named `ENV_AtlasSurface`**. Geometry is triangulated and uses weighted normals on broad bevels. Three tiny 16 × 2 PNGs are embedded per GLB; no opacity, bones, animation or collision data is embedded.
- The fence uses opaque square rods with a complete diamond grid; it does not depend on a double-sided transparent plane.
- Blender source displays a spaced lineup for editing. Each object still has its own bottom pivot; the GLBs are exported before the display lineup offsets are applied.
- The barrier warning strips are single-sided geometry placed above the sloping concrete surface; they have correct outward normals on both sides.

| Asset | Triangles | Unity measured dimensions (W × D × H, m) |
| --- | ---: | --- |
| ConcreteBarrier | 456 | 3.006 × 0.600 × 0.951 |
| FenceSegment | 1,024 | 4.000 × 0.300 × 2.200 |
| SupplyTerminal | 1,348 | 0.822 × 0.678 × 1.800 |
| CargoCrate | 904 | 1.200 × 1.261 × 1.000 |
| SmallGenerator | 1,716 | 2.180 × 1.250 × 1.327 |

Total: **5,448 triangles** across unique source modules. This is not the scene total or a performance benchmark.

## Shared palette

The Blender source retains the original named Principled BSDF materials for editing. Only a temporary export copy receives the single atlas material and new `PaletteUV`; source material assignments and original geometry are preserved. Colour values below are sRGB. Each palette colour occupies a 2 × 2 pixel block in the 16 × 2 strip, ordered left to right as in the table below. Every loop belonging to that source material samples the centre of its block at `((index + 0.5) / 8, 0.5)`.

| Editable source material / palette block | sRGB | Metallic | Roughness |
| --- | --- | ---: | ---: |
| ENV_Concrete | `#96958A` | 0.00 | 0.91 |
| ENV_OlivePaint | `#687265` | 0.38 | 0.65 |
| ENV_Charcoal | `#303936` | 0.35 | 0.66 |
| ENV_FadedAmber | `#B6843B` | 0.15 | 0.77 |
| ENV_CyanIndicator | `#5EC5CC` | 0.05 | 0.31 |
| ENV_DarkSteel | `#566462` | 0.80 | 0.44 |
| ENV_Rubber | `#232A29` | 0.00 | 0.95 |
| ENV_Stencil | `#B8B8A6` | 0.05 | 0.84 |

All five GLBs use `ENV_AtlasSurface`; Unity should map these to one shared material asset. The atlas shader uses emission strength 1.1, with black pixels for every colour block except cyan, so only the cyan indicator surfaces emit. Unity smoothness corresponds to `1 − roughness`.

Shared external textures are in `BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Textures/`:

| File | Colour space | Channels / use |
| --- | --- | --- |
| ENV_Palette_BaseColor.png | sRGB | RGB = base colour; A = 1 |
| ENV_Palette_Emission.png | sRGB | RGB = cyan only at palette block 4 (zero based); all other blocks black |
| ENV_Palette_MetallicSmoothness.png | Linear / Non-Color | R = metallic; A = smoothness; direct URP/Lit packing |

The MetallicSmoothness texture is provided for Unity remapping and is not embedded in the GLBs. Use **Point/Nearest filtering, Clamp wrapping, disabled mipmaps, no lossy compression** for every palette texture. The GLB sampler is explicitly stored as `magFilter=9728`, `minFilter=9728` and `CLAMP_TO_EDGE`; this prevents downsampling from averaging adjacent colour blocks. The public palette PNGs are tiny on disk; each embedded triplet is 288 bytes after glTF PNG encoding. Each full triplet is 96 pixels, or 384 bytes if decoded as uncompressed RGBA8 before graphics alignment overhead.

The 5 GLBs total **408,172 bytes** and retain all **5,448 triangles**. A mesh with one primitive needs one material submission per rendered instance and pass; this does not guarantee a specific whole-scene draw-call count or frame rate.

## Visual checks completed

Actual Cycles renders were inspected for all five assets. The grid is present, terminal screen inset and controls remain distinct, bevels and feet read cleanly, and the crate lid reinforcing ribs no longer coincide with the top face. Preview framing was corrected to include all five complete silhouettes. The contact sheet is `Docs/ArtDirection/2026-09-08/Implementation/modular-kit.png`.

The original v0.4 intake validation checked one material/primitive per mesh, unchanged triangle counts, palette UVs, nearest sampling, emission, and pixel parity for embedded palette images. The recorded result remains `modular-atlas-validation.json`; one-off verification scripts and local logs were removed during repository cleanup after the evidence was captured.

The finish intentionally uses quiet, broad colour surfaces for the current top-down combat camera. It does not add baked grunge or promise photorealism. Final light response, material remapping, collider sizes and game-camera readability remain the responsibility of Unity sample integration.
