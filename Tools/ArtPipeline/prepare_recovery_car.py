"""Inspect/finalize Meshy recovery vehicle with Blender 5.x in background mode."""
import argparse
import json
import math
from pathlib import Path
import sys
import bpy
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Tools/ArtPipeline/Source'
REPORT = ROOT / 'Docs/ArtDirection/2026-09-08/Implementation'
MODEL = ROOT / 'BackpackSurvivor/Assets/BackpackSurvivor/Art/Environment/VNext/Models/RecoveryCar.glb'

def bounds(meshes):
    vertices = [o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
    low = Vector([min(v[i] for v in vertices) for i in range(3)])
    high = Vector([max(v[i] for v in vertices) for i in range(3)])
    return low, high

def aim(obj, target):
    obj.rotation_euler = (Vector(target) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def remap_longitudinal(x):
    # The generated SUV has oversized wheels. Compress the two wheel zones along
    # the length axis by the same factor as height, while giving overhangs/cabin
    # the remaining length. Tires stay circular at ~0.60 m, wheelbase is 2.60 m.
    vertical_scale = 1.6 / 0.654296875
    r = .122 * vertical_scale
    knots = [(-.5, -2.2), (-.450, -1.3-r), (-.206, -1.3+r),
             (.140, 1.3-r), (.384, 1.3+r), (.5, 2.2)]
    for (a, c), (b, d) in zip(knots, knots[1:]):
        if x <= b:
            return c + (x-a)/(b-a)*(d-c)
    return knots[-1][1]

def simplify_materials(meshes):
    # Correct the generated glass's baked white reflections for game readability.
    # The source windows are deliberately broad nearly planar faces, selected in
    # the source frame before any geometry normalization.
    glass = bpy.data.materials.new('RecoveryCar_CharcoalGlass')
    glass.use_nodes = True
    glass.diffuse_color = (.055, .075, .077, 1)
    bsdf = glass.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = glass.diffuse_color
    bsdf.inputs['Metallic'].default_value = .12
    bsdf.inputs['Roughness'].default_value = .79
    count = 0
    for obj in meshes:
        obj.data.materials.append(glass)
        glass_index = len(obj.data.materials) - 1
        for poly in obj.data.polygons:
            c = sum((obj.matrix_world @ obj.data.vertices[i].co for i in poly.vertices), Vector()) / len(poly.vertices)
            n = (obj.matrix_world.to_3x3() @ poly.normal).normalized()
            side = abs(c.y) > .15 and -.185 < c.x < .395 and .063 < c.z < .189 and abs(n.y) > .78
            front = -.229 < c.x < -.157 and .055 < c.z < .189 and abs(n.x) > .6
            rear = .409 < c.x < .454 and .065 < c.z < .19 and abs(n.x) > .55
            if side or front or rear:
                poly.material_index = glass_index
                count += 1
        for mat in obj.data.materials:
            if not mat or not mat.use_nodes or mat == glass:
                continue
            principled = next((n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
            if principled:
                # Runtime prop uses one small albedo, with scalar PBR values.
                # Original high-resolution source maps remain in the refine GLB.
                albedo_node = next((link.from_node for link in principled.inputs['Base Color'].links
                                    if link.from_node.type == 'TEX_IMAGE'), None)
                for node in list(mat.node_tree.nodes):
                    if node != principled and node != albedo_node and node.type != 'OUTPUT_MATERIAL':
                        mat.node_tree.nodes.remove(node)
                for socket_name in ['Roughness', 'Metallic', 'Normal', 'Alpha', 'Emission Color', 'Emission Strength']:
                    for link in list(principled.inputs[socket_name].links):
                        mat.node_tree.links.remove(link)
                principled.inputs['Roughness'].default_value = .82
                principled.inputs['Metallic'].default_value = .1
                principled.inputs['Alpha'].default_value = 1
                principled.inputs['Emission Color'].default_value = (0, 0, 0, 1)
                principled.inputs['Emission Strength'].default_value = 0
                if albedo_node and albedo_node.image:
                    original = albedo_node.image
                    original.scale(512, 512)
                    original.filepath_raw = str(SOURCE / 'RecoveryCar-Albedo512.jpg')
                    original.file_format = 'JPEG'
                    original.save()
                    # Load the new file rather than reusing the source's packed
                    # image; glTF otherwise reuses its original 2048px JPEG bytes.
                    runtime_image = bpy.data.images.load(original.filepath_raw, check_existing=False)
                    runtime_image.name = 'RecoveryCar_Albedo512'
                    runtime_image.pack()
                    albedo_node.image = runtime_image
    return count

def scene_camera(meshes, final):
    scene = bpy.context.scene
    low, high = bounds(meshes)
    center = (high + low) / 2
    size = max(high - low)
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 32
    scene.cycles.use_denoising = True
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('RecoveryCar_StudioWorld')
    scene.world.color = (0.2, 0.2, 0.2)
    scene.world.use_nodes = True
    scene.world.node_tree.nodes['Background'].inputs['Color'].default_value = (0.22, 0.25, 0.24, 1)
    scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0.7
    scene.view_settings.view_transform = 'AgX'
    # Render staging is excluded from the model export and editable source.
    for name, pos, energy, scale in [
        ('Key', (size, -size, size * 1.5), 1100, size),
        ('Fill', (-size, -size * .25, size), 650, size),
        ('Rim', (0, size, size * 1.3), 950, size * .8)]:
        bpy.ops.object.light_add(type='AREA', location=center + Vector(pos))
        light = bpy.context.object
        light.name = 'Render_' + name
        light.data.energy = energy * (size / 4.4) ** 2
        light.data.shape = 'DISK'
        light.data.size = scale
        aim(light, center)
    bpy.ops.object.camera_add(location=center + Vector((size * 1.1, -size * 1.5, size * 1.1)))
    camera = bpy.context.object
    camera.name = 'Render_Camera'
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = size * 1.45
    aim(camera, center)
    scene.camera = camera
    if final:
        bpy.ops.mesh.primitive_plane_add(size=size * 200, location=(center.x, center.y, low.z - 0.015))
        plane = bpy.context.object
        plane.name = 'Render_Ground'
        mat = bpy.data.materials.new('Render_Ground')
        mat.diffuse_color = (0.19, 0.22, 0.21, 1)
        plane.data.materials.append(mat)
    REPORT.mkdir(parents=True, exist_ok=True)
    views = [('front-three-quarter', (1.1, -1.5, 1.1)),
             ('rear-three-quarter', (-1.1, 1.5, 1.1)),
             ('game-angle', (1.15, -1.15, 1.87))]
    for name, offset in views:
        camera.location = center + Vector(offset) * size
        aim(camera, center)
        scene.render.filepath = str(REPORT / f'RecoveryCar-{name}{"" if final else "-inspect"}.png')
        bpy.ops.render.render(write_still=True)

def main():
    argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument('--source', default='RecoveryCar-preview.glb')
    parser.add_argument('--final', action='store_true')
    parser.add_argument('--rotate-z', type=float, default=0)
    args = parser.parse_args(argv)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SOURCE / args.source))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    low, high = bounds(meshes)
    print('SOURCE_BOUNDS', json.dumps({'min': list(low), 'max': list(high), 'size': list(high - low)}))
    print('SOURCE_MESHES', json.dumps([{'name': o.name, 'vertices': len(o.data.vertices),
          'polygons': len(o.data.polygons), 'triangles': sum(len(p.vertices) - 2 for p in o.data.polygons)} for o in meshes]))
    if args.final:
        glass_polygons = simplify_materials(meshes)
        # Bake incoming hierarchy to world geometry before normalizing origin and scale.
        rotation = Matrix.Rotation(math.radians(args.rotate_z), 4, 'Z')
        for o in meshes:
            world = o.matrix_world.copy()
            o.parent = None
            o.data.transform(world)
            for v in o.data.vertices:
                v.co.x = remap_longitudinal(v.co.x)
            o.data.transform(rotation)
            o.matrix_world = Matrix.Identity(4)
        bpy.context.view_layer.update()
        low, high = bounds(meshes)
        # Blender -Y is the intended vehicle forward; glTF exporter turns this into +Z.
        scale = Vector((1.9 / (high.x - low.x), 4.4 / (high.y - low.y), 1.6 / (high.z - low.z)))
        center_bottom = Vector(((low.x + high.x) / 2, (low.y + high.y) / 2, low.z))
        for index, obj in enumerate(meshes):
            obj.name = 'RecoveryCar_Visual' if len(meshes) == 1 else f'RecoveryCar_Part_{index:02d}'
            for v in obj.data.vertices:
                v.co = (v.co - center_bottom) * scale
            # Keep the original authored split normals and UVs; no destructive remesh.
            obj.data.update()
        bpy.context.view_layer.update()
        for index, mat in enumerate(list(bpy.data.materials)):
            mat.name = 'RecoveryCar_Glass' if 'Glass' in mat.name else 'RecoveryCar_Paint'
            mat.use_backface_culling = True
        for image in list(bpy.data.images):
            if image.users == 0:
                bpy.data.images.remove(image)
        # Remove camera/empty remnants imported from the service before saving the editable master.
        for obj in list(bpy.context.scene.objects):
            if obj.type != 'MESH':
                bpy.data.objects.remove(obj, do_unlink=True)
        bpy.context.scene.unit_settings.system = 'METRIC'
        bpy.context.scene.unit_settings.scale_length = 1.0
        for image in bpy.data.images:
            if image.source == 'FILE' and not image.packed_file:
                image.pack()
        SOURCE.mkdir(parents=True, exist_ok=True)
        MODEL.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE / 'RecoveryCar.blend'))
        bpy.ops.object.select_all(action='DESELECT')
        for obj in meshes:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = meshes[0]
        bpy.ops.export_scene.gltf(filepath=str(MODEL), export_format='GLB', use_selection=True,
                                 export_yup=True, export_apply=True, export_cameras=False, export_lights=False,
                                 export_image_format='JPEG', export_jpeg_quality=82,
                                 export_unused_images=False, export_unused_textures=False)
        low, high = bounds(meshes)
        manifest_path = SOURCE / 'meshy-car-manifest.json'
        manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
        manifest['blender_processing'] = {
            'blender_version': bpy.app.version_string, 'source': args.source,
            'rotation_z_degrees': args.rotate_z, 'dimensions_metres_xyz_blender': list(high - low),
            'proportion_correction': 'Piecewise longitudinal correction preserves circular 0.60m tires at 1.6m total height and 2.6m wheelbase',
            'glass_polygons_reassigned': glass_polygons,
            'material_correction': 'Charcoal glass removes baked bright reflection blobs; paint roughness 0.82 and metallic 0.10',
            'runtime_texture_policy': 'One 512x512 JPEG albedo, no normal/metallic/roughness/AO texture; solid-color glass',
            'file_bytes': MODEL.stat().st_size,
            'dimensions_metres_xyz_gltf_unity': [high.x - low.x, high.z - low.z, high.y - low.y],
            'origin': 'Bottom center', 'forward_gltf_unity': '+Z', 'forward_blender': '-Y',
            'triangle_count': sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes),
            'mesh_count': len(meshes), 'material_count': len(bpy.data.materials),
            'image_count': len(bpy.data.images), 'textures_packed': True,
            'model_output': str(MODEL.relative_to(ROOT)).replace('\\', '/'),
            'source_output': 'Tools/ArtPipeline/Source/RecoveryCar.blend'}
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
        print('FINAL_STATS', json.dumps(manifest['blender_processing']))
        # Render the exported GLB after a clean re-import; catches missing texture
        # links or unsupported node effects that would not survive game import.
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.gltf(filepath=str(MODEL))
        meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        low, high = bounds(meshes)
        manifest['roundtrip_verification'] = {
            'file': str(MODEL.relative_to(ROOT)).replace('\\', '/'),
            'dimensions_metres_xyz_blender': list(high-low),
            'bounds_min_xyz_blender': list(low), 'bounds_max_xyz_blender': list(high),
            'triangle_count': sum(sum(len(p.vertices)-2 for p in o.data.polygons) for o in meshes),
            'material_count': len(bpy.data.materials), 'embedded_image_count': len(bpy.data.images),
            'embedded_images': [{'name': i.name, 'size': list(i.size), 'packed': bool(i.packed_file)} for i in bpy.data.images],
            'missing_images': [i.name for i in bpy.data.images if not i.packed_file and not i.has_data],
            'rendered_from_exported_glb': True}
        manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')
        print('ROUNDTRIP_STATS', json.dumps(manifest['roundtrip_verification']))
    scene_camera(meshes, args.final)

if __name__ == '__main__':
    main()
