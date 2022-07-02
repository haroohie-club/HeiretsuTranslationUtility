import bpy
from mathutils import Vector
import json
import os
from re import U
import sys

def construct_materials(sge):
    print('Constructing materials...')
    materials = []
    for sge_material in sge['SgeMaterials']:
        material = bpy.data.materials.new(sge_material['Name'])
        material.use_nodes = True
        bsdf = material.node_tree.nodes['Principled BSDF']
        if sge_material['TexturePath'] is not None and len(sge_material['TexturePath']) > 0:
            img = bpy.data.images.load(sge_material['TexturePath'])
            texture = material.node_tree.nodes.new('ShaderNodeTexImage')
            texture.image = img
            material.node_tree.links.new(texture.outputs['Color'], bsdf.inputs['Base Color'])
            material.node_tree.links.new(texture.outputs['Alpha'], bsdf.inputs['Alpha'])
            material.blend_method = 'CLIP'
        materials.append(material)
    return materials

def construct_armature(sge):
    print('Constructing armature...')
    bpy.ops.object.add(
        type='ARMATURE',
        enter_editmode=True,
        location=(json_vector_to_vector(sge['SgeBones'][0]['Position']))
    )
    obj = bpy.context.object
    obj.name = sge['Name']
    armature = obj.data
    armature.name = sge['Name'] + "_Armature"
    for bone in sge['SgeBones']:
        new_bone = armature.edit_bones.new("Bone" + str(bone['Address']))
        new_bone.head = json_vector_to_vector(bone['Position'])
        new_bone.tail = new_bone.head + Vector((0, 0.1, 0))
    for bone in armature.edit_bones:
        i = 0
        for potential_child in sge['SgeBones']:
            if ("Bone" + str(potential_child['ParentAddress']) == bone.name):
                armature.edit_bones[i].parent = bone
                armature.edit_bones[i].tail = bone.head
            i += 1
    return obj

def construct_mesh(sge, submesh, materials, meshNum):
    print('Constructing mesh...')
    mesh = bpy.data.meshes.new(sge['Name'] + "_Mesh" + str(meshNum))
    mesh.validate(verbose=True)
    mesh.use_auto_smooth = True
    obj = bpy.data.objects.new(sge['Name'] + "_Mesh" + str(meshNum), mesh)
    for material in materials:
        obj.data.materials.append(material)

    vertices = []
    normals = []
    uvcoords = []
    colors = []
    for sge_vertex in submesh['SubmeshVertices']:
        vertices.append(json_vector_to_vector(sge_vertex['Position']))
        normals.append(json_vector_to_vector(sge_vertex['Normal']))
        uvcoords.append(json_vector2_to_vector2(sge_vertex['UVCoords']))
        colors.append(sge_vertex['Color'])
    faces = []
    for sge_face in submesh['SubmeshFaces']:
        faces.append((sge_face['Polygon'][0], sge_face['Polygon'][1], sge_face['Polygon'][2])) # Faces are inverted so this is the correct order

    mesh.from_pydata(vertices, [], faces) # Edges are autocalculated by blender so we can pass a blank array
    mesh.normals_split_custom_set_from_vertices(normals)

    uvlayer = mesh.uv_layers.new()
    color_layer = mesh.vertex_colors.new()
    for face in mesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            color_layer.data[vert_idx].color = (colors[vert_idx]['R'], colors[vert_idx]['G'], colors[vert_idx]['B'], colors[vert_idx]['A'])
            uvlayer.data[loop_idx].uv = uvcoords[vert_idx]
    
    for i in range(len(mesh.polygons)):
        if submesh['SubmeshFaces'][i]['Material'] is not None:
            mesh.polygons[i].material_index = submesh['SubmeshFaces'][i]['Material']['Index']
    mesh.update()
    
    for bone in sge['SgeBones']:
        bone_vertex_group = obj.vertex_groups.new(name='Bone' + str(bone['Address']))
        for attached_vertex in bone['VertexGroup']:
            attached_vertex_split = attached_vertex.split(',')
            (attached_vertex_mesh, attached_vertex_index) = (int(attached_vertex_split[0]), int(attached_vertex_split[1]))
            if attached_vertex_mesh == meshNum:
                bone_vertex_group.add([attached_vertex_index], bone['VertexGroup'][attached_vertex], 'ADD')
    return obj

def json_vector_to_vector(json_vector):
    # Flip Z & Y for blender
    return Vector((float(json_vector['X']), float(json_vector['Z']), float(json_vector['Y'])))

def json_vector2_to_vector2(json_vector):
    return Vector((float(json_vector['X']), float(json_vector['Y'])))

def main(filename):
    f = open(filename)
    sge = json.load(f)
    materials = construct_materials(sge)
    armature = construct_armature(sge)

    sge_collection = bpy.data.collections.new('sge_collection')
    bpy.context.scene.collection.children.link(sge_collection)

    i = 0
    for submesh in sge['SgeSubmeshes']:
        mesh = construct_mesh(sge, submesh, materials, i)
        i += 1

        sge_collection.objects.link(mesh)

        mesh.parent = armature
        modifier = mesh.modifiers.new(type='ARMATURE', name='Armature')
        modifier.object = armature
    
    return {'FINISHED'}

if __name__ == '__main__':
    # Clean scene
    for o in bpy.context.scene.objects:
        o.select_set(True)
    bpy.ops.object.delete()

    input_file = sys.argv[-2]
    output_format = sys.argv[-1]

    main(input_file)
    output_file = os.path.join(os.path.dirname(input_file), os.path.splitext(os.path.basename(input_file))[0])

    if output_format.lower() == 'gltf':
        output_file += ".glb"
        bpy.ops.export_scene.gltf(filepath=os.path.abspath(output_file), check_existing=False, export_format="GLB")
    elif output_format.lower() == 'fbx':
        output_file += '.fbx'
        bpy.ops.export_scene.fbx(filepath=os.path.abspath(output_file), check_existing=False, path_mode='COPY', batch_mode='OFF', embed_textures=True)
    else:
        output_file += '.blend'
        bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(output_file), check_existing=False)