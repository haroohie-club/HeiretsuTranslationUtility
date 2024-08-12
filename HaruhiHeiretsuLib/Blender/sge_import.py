import bpy
from mathutils import Vector, Matrix, Quaternion
import math
import json
import os
import shutil
import sys

model_scale = 1

def gen_shader_text(filename):
    file = open(filename)
    text = bpy.data.texts.new('shader_int_color_text')
    line = file.readline()
    while line:
        text.write(line)
        line = file.readline()
    file.close()
    return text

def add_shader_script_node(mat, type):
    if type == 'SHADER_INT_COLOR':
        shader_int_color = mat.node_tree.nodes.new('ShaderNodeScript')
        shader_int_color.script = gen_shader_text(os.path.join(os.path.dirname(__file__ ), './shader_int_color.osl'))
        shader_int_color.update()
        return shader_int_color
    elif type == 'SHADER_VEC_COLOR':
        shader_vec_color = mat.node_tree.nodes.new('ShaderNodeScript')
        shader_vec_color.script = gen_shader_text(os.path.join(os.path.dirname(__file__ ), './shader_vec_color.osl'))
        shader_vec_color.update()
        return shader_vec_color

def create_blend_nodes(obj, mat, manip, manip_alpha, src, src_alpha, dst, dst_alpha, color_mix, alpha_mix, op, input_index):
    if obj[op] == 0:
        clr_zero_node = mat.node_tree.nodes.new('ShaderNodeVectorMath')
        clr_zero_node.operation = 'MULTIPLY'
        mat.node_tree.links.new(manip.outputs['Color'], clr_zero_node.inputs[0])
        clr_zero_node.inputs[1].default_value = Vector((0, 0, 0))
        mat.node_tree.links.new(clr_zero_node.outputs[0], color_mix.inputs[input_index])
        # alpha_zero_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # alpha_zero_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha', alpha_zero_node.inputs[0]])
        # alpha_zero_node.inputs[1].default_value = 0
        # mat.node_tree.links.new(alpha_zero_node.outputs[0], alpha_mix.inputs[input_index])
    if obj[op] == 1:
        color_conv_manip = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        mat.node_tree.links.new(manip.outputs['Color'], color_conv_manip.inputs[0])
        mat.node_tree.links.new(color_conv_manip.outputs[0], color_mix.inputs[input_index])
        # mat.node_tree.links.new(manip.outputs['Alpha'], alpha_mix.inputs[input_index])
    if obj[op] == 2:
        color_conv_manip = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        color_conv_src = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        src_clr_node = mat.node_tree.nodes.new('ShaderNodeMath')
        src_clr_node.operation = 'MULTIPLY'
        mat.node_tree.links.new(manip.outputs['Color'], color_conv_manip.inputs[0])
        mat.node_tree.links.new(src.outputs['Color'], color_conv_src.inputs[0])
        mat.node_tree.links.new(color_conv_manip.outputs[0], src_clr_node.inputs[0])
        mat.node_tree.links.new(color_conv_src.outputs[0], src_clr_node.inputs[1])
        mat.node_tree.links.new(src_clr_node.outputs[0], color_mix.inputs[input_index])
        # src_alpha_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # src_alpha_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha'], src_alpha_node.inputs[0])
        # mat.node_tree.links.new(src.outputs['Alpha'], src_alpha_node.inputs[1])
        # mat.node_tree.links.new(src_alpha_node.outputs[0], alpha_mix.inputs[input_index])
    if obj[op] == 3:
        src_clr_sub_conv = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        src_clr_sub_node = mat.node_tree.nodes.new('ShaderNodeMath')
        src_clr_sub_node.operation = 'SUBTRACT'
        src_clr_sub_node.inputs[0].default_value = 1.0
        mat.node_tree.links.new(src.outputs['Color'], src_clr_sub_conv.inputs[0])
        mat.node_tree.links.new(src_clr_sub_conv.outputs[0], src_clr_sub_node.inputs[1])
        src_clr_conv = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        src_clr_node = mat.node_tree.nodes.new('ShaderNodeMath')
        src_clr_node.operation = 'MULTIPLY'
        mat.node_tree.links.new(manip.outputs['Color'], src_clr_conv.inputs[0])
        mat.node_tree.links.new(src_clr_conv.outputs[0], src_clr_node.inputs[0])
        mat.node_tree.links.new(src_clr_sub_node.outputs[0], src_clr_node.inputs[1])
        mat.node_tree.links.new(src_clr_node.outputs[0], color_mix.inputs[input_index])
        # src_alpha_sub_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # src_alpha_sub_node.operation = 'SUBTRACT'
        # src_alpha_sub_node.inputs[0].default_value = 1
        # mat.node_tree.links.new(src.outputs['Alpha'], src_alpha_sub_node.inputs[1])
        # src_alpha_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # src_alpha_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha'], src_alpha_node.inputs[0])
        # mat.node_tree.links.new(src_alpha_sub_node.outputs[0], src_alpha_node.inputs[1])
        # mat.node_tree.links.new(src_alpha_node.outputs[0], alpha_mix.inputs[input_index])
    if obj[op] == 4:
        src_clr_conv = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        src_clr_node = mat.node_tree.nodes.new('ShaderNodeMath')
        src_clr_node.operation = 'MULTIPLY'
        mat.node_tree.links.new(manip.outputs['Color'], src_clr_conv.inputs[0])
        mat.node_tree.links.new(src_clr_conv.outputs[0], src_clr_node.inputs[0])
        if src_alpha is None:
            mat.node_tree.links.new(src.outputs['Alpha'], src_clr_node.inputs[1])
        else:
            mat.node_tree.links.new(src_alpha.outputs[0], src_clr_node.inputs[1])
        mat.node_tree.links.new(src_clr_node.outputs[0], color_mix.inputs[input_index])
        # src_alpha_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # src_alpha_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha'], src_alpha_node.inputs[0])
        # mat.node_tree.links.new(src.outputs['Alpha'], src_alpha_node.inputs[1])
        # mat.node_tree.links.new(src_alpha_node.outputs[0], alpha_mix.inputs[input_index])
    if obj[op] == 5:
        src_clr_sub_node = mat.node_tree.nodes.new('ShaderNodeMath')
        src_clr_sub_node.operation = 'SUBTRACT'
        src_clr_sub_node.inputs[0].default_value = 1
        if src_alpha is None:
            mat.node_tree.links.new(src.outputs['Alpha'], src_clr_sub_node.inputs[1])
        else:
            mat.node_tree.links.new(src_alpha.outputs[0], src_clr_sub_node.inputs[1])
        src_clr_conv = add_shader_script_node(mat, 'SHADER_INT_COLOR')
        src_clr_node = mat.node_tree.nodes.new('ShaderNodeMath')
        src_clr_node.operation = 'MULTIPLY'
        mat.node_tree.links.new(manip.outputs['Color'], src_clr_conv.inputs[0])
        mat.node_tree.links.new(src_clr_conv.outputs[0], src_clr_node.inputs[0])
        mat.node_tree.links.new(src_clr_sub_node.outputs[0], src_clr_node.inputs[1])
        mat.node_tree.links.new(src_clr_node.outputs[0], color_mix.inputs[input_index])
        # src_alpha_sub_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # src_alpha_sub_node.operation = 'SUBTRACT'
        # src_alpha_sub_node.inputs[0].default_value = 1
        # mat.node_tree.links.new(src.outputs['Alpha'], src_alpha_sub_node.inputs[1])
        # src_alpha_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # src_alpha_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha'], src_alpha_node.inputs[0])
        # mat.node_tree.links.new(src_alpha_sub_node.outputs[0], src_alpha_node.inputs[1])
        # mat.node_tree.links.new(src_alpha_node.outputs[0], alpha_mix.inputs[input_index])
    # if obj[op] == 6:
    #     dst_clr_node = mat.node_tree.nodes.new('ShaderNodeVectorMath')
    #     dst_clr_node.operation = 'MULTIPLY'
    #     mat.node_tree.links.new(manip.outputs['Color'], dst_clr_node.inputs[0])
    #     if dst_alpha is None:
    #         mat.node_tree.links.new(dst.outputs['Alpha'], dst_clr_node.inputs[1])
    #     else:
    #         mat.node_tree.links.new(dst_alpha.outputs[0], dst_clr_node.inputs[1])
    #     mat.node_tree.links.new(dst_clr_node.outputs[0], color_mix.inputs[input_index])
        # dst_alpha_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # dst_alpha_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha'], dst_alpha_node.inputs[0])
        # mat.node_tree.links.new(dst.outputs['Alpha'], dst_alpha_node.inputs[1])
        # mat.node_tree.links.new(dst_alpha_node.outputs[0], alpha_mix.inputs[input_index])
    # if obj[op] == 7:
    #     dst_clr_sub_node = mat.node_tree.nodes.new('ShaderNodeMath')
    #     dst_clr_sub_node.operation = 'SUBTRACT'
    #     dst_clr_sub_node.inputs[0].default_value = 1
    #     if dst_alpha is None:
    #         mat.node_tree.links.new(dst.outputs['Alpha'], dst_clr_sub_node.inputs[1])
    #     else:
    #         mat.node_tree.links.new(dst_alpha.outputs[0], dst_clr_sub_node.inputs[1])
    #     dst_clr_node = mat.node_tree.nodes.new('ShaderNodeVectorMath')
    #     dst_clr_node.operation = 'MULTIPLY'
    #     mat.node_tree.links.new(manip.outputs['Color'], dst_clr_node.inputs[0])
    #     mat.node_tree.links.new(dst_clr_sub_node.outputs[0], dst_clr_node.inputs[1])
    #     mat.node_tree.links.new(dst_clr_node.outputs[0], color_mix.inputs[input_index])
        # dst_alpha_sub_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # dst_alpha_sub_node.operation = 'SUBTRACT'
        # dst_alpha_sub_node.inputs[0].default_value = 1
        # mat.node_tree.links.new(dst.outputs['Alpha', dst_alpha_sub_node.inputs[1]])
        # dst_alpha_node = mat.node_tree.nodes.new('ShaderNodeMath')
        # dst_alpha_node.operation = 'MULTIPLY'
        # mat.node_tree.links.new(manip.outputs['Alpha'], dst_alpha_node.inputs[0])
        # mat.node_tree.links.new(dst_alpha_sub_node.outputs[0], dst_alpha_node.inputs[1])
        # mat.node_tree.links.new(dst_alpha_node.outputs[0], alpha_mix.inputs[input_index])

def construct_materials(sge):
    print('Constructing materials...')
    materials = []
    for sge_material in sge['SgeMaterials']:
        material = bpy.data.materials.new(sge_material['Name'])
        material.use_backface_culling = True
        material.use_nodes = True
        bsdf = material.node_tree.nodes['Principled BSDF']
        if not (sge_material['TexturePath'] is not None and len(sge_material['TexturePath']) > 0):
            # img = bpy.data.images.load(sge_material['TexturePath'])
            # texture = material.node_tree.nodes.new('ShaderNodeTexImage')
            # texture.image = img
            # color_mix = material.node_tree.nodes.new('ShaderNodeMix')
            # alpha_mix = material.node_tree.nodes.new('ShaderNodeMix')
            # color_mix.data_type = 'RGBA'
            # alpha_mix.data_type = 'RGBA'
            # color_mix.blend_type = 'BURN'
            # alpha_mix.blend_type = 'MIX'
            # material.node_tree.links.new(texture.outputs['Color'], color_mix.inputs['A'])
            # material.node_tree.links.new(texture.outputs['Alpha'], alpha_mix.inputs['A'])
            # material.node_tree.links.new(vertex_color.outputs['Color'], color_mix.inputs['B'])
            # material.node_tree.links.new(vertex_color.outputs['Alpha'], alpha_mix.inputs['B'])
            # material.node_tree.links.new(color_mix.outputs['Result'], bsdf.inputs['Base Color'])
            # material.node_tree.links.new(alpha_mix.outputs['Result'], bsdf.inputs['Alpha'])
        # else:
            vertex_color = material.node_tree.nodes.new('ShaderNodeVertexColor')
            vertex_color.layer_name = 'vertex_colors'
            material.node_tree.links.new(vertex_color.outputs['Color'], bsdf.inputs['Base Color'])
            material.node_tree.links.new(vertex_color.outputs['Alpha'], bsdf.inputs['Alpha'])
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
    # for material in materials:
    #     obj.data.materials.append(material)

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
    # mesh.normals_split_custom_set([(0, 0, 0) for l in mesh.loops])
    mesh.normals_split_custom_set_from_vertices(normals)
    mesh.flip_normals()

    uvlayer = mesh.uv_layers.new()
    uvlayer_name = uvlayer.name
    color_layer = mesh.color_attributes.new('vertex_colors', 'FLOAT_COLOR', 'POINT')
    # Creating the color layer has invalidated the reference to the uv layer, so get it again.
    uvlayer = mesh.uv_layers[uvlayer_name]
    for face in mesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            color_layer.data[vert_idx].color_srgb = (colors[vert_idx]['R'], colors[vert_idx]['G'], colors[vert_idx]['B'], colors[vert_idx]['A'])
            uvlayer.uv[loop_idx].vector = uvcoords[vert_idx]
    
    for bone in sge['SgeBones']:
        bone_vertex_group = obj.vertex_groups.new(name='Bone' + str(bone['Address']))
        for attached_vertex in bone['VertexGroup']:
            attached_vertex_split = attached_vertex.split(',')
            (attached_vertex_group, attached_vertex_submesh, attached_vertex_index) = (int(attached_vertex_split[0]), int(attached_vertex_split[1]), int(attached_vertex_split[2]))
            if attached_vertex_group == group_num and attached_vertex_submesh == submesh_num:
                bone_vertex_group.add([attached_vertex_index], bone['VertexGroup'][attached_vertex], 'ADD')

    blend_data = next((b for b in sge["SubmeshBlendDataTable"] if b["Offset"] == submesh["BlendDataAddress"]), None)
    if blend_data is not None:
        obj["UseCustomBlendMode"] = blend_data["UseCustomBlendMode"]
        obj["CustomBlendSrcFactor"] = blend_data["CustomBlendSrcFactor"] - 1
        obj["CustomBlendDstFactor"] = blend_data["CustomBlendDstFactor"] - 1
        obj["BlendVertexColorAlpha"] = blend_data["VertexColorAlpha"]
        obj["BlendAlphaCompareAndZMode"] = blend_data["AlphaCompareAndZMode"]
    
    if submesh['Material'] is not None:
        obj['MaterialIndex'] = submesh['Material']['Index']
        obj.data.materials.append(materials[submesh['Material']['Index']].copy())
        mat = obj.data.materials[0]
        texture = mat.node_tree.nodes.new('ShaderNodeTexImage')
        bsdf = mat.node_tree.nodes['Principled BSDF']
        img_file = submesh['Material']['TexturePath']
        new_img_file = os.path.join(os.path.dirname(img_file), f'{mat.name}.png')
        shutil.copyfile(img_file, new_img_file)
        img = bpy.data.images.load(new_img_file)
        texture.image = img
        if "UseCustomBlendMode" in obj.keys() and obj["UseCustomBlendMode"] == True:
            # mat.blend_method = 'BLEND'
            vertex_color = mat.node_tree.nodes.new('ShaderNodeVertexColor')
            vertex_color.layer_name = 'vertex_colors'
            vertex_alpha = mat.node_tree.nodes.new('ShaderNodeValue')
            vertex_alpha.outputs[0].default_value = obj["BlendVertexColorAlpha"]
            color_conv = add_shader_script_node(mat, 'SHADER_VEC_COLOR')
            color_mix = mat.node_tree.nodes.new('ShaderNodeMath')
            color_mix.operation = 'ADD'
            alpha_mix = mat.node_tree.nodes.new('ShaderNodeMath')
            alpha_mix.operation = 'MULTIPLY'
            create_blend_nodes(obj, mat, texture, None, texture, None, vertex_color, vertex_alpha, color_mix, alpha_mix, 'CustomBlendSrcFactor', 0)
            create_blend_nodes(obj, mat, vertex_color, vertex_alpha, vertex_color, vertex_alpha, texture, None, color_mix, alpha_mix, 'CustomBlendDstFactor', 1)
            mat.node_tree.links.new(texture.outputs['Alpha'], alpha_mix.inputs[0])
            mat.node_tree.links.new(vertex_alpha.outputs[0], alpha_mix.inputs[1])
            mat.node_tree.links.new(color_mix.outputs[0], color_conv.inputs[0])
            mat.node_tree.links.new(color_conv.outputs[0], bsdf.inputs['Base Color'])
            mat.node_tree.links.new(alpha_mix.outputs[0], bsdf.inputs['Alpha'])
        else:
            mat.node_tree.links.new(texture.outputs['Color'], bsdf.inputs['Base Color'])
            mat.node_tree.links.new(texture.outputs['Alpha'], bsdf.inputs['Alpha'])
    
    for i in range(len(mesh.polygons)):
        if submesh['Material'] is not None:
            mesh.polygons[i].material_index = submesh['Material']['Index']
    mesh.update()

    outline_data = next((o for o in sge["OutlineDataTable"] if o["Offset"] == submesh["OutlineAddress"]), None)
    if outline_data is not None:
        obj["OutlineWeight"] = outline_data["Weight"]
        obj["OutlineColor"] = outline_data["Color"]
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

def import_sge(filename, output_format):
    bpy.context.scene.render.engine = 'CYCLES'
    bpy.context.scene.cycles.shading_system = True
    bpy.context.scene.cycles.device = 'GPU'
    print()
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

    if output_format.lower() != 'obj':
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

    import_sge(input_file, output_format)
    output_file = os.path.join(os.path.dirname(input_file), os.path.splitext(os.path.basename(input_file))[0])

    if output_format.lower() == 'gltf':
        output_file += ".glb"
        bpy.ops.export_scene.gltf(filepath=os.path.abspath(output_file), check_existing=False, export_format="GLB", export_extras=True)
    elif output_format.lower() == 'fbx':
        output_file += '.fbx'
        bpy.ops.export_scene.fbx(filepath=os.path.abspath(output_file), check_existing=False, path_mode='COPY', batch_mode='OFF', embed_textures=True)
    elif output_format.lower() == 'obj':
        output_file += '.obj'
        bpy.ops.wm.obj_export(filepath=os.path.abspath(output_file), check_existing=False, forward_axis='NEGATIVE_Z', up_axis='Y', export_uv=True, export_normals=True,
                              export_colors=True, export_materials=True, path_mode='COPY', export_vertex_groups=True, export_material_groups=True)
    else:
        output_file += '.blend'
        bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(output_file), check_existing=False)