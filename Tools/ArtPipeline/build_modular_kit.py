"""Deterministic VNext environment kit. Run with Blender 5.2 background.

Blender world: metres, Z up, -Y front. glTF export: +Y up, +Z front.
All exported meshes have applied transforms and a floor-centred origin.
Editable sources retain eight Principled materials. Exports use one material and
three embedded 16 x 2 palette images with point sampling and no mipmapping.
"""
import bpy
import math
import json
import struct
import zlib
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODELS = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Models'
SOURCE = ROOT / 'Tools/ArtPipeline/Source'
PREVIEW = ROOT / 'Docs/ArtDirection/2026-09-08/Implementation'
TEXTURES = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Textures'
for folder in (MODELS, SOURCE, PREVIEW, TEXTURES):
    folder.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)
scene = bpy.context.scene
bpy.context.preferences.filepaths.save_version = 0
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

def srgb(c):
    return c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4

def material(name, hx, metal=0.0, rough=.7, emission=0):
    rgb = tuple(srgb(int(hx[i:i+2],16)/255) for i in (0,2,4))
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (*rgb, 1)
    mat.use_nodes = True
    bsdf = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None:
        bsdf = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Metallic'].default_value = metal
    bsdf.inputs['Roughness'].default_value = rough
    if emission:
        bsdf.inputs['Emission Color'].default_value = (*rgb,1)
        bsdf.inputs['Emission Strength'].default_value = emission
    return mat

M = {
 'concrete':material('ENV_Concrete', '96958A', rough=.91),
 'olive':material('ENV_OlivePaint', '687265', metal=.38, rough=.65),
 'charcoal':material('ENV_Charcoal', '303936', metal=.35, rough=.66),
 'orange':material('ENV_FadedAmber', 'B6843B', metal=.15, rough=.77),
 'cyan':material('ENV_CyanIndicator', '5EC5CC', metal=.05, rough=.31, emission=1.1),
 'metal':material('ENV_DarkSteel', '566462', metal=.8, rough=.44),
 'rubber':material('ENV_Rubber', '232A29', rough=.95),
 'label':material('ENV_Stencil', 'B8B8A6', metal=.05, rough=.84),
}
PALETTE = [
 ('ENV_Concrete','96958A',0.00,.91),
 ('ENV_OlivePaint','687265',.38,.65),
 ('ENV_Charcoal','303936',.35,.66),
 ('ENV_FadedAmber','B6843B',.15,.77),
 ('ENV_CyanIndicator','5EC5CC',.05,.31),
 ('ENV_DarkSteel','566462',.80,.44),
 ('ENV_Rubber','232A29',0.00,.95),
 ('ENV_Stencil','B8B8A6',.05,.84),
]
PALETTE_INDEX = {row[0]: i for i,row in enumerate(PALETTE)}

def palette_png(path, pixels):
    """Write exact 8-bit RGBA pixels without colour-management side effects."""
    def chunk(kind,data):
        return struct.pack('>I',len(data))+kind+data+struct.pack('>I',zlib.crc32(kind+data)&0xffffffff)
    row=bytes(sum((list(p)*2 for p in pixels),[]))
    raw=(b'\0'+row)*2
    path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('>IIBBBBB',16,2,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw,9))+chunk(b'IEND',b''))

base_pixels=[]
mr_pixels=[]
emission_pixels=[]
unity_ms_pixels=[]
for name,hx,metal,rough in PALETTE:
    rgb=tuple(int(hx[i:i+2],16) for i in (0,2,4))
    base_pixels.append((*rgb,255))
    mr_pixels.append((255,round(rough*255),round(metal*255),255))
    emission_pixels.append((*rgb,255) if name=='ENV_CyanIndicator' else (0,0,0,255))
    unity_ms_pixels.append((round(metal*255),0,0,round((1-rough)*255)))
for suffix,pixels in [('BaseColor',base_pixels),('MetallicRoughness',mr_pixels),('Emission',emission_pixels),('MetallicSmoothness',unity_ms_pixels)]:
    palette_png(TEXTURES/('ENV_Palette_'+suffix+'.png'),pixels)

ATLAS = material('ENV_AtlasSurface','FFFFFF',metal=1,rough=1,emission=1.1)
nodes=ATLAS.node_tree.nodes
links=ATLAS.node_tree.links
bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
def texture_node(suffix,colour_space):
    image=bpy.data.images.load(str(TEXTURES/('ENV_Palette_'+suffix+'.png')),check_existing=False)
    image.colorspace_settings.name=colour_space
    image.pack()
    node=nodes.new('ShaderNodeTexImage')
    node.name='Palette '+suffix
    node.image=image
    node.interpolation='Closest'
    node.extension='EXTEND'
    return node
base_tex=texture_node('BaseColor','sRGB')
mr_tex=texture_node('MetallicRoughness','Non-Color')
emission_tex=texture_node('Emission','sRGB')
links.new(base_tex.outputs['Color'],bsdf.inputs['Base Color'])
links.new(emission_tex.outputs['Color'],bsdf.inputs['Emission Color'])
separate=nodes.new('ShaderNodeSeparateColor')
separate.mode='RGB'
links.new(mr_tex.outputs['Color'],separate.inputs['Color'])
links.new(separate.outputs['Green'],bsdf.inputs['Roughness'])
links.new(separate.outputs['Blue'],bsdf.inputs['Metallic'])

def no_mipmap_glb(path):
    """glTF palette samplers must not average adjacent material swatches."""
    content=path.read_bytes()
    json_length=struct.unpack_from('<I',content,12)[0]
    document=json.loads(content[20:20+json_length])
    for sampler in document.get('samplers',[]):
        sampler.update({'magFilter':9728,'minFilter':9728,'wrapS':33071,'wrapT':33071})
    assert len(document['meshes'])==1 and len(document['meshes'][0]['primitives'])==1
    assert len(document['materials'])==1 and document['materials'][0]['name']=='ENV_AtlasSurface'
    encoded=json.dumps(document,separators=(',',':')).encode('utf-8')
    encoded+=b' '*((-len(encoded))%4)
    remainder=content[20+json_length:]
    path.write_bytes(struct.pack('<4sII',b'glTF',2,20+len(encoded)+len(remainder))+struct.pack('<I4s',len(encoded),b'JSON')+encoded+remainder)

def export_atlas_object(source,name):
    """Temporary export copy preserves source material editing and normals."""
    source.name=name+'_Editable'
    obj=source.copy()
    obj.data=source.data.copy()
    obj.name=name
    scene.collection.objects.link(obj)
    while obj.data.uv_layers:
        obj.data.uv_layers.remove(obj.data.uv_layers[0])
    uv=obj.data.uv_layers.new(name='PaletteUV')
    slot_names=[m.name for m in obj.data.materials]
    for polygon in obj.data.polygons:
        index=PALETTE_INDEX[slot_names[polygon.material_index]]
        for loop_index in polygon.loop_indices:
            uv.data[loop_index].uv=((index+.5)/8,.5)
        polygon.material_index=0
    obj.data.materials.clear()
    obj.data.materials.append(ATLAS)
    obj['palette']='16x2 / 8 swatches / nearest / no mipmaps'
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    path=MODELS/(name+'.glb')
    bpy.ops.export_scene.gltf(filepath=str(path),export_format='GLB',use_selection=True,export_yup=True,export_extras=True,export_apply=True,export_cameras=False,export_lights=False,export_texcoords=True)
    no_mipmap_glb(path)
    data=obj.data
    bpy.data.objects.remove(obj,do_unlink=True)
    bpy.data.meshes.remove(data)
    source.name=name

parts = []

def add(obj, name, mat):
    obj.name=name
    obj.data.materials.append(M[mat])
    parts.append(obj)
    return obj

def bevel(obj, width=.015, segments=1):
    if not width:
        return
    bpy.context.view_layer.objects.active=obj
    mod=obj.modifiers.new('Manufactured edge bevel','BEVEL')
    mod.width=width
    mod.segments=segments
    mod.affect='EDGES'
    bpy.ops.object.modifier_apply(modifier=mod.name)
    norm=obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
    norm.keep_sharp=True
    norm.weight=40
    bpy.ops.object.modifier_apply(modifier=norm.name)

def box(name, p, size, mat, b=.012, rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj=add(bpy.context.object,name,mat)
    obj.dimensions=size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj,b)
    if rotation:
        obj.rotation_euler=rotation
    return obj

def mesh(name, verts, faces, mat, b=0):
    data=bpy.data.meshes.new(name)
    data.from_pydata(verts,[],faces)
    data.update()
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    add(obj,name,mat)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active=obj
    if b:
        bevel(obj,b)
    return obj

def beam(name, a,b,width,mat, edge=0):
    a,b=Vector(a),Vector(b)
    obj=box(name,(a+b)*.5,(width,width,(b-a).length),mat,edge)
    obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return obj

def cylinder(name,p,radius,depth,mat,vertices=8, axis=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices,radius=radius,depth=depth,location=p)
    obj=add(bpy.context.object,name,mat)
    if axis:
        obj.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return obj

def panel_quad(name, coords, mat):
    return mesh(name,coords,[(0,1,2,3)],mat)

def finish(name):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active=parts[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=name
    scene.cursor.location=(0,0,0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    tri=obj.modifiers.new('Export triangles','TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=tri.name)
    obj['units']='metres'
    obj['unity_front']='+Z (Blender -Y)'
    obj['pivot']='bottom centre'
    obj['art_direction']='Quarantine / Recovery Yard - Sample A'
    collection=bpy.data.collections.new(name+'_Source')
    scene.collection.children.link(collection)
    for col in list(obj.users_collection):
        col.objects.unlink(obj)
    collection.objects.link(obj)
    export_atlas_object(obj,name)
    parts=[]
    return obj

# CONCRETE BARRIER: Jersey profile, trapezoid ends and embedded warning band.
# The stepped profile is more readable from the game camera than a rectangular box.
profile=[(-.30,0),(.30,0),(.30,.14),(.20,.38),(.13,.91),(.10,.95),(-.10,.95),(-.13,.91),(-.20,.38),(-.30,.14)]
verts=[(x,y,z) for x in (-1.50,1.50) for y,z in profile]
n=len(profile)
faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
mesh('Concrete cast body',verts,faces,'concrete',.014)
# Bands follow the incline, leaving a clear pale top surface.
for side in (-1,1):
    for a,b in [(-1.27,-.42),(-.28,.57),(.71,1.27)]:
        band=[(a,side*.267,.224),(b,side*.267,.224),(b,side*.215,.347),(a,side*.215,.347)]
        panel_quad('Cast-in faded warning strip',band if side<0 else list(reversed(band)),'orange')
    for x in (-1.12,1.12):
        box('Inset lifting recess',(x,side*.144,.82),(.13,.006,.052),'charcoal',.005)
for x in (-.93,.93):
    box('Top lifting socket',(x,0,.948),(.18,.09,.006),'charcoal',.004)
for side in (-1,1):
    box('Dowel end joint',(side*1.5,0,.47),(.006,.074,.21),'charcoal',.002)
barrier=finish('ConcreteBarrier')

# FENCE: rigid square-section wire diamond grid; no alpha materials.
for x in (-1.91,1.91):
    box('Fence concrete shoe',(x,0,.065),(.18,.30,.13),'concrete',.015)
    box('Fence vertical post',(x,0,1.145),(.11,.115,2.07),'metal',.014)
    box('Post protective cap',(x,0,2.175),(.14,.145,.05),'charcoal',.008)
    box('Reflector orange collar',(x,-.062,.68),(.085,.014,.14),'orange',.003)
for z in (.33,2.085):
    box('Fence frame rail',(0,0,z),(3.84,.072,.074),'metal',.007)
# Clip both diagonal line families to the inner rectangular frame.
xlo,xhi,zlo,zhi=-1.855,1.855,.37,2.045
for slope in (1,-1):
    for k in range(-18,19):
        c=k*.31
        hits=[]
        for x in (xlo,xhi):
            z=slope*x+c
            if zlo-1e-6 <= z <= zhi+1e-6:
                hits.append(Vector((x,.016,z)))
        for z in (zlo,zhi):
            x=(z-c)/slope
            if xlo-1e-6 <= x <= xhi+1e-6:
                pt=Vector((x,.016,z))
                if not any((pt-p).length<1e-5 for p in hits):
                    hits.append(pt)
        if len(hits)==2 and (hits[0]-hits[1]).length>.02:
            beam('Diamond steel wire',hits[0],hits[1],.018,'metal')
for x in (-1.83,1.83):
    for z in (.39,1.96):
        box('Frame clamp',(x,-.052,z),(.16,.06,.095),'charcoal',.005)
fence=finish('FenceSegment')

# SUPPLY TERMINAL: sloping metal hood, recessed screen, controlled cyan accents.
box('Terminal foot',(0,0,.075),(.8,.65,.15),'charcoal',.025)
box('Terminal lower plinth',(0,0,.19),(.70,.56,.19),'metal',.018)
box('Terminal olive housing',(0,0,1.005),(.72,.55,1.49),'olive',.05)
for x in (-.355,.355):
    box('Terminal corner armour',(x,0,1.025),(.085,.555,1.48),'charcoal',.018)
box('Upper weather hood',(0,-.025,1.74),(.79,.60,.12),'charcoal',.018)
box('Hood warning lip',(0,-.333,1.717),(.53,.018,.045),'orange',.007)
box('Screen recess backing',(0,-.285,1.35),(.55,.026,.50),'rubber',.025)
box('Screen glass',(0,-.306,1.375),(.425,.009,.316),'cyan',.007)
for x in (-.256,.256):
    box('Screen frame side',(x,-.317,1.35),(.056,.071,.502),'charcoal',.01)
for z in (1.112,1.589):
    box('Screen frame top-bottom',(0,-.317,z),(.52,.072,.045),'charcoal',.008)
# A few large technical shapes read as a display, not a luminous featureless slab.
box('Screen information field',(-.075,-.313,1.373),(.221,.007,.248),'charcoal',.003)
for i,w in enumerate((.13,.095,.055)):
    box('Screen status bar',(-.14+w/2,-.318,1.437-i*.056),(w,.005,.014),'cyan',0)
box('Screen symbol vertical',(.123,-.315,1.38),(.025,.005,.084),'label',0)
box('Screen symbol horizontal',(.123,-.315,1.38),(.080,.005,.025),'label',0)
box('Recessed keypad panel',(0,-.286,.948),(.52,.019,.185),'charcoal',.013)
for x in (-.16,-.055,.05):
    for z in (.918,.978):
        box('Keypad key',(x,-.304,z),(.057,.021,.036),'metal',.006)
box('Amber confirm button',(.172,-.308,.95),(.076,.025,.083),'orange',.006)
box('Service hatch',(0,-.291,.589),(.48,.027,.394),'charcoal',.018)
box('Service hatch inset',(0,-.307,.593),(.41,.012,.31),'olive',.011)
box('Service hatch latch',(.155,-.32,.61),(.037,.023,.12),'metal',.003)
for z in (.334,.376,.418):
    box('Lower cooling slit',(0,-.292,z),(.31,.012,.017),'rubber',0)
for side in (-1,1):
    for z in (1.21,1.28,1.35):
        box('Side cooling vent',(side*.402,.045,z),(.009,.285,.027),'rubber',0)
    for z in (.48,1.59):
        cylinder('Terminal armour fastener',(side*.407,-.17,z),.023,.008,'metal',6,axis=(1,0,0))
terminal=finish('SupplyTerminal')

# CARGO CRATE: reinforced container with broad quiet panels and asymmetric latch.
box('Crate floor',(0,0,.10),(1.16,1.16,.20),'charcoal',.025)
box('Crate core',(0,0,.55),(1.12,1.12,.80),'olive',.025)
box('Crate lid seam',(0,0,.887),(1.17,1.17,.042),'rubber',.008)
box('Crate lid',(0,0,.935),(1.20,1.20,.10),'olive',.026)
for x in (-.525,.525):
    for y in (-.525,.525):
        box('Crate corner armour',(x,y,.52),(.15,.15,.82),'charcoal',.015)
for x in (-.35,.35):
    box('Lid stiffener',(x,0,.989),(.06,1.02,.022),'metal',.006)
for y in (-.583,.583):
    for x in (-.31,.31):
        box('Side reinforcing rib',(x,y,.545),(.055,.026,.52),'metal',.007)
    box('Lower edge binding',(0,y,.20),(1.00,.033,.06),'metal',.006)
box('Crate latch backing',(0,-.609,.84),(.22,.031,.17),'charcoal',.011)
box('Crate latch',(0,-.633,.832),(.115,.032,.17),'metal',.009)
box('Crate latch amber tab',(0,-.653,.866),(.072,.015,.041),'orange',.004)
box('Crate inventory plate',(.04,-.579,.51),(.30,.015,.17),'charcoal',.009)
box('Crate ID stripe',(-.055,-.59,.531),(.066,.009,.039),'label',0)
box('Crate ID line',(.059,-.59,.531),(.107,.009,.021),'label',0)
crate=finish('CargoCrate')

# SMALL GENERATOR: skid frame, recessed exhaust grille and readable olive casing.
for y in (-.49,.49):
    box('Generator runner',(0,y,.095),(2.18,.16,.19),'charcoal',.025)
for x in (-.91,.91):
    box('Generator skid crossmember',(x,0,.17),(.15,1.25,.13),'metal',.02)
box('Generator main shell',(0,0,.76),(1.85,1.05,.93),'olive',.065)
box('Generator roof',(0,0,1.23),(1.95,1.10,.14),'charcoal',.032)
for x in (-.99,.99):
    for y in (-.565,.565):
        box('Generator protection upright',(x,y,.70),(.11,.10,1.08),'metal',.018)
for y in (-.565,.565):
    box('Generator upper frame',(0,y,1.245),(2.12,.105,.11),'metal',.016)
box('Generator amber access band',(-.53,-.543,.745),(.18,.025,.76),'orange',.015)
box('Generator front vent recess',(.26,-.532,.81),(.76,.025,.52),'rubber',.016)
for z in (.61,.70,.79,.88,.97):
    box('Generator vent slat',(.26,-.568,z),(.684,.064,.037),'metal',.007,rotation=(math.radians(-18),0,0))
box('Generator control recess',(-.665,-.556,.88),(.28,.031,.28),'charcoal',.012)
box('Generator control light',(-.662,-.579,.953),(.11,.012,.042),'cyan',.004)
cylinder('Generator control switch',(-.662,-.60,.834),.044,.025,'rubber',8,axis=(0,-1,0))
box('Generator right vent backing',( .939,0,.77),(.022,.73,.61),'rubber',.025)
for y in (-.26,-.13,0,.13,.26):
    box('End vent vertical louver',(.973,y,.77),(.048,.044,.52),'metal',.007)
box('Generator service panel',(-.937,.035,.75),(.018,.75,.59),'charcoal',.014)
box('Generator service cover',(-.951,.035,.76),(.014,.63,.45),'olive',.011)
box('Generator service handle',(-.98,-.18,.77),(.045,.047,.16),'metal',.005)
for x in (-.52,.52):
    box('Generator roof reinforcement',(x,0,1.304),(.10,.84,.022),'metal',.006)
for x in (-.805,.805):
    for y in (-.423,.423):
        cylinder('Roof tie down socket',(x,y,1.312),.042,.014,'rubber',8)
        cylinder('Roof socket steel core',(x,y,1.321),.018,.012,'metal',6)
for x in (-.78,.76):
    box('Generator lower amber reflector',(x,-.580,.265),(.18,.027,.048),'orange',.006)
generator=finish('SmallGenerator')

assets=[barrier,fence,terminal,crate,generator]
manifest=[]
for asset in assets:
    pts=[asset.matrix_world @ Vector(v) for v in asset.bound_box]
    low=[round(min(v[i] for v in pts),5) for i in range(3)]
    high=[round(max(v[i] for v in pts),5) for i in range(3)]
    manifest.append({'name':asset.name,'file':str((MODELS/(asset.name+'.glb')).relative_to(ROOT)).replace('\\','/'),'triangles':len(asset.data.polygons),'vertices':len(asset.data.vertices),'blenderBounds':{'min':low,'max':high},'unityDimensions':[round(high[0]-low[0],4),round(high[2]-low[2],4),round(high[1]-low[1],4)],'materials':['ENV_AtlasSurface'],'exportPrimitives':1,'editableSourceMaterials':sorted({slot.material.name for slot in asset.material_slots if slot.material}),'rootScale':[1,1,1],'unityFront':'+Z','pivot':'bottom centre'})
(SOURCE/'modular-kit-manifest.json').write_text(json.dumps({'generatedBy':'build_modular_kit.py / Blender 5.2.1','direction':'Quarantine / Recovery Yard','assets':manifest},indent=2),encoding='utf-8')

# True offline renders of the exported meshes, with soft daylight and ground contact.
floor=box('Preview floor',(0,0,-.075),(200,200,.15),'concrete',0)
floor.data.materials.clear()
floor_mat=material('PreviewOnly_Floor','535E60',rough=.86)
floor.data.materials.append(floor_mat)
parts=[]
world=bpy.data.worlds.new('Soft industrial sky')
world.use_nodes=True
background=next((n for n in world.node_tree.nodes if n.type=='BACKGROUND'),None)
if background is None:
    background=world.node_tree.nodes.new('ShaderNodeBackground')
    output=world.node_tree.nodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(background.outputs[0],output.inputs[0])
background.inputs['Color'].default_value=(.30,.37,.42,1)
background.inputs['Strength'].default_value=.65
scene.world=world
def area(name,p,energy,size):
    data=bpy.data.lights.new(name,'AREA')
    data.energy=energy
    data.shape='DISK'
    data.size=size
    obj=bpy.data.objects.new(name,data)
    scene.collection.objects.link(obj)
    obj.location=p
    obj.rotation_euler=(Vector((0,0,.8))-obj.location).to_track_quat('-Z','Y').to_euler()
area('Large soft key',(1,-4,7),1150,5)
area('Cool sky fill',(-4,-1,4),700,5)
area('Top rim',(2,4,6),1000,4)
cam_data=bpy.data.cameras.new('Asset verification camera')
cam=bpy.data.objects.new('Asset verification camera',cam_data)
scene.collection.objects.link(cam)
scene.camera=cam
cam_data.type='ORTHO'
scene.render.engine='CYCLES'
scene.cycles.samples=40
scene.cycles.use_denoising=True
scene.render.resolution_x=900
scene.render.resolution_y=700
scene.render.resolution_percentage=100
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
scene.render.image_settings.file_format='PNG'
scene.render.film_transparent=False
for obj in assets:
    obj.hide_render=True
for obj in assets:
    obj.hide_render=False
    corners=[obj.matrix_world @ Vector(v) for v in obj.bound_box]
    target=sum(corners,Vector())/8
    cam.location=target+Vector((4,-6,4.2))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.view_layer.update()
    view=cam.matrix_world.inverted()
    projected=[view @ p for p in corners]
    span_x=max(p.x for p in projected)-min(p.x for p in projected)
    span_y=max(p.y for p in projected)-min(p.y for p in projected)
    cam_data.ortho_scale=max(span_x,span_y*scene.render.resolution_x/scene.render.resolution_y)*1.20
    scene.render.filepath=str(PREVIEW/(obj.name+'.png'))
    bpy.ops.render.render(write_still=True)
    obj.hide_render=True
for obj in assets:
    obj.hide_render=False
# Source file opens as a readable workshop lineup. Exported GLBs remain at origin.
for obj,x in zip(assets,(-5.1,-1.2,1.65,3.30,5.50)):
    obj.location.x=x
floor.hide_render=False
cam.location=(9,-15,10)
cam.rotation_euler=(Vector((0,0,.7))-cam.location).to_track_quat('-Z','Y').to_euler()
cam_data.ortho_scale=14.8
bpy.ops.object.select_all(action='DESELECT')
terminal.select_set(True)
bpy.context.view_layer.objects.active=terminal
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Quarantine_ModularKit.blend'))
print('MODULAR_KIT_COMPLETE '+json.dumps(manifest))
