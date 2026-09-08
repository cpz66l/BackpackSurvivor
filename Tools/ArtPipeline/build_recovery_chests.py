"""Build five lightweight hinged salvage chests. Blender Z up, -Y front.

Run E:/Blender/blender.exe --background --factory-startup --python this_file.py
Exported Unity coordinates: Y up, +Z front, floor-centred unit root.
Body and Lid are independent single-primitive meshes sharing one 32x2 atlas.
Lid local X rotation -100 degrees opens away from the front of the chest.
"""
import bpy
import json
import math
import struct
import zlib
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODELS = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/Loot/VNext/Models'
TEXTURES = MODELS.parent / 'Textures'
SOURCE = ROOT / 'Tools/ArtPipeline/Source'
DOCS = ROOT / 'Docs/ArtDirection/2026-09-08/ChestNightUI'
for folder in (MODELS, TEXTURES, SOURCE, DOCS):
    folder.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for mat in list(bpy.data.materials):
    bpy.data.materials.remove(mat)
bpy.context.preferences.filepaths.save_version = 0
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1.0

RARITIES = ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary']
# Exact ChestSpawner authored sRGB rarity values, quantized only for 8-bit atlas.
COLORS = [(1, 1, 1), (0, 1, .4698286), (.3223, .5812, .89434),
          (.153, .2571, .9321), (1, .45465, 0)]
COLOR_BYTES = [tuple(round(c * 255) for c in rgb) for rgb in COLORS]
PALETTE = [
    ('Shell', (36, 47, 61), .42, .57, False),
    ('Frame', (16, 25, 35), .55, .48, False),
    ('Steel', (95, 113, 132), .78, .38, False),
    ('Rubber', (13, 21, 32), .0, .89, False),
    ('Stencil', (177, 191, 201), .1, .62, False),
]
PALETTE += [(r + 'Paint', c, .28, .43, False) for r, c in zip(RARITIES, COLOR_BYTES)]
PALETTE += [(r + 'Light', c, .08, .32, True) for r, c in zip(RARITIES, COLOR_BYTES)]
PALETTE.append(('Interior', (25, 34, 46), .2, .75, False))

def linear(c):
    c /= 255
    return c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4

def material(name, rgb, metallic, rough, emission=False):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get('Principled BSDF')
    rgba = tuple(linear(c) for c in rgb) + (1,)
    mat.diffuse_color = rgba
    bsdf.inputs['Base Color'].default_value = rgba
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = rough
    if emission:
        bsdf.inputs['Emission Color'].default_value = rgba
        bsdf.inputs['Emission Strength'].default_value = 1.0
    return mat

MATERIALS = [material('CHEST_' + n, c, m, r, e) for n, c, m, r, e in PALETTE]

def palette_png(path, pixels):
    def chunk(kind, data):
        return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
    row = bytes(sum((list(p) * 2 for p in pixels), []))
    raw = (b'\0' + row) * 2
    path.write_bytes(b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 32, 2, 8, 6, 0, 0, 0)) + chunk(b'IDAT', zlib.compress(raw, 9)) + chunk(b'IEND', b''))

for suffix, pixels in [
    ('BaseColor', [(*c, 255) for _, c, _, _, _ in PALETTE]),
    ('Emission', [(*c, 255) if e else (0, 0, 0, 255) for _, c, _, _, e in PALETTE]),
    ('MetallicRoughness', [(255, round(r * 255), round(m * 255), 255) for _, _, m, r, _ in PALETTE]),
    ('MetallicSmoothness', [(round(m * 255), 0, 0, round((1-r)*255)) for _, _, m, r, _ in PALETTE]),
]:
    palette_png(TEXTURES / ('CHEST_Palette_' + suffix + '.png'), pixels)

ATLAS = material('CHEST_AtlasSurface', (255, 255, 255), 1, 1, True)
nodes, links = ATLAS.node_tree.nodes, ATLAS.node_tree.links
bsdf = nodes.get('Principled BSDF')
def tex(suffix, colorspace):
    image = bpy.data.images.load(str(TEXTURES / ('CHEST_Palette_' + suffix + '.png')))
    image.colorspace_settings.name = colorspace
    image.pack()
    n = nodes.new('ShaderNodeTexImage')
    n.image = image
    n.interpolation = 'Closest'
    n.extension = 'EXTEND'
    return n
links.new(tex('BaseColor', 'sRGB').outputs['Color'], bsdf.inputs['Base Color'])
links.new(tex('Emission', 'sRGB').outputs['Color'], bsdf.inputs['Emission Color'])
split = nodes.new('ShaderNodeSeparateColor')
links.new(tex('MetallicRoughness', 'Non-Color').outputs['Color'], split.inputs['Color'])
links.new(split.outputs['Green'], bsdf.inputs['Roughness'])
links.new(split.outputs['Blue'], bsdf.inputs['Metallic'])

parts = {'Body': [], 'Lid': []}
def box(name, centre, dims, color=0, bevel=.012, group='Body', rotation=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=centre)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dims
    if rotation:
        obj.rotation_euler = rotation
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    obj.data.materials.append(MATERIALS[color])
    if bevel:
        mod = obj.modifiers.new('Single-step edge chamfer', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        bpy.ops.object.modifier_apply(modifier=mod.name)
        normals = obj.modifiers.new('Face-weighted normals', 'WEIGHTED_NORMAL')
        normals.keep_sharp = True
        bpy.ops.object.modifier_apply(modifier=normals.name)
    parts[group].append(obj)
    return obj

def polygon_plate(name, coords, depth, location, color, group='Body'):
    # Extrude a polygon in the local XZ plane, useful for a readable vault lock.
    n = len(coords)
    verts = [(x, y, z) for y in (-depth/2, depth/2) for x, z in coords]
    faces = [tuple(range(n-1, -1, -1)), tuple(range(n, n*2))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(MATERIALS[color])
    obj = bpy.data.objects.new(name, mesh)
    scene.collection.objects.link(obj)
    obj.location = location
    parts[group].append(obj)
    return obj

def join(group, rarity, pivot):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts[group]:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[group][0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = rarity + '_' + group + '_Editable'
    # Origin at the fixed local hinge, root Body at floor centre.
    scene.cursor.location = pivot
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    mod = obj.modifiers.new('Game triangles', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return obj

def export_chest(rarity, body, lid):
    root = bpy.data.objects.new(rarity + 'Chest', None)
    scene.collection.objects.link(root)
    clones = []
    for src, name in [(body, 'Body'), (lid, 'Lid')]:
        obj = src.copy()
        obj.data = src.data.copy()
        scene.collection.objects.link(obj)
        obj.name = name
        obj.parent = root
        while obj.data.uv_layers:
            obj.data.uv_layers.remove(obj.data.uv_layers[0])
        uv = obj.data.uv_layers.new(name='PaletteUV')
        mat_indices = [MATERIALS.index(m) for m in obj.data.materials]
        for face in obj.data.polygons:
            idx = mat_indices[face.material_index]
            for li in face.loop_indices:
                uv.data[li].uv = ((idx + .5) / 16, .5)
            face.material_index = 0
        obj.data.materials.clear()
        obj.data.materials.append(ATLAS)
        clones.append(obj)
    root['rarity'] = rarity
    root['unityForward'] = '+Z'
    root['lidOpenEulerXDegrees'] = -100
    bpy.ops.object.select_all(action='DESELECT')
    root.select_set(True)
    for obj in clones:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = root
    output = MODELS / (rarity + 'Chest.glb')
    bpy.ops.export_scene.gltf(filepath=str(output), export_format='GLB', use_selection=True,
                              export_yup=True, export_extras=True, export_apply=True,
                              export_cameras=False, export_lights=False, export_texcoords=True)
    content = output.read_bytes()
    length = struct.unpack_from('<I', content, 12)[0]
    document = json.loads(content[20:20+length])
    for sampler in document.get('samplers', []):
        sampler.update({'magFilter': 9728, 'minFilter': 9728, 'wrapS': 33071, 'wrapT': 33071})
    assert len(document['meshes']) == 2
    assert all(len(mesh['primitives']) == 1 for mesh in document['meshes'])
    assert len(document['materials']) == 1
    assert {n['name'] for n in document['nodes']} == {rarity + 'Chest', 'Body', 'Lid'}
    encoded = json.dumps(document, separators=(',', ':')).encode()
    encoded += b' ' * (-len(encoded) % 4)
    rest = content[20+length:]
    output.write_bytes(struct.pack('<4sII', b'glTF', 2, 20+len(encoded)+len(rest)) + struct.pack('<I4s', len(encoded), b'JSON') + encoded + rest)
    hinge = next(n.get('translation', [0, 0, 0]) for n in document['nodes'] if n['name'] == 'Lid')
    for obj in clones + [root]:
        bpy.data.objects.remove(obj, do_unlink=True)
    return output, hinge

assets = []
manifest = []
# Dimensions include external armour. All dimensions safely under 1.6x1.1x1.2.
for rank, (rarity, w, d, body_h, lid_h) in enumerate([
    ('Common', 1.08, .74, .47, .16),
    ('Uncommon', 1.18, .83, .55, .18),
    ('Rare', 1.30, .94, .64, .18),
    ('Epic', 1.40, 1.02, .71, .18),
    ('Legendary', 1.50, 1.04, .70, .18),
]):
    parts = {'Body': [], 'Lid': []}
    paint, glow = 5 + rank, 10 + rank
    floor, wall, thick = .055, .06, .035
    core_w = w - (.16 if rank >= 2 else .10)
    core_d = d - .09
    top = floor + body_h
    # Hollow five-sided tub: usable open silhouette, without a solid fake cavity.
    box('Lower impact tray', (0, 0, floor), (core_w, core_d, .11), 1, .025)
    box('Interior base liner', (0, 0, .12), (core_w-.10, core_d-.10, .06), 15, .008)
    for x in (-1, 1):
        box('Side body wall', (x*(core_w/2-wall/2), 0, (top+.11)/2), (wall, core_d, top-.11), 0, .016)
    for y in (-1, 1):
        box('End body wall', (0, y*(core_d/2-wall/2), (top+.11)/2), (core_w-wall*2, wall, top-.11), 0, .016)
    for x in (-1, 1):
        box('Foot skid', (x*core_w*.33, 0, .025), (.11, core_d+.025, .05), 3, .007)
    # Contrasting front equipment band is distinct at the gameplay camera scale.
    box('Front rarity identity panel', (0, -core_d/2-.009, top*.55), (core_w*.68, .025, body_h*.29), paint, .010)
    box('Identity panel dark recess', (0, -core_d/2-.025, top*.55), (core_w*.33, .015, body_h*.17), 1, .006)
    for i in range(rank+1):
        box('Rank tick ' + str(i+1), ((i-rank*.5)*.045, -core_d/2-.037, top*.55), (.021, .013, .041), glow, .002)
    # Lid rests above the open tub; rotation is around its rear bottom edge.
    hinge = (0, core_d/2, top+.014)
    box('Lid gasket', (0, 0, top+.016), (core_w+.028, core_d+.028, .028), 3, .006, 'Lid')
    box('Lid shell', (0, 0, top+.032+lid_h/2), (core_w+.04, core_d+.04, lid_h), 0, .024, 'Lid')
    box('Recessed lid colour panel', (0, -.01, top+.035+lid_h), (core_w*.72, core_d*.60, .025), paint, .010, 'Lid')
    # A broad hinge and rear stops make the functional movement readable.
    for x in (-core_w*.29, core_w*.29):
        box('Rear hinge knuckle', (x, core_d/2+.012, top+.025), (.13, .06, .07), 2, .010, 'Lid')
    if rank == 0:
        # Small service toolbox: one central latch, broad carry handle and white cap.
        box('Central latch body', (0, -core_d/2-.048, top-.016), (.14, .067, .18), 2, .010)
        box('Latch indicator', (0, -core_d/2-.085, top+.018), (.072, .012, .029), glow, .003)
        for x in (-.20, .20):
            box('Handle upright', (x, .02, top+lid_h+.077), (.047, .072, .070), 1, .008, 'Lid')
        box('Toolbox carry handle', (0, .02, top+lid_h+.107), (.43, .075, .050), 1, .010, 'Lid')
    elif rank == 1:
        # Twin latches and continuous green bindings distinguish the supply case.
        for x in (-core_w*.29, core_w*.29):
            box('Lid structural binding', (x, 0, top+lid_h+.055), (.07, core_d+.065, .052), 1, .009, 'Lid')
            box('Twin clasp', (x, -core_d/2-.045, top-.04), (.13, .063, .20), 2, .009)
            box('Clasp glow', (x, -core_d/2-.081, top+.012), (.066, .011, .032), glow, .002)
        for x in (-1, 1):
            box('Side case binding', (x*core_w/2, 0, top*.50), (.056, core_d*.65, .10), paint, .008)
    elif rank == 2:
        # Four heavy armour corners and raised transverse ribs: armoured freight.
        for x in (-1, 1):
            for y in (-1, 1):
                box('Armour corner', (x*(core_w/2-.005), y*(core_d/2-.012), top*.5), (.16, .15, top-.025), 1, .022)
                box('Corner shoulder', (x*(core_w/2-.006), y*(core_d/2-.009), top+lid_h*.5), (.16, .15, lid_h+.07), 2, .016, 'Lid')
        for x in (-.23, .23):
            box('Top armour rib', (x, 0, top+lid_h+.066), (.074, core_d*.83, .085), 1, .012, 'Lid')
        box('Wide armored clasp', (0, -core_d/2-.070, top-.02), (.29, .075, .20), 1, .019)
        box('Clasp status strip', (0, -core_d/2-.112, top+.01), (.18, .015, .042), glow, .004)
    elif rank == 3:
        # Bilateral raised protection frames are a new outline, not just a recolour.
        for x in (-1, 1):
            for y in (-1, 1):
                box('Energy guard upright', (x*(w/2-.05), y*(core_d/2-.055), top*.50), (.10, .12, top), 1, .015)
                box('Guard colour insert', (x*(w/2-.105), y*(core_d/2-.055), top*.56), (.016, .10, top*.50), paint, .004)
            box('Raised side guard', (x*(w/2-.05), 0, top+lid_h+.052), (.10, core_d+.03, .13), 2, .012, 'Lid')
            box('Side energy rail', (x*(w/2-.045), 0, top+lid_h+.120), (.07, core_d*.66, .013), glow, .003, 'Lid')
        box('Raised central ridge', (0, .01, top+lid_h+.058), (.20, core_d*.75, .07), 1, .012, 'Lid')
        box('Ridge light strip', (0, -.03, top+lid_h+.098), (.047, core_d*.47, .013), glow, .002, 'Lid')
        box('Double safety latch', (0, -core_d/2-.065, top-.035), (.32, .07, .22), 2, .018)
        box('Dual-lock recess', (0, -core_d/2-.106, top-.035), (.24, .015, .13), 1, .010)
    else:
        # Vault silhouette with angular lock, dense corner shoulders and orange crown.
        for x in (-1, 1):
            for y in (-1, 1):
                box('Vault corner column', (x*(core_w/2-.009), y*(core_d/2-.013), top*.5), (.18, .18, top), 1, .023)
                box('Vault top shoulder', (x*(core_w/2-.009), y*(core_d/2-.015), top+lid_h*.53), (.19, .18, lid_h+.07), paint, .024, 'Lid')
        box('Vault lower front band', (0, -core_d/2-.022, .16), (core_w*.86, .055, .13), 2, .012)
        coords = [(-.17,-.11),(-.10,-.18),(.10,-.18),(.17,-.11),(.17,.11),(.10,.18),(-.10,.18),(-.17,.11)]
        polygon_plate('Octagonal vault lock', coords, .10, (0, -core_d/2-.071, top-.10), 2)
        polygon_plate('Lock illuminated core', [(x*.64,z*.64) for x,z in coords], .018, (0, -core_d/2-.127, top-.10), glow)
        box('Vault lock divider', (0, -core_d/2-.142, top-.10), (.037, .018, .19), 1, .005)
        box('Crown central shield', (0, -.015, top+lid_h+.067), (core_w*.46, core_d*.64, .09), 1, .025, 'Lid')
        box('Crown orange crest', (0, -.015, top+lid_h+.118), (core_w*.30, core_d*.46, .022), paint, .009, 'Lid')
        box('Crown core light', (0, -.015, top+lid_h+.134), (core_w*.13, core_d*.25, .014), glow, .004, 'Lid')
    body = join('Body', rarity, (0, 0, 0))
    lid = join('Lid', rarity, hinge)
    output, unity_hinge = export_chest(rarity, body, lid)
    corners = [o.matrix_world @ Vector(c) for o in (body, lid) for c in o.bound_box]
    low = [min(c[i] for c in corners) for i in range(3)]
    high = [max(c[i] for c in corners) for i in range(3)]
    dims = [high[0]-low[0], high[2]-low[2], high[1]-low[1]]
    assert dims[0] <= 1.6 and dims[1] <= 1.1 and dims[2] <= 1.2, (rarity, dims)
    manifest.append({'rarity': rarity, 'file': str(output.relative_to(ROOT)).replace('\\','/'),
                     'triangles': len(body.data.polygons)+len(lid.data.polygons),
                     'bodyTriangles': len(body.data.polygons), 'lidTriangles': len(lid.data.polygons),
                     'bytes': output.stat().st_size, 'unityDimensionsXYZ': [round(v,5) for v in dims],
                     'rootNode': rarity+'Chest', 'bodyNode': 'Body', 'lidNode': 'Lid',
                     'unityLidPivot': [round(v,5) for v in unity_hinge], 'openLocalEulerX': -100,
                     'materials': ['CHEST_AtlasSurface'], 'meshes': 2, 'primitives': 2,
                     'rarityColorOriginal': COLORS[rank], 'rarityPaletteRGB8': COLOR_BYTES[rank]})
    assets.append((body, lid))

assert sum(m['triangles'] for m in manifest) < 8000
assert sum(m['bytes'] for m in manifest) < 600*1024
data = {'generatedBy': 'build_recovery_chests.py / Blender '+bpy.app.version_string,
        'style': 'Recovery Yard / blue grey industrial night operations',
        'rootScale': [1,1,1], 'unityForward': '+Z', 'units': 'metres',
        'paletteDimensions': [32,2], 'paletteSampling': 'Nearest / Clamp / no mipmaps',
        'totalTriangles': sum(m['triangles'] for m in manifest),
        'totalGLBBytes': sum(m['bytes'] for m in manifest), 'assets': manifest}
(SOURCE/'recovery-chests-manifest.json').write_text(json.dumps(data, indent=2), encoding='utf-8')
(DOCS/'chest-manifest.json').write_text(json.dumps(data, indent=2), encoding='utf-8')

# Renders use the exact colour assignments and game topology; no generated imagery.
floor_mat = material('PreviewOnly_Floor', (38,49,63), .10, .80)
bpy.ops.mesh.primitive_plane_add(size=200, location=(0,0,-.005))
floor = bpy.context.object
floor.name = 'PreviewOnly Floor'
floor.data.materials.append(floor_mat)
world = bpy.data.worlds.new('Cool night worksite')
world.use_nodes = True
world.node_tree.nodes['Background'].inputs['Color'].default_value = (.19,.26,.37,1)
world.node_tree.nodes['Background'].inputs['Strength'].default_value = .40
scene.world = world
def area(name, p, energy, size, color):
    light = bpy.data.lights.new(name,'AREA')
    light.energy, light.size, light.color = energy,size,color
    obj = bpy.data.objects.new(name,light)
    scene.collection.objects.link(obj)
    obj.location = p
    obj.rotation_euler = (Vector((0,0,.4))-obj.location).to_track_quat('-Z','Y').to_euler()
area('Cool broad worklight',(1,-4,6),600,5,(.72,.84,1))
area('Front fill',(-3,-2,2),180,4,(.68,.79,1))
area('Amber distant rim',(2,3,4),520,3,(1,.78,.52))
camdata = bpy.data.cameras.new('Chest verification camera')
cam = bpy.data.objects.new('Chest verification camera',camdata)
scene.collection.objects.link(cam)
scene.camera = cam
camdata.type = 'ORTHO'
scene.render.engine='CYCLES'
scene.cycles.samples=40
scene.cycles.use_denoising=True
scene.render.resolution_x=920
scene.render.resolution_y=720
scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'
scene.view_settings.view_transform='AgX'
scene.view_settings.look='AgX - Medium High Contrast'
for pair in assets:
    for obj in pair:
        obj.hide_render=True
def fit_camera(objects):
    bpy.context.view_layer.update()
    corners=[o.matrix_world @ Vector(c) for o in objects for c in o.bound_box]
    low=Vector(tuple(min(p[i] for p in corners) for i in range(3)))
    high=Vector(tuple(max(p[i] for p in corners) for i in range(3)))
    target=(low+high)*.5
    cam.location=target+Vector((3.3,-5,3.5))
    cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.view_layer.update()
    points=[cam.matrix_world.inverted() @ p for p in corners]
    sx=max(p.x for p in points)-min(p.x for p in points)
    sy=max(p.y for p in points)-min(p.y for p in points)
    camdata.ortho_scale=max(sx,sy*scene.render.resolution_x/scene.render.resolution_y)*1.16
for rarity,pair in zip(RARITIES,assets):
    for obj in pair:
        obj.hide_render=False
    fit_camera(pair)
    scene.render.filepath=str(DOCS/(rarity+'Chest.png'))
    bpy.ops.render.render(write_still=True)
    if rarity=='Legendary':
        pair[1].rotation_euler.x=math.radians(-100)
        scene.render.filepath=str(DOCS/'LegendaryChest-open.png')
        fit_camera(pair)
        bpy.ops.render.render(write_still=True)
        pair[1].rotation_euler.x=0
    for obj in pair:
        obj.hide_render=True
# Source opens as a labelled modelling lineup, with hinges preserved locally.
for i,pair in enumerate(assets):
    root=bpy.data.objects.new(RARITIES[i]+'Chest_Editable',None)
    scene.collection.objects.link(root)
    for obj in pair:
        obj.parent=root
        obj.hide_render=False
    root.location.x=(i-2)*1.95
scene.render.resolution_x=1920
scene.render.resolution_y=760
cam.location=(7,-12,9)
cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler()
camdata.ortho_scale=11.9
scene.render.filepath=str(DOCS/'chest-lineup.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'RecoveryChests.blend'))
print('RECOVERY_CHESTS_COMPLETE '+json.dumps(data))
