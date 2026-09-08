"""Full-map graphite/steel-blue palette, matching the player's dark body.
Generated separately so the approved olive sample and original GLBs stay editable.
"""
from pathlib import Path
from PIL import Image
import json
root=Path(__file__).resolve().parents[2]
colors=['8C959B','4D5D6A','293640','BA8D46','64C5CF','647582','202831','B6B9B6']
pixels=[]
for hx in colors:
    rgb=tuple(int(hx[i:i+2],16) for i in (0,2,4))+(255,)
    pixels.extend([rgb,rgb])
out=root/'BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Textures/FullMap/ENV_Palette_BaseColor.png'
im=Image.new('RGBA',(16,2)); im.putdata(pixels*2); im.save(out)
(root/'Docs/ArtDirection/2026-09-08/FullMap/palette.json').write_text(json.dumps({'colors':colors,'size':[16,2],'bytes':out.stat().st_size,'purpose':'Cool graphite and steel blue, with restrained amber and cyan accents; existing palette order unchanged.'},indent=2),encoding='utf-8')
print(str(out))
