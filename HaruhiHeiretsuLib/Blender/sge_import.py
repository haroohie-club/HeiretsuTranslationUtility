import bpy
from mathutils import Vector, Matrix, Quaternion
import math
import json
import os
import sys

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
        location=(json_vector_to_vector(sge['SgeBones'][0]['HeadPosition']))
    )
    obj = bpy.context.object
    obj.name = sge['Name']
    armature = obj.data
    armature.name = sge['Name'] + "_Armature"
    bones_list = []
    for bone in sge['SgeBones']:
        bone_name = f"Bone{bone['Address']}"
        new_bone = armature.edit_bones.new(bone_name)
        new_bone.head = (json_vector_to_vector(bone['HeadPosition'])) * model_scale
        new_bone.tail = new_bone.head + Vector((0, 1, 0))
        bones_list.append(bone_name)
        # Uncomment to visualize Unknown00
        # new_00_bone = armature.edit_bones.new(f'{bone_name}_00')
        # new_00_bone.head = new_bone.head
        # new_00_bone.tail = new_00_bone.head + json_vector_to_vector(bone['TailOffset']) * model_scale
        # new_00_bone.color.palette = 'THEME02'
    for bone in armature.edit_bones:
        i = 0
        for potential_child in sge['SgeBones']:
            if (f"Bone{potential_child['ParentAddress']}" == bone.name):
                armature.edit_bones[i].parent = bone
            i += 1
    bpy.ops.object.mode_set(mode='OBJECT')
    neck_bone = armature.collections.new('NeckBone')
    face_bone = armature.collections.new('FaceBone')
    chest_bones = armature.collections.new('ChestBones')
    stomach_bone = armature.collections.new('StomachBone')
    right_hand_bone = armature.collections.new('RightHandBone')
    left_hand_bone = armature.collections.new('LeftHandBone')
    unknown0080_group = armature.collections.new('Unknown0080Group')
    unknown0100_group = armature.collections.new('Unknown0100Group')
    right_foot_bone = armature.collections.new('RightFootBone')
    left_foot_bone = armature.collections.new('LeftFootBone')
    eyebrow_bones = armature.collections.new('EybrowBones')
    right_leg_bone = armature.collections.new('RightLegBone')
    left_leg_bone = armature.collections.new('LeftLegBone')
    right_cheek_bone = armature.collections.new('RightCheekBone')
    left_cheek_bone = armature.collections.new('LeftCheekBone')
    for bone in sge['SgeBones']:
        if bone['BodyPart'] == 0x0002:
            neck_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0004:
            face_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0008:
            chest_bones.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0010:
            stomach_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0020:
            right_hand_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0040:
            left_hand_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0080:
            unknown0080_group.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0100:
            unknown0100_group.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0200:
            right_foot_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0400:
            left_foot_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x0800:
            eyebrow_bones.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x1000:
            right_leg_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x2000:
            left_leg_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == 0x4000:
            right_cheek_bone.assign(armature.bones[f"Bone{bone['Address']}"])
        elif bone['BodyPart'] == -32768: # 0x8000 but since it's a short it'll be negative
            left_cheek_bone.assign(armature.bones[f"Bone{bone['Address']}"])

    animation_groups = ["Eyes", "Mouth"]
    u = 0
    for anim_gorup in sge['BoneAnimationGroups']:
        bone_animation_group = armature.collections.new(f'{animation_groups[u] if u < len(animation_groups) else u}AnimationGroup')
        for bone_idx in anim_gorup['BoneIndices']:
            bone_animation_group.assign(armature.bones[f"Bone{sge['SgeBones'][bone_idx]['Address']}"])
        u += 1

    return (obj, bones_list)

def construct_mesh(sge, submesh, materials, group_num, submesh_num):
    print('Constructing mesh...')
    mesh = bpy.data.meshes.new(sge['Name'] + "_Group" + str(group_num) + "_Submesh" + str(submesh_num))
    mesh.validate(verbose=True)
    mesh.use_auto_smooth = True
    obj = bpy.data.objects.new(sge['Name'] + "_Group" + str(group_num) + "_Submesh" + str(submesh_num), mesh)
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
    mesh.normals_split_custom_set([(0, 0, 0) for l in mesh.loops])
    mesh.normals_split_custom_set_from_vertices(normals)

    uvlayer = mesh.uv_layers.new()
    uvlayer_name = uvlayer.name
    color_layer = mesh.color_attributes.new('vertex_colors', 'FLOAT_COLOR', 'POINT')
    # Creating the color layer has invalidated the reference to the uv layer, so get it again.
    uvlayer = mesh.uv_layers[uvlayer_name]
    for face in mesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            color_layer.data[vert_idx].color_srgb = (colors[vert_idx]['R'], colors[vert_idx]['G'], colors[vert_idx]['B'], colors[vert_idx]['A'])
            uvlayer.uv[loop_idx].vector = uvcoords[vert_idx]
    
    for i in range(len(mesh.polygons)):
        if submesh['Material'] is not None:
            mesh.polygons[i].material_index = submesh['Material']['Index']
    mesh.update()
    
    for bone in sge['SgeBones']:
        bone_vertex_group = obj.vertex_groups.new(name='Bone' + str(bone['Address']))
        for attached_vertex in bone['VertexGroup']:
            attached_vertex_split = attached_vertex.split(',')
            (attached_vertex_group, attached_vertex_submesh, attached_vertex_index) = (int(attached_vertex_split[0]), int(attached_vertex_split[1]), int(attached_vertex_split[2]))
            if attached_vertex_group == group_num and attached_vertex_submesh == submesh_num:
                bone_vertex_group.add([attached_vertex_index], bone['VertexGroup'][attached_vertex], 'ADD')

    outlineData = next((o for o in sge["OutlineDataTable"] if o["Offset"] == submesh["OutlineAddress"]), None)
    if outlineData is not None:
        obj["OutlineWeight"] = outlineData["Weight"]
        obj["OutlineColor"] = outlineData["Color"]
    return obj

def construct_animation(sge, anim, bones_list : list, anim_num):
    print(f'Creating animation {anim_num}...')
    pose = bpy.context.object.pose

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
            bone.matrix = bone.matrix @ matrix

            bone.keyframe_insert(
                data_path=f'location',
                frame=keyframe['EndFrame'] - keyframe['NumFrames']
            )
            bone.keyframe_insert(
                data_path=f'rotation_quaternion',
                frame=keyframe['EndFrame'] - keyframe['NumFrames']
            )
            bone.keyframe_insert(
                data_path=f'scale',
                frame=keyframe['EndFrame'] - keyframe['NumFrames']
            )
            bone_idx += 1
        i += 1

def json_vector_to_vector(json_vector):
    return Vector((float(json_vector['X']), float(json_vector['Y']), float(json_vector['Z'])))

def json_vector2_to_vector2(json_vector):
    return (float(json_vector['X']), float(json_vector['Y']))

def json_quaternion_to_quaternion(json_quaternion):
    return Quaternion((float(json_quaternion['W']), float(json_quaternion['X']), float(json_quaternion['Y']), float(json_quaternion['Z'])))

def import_sge(filename):
    f = open(filename)
    sge = json.load(f)
    materials = construct_materials(sge)
    sge_armature_collection = bpy.data.collections.new(f'sge_armature')
    bpy.context.scene.collection.children.link(sge_armature_collection)
    (armature, bones_list) = construct_armature(sge)
    sge_armature_collection.objects.link(armature)

    bpy.context.scene.render.fps = 60

    i = 0
    j = 0
    for submeshGroup in sge['SgeSubmeshes']:
        sge_collection = bpy.data.collections.new(f'sge_collection{j}')
        bpy.context.scene.collection.children.link(sge_collection)
        for submesh in submeshGroup:
            mesh = construct_mesh(sge, submesh, materials, j, i)
            i += 1

            sge_collection.objects.link(mesh)

            mesh.parent = armature
            modifier = mesh.modifiers.new(type='ARMATURE', name='Armature')
            modifier.object = armature
        j += 1

    if armature.animation_data is None:
        armature.animation_data_create()
        armature.animation_data.use_nla = True

    bpy.ops.object.mode_set(mode='POSE')
    if i >= 0:    
        i = 0
        for anim in sge['SgeAnimations']:
            if len(anim['UsedKeyframes']) > 0:
                action = bpy.data.actions.new(f'Animation{i:3d}')
                armature.animation_data.action = action
                action.animation_data_clear()
                nla = armature.animation_data.nla_tracks.new()
                nla.strips.new(f'Animation{i:3d}', 0, action)
            i += 1

        i = 0
        for anim in sge['SgeAnimations']:
            if len(anim['UsedKeyframes']) > 0:
                armature.animation_data.action = bpy.data.actions[f'Animation{i:3d}']
                construct_animation(sge, anim, bones_list, i)
            i += 1
    
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.object.matrix_world = bpy.context.object.matrix_world @ Matrix.Rotation(math.radians(90), 4, 'X')
    bpy.ops.object.select_same_collection(collection=sge_armature_collection.name)
    bpy.ops.transform.mirror(constraint_axis=(False, True, False), orient_type='GLOBAL')
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.object.mode_set(mode='POSE')

    return {'FINISHED'}

if __name__ == '__main__':
    # Clean scene
    for o in bpy.context.scene.objects:
        o.select_set(True)
    bpy.ops.object.delete()

    input_file = sys.argv[-2]
    output_format = sys.argv[-1]

    import_sge(input_file)
    output_file = os.path.join(os.path.dirname(input_file), os.path.splitext(os.path.basename(input_file))[0])

    if output_format.lower() == 'gltf':
        output_file += ".glb"
        bpy.ops.export_scene.gltf(filepath=os.path.abspath(output_file), check_existing=False, export_format="GLB", export_extras=True)
    elif output_format.lower() == 'fbx':
        output_file += '.fbx'
        bpy.ops.export_scene.fbx(filepath=os.path.abspath(output_file), check_existing=False, path_mode='COPY', batch_mode='OFF', embed_textures=True)
    else:
        output_file += '.blend'
        bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(output_file), check_existing=False)