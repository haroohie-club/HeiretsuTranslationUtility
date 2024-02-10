import bpy
from mathutils import Vector, Matrix, Quaternion, Euler
import math
import json
import os
from re import U
import sys
from random import randrange

model_scale = 25.4

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
    bones_list = []
    for bone in sge['SgeBones']:
        bone_name = f"Bone{bone['Address']}"
        new_bone = armature.edit_bones.new(bone_name)
        new_bone.head = json_vector_to_vector(bone['Position']) * model_scale
        new_bone.tail = new_bone.head + Vector((0, 0.1, 0))
        bones_list.append(bone_name)
    for bone in armature.edit_bones:
        i = 0
        for potential_child in sge['SgeBones']:
            if (f"Bone{potential_child['ParentAddress']}" == bone.name):
                if armature.edit_bones[i].head == bone.head:
                    armature.edit_bones[i].head += Vector((0, 0.1, 0))
                armature.edit_bones[i].parent = bone
                armature.edit_bones[i].tail = bone.head
            i += 1
    return (obj, bones_list)

def construct_mesh(sge, submesh, materials , mesh_num):
    print('Constructing mesh...')
    mesh = bpy.data.meshes.new(sge['Name'] + "_Mesh" + str(mesh_num))
    mesh.validate(verbose=True)
    mesh.use_auto_smooth = True
    obj = bpy.data.objects.new(sge['Name'] + "_Mesh" + str(mesh_num), mesh)
    for material in materials:
        obj.data.materials.append(material)

    vertices = []
    normals = []
    uvcoords = []
    colors = []
    for sge_vertex in submesh['SubmeshVertices']:
        vertices.append(json_vector_to_vector(sge_vertex['Position']) * model_scale)
        normals.append(json_vector_to_vector(sge_vertex['Normal']))
        uvcoords.append(json_vector2_to_vector2(sge_vertex['UVCoords']))
        colors.append(sge_vertex['Color'])
    faces = []
    for sge_face in submesh['SubmeshFaces']:
        faces.append((sge_face['Polygon'][0], sge_face['Polygon'][1], sge_face['Polygon'][2])) # Faces are inverted so this is the correct order

    mesh.from_pydata(vertices, [], faces) # Edges are autocalculated by blender so we can pass a blank array
    mesh.normals_split_custom_set_from_vertices(normals)

    uvlayer = mesh.uv_layers.new()
    uvlayer_name = uvlayer.name
    color_layer = mesh.vertex_colors.new()
    # Creating the color layer has invalidated the reference to the uv layer, so get it again.
    uvlayer = mesh.uv_layers[uvlayer_name]
    for face in mesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            color_layer.data[vert_idx].color = (colors[vert_idx]['R'], colors[vert_idx]['G'], colors[vert_idx]['B'], colors[vert_idx]['A'])
            uvlayer.uv[loop_idx].vector = uvcoords[vert_idx]
    
    for i in range(len(mesh.polygons)):
        if submesh['SubmeshFaces'][i]['Material'] is not None:
            mesh.polygons[i].material_index = submesh['SubmeshFaces'][i]['Material']['Index']
    mesh.update()
    
    for bone in sge['SgeBones']:
        bone_vertex_group = obj.vertex_groups.new(name='Bone' + str(bone['Address']))
        for attached_vertex in bone['VertexGroup']:
            attached_vertex_split = attached_vertex.split(',')
            (attached_vertex_mesh, attached_vertex_index) = (int(attached_vertex_split[0]), int(attached_vertex_split[1]))
            if attached_vertex_mesh == mesh_num:
                bone_vertex_group.add([attached_vertex_index], bone['VertexGroup'][attached_vertex], 'ADD')
    return obj

def construct_animation(sge, anim, bones_list : list, anim_num):
    print(f'Creating animation {anim_num}...')
    bpy.ops.object.mode_set(mode='POSE')
    pose = bpy.context.object.pose
    bpy.ops.pose.group_add()
    bone_idx = 0
    # for armature_bone in bones_list:
    #     if bone_idx < 1:
    #         bone_idx += 1
    #         continue
    #     bone = pose.bones[armature_bone]
    #     trans_vec = Vector((0, 0, 0))
    #     scale_vec = Vector((1, 1, 1))
    #     rot_euler = Euler(json_vector_to_vector(sge['SgeBones'][bone_idx]['Unknown00']))
    #     matrix = Matrix.LocRotScale(trans_vec, rot_euler, scale_vec)
    #     bone.matrix = bone.matrix @ matrix
    #     bone.keyframe_insert(
    #         data_path=f'location',
    #         frame=0,
    #         group=f'Animation{anim_num}'
    #     )
    #     bone.keyframe_insert(
    #         data_path=f'rotation_quaternion',
    #         frame=0,
    #         group=f'Animation{anim_num}'
    #     )
    #     bone.keyframe_insert(
    #         data_path=f'scale',
    #         frame=0,
    #         group=f'Animation{anim_num}'
    #     )
    #     bone_idx += 1
    
    i = 0
    for keyframe_idx in anim['UsedKeyframes']:
        keyframe = sge['KeyframeDefinitions'][keyframe_idx]
        bone_idx = 0
        for armature_bone in bones_list:
            if bone_idx < 1:
                bone_idx += 1
                continue
            bone = pose.bones[armature_bone]
            bone_keyframe = anim['BoneTable'][bone_idx - 1]['Keyframes']
            trans_vec = json_vector_to_vector(sge['TranslateDataEntries'][bone_keyframe[i]['TranslateIndex']]) * model_scale
            rot_quaternion = json_quaternion_to_quaternion(sge['RotateDataEntries'][bone_keyframe[i]['RotateIndex']])
            scale_vec = json_vector_to_vector(sge['ScaleDataEntries'][bone_keyframe[i]['ScaleIndex']])
            matrix = Matrix.LocRotScale(trans_vec, rot_quaternion, scale_vec)
            if i == 0:
                bone.matrix = bone.matrix @ matrix
                prev_mat = bone.matrix
            else:
                bone.matrix = prev_mat @ matrix
                prev_mat = bone.matrix

            bone.keyframe_insert(
                data_path=f'location',
                frame=keyframe['EndFrame'] - keyframe['NumFrames'],
                group=f'Animation{anim_num}'
            )
            bone.keyframe_insert(
                data_path=f'rotation_quaternion',
                frame=keyframe['EndFrame'] - keyframe['NumFrames'],
                group=f'Animation{anim_num}'
            )
            bone.keyframe_insert(
                data_path=f'scale',
                frame=keyframe['EndFrame'] - keyframe['NumFrames'],
                group=f'Animation{anim_num}'
            )
            bone_idx += 1
        i += 1

def json_vector_to_vector(json_vector):
    return Vector((float(json_vector['X']), float(json_vector['Y']), float(json_vector['Z'])))

def json_vector2_to_vector2(json_vector):
    return (float(json_vector['X']), float(json_vector['Y']))

def json_quaternion_to_quaternion(json_quaternion):
    return Quaternion((float(json_quaternion['W']), float(json_quaternion['X']), float(json_quaternion['Y']), float(json_quaternion['Z'])))

def main(filename, anim_number):
    f = open(filename)
    sge = json.load(f)
    materials = construct_materials(sge)
    (armature, bones_list) = construct_armature(sge)

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
    
    if i >= 0:
        i = 0
        for anim in sge['SgeAnimations']:
            if i < anim_number:
                i += 1
                continue
            construct_animation(sge, anim, bones_list, i)
            break
            # i += 1
    
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.object.matrix_world = bpy.context.object.matrix_world @ Matrix.Rotation(math.radians(90), 4, 'X')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.transform.mirror(constraint_axis=(False, True, False), orient_type='GLOBAL')
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='POSE')

    return {'FINISHED'}

if __name__ == '__main__':
    # Clean scene
    for o in bpy.context.scene.objects:
        o.select_set(True)
    bpy.ops.object.delete()

    input_file = sys.argv[-3]
    output_format = sys.argv[-2]
    anim_number = int(sys.argv[-1])

    main(input_file, anim_number)
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