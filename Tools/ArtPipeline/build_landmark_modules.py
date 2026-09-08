"""Build two lightweight Recovery Yard landmarks; never rewrites the original kit.

Run: E:/Blender/blender.exe --background --factory-startup --python this_file.py
Units: metres; Blender Z up, -Y front -> Unity Y up, +Z front.
The 20 ft container is long along X; its cargo doors face +X.
Uses the existing ENV_Palette images read-only, one exported material per model.
"""
import bpy
import bmesh
import math
import json
import struct
import hashlib
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
MODELS = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Models'
TEXTURES = MODELS.parent / 'Textures'
SOURCE = ROOT / 'Tools/ArtPipeline/Source'
REPORT = ROOT / 'Docs/ArtDirection/2026-09-08/FullMap'
for folder in (MODELS, SOURCE, REPORT):
    folder.mkdir(parents=True, exist_ok=True)
PALETTE = [
    ('concrete', 'ENV_Concrete', '96958A', 0, .91),
    ('olive', 'ENV_OlivePaint', '687265', .38, .65),
    ('charcoal', 'ENV_Charcoal', '303936', .35, .66),
    ('orange', 'ENV_FadedAmber', 'B6843B', .15, .77),
    ('cyan', 'ENV_CyanIndicator', '5EC5CC', .05, .31),
    ('metal', 'ENV_DarkSteel', '566462', .80, .44),
    ('rubber', 'ENV_Rubber', '232A29', 0, .95),
    ('label', 'ENV_Stencil', 'B8B8A6', .05, .84),
]
PALETTE_INDEX = {row[1]: i for i, row in enumerate(PALETTE)}
palette_before = {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
                  for p in TEXTURES.glob('ENV_Palette_*.png')}
assert len(palette_before) == 4, 'The four existing palette PNGs are required.'
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
scene = bpy.context.scene
scene.unit_settings.system = 'METRIC'
scene.unit_settings.scale_length = 1
bpy.context.preferences.filepaths.save_version = 0


def linear(c):
    return c / 12.92 if c <= .04045 else ((c + .055) / 1.055) ** 2.4


def material(name, hx, metal=0, rough=.8, emit=0):
    rgb = tuple(linear(int(hx[i:i+2], 16) / 255) for i in (0, 2, 4))
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.diffuse_color = (*rgb, 1)
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next((n for n in nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if bsdf is None:
        bsdf = nodes.new('ShaderNodeBsdfPrincipled')
        output = nodes.new('ShaderNodeOutputMaterial')
        links.new(bsdf.outputs['BSDF'], output.inputs['Surface'])
    bsdf.inputs['Base Color'].default_value = (*rgb, 1)
    bsdf.inputs['Metallic'].default_value = metal
    bsdf.inputs['Roughness'].default_value = rough
    if emit:
        bsdf.inputs['Emission Color'].default_value = (*rgb, 1)
        bsdf.inputs['Emission Strength'].default_value = emit
    return mat


M = {key: material(name, hx, metal, rough, 1.1 if key == 'cyan' else 0)
     for key, name, hx, metal, rough in PALETTE}
ATLAS = material('ENV_AtlasSurface', 'FFFFFF', 1, 1, 1.1)
nodes, links = ATLAS.node_tree.nodes, ATLAS.node_tree.links
bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')


def palette_texture(suffix, space):
    img = bpy.data.images.load(str(TEXTURES / ('ENV_Palette_' + suffix + '.png')))
    img.colorspace_settings.name = space
    img.pack()
    node = nodes.new('ShaderNodeTexImage')
    node.image = img
    node.interpolation = 'Closest'
    node.extension = 'EXTEND'
    return node


base = palette_texture('BaseColor', 'sRGB')
mr = palette_texture('MetallicRoughness', 'Non-Color')
emission = palette_texture('Emission', 'sRGB')
links.new(base.outputs['Color'], bsdf.inputs['Base Color'])
links.new(emission.outputs['Color'], bsdf.inputs['Emission Color'])
split = nodes.new('ShaderNodeSeparateColor')
split.mode = 'RGB'
links.new(mr.outputs['Color'], split.inputs['Color'])
links.new(split.outputs['Green'], bsdf.inputs['Roughness'])
links.new(split.outputs['Blue'], bsdf.inputs['Metallic'])
parts = []


def add(obj, name, mat):
    obj.name = name
    obj.data.materials.append(M[mat])
    parts.append(obj)
    return obj


def bevel(obj, amount):
    if not amount:
        return
    bpy.context.view_layer.objects.active = obj
    mod = obj.modifiers.new('One-segment edge bevel', 'BEVEL')
    mod.width, mod.segments = amount, 1
    bpy.ops.object.modifier_apply(modifier=mod.name)
    mod = obj.modifiers.new('Weighted corner normals', 'WEIGHTED_NORMAL')
    mod.keep_sharp = True
    bpy.ops.object.modifier_apply(modifier=mod.name)


def box(name, p, size, mat, edge=0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=p)
    obj = add(bpy.context.object, name, mat)
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel(obj, edge)
    return obj


def mesh(name, verts, faces, mat):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    return add(obj, name, mat)


def beam(name, a, b, width, mat):
    a, b = Vector(a), Vector(b)
    obj = box(name, (a+b)*.5, (width, width, (b-a).length), mat)
    obj.rotation_euler = (b-a).to_track_quat('Z', 'Y').to_euler()
    return obj


def cylinder(name, p, radius, depth, mat, axis=None):
    bpy.ops.mesh.primitive_cylinder_add(vertices=6, radius=radius, depth=depth, location=p)
    obj = add(bpy.context.object, name, mat)
    if axis:
        obj.rotation_euler = Vector(axis).to_track_quat('Z', 'Y').to_euler()
    return obj


def export_atlas(source, name):
    obj = source.copy()
    obj.data = source.data.copy()
    obj.name = name + '_Export'
    scene.collection.objects.link(obj)
    while obj.data.uv_layers:
        obj.data.uv_layers.remove(obj.data.uv_layers[0])
    uv = obj.data.uv_layers.new(name='PaletteUV')
    slot_names = [m.name for m in obj.data.materials]
    for poly in obj.data.polygons:
        idx = PALETTE_INDEX[slot_names[poly.material_index]]
        for loop in poly.loop_indices:
            uv.data[loop].uv = ((idx+.5)/8, .5)
        poly.material_index = 0
    obj.data.materials.clear()
    obj.data.materials.append(ATLAS)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = MODELS / (name + '.glb')
    bpy.ops.export_scene.gltf(filepath=str(path), export_format='GLB', use_selection=True,
        export_yup=True, export_extras=True, export_apply=True, export_cameras=False,
        export_lights=False, export_texcoords=True)
    raw = path.read_bytes()
    jlen = struct.unpack_from('<I', raw, 12)[0]
    doc = json.loads(raw[20:20+jlen])
    for sampler in doc.get('samplers', []):
        sampler.update(magFilter=9728, minFilter=9728, wrapS=33071, wrapT=33071)
    assert len(doc['meshes']) == len(doc['materials']) == 1
    assert len(doc['meshes'][0]['primitives']) == 1
    assert doc['materials'][0]['name'] == 'ENV_AtlasSurface'
    encoded = json.dumps(doc, separators=(',', ':')).encode()
    encoded += b' ' * (-len(encoded) % 4)
    remainder = raw[20+jlen:]
    path.write_bytes(struct.pack('<4sII', b'glTF', 2, 20+len(encoded)+len(remainder))
        + struct.pack('<I4s', len(encoded), b'JSON') + encoded + remainder)
    data = obj.data
    bpy.data.objects.remove(obj, do_unlink=True)
    bpy.data.meshes.remove(data)


def finish(name):
    global parts
    bpy.ops.object.select_all(action='DESELECT')
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    tri = obj.modifiers.new('Export triangles', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=tri.name)
    obj['units'] = 'metres'
    obj['unity_front'] = '+Z (Blender -Y)'
    obj['pivot'] = 'bottom centre'
    obj['art_direction'] = 'Quarantine / Recovery Yard'
    export_atlas(obj, name)
    parts = []
    return obj


# 20 FT CONTAINER: a quiet rectangular silhouette with actual shallow corrugation.
box('Container inner shell', (0, 0, 1.295), (5.90, 2.26, 2.38), 'olive', .025)
for side in (-1, 1):
    # Continuous trapezoidal sheet uses 150 tris per side, with no duplicate boxes.
    profile = []
    count = 15
    pitch = 5.82 / count
    for i in range(count):
        x = -2.91 + i*pitch
        profile.extend([(x, side*1.145), (x+pitch*.24, side*1.145),
                        (x+pitch*.38, side*1.205), (x+pitch*.77, side*1.205),
                        (x+pitch*.91, side*1.145)])
    profile.append((2.91, side*1.145))
    verts = [(x, y, z) for x, y in profile for z in (.14, 2.45)]
    faces = [(2*i, 2*i+2, 2*i+3, 2*i+1) for i in range(len(profile)-1)]
    if side == 1:
        faces = [tuple(reversed(f)) for f in faces]
    mesh('Pressed side sheet', verts, faces, 'olive')
    for z in (.065, 2.525):
        box('Long edge rail', (0, side*1.16, z), (6.06, .12, .13), 'charcoal', .008)
    for x in (-2.98, 2.98):
        box('Corner post', (x, side*1.17, 1.295), (.10, .10, 2.59), 'charcoal', .008)
        for z in (.14, 2.41):
            box('Lift fitting inset', (x, side*1.216, z), (.049, .006, .076), 'rubber')
    # Small geometric identity marks; no texture lettering and no extra materials.
    box('Side inspection plate', (-2.38, side*1.211, 1.99), (.58, .012, .25), 'charcoal')
    box('Inventory stripe', (-2.55, side*1.214, 2.01), (.09, .009, .12), 'label')
    box('Inventory bar', (-2.30, side*1.214, 2.04), (.27, .009, .035), 'label')
    box('Amber recognition band', (2.65, side*1.213, 1.285), (.12, .012, 2.06), 'orange')
for x in (-2.98, 2.98):
    for z in (.065, 2.525):
        box('End rail', (x, 0, z), (.10, 2.30, .13), 'charcoal', .008)
for i in range(12):
    x = -2.66 + i*(5.32/11)
    box('Roof pressed rib', (x, 0, 2.501), (.075, 2.15, .035), 'olive')
# End doors fit inside the ISO exterior bounds.
for side in (-1, 1):
    box('Cargo door', (2.972, side*.567, 1.294), (.049, 1.10, 2.30), 'olive', .008)
    for y in (side*.29, side*.85):
        cylinder('Door locking rod', (3.010, y, 1.285), .014, 2.13, 'metal')
        for z in (.34, 2.21):
            box('Rod bracket', (3.014, y, z), (.025, .087, .058), 'charcoal')
        box('Locking handle', (3.017, y-.052, 1.08), (.02, .155, .037), 'metal')
    for z in (.49, 2.04):
        box('Door hinge', (3.015, side*1.035, z), (.026, .12, .075), 'metal')
container = finish('ShippingContainer')

# OPEN CHECKPOINT: only the four feet/columns need ground-level collision.
for x in (-3.50, 3.50):
    for y in (-1.10, 1.10):
        box('Concrete post foot', (x, y, .12), (.52, .52, .24), 'concrete', .025)
        box('Steel column', (x, y, 2.04), (.28, .28, 3.72), 'charcoal', .012)
        box('Amber lower sleeve', (x, y, .83), (.287, .287, .28), 'orange')
        for inward in (-1, 1):
            if inward == (-1 if x > 0 else 1):
                beam('Short structural knee', (x, y, 3.30),
                     (x+inward*.52, y, 3.89), .10, 'metal')
for y in (-1.10, 1.10):
    box('Span girder', (0, y, 3.91), (7.48, .23, .32), 'charcoal', .012)
for x in (-3.50, 0, 3.50):
    box('Cross rafter', (x, 0, 4.02), (.17, 2.84, .20), 'metal')
box('Thin canopy roof', (0, 0, 4.24), (8, 3, .32), 'olive', .04)
for y in (-1.487, 1.487):
    box('Perimeter fascia', (0, y, 4.23), (7.90, .026, .19), 'charcoal')
    box('Small amber fascia marker', (-2.95, y*1.005, 4.23), (.77, .012, .12), 'orange')
for x in (-3.82, -2.28, -.76, .76, 2.28, 3.82):
    box('Roof seam cap', (x, 0, 4.398), (.035, 2.75, .004), 'metal')
for x in (-2.5, 2.5):
    box('Under canopy light casing', (x, 0, 4.036), (1.1, .18, .085), 'charcoal')
    box('Under canopy cyan strip', (x, 0, 3.991), (.88, .12, .008), 'cyan')
canopy = finish('CheckpointCanopy')
assets = [container, canopy]
manifest = []
for obj in assets:
    corners = [obj.matrix_world @ Vector(v) for v in obj.bound_box]
    lo = [min(p[i] for p in corners) for i in range(3)]
    hi = [max(p[i] for p in corners) for i in range(3)]
    path = MODELS / (obj.name + '.glb')
    entry = dict(name=obj.name, triangles=len(obj.data.polygons), vertices=len(obj.data.vertices),
        unityDimensions=[round(hi[0]-lo[0], 5), round(hi[2]-lo[2], 5), round(hi[1]-lo[1], 5)],
        blenderBounds={'min':lo, 'max':hi}, materials=['ENV_AtlasSurface'], primitives=1,
        glbBytes=path.stat().st_size, rootScale=[1,1,1], pivot='bottom centre', unityFront='+Z',
        file=str(path.relative_to(ROOT)).replace('\\', '/'))
    if obj == container:
        entry['simpleCollidersUnity'] = [{'center':[0,1.295,0], 'size':[6.06,2.59,2.44]}]
        entry['doorDirection'] = '+X'
    else:
        entry['simpleCollidersUnity'] = [{'center':[x,2.01,z], 'size':[.52,4.02,.52]}
            for x in (-3.50,3.50) for z in (-1.10,1.10)]
        entry['placement'] = 'Perimeter only; four column boxes, never a solid full-canopy box.'
    manifest.append(entry)
assert sum(a['triangles'] for a in manifest) < 3000
assert sum(a['glbBytes'] for a in manifest) < 200*1024
assert palette_before == {p.name:hashlib.sha256(p.read_bytes()).hexdigest()
                          for p in TEXTURES.glob('ENV_Palette_*.png')}
report = dict(generatedBy='build_landmark_modules.py / Blender ' + bpy.app.version_string,
    assets=manifest, totalTriangles=sum(a['triangles'] for a in manifest),
    totalGlbBytes=sum(a['glbBytes'] for a in manifest), paletteSha256=palette_before)
(REPORT/'landmark-manifest.json').write_text(json.dumps(report, indent=2), encoding='utf-8')

# Offline visual QA uses true exported GLBs, checking both geometry and palette UVs.
for obj in assets:
    obj.hide_render = True
floor = box('Preview floor', (0,0,-.055), (120,120,.10), 'concrete')
floor.data.materials.clear()
floor.data.materials.append(material('PreviewOnly_Floor', '535E60'))
parts = []
world = bpy.data.worlds.new('Soft industrial sky')
world.use_nodes = True
bg = next((n for n in world.node_tree.nodes if n.type == 'BACKGROUND'), None)
if bg is None:
    bg = world.node_tree.nodes.new('ShaderNodeBackground')
    output = world.node_tree.nodes.new('ShaderNodeOutputWorld')
    world.node_tree.links.new(bg.outputs[0], output.inputs[0])
bg.inputs['Color'].default_value = (.30,.37,.42,1)
bg.inputs['Strength'].default_value = .65
scene.world = world
for name, p, energy, size in [('Key',(3,-6,9),2000,7),
                              ('Fill',(-5,-2,6),1000,6), ('Rim',(3,6,8),1600,6)]:
    data = bpy.data.lights.new(name, 'AREA')
    data.energy, data.size = energy, size
    obj = bpy.data.objects.new(name, data)
    scene.collection.objects.link(obj)
    obj.location = p
    obj.rotation_euler = (Vector((0,0,1.8))-obj.location).to_track_quat('-Z','Y').to_euler()
cam_data = bpy.data.cameras.new('Landmark verification camera')
cam = bpy.data.objects.new('Landmark verification camera', cam_data)
scene.collection.objects.link(cam)
scene.camera = cam
cam_data.type = 'ORTHO'
scene.render.engine = 'CYCLES'
scene.cycles.samples = 40
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1100, 800
scene.render.resolution_percentage = 100
scene.view_settings.view_transform = 'AgX'
scene.view_settings.look = 'AgX - Medium High Contrast'
scene.render.image_settings.file_format = 'PNG'
for source in assets:
    prior = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(MODELS/(source.name+'.glb')))
    imported = list(set(bpy.data.objects) - prior)
    corners = [obj.matrix_world @ Vector(v) for obj in imported if obj.type == 'MESH'
               for v in obj.bound_box]
    target = sum(corners, Vector()) / len(corners)
    cam.location = target + Vector((8,-10,7))
    cam.rotation_euler = (target-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.view_layer.update()
    view = cam.matrix_world.inverted()
    projected = [view @ p for p in corners]
    span_x = max(p.x for p in projected)-min(p.x for p in projected)
    span_y = max(p.y for p in projected)-min(p.y for p in projected)
    cam_data.ortho_scale = max(span_x, span_y*1100/800)*1.22
    scene.render.filepath = str(REPORT/(source.name+'.png'))
    bpy.ops.render.render(write_still=True)
    for obj in imported:
        bpy.data.objects.remove(obj, do_unlink=True)
for obj in assets:
    obj.hide_render = False
container.location.x = -4.2
canopy.location.x = 4.4
cam.location = (13,-20,14)
cam.rotation_euler = (Vector((0,0,2))-cam.location).to_track_quat('-Z','Y').to_euler()
cam_data.ortho_scale = 20
bpy.ops.object.select_all(action='DESELECT')
container.select_set(True)
bpy.context.view_layer.objects.active = container
bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'Quarantine_Landmarks.blend'))
print('LANDMARKS_COMPLETE ' + json.dumps(report))
