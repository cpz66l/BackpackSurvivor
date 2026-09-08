"""Reproducible, seamless packed ground maps; no source photos or baked lighting.

SurfaceMask RGBA = aggregate / cracks / moisture / broad tonal variation.
NormalDetail RG = signed world X/Z detail encoded to 0..1; import as linear Default.
Both maps are 512 squared and shared by all full-map ground materials.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Textures/FullMap'
OUT.mkdir(parents=True, exist_ok=True)
N = 512
rng = np.random.default_rng(709120)

def noise(grid):
    # Periodic bilinear value noise, including the wrap-around cells.
    v = rng.random((grid, grid)).astype(np.float32)
    xy = np.arange(N) * grid / N
    i = xy.astype(int); f = xy - i; f = f*f*(3-2*f)
    a = v[i[:, None] % grid, i[None, :] % grid]
    b = v[i[:, None] % grid, (i[None, :] + 1) % grid]
    c = v[(i[:, None] + 1) % grid, i[None, :] % grid]
    d = v[(i[:, None] + 1) % grid, (i[None, :] + 1) % grid]
    return (a*(1-f[None,:])+b*f[None,:])*(1-f[:,None])+(c*(1-f[None,:])+d*f[None,:])*f[:,None]

aggregate = np.clip(.45 + (noise(100)-.5)*.48 + (noise(210)-.5)*.22 + (noise(19)-.5)*.13, 0, 1)
cracks = Image.new('L', (N,N))
draw = ImageDraw.Draw(cracks)
for j in range(11):
    p = rng.uniform(0,N,2)
    heading = rng.uniform(0,2*np.pi)
    points = [tuple(p)]
    for k in range(int(rng.integers(4,12))):
        heading += rng.uniform(-.55,.55)
        p = p + np.array([np.cos(heading),np.sin(heading)]) * rng.uniform(7,20)
        points.append(tuple(p))
    stroke_value = int(rng.integers(140,235))
    for ox in (-N,0,N):
        for oy in (-N,0,N):
            draw.line([(x+ox,y+oy) for x,y in points], fill=stroke_value, width=2)
            if j % 2 == 0:
                x,y = points[len(points)//2]
                draw.line([(x+ox,y+oy),(x+ox+17,y+oy-12),(x+ox+26,y+oy-33)],fill=135,width=1)
# Blur an actual periodic neighbourhood, not a clamped image edge.
tiled_cracks = Image.new('L', (N*3,N*3))
for tile_y in range(3):
    for tile_x in range(3):
        tiled_cracks.paste(cracks, (tile_x*N,tile_y*N))
blurred_cracks = tiled_cracks.filter(ImageFilter.GaussianBlur(.55)).crop((N,N,N*2,N*2))
crack = np.asarray(blurred_cracks, dtype=np.float32)/255
moisture = np.clip((noise(4)*.7+noise(9)*.3-.39)*2.8,0,1)
macro = np.clip(.5 + (noise(3)-.5)*.42+(noise(11)-.5)*.1,0,1)
mask = np.stack([aggregate,crack,moisture,macro],axis=-1)
height = aggregate*.7-crack*.32
dx = (np.roll(height,-1,axis=1)-np.roll(height,1,axis=1))*2.5
dy = (np.roll(height,-1,axis=0)-np.roll(height,1,axis=0))*2.5
# Image rows increase downwards; ordinary Unity texture V (world +Z) increases upwards.
# Thus dHeight/dWorldZ has the opposite sign to the image-row derivative.
normal = np.stack([-dx,+dy,np.ones_like(dx)],axis=-1)
normal /= np.linalg.norm(normal,axis=-1,keepdims=True)
encoded = np.concatenate([normal[:,:,:2]*.5+.5, np.ones((N,N,2))],axis=-1)
paths = []
for name, pixels in [('Ground_SurfaceMask',mask),('Ground_NormalDetail',encoded)]:
    path = OUT/(name+'.png')
    Image.fromarray(np.uint8(np.clip(pixels,0,1)*255),'RGBA').save(path,optimize=True)
    paths.append({'file':str(path.relative_to(ROOT)).replace('\\','/'),'bytes':path.stat().st_size,'width':N,'height':N})
report = ROOT/'Docs/ArtDirection/2026-09-08/FullMap'
report.mkdir(parents=True,exist_ok=True)
(report/'ground-textures.json').write_text(json.dumps({'seed':709120,'maps':paths,'sourceBytes':sum(p['bytes'] for p in paths)},indent=2),encoding='utf-8')

# Inspect the delivered PNG data, including wrap neighbours and normal direction
# after converting image rows to Unity's bottom-to-top texture coordinates.
delivered_mask = np.asarray(Image.open(OUT/'Ground_SurfaceMask.png'),dtype=np.float64)/255
delivered_normal = np.asarray(Image.open(OUT/'Ground_NormalDetail.png'),dtype=np.float64)/255
channel_statistics = {}
for index,name in enumerate(('aggregate','cracks','moisture','macro')):
    channel = delivered_mask[:,:,index]
    seam_x = np.abs(channel[:,0]-channel[:,-1])
    seam_y = np.abs(channel[0]-channel[-1])
    interior_x = np.abs(np.diff(channel,axis=1))
    interior_y = np.abs(np.diff(channel,axis=0))
    channel_statistics[name] = {
        'range':[float(channel.min()),float(channel.max())],
        'mean':float(channel.mean()),
        'edgeDifferenceX':{'mean':float(seam_x.mean()),'max':float(seam_x.max())},
        'edgeDifferenceY':{'mean':float(seam_y.mean()),'max':float(seam_y.max())},
        'interiorNeighborDifferenceX':{'mean':float(interior_x.mean()),'max':float(interior_x.max())},
        'interiorNeighborDifferenceY':{'mean':float(interior_y.mean()),'max':float(interior_y.max())},
    }
height_uv = np.flipud(delivered_mask[:,:,0]*.7-delivered_mask[:,:,1]*.32)
normal_uv = np.flipud(delivered_normal[:,:,:2])*2-1
gradient_x = (np.roll(height_uv,-1,axis=1)-np.roll(height_uv,1,axis=1))*2.5
gradient_z = (np.roll(height_uv,-1,axis=0)-np.roll(height_uv,1,axis=0))*2.5
expected_normal = np.stack([-gradient_x,-gradient_z,np.ones_like(gradient_x)],axis=-1)
expected_normal /= np.linalg.norm(expected_normal,axis=-1,keepdims=True)
normal_correlation = [float(np.corrcoef(normal_uv[:,:,i].ravel(),expected_normal[:,:,i].ravel())[0,1]) for i in range(2)]
normal_error = np.abs(normal_uv-expected_normal[:,:,:2])
normal_direction_valid = bool(min(normal_correlation) > .99 and normal_error.mean() < .01)
validation = {
    'seed':709120,
    'dimensions':[N,N],
    'sourceBytes':sum(p['bytes'] for p in paths),
    'corrections':['One stroke intensity shared by all periodic copies','Gaussian blur across a 3x3 periodic neighbourhood','Normal G stores +image-row derivative for world +Z'],
    'edgeDifferenceNote':'First and last texels are adjacent samples, not duplicates; nonzero differences are expected. Compare with interior neighbouring samples, especially for narrow crossing cracks.',
    'channels':channel_statistics,
    'normalDirection':{
        'coordinateSystem':'Unity UV bottom-to-top; R world X and G world Z',
        'expectedEncodedImageComponents':['-dx','+dy'],
        'correlationWithDeliveredHeightX':normal_correlation[0],
        'correlationWithDeliveredHeightZ':normal_correlation[1],
        'meanAbsoluteComponentError':float(normal_error.mean()),
        'maxAbsoluteComponentError':float(normal_error.max()),
        'passed':normal_direction_valid,
    },
}
(report/'ground-texture-validation.json').write_text(json.dumps(validation,indent=2),encoding='utf-8')
if not normal_direction_valid:
    raise RuntimeError('Delivered normal texture does not match Unity world X/Z height derivatives.')
print(json.dumps(paths,indent=2))
