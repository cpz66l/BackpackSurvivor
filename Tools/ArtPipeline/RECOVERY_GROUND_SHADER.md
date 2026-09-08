# Recovery Ground material interface

Shader: `BackpackSurvivor/Environment/Recovery Ground`.
Source: `BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Shaders/RecoveryGround.shader`.

This opaque URP 17 shader is intended for horizontal industrial asphalt, concrete and repair slabs. It uses world XZ coordinates in metres, so rotating or scaling a slab does not stretch its texture. It has one PBR forward lighting pass and auxiliary ShadowCaster, DepthOnly and matching DepthNormalsOnly passes. Depth/shadow passes do not imply four colour passes each frame: URP requests auxiliary passes only when needed by the renderer.

| Property | Meaning | Suggested starting value |
|---|---|---|
| `_BaseColor` | Darker surface tone, authored as a Unity colour | Asphalt grey/olive |
| `_SecondaryColor` | Lighter aggregate tone; blend weight from detail mask R | Close to base tone |
| `_SurfaceMask` | **Linear** RGBA texture: R aggregate blend, G crack strength, B wet patch mask, A macro grime (0.5 neutral) | Shared 512 x 512, Repeat, mipmaps, non-readable |
| `_NormalDetail` | **Linear default texture**, RG encodes normal X/Z components from [-1, 1] to [0, 1]. A neutral texel is (0.5, 0.5). **Do not import as Unity NormalMap**: custom code decodes ordinary RG without platform channel swizzling. | Shared 512 x 512, Repeat, mipmaps, non-readable |
| `_MetersPerTile` | World metres covered by one detailed texture tile | 4 |
| `_MacroScale` | Scale applied to a rotated second sample of the surface mask, for broad grime and damp patches | 0.13, giving about 31 m macro repetition at a 4 m detail tile |
| `_BumpScale` | Strength of grain normal perturbation | 0.20 asphalt, 0.10 concrete |
| `_Smoothness` | Dry PBR smoothness | 0.22 asphalt, 0.30 concrete |
| `_Wetness` | Strength of wet/dark/glossy zones | 0.45 asphalt, 0.20 concrete |
| `_CrackStrength` | Dark crack and small crevice occlusion amount | 0.45 asphalt, 0.20 concrete |

The surface texture should have smooth B-channel islands spanning both below 0.45 and above 0.85; the shader uses `smoothstep(0.45, 0.85, macro.b)` and multiplies by `_Wetness`. B is sampled again at detailed scale to add slight local variation. The normal texture should contain small aggregate relief and can also include crack relief derived from the same surface mask G. Seamless periodic source textures prevent tile borders; world projection intentionally keeps the detail continuous across adjacent slabs.

The shader performs **three material texture samples per colour fragment**: the detail surface mask, its rotated low-frequency sample, and one RG normal sample. Depth normals, when requested, repeat these three samples for matching wet flattening. Depth-only and shadow-caster passes sample no material textures. Standard URP light/shadow/reflection sampling is additional and depends on the active pipeline and lights. There are no runtime noise loops, transparency, parallax, tessellation, scene-colour/depth sampling, custom reflection render textures or per-material shader keywords. Material properties share one `UnityPerMaterial` buffer for SRP batching.

Lighting uses URP `UniversalFragmentPBR`, real main-light/cascade shadows, additional lights/shadows, spherical-harmonic ambient light, and the pipeline's default environment reflection. The normal grain changes the light response, while wet patches darken albedo, flatten normals and approach smoothness 0.82. These remain non-metallic surfaces. Fog uses the installed URP Fog include. Forward+ uses the installed URP 17 `_CLUSTER_LIGHT_LOOP` variant. No screen-space shadow/AO feature, baked lightmap, APV, decals, light layers or cookie variants are requested by this focused ground shader.

For visible point/spot specular highlights, use URP **Per Pixel** additional lights or Forward+. Vertex additional lights are supported, but large slabs then receive only interpolated vertex diffuse lighting and can miss small local lights. A limited number of unshadowed accent lights is enough; do not enable shadows on every decorative lamp.

Implementation was checked against the installed `com.unity.render-pipelines.universal@37e06a5b08b3` source: `LitForwardPass.hlsl`, `Lighting.hlsl`, `GlobalIllumination.hlsl`, `Input.hlsl`, `ShadowCasterPass.hlsl`, `DepthOnlyPass.hlsl`, `DepthNormalsPass.hlsl` and `DepthNormalOnlyPass.cs`. Unity import/compilation and scene/light comparisons are performed by the scene integration task.
