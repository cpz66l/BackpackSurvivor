"""Compose labelled evidence from actual Blender asset renders (Pillow)."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import json

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/ArtDirection/2026-09-08/Implementation'
manifest=json.loads((ROOT/'Tools/ArtPipeline/Source/modular-kit-manifest.json').read_text(encoding='utf-8'))
FONT=Path('C:/Windows/Fonts/msyh.ttc')
def font(size):
    return ImageFont.truetype(str(FONT),size)
canvas=Image.new('RGB',(1800,1240),'#121d24')
d=ImageDraw.Draw(canvas)
d.text((38,25),'封锁区回收站 / 第一批场景模块',font=font(37),fill='#E5ECEE')
d.text((40,82),'Blender 5.2.1 实际渲染 · 共享单材质 / 16×2 色板 · 米制 / 底部轴心 / Unity +Z 正面',font=font(20),fill='#AABBC1')
names=['ConcreteBarrier','FenceSegment','SupplyTerminal','CargoCrate','SmallGenerator']
labels=['01  混凝土路障','02  菱形钢网围栏','03  补给终端','04  加固物资箱','05  小型发电机']
for i,(name,label) in enumerate(zip(names,labels)):
    x=24+(i%3)*592
    y=130+(i//3)*516
    im=Image.open(OUT/(name+'.png')).convert('RGB').resize((568,442),Image.Resampling.LANCZOS)
    canvas.paste(im,(x,y))
    item=next(a for a in manifest['assets'] if a['name']==name)
    d.text((x+4,y+451),label,font=font(22),fill='#E4EBED')
    d.text((x+382,y+454),f"{item['triangles']:,} tris",font=font(19),fill='#AABBC1')
    dims=item['unityDimensions']
    d.text((x+4,y+483),f"{dims[0]:.2f} × {dims[2]:.2f} × {dims[1]:.2f} m  (宽 × 深 × 高)",font=font(16),fill='#8CA5AF')
x,y=1208,663
d.text((x,y),'材质与复用规范',font=font(27),fill='#E5ECEE')
colors=[('#96958A','混凝土'),('#687265','灰绿漆'),('#303936','炭黑'),('#B6843B','褪色橙'),('#5EC5CC','青指示'),('#566462','深钢'),('#232A29','橡胶'),('#B8B8A6','浅标记')]
for i,(color,label) in enumerate(colors):
    px=x+(i%4)*130
    py=y+64+(i//4)*109
    d.rounded_rectangle((px,py,px+101,py+49),radius=4,fill=color)
    d.text((px,py+58),label,font=font(17),fill='#B1C0C6')
for j,line in enumerate(['轮廓以实用结构为依据，减少杂乱锈迹。','围栏采用实体细杆，网面无需透明贴图。','屏幕、警戒条与铭牌保持小面积点缀。','各格独立取景；尺寸以标注为准。']):
    d.text((x,y+297+j*35),line,font=font(18),fill='#AABBC1')
d.line((26,1196,1774,1196),fill='#30414A',width=1)
d.text((37,1207),'交付：5 个 GLB + 可编辑 .blend + 可复现脚本与尺寸/面数清单。此图为资产验收，不是 Unity 实机画面。',font=font(16),fill='#91AAB5')
canvas.save(OUT/'modular-kit.png')
print(OUT/'modular-kit.png')
