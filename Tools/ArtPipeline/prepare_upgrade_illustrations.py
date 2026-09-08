"""Make an actual RGBA atlas from the approved RGB illustration contact sheet.

Local mask cleanup, background removal and PNG compression were explicitly
authorized by the user. Requires Pillow, NumPy and SciPy; no online service.
Run: python Tools/ArtPipeline/prepare_upgrade_illustrations.py
"""
import hashlib
import json
import shutil
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage as ndi

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Tools/ArtPipeline/Source'
DOCS = ROOT / 'Docs/ArtDirection/2026-09-08/UpgradeUI'
OUTPUT = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/UI/Upgrades'
SOURCE_IMAGE = SOURCE / 'UpgradeIllustrations-source.png'
ORIGINAL = Path('C:/Users/cp/.codex/generated_images/01a080c1-7fa3-7a03-b5f9-84672bb43b0c/exec-fd09b702-5b31-4877-9b66-18121658572b.png')
NAMES = ['DamageUp', 'FireRateUp', 'MoveSpeedUp', 'MaxHpUp', 'PickupRangeUp',
         'CritChanceUp', 'ProjectileSpeedUp', 'WeaponRangeUp', 'CritDamageUp',
         'DamageReductionUp', 'XpGainUp', 'GoldGainUp', 'ActiveWeaponLimitUp']
LABELS = ['伤害强化', '射速强化', '移动速度', '最大生命', '拾取范围', '暴击概率',
          '弹道速度', '武器射程', '暴击伤害', '伤害减免', '经验获取', '金币获取', '武器槽位']
for folder in (SOURCE, DOCS, OUTPUT):
    folder.mkdir(parents=True, exist_ok=True)
if not SOURCE_IMAGE.exists():
    shutil.copy2(ORIGINAL, SOURCE_IMAGE)
source = np.asarray(Image.open(SOURCE_IMAGE).convert('RGB'))

def clear_background(rgb):
    """Separate actual checker regions from bright interior metal/glow pixels.

    Background is mostly neutral 213/253 grey. Connected exterior neutral
    pixels are definite background; enclosed patches are only background if
    they include both checker tones. Small bright specular patches survive.
    The final 2px inner silhouette band is unmatted against the nearest grey
    sample using the nearest solid edge colour, avoiding a pale baked halo.
    """
    f = rgb.astype(np.float32)
    low, high = f.min(axis=2), f.max(axis=2)
    candidate = (low >= 186) & ((high-low) <= 10)
    labels, count = ndi.label(candidate)
    remove = np.zeros(candidate.shape, bool)
    exterior = set(np.unique(np.concatenate((labels[0], labels[-1], labels[:,0], labels[:,-1])))) - {0}
    removed_holes = 0
    for i, section in enumerate(ndi.find_objects(labels), 1):
        if section is None:
            continue
        local = labels[section] == i
        vals = low[section][local]
        checker_hole = len(vals) >= 25 and np.percentile(vals, 10) < 231 and np.percentile(vals, 90) > 241
        if i in exterior or checker_hole:
            remove[section] |= local
            if i not in exterior:
                removed_holes += 1
    # All exterior is a single large region in each tile. Isolated raster dust
    # below 3 pixels is not part of an illustration and cannot produce a speck.
    foreground = ~remove
    components, component_count = ndi.label(foreground)
    for i, section in enumerate(ndi.find_objects(components), 1):
        if section is not None:
            region = components[section] == i
            if np.count_nonzero(region) <= 3:
                foreground[section] &= ~region
    inside = ndi.distance_transform_edt(foreground)
    core = inside > 2.2
    _, core_index = ndi.distance_transform_edt(~core, return_indices=True)
    foreground_color = f[core_index[0], core_index[1]]
    _, bg_index = ndi.distance_transform_edt(foreground, return_indices=True)
    background_color = f[bg_index[0], bg_index[1]]
    # The checker is neutral even along its grey/white cell transitions.
    background_color = np.mean(background_color, axis=2, keepdims=True)
    delta = foreground_color - background_color
    denominator = np.sum(delta*delta, axis=2)
    estimated_alpha = np.sum((f-background_color)*delta, axis=2) / np.maximum(denominator, 1)
    estimated_alpha = np.clip(estimated_alpha, 0, 1)
    alpha = foreground.astype(np.float32)
    band = foreground & (inside <= 2.2)
    # Do not dim a white/cyan light core where its edge is brighter than the
    # nearest core pixel. Those colours are authored foreground, not white matte.
    reliable = band & (denominator > 1800)
    alpha[reliable] = estimated_alpha[reliable]
    # Edge decontamination preserves the measured original colour where opaque.
    cleaned = f.copy()
    partial = reliable & (alpha < .99)
    a = np.maximum(alpha[...,None], .04)
    unmatted = (f-background_color*(1-a))/a
    cleaned[partial] = np.clip(unmatted[partial],0,255)
    alpha[alpha < .045] = 0
    cleaned[alpha == 0] = 0
    rgba = np.dstack((cleaned.clip(0,255).astype(np.uint8), np.rint(alpha*255).astype(np.uint8)))
    # Pad source crops before resampling; premultiplication avoids dark fringes.
    return Image.fromarray(rgba, 'RGBA'), {'checkerHoleComponentsRemoved': removed_holes,
                                         'sourceForegroundPixels': int(np.count_nonzero(alpha))}

def alpha_resize(image, size):
    # Pillow RGBa is premultiplied RGBA. Returning ordinary RGBA is important
    # because Unity sprites expect straight alpha in their source PNG.
    return image.convert('RGBa').resize(size, Image.Resampling.LANCZOS).convert('RGBA')

def clean_projectile_glows(rgba, rgb):
    """Unmatte the two translucent cyan streaks which cover the checker itself.

    In these bounded effect-only regions, cyan chroma survives an unknown grey
    checker level. Recover opacity from that chroma, preserve enclosed white
    light cores, and remove the neutral checker teeth instead of eroding light.
    The metal projectile is excluded from both hand-verified regions.
    """
    arr=np.asarray(rgba).copy()
    raw=rgb.astype(np.float32)
    blue=raw[:,:,2]-raw[:,:,0]
    green=raw[:,:,2]-raw[:,:,1]
    # The black outline forms one connected metal projectile. Isolate it first,
    # then recover the two glows independently; faint tinted checker patches
    # outside the light masks must not become opaque foreground fragments.
    body_seed=raw.min(axis=2)<170
    body_labels,body_count=ndi.label(body_seed)
    body_sizes=np.bincount(body_labels.ravel()); body_sizes[0]=0
    body_mask=ndi.binary_fill_holes(body_labels==int(body_sizes.argmax()))
    arr[~body_mask]=0
    polygons=[[(35,176),(137,108),(152,116),(142,135),(64,181)],
              [(111,261),(204,190),(226,192),(214,214),(140,264)]]
    total=0
    for polygon in polygons:
        canvas=Image.new('L',rgba.size,0)
        ImageDraw.Draw(canvas).polygon(polygon,fill=255)
        region=np.asarray(canvas)>0
        region &= ~body_mask
        cyan=(blue>19)&region
        filled=ndi.binary_fill_holes(cyan)
        white_core=filled & ~cyan & (raw.min(axis=2)>215)
        # A moderate cyan core creates authored glow when composited on blue ink.
        alpha=ndi.gaussian_filter(np.clip((blue-5)/125,0,1)*region,.6)*region
        alpha[white_core]=1
        alpha[alpha<.045]=0
        safe=np.maximum(alpha,.04)
        fg=np.empty_like(raw)
        fg[:,:,0]=125
        fg[:,:,1]=np.clip(255-green/safe,150,255)
        fg[:,:,2]=255
        fg[white_core]=raw[white_core]
        fg[alpha==0]=0
        arr[region,:3]=fg[region].astype(np.uint8)
        arr[region,3]=np.rint(alpha[region]*255).astype(np.uint8)
        total+=int(np.count_nonzero(white_core))
    # The barely visible tint below the rear rim is a baked background halo.
    arr[286:,:,:]=0
    return Image.fromarray(arr,'RGBA'),total

atlas = Image.new('RGBA',(1024,1024),(0,0,0,0))
sprites = []
stats = []
cuts_x = [round(i*source.shape[1]/4) for i in range(5)]
cuts_y = [round(i*source.shape[0]/4) for i in range(5)]
for index,name in enumerate(NAMES):
    row,col = divmod(index,4)
    tile = source[cuts_y[row]:cuts_y[row+1],cuts_x[col]:cuts_x[col+1]]
    rgba,info = clear_background(tile)
    if name=='ProjectileSpeedUp':
        rgba,info['protectedWhiteGlowCorePixels']=clean_projectile_glows(rgba,tile)
    bbox = rgba.getbbox()
    assert bbox, name+' has no foreground'
    crop = rgba.crop(bbox)
    scale = 224/max(crop.size)
    dims = tuple(max(1,round(n*scale)) for n in crop.size)
    sprite = alpha_resize(crop,dims)
    px,py = col*256+(256-dims[0])//2,row*256+(256-dims[1])//2
    atlas.alpha_composite(sprite,(px,py))
    # Rect tightly wraps the visible sprite plus 4px actual transparent padding.
    sx0,sy0,sx1,sy1 = sprite.getbbox()
    x0,y0,x1,y1 = px+sx0-4,py+sy0-4,px+sx1+4,py+sy1+4
    assert x0>=col*256+12 and y0>=row*256+12
    assert x1<=(col+1)*256-12 and y1<=(row+1)*256-12
    rect=[x0,1024-y1,x1-x0,y1-y0]
    sprites.append({'name':name,'rect':rect})
    info.update({'name':name,'sourceCell':[cuts_x[col],cuts_y[row],cuts_x[col+1]-cuts_x[col],cuts_y[row+1]-cuts_y[row]],
                 'sourceForegroundBox':list(bbox),'atlasCell':[col*256,row*256,256,256],
                 'visibleDimensions':list(dims),'unityRect':rect})
    stats.append(info)

output=OUTPUT/'UpgradeIllustrations.png'
# One-bit RGB precision reduction is visually below the source compression noise
# and makes the true-colour RGBA texture smaller without palette/indexed alpha.
quantized=np.asarray(atlas).copy()
quantized[:,:,:3]=((quantized[:,:,:3].astype(np.uint16)+1)//2*2).clip(0,255).astype(np.uint8)
atlas=Image.fromarray(quantized,'RGBA')
atlas.save(output,optimize=True,compress_level=9)
array=np.asarray(atlas)
assert not np.any(array[768:1024,256:1024,3]), 'Unused three cells must remain fully transparent'
for i in range(16):
    row,col=divmod(i,4)
    a=array[row*256:(row+1)*256,col*256:(col+1)*256,3]
    assert not np.any(a[:12]) and not np.any(a[-12:]) and not np.any(a[:,:12]) and not np.any(a[:,-12:])
assert output.stat().st_size<900*1024, 'Runtime atlas exceeded 900 KiB'
manifest={'width':1024,'height':1024,'sprites':sprites,'runtimeBytes':output.stat().st_size,
          'format':'RGBA8 / straight alpha','spriteCount':13,'grid':[4,4],'maxVisibleSide':224,
          'minimumTransparentCellBorder':12,'unusedCellsAreTransparent':True,
          'sourceSha256':hashlib.sha256(SOURCE_IMAGE.read_bytes()).hexdigest(),
          'atlasSha256':hashlib.sha256(output.read_bytes()).hexdigest(),
          'transparentPixels':int(np.count_nonzero(array[:,:,3]==0)),
          'partiallyTransparentPixels':int(np.count_nonzero((array[:,:,3]>0)&(array[:,:,3]<255))),
          'spriteDiagnostics':stats}
(SOURCE/'upgrade-illustrations-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(DOCS/'illustrations-manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')

# Verification uses the same dark ink family as the tactical upgrade dialog.
board=Image.new('RGBA',(1100,1172),(11,20,31,255))
board.alpha_composite(atlas,(38,88))
draw=ImageDraw.Draw(board)
font_path='C:/Windows/Fonts/msyh.ttc'
title=ImageFont.truetype(font_path,27)
font=ImageFont.truetype(font_path,16)
draw.text((39,22),'夜战升级插画 / 13 项真实透明 PNG',font=title,fill=(222,236,243,255))
draw.text((40,59),'最终 1024×1024 图集 · 深蓝背景验收 · 每格 256px / 图案最长边 224px',font=font,fill=(134,162,180,255))
for i,(name,label) in enumerate(zip(NAMES,LABELS)):
    row,col=divmod(i,4)
    draw.text((46+col*256,326+row*256),f'{i+1:02d}  {label}',font=font,fill=(170,195,209,255))
draw.text((42,1132),f'RGBA / {output.stat().st_size/1024:.1f} KiB · 格子外框及后三格 alpha 为 0 · 未改写稀有度/升级规则',font=font,fill=(115,146,165,255))
board.convert('RGB').save(DOCS/'illustrations-on-dark.png',optimize=True)
# A brighter neutral contact sheet makes accidental dark halos visible too.
light=Image.new('RGBA',(1024,1024),(176,190,201,255))
light.alpha_composite(atlas)
light.convert('RGB').save(DOCS/'illustrations-on-grey.png',optimize=True)
print(json.dumps({'output':str(output),'bytes':output.stat().st_size,'spriteCount':len(sprites),
                  'manifest':str(SOURCE/'upgrade-illustrations-manifest.json'),'alphaChecks':'passed'},indent=2))
