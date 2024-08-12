import bpy
from mathutils import Vector, Matrix, Quaternion
import math
import json
import os
import sys

model_scale = 25.4

def extract_submesh(obj, submesh, model, start_vertex, start_face):
    sge_submesh = {}
    sge_submesh["SubmeshVertices"] = []
    sge_submesh["Material"] = None
    bone_palette = []
    submesh.calc_normals_split()
    for vert in submesh.vertices:
        for group in vert.groups:
            bone = [b for b in model["SgeBones"] if b["BlenderName"] == obj.vertex_groups[group.group].name][0]
            bone_palette.append(bone['Address'])
        sge_submesh["SubmeshVertices"].append({
            "Position": vector_to_json_vector(vert.co / model_scale),
            "Unknown2": 65535
        })
    bone_palette = list(set(bone_palette))
    while len(bone_palette) < 16:
        bone_palette.append(0) # will be -1 when we subtract below
    for vert in submesh.vertices:
        bone_indices = []
        weight = []
        for group in vert.groups:
            bone = [b for b in model["SgeBones"] if b["BlenderName"] == obj.vertex_groups[group.group].name][0]
            bone_indices.append(bone_palette.index(bone['Address']))
            weight.append(group.weight)
        while len(bone_indices) < 4:
            bone_indices.append(0)
        while len(weight) < 4:
            weight.append(0)
        sge_submesh["SubmeshVertices"][vert.index]["BoneIndices"] = bone_indices
        sge_submesh["SubmeshVertices"][vert.index]["Weight"] = weight
    bone_palette = [b - 1 for b in bone_palette] # modify bone palette to reflect actual indices
    sge_submesh["BonePalette"] = bone_palette
    uv_layer = submesh.uv_layers[0]
    color = None
    if len(submesh.color_attributes) > 0:
        color = submesh.color_attributes[0]
    sge_submesh["SubmeshFaces"] = []
    for face in submesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            # print(f'{len(submesh.loops)}, {vert_idx}, {loop_idx}')
            sge_submesh["SubmeshVertices"][vert_idx]["Normal"] = vector_to_json_vector(submesh.loops[loop_idx].normal)
            sge_submesh["SubmeshVertices"][vert_idx]["UVCoords"] = vector2_to_json_vector2(uv_layer.uv[loop_idx].vector)
            if color is not None:
                sge_submesh["SubmeshVertices"][vert_idx]["Color"] = {
                    "R": color.data[vert_idx].color_srgb[0],
                    "G": color.data[vert_idx].color_srgb[1],
                    "B": color.data[vert_idx].color_srgb[2],
                    "A": color.data[vert_idx].color_srgb[3]
                }
            else:
                sge_submesh["SubmeshVertices"][vert_idx]["Color"] = {
                    "R": 1,
                    "G": 1,
                    "B": 1,
                    "A": 1
                }
        sge_submesh["SubmeshFaces"].append({
            "Polygon": [ int(face.vertices[0]), int(face.vertices[1]), int(face.vertices[2]) ],
        })
        if sge_submesh["Material"] == None:
            if submesh.materials[face.material_index] and submesh.materials[face.material_index].use_nodes:
                for n in submesh.materials[face.material_index].node_tree.nodes:
                    if n.type == 'TEX_IMAGE':
                        sge_submesh["Material"] = [m for m in model["SgeMaterials"] if m["Name"] == n.image.name.split(".")[0]][0]
    
    sge_submesh["GXLightingAddress"] = 1
    sge_submesh["StartVertex"] = start_vertex
    sge_submesh["EndVertex"] = start_vertex + len(sge_submesh["SubmeshVertices"]) - 1
    sge_submesh["StartFace"] = start_face
    sge_submesh["FaceCount"] = len(sge_submesh["SubmeshFaces"])
    return sge_submesh

def split_submeshes(obj):
    orig_submesh = obj.data
    submeshes = []

    bone_palette = []
    verts = []
    verts_groups = []
    verts_weights = []
    faces = []
    uvs = []
    colors = None
    prev_verts_len = 0
    if len(orig_submesh.color_attributes) > 0:
        colors = []
    i = 0
    for face in orig_submesh.polygons:
        new_bone_palette = list(bone_palette)
        next_bone_palette = []
        for vert_idx in face.vertices:
            verts_groups.append([])
            verts_weights.append({})
            for group in orig_submesh.vertices[vert_idx].groups:
                new_bone_palette.append(obj.vertex_groups[group.group].name)
                next_bone_palette.append(obj.vertex_groups[group.group].name)
                verts_groups[len(verts_groups) - 1].append(obj.vertex_groups[group.group].name)
                verts_weights[len(verts_weights) - 1][obj.vertex_groups[group.group].name] = group.weight
        new_bone_palette = list(set(new_bone_palette))
        if len(new_bone_palette) > 16:
            submesh = bpy.data.meshes.new(f'submesh{i}')
            subobj = bpy.data.objects.new(f'submesh{i}', submesh)
            min_face = min(min([[a, b, c] for a, b, c in faces]))
            faces = [(a - min_face, b - min_face, c - min_face) for a, b, c in faces]
            submesh.from_pydata(verts, [], faces)
            uvlayer = submesh.uv_layers.new()
            uvlayer_name = uvlayer.name
            if colors is not None:
                color_layer = submesh.color_attributes.new('vertex_colors', 'FLOAT_COLOR', 'POINT')
            # Creating the color layer has invalidated the reference to the uv layer, so get it again.
            uvlayer = submesh.uv_layers[uvlayer_name]
            for loop_idx in range(len(uvs)):
                uvlayer.uv[loop_idx].vector = uvs[loop_idx]
            if colors is not None:
                for vert_idx in range(len(colors)):
                    color_layer.data[vert_idx].color_srgb = colors[vert_idx]
            for material in orig_submesh.materials:
                submesh.materials.append(material)
            if len(submesh.materials) > 0:
                for face in submesh.polygons:
                    face.material_index = 0
            for vertex_group in obj.vertex_groups:
                new_group = subobj.vertex_groups.new(name=vertex_group.name)
                for v_idx in range(len(verts_groups)):
                    if new_group.name in verts_groups[v_idx]:
                        new_group.add([v_idx], verts_weights[v_idx][new_group.name], 'ADD')
            submeshes.append((submesh, subobj))
            verts = []
            faces = []
            uvs = []
            if colors is not None:
                colors = []
            new_bone_palette = list(set(next_bone_palette))
            i += 1
        bone_palette = new_bone_palette
        faces.append(face.vertices)
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            verts.append(orig_submesh.vertices[vert_idx].co)
            uvs.append(orig_submesh.uv_layers[0].uv[loop_idx].vector)
            if colors is not None:
                colors.append(orig_submesh.color_attributes[0].data[vert_idx].color_srgb)
    
    if len(submeshes) == 0:
        submeshes.append((orig_submesh, obj))
    elif len(verts) > 0 and len(faces) > 0:
        submesh = bpy.data.meshes.new(f'submesh{i}')
        subobj = bpy.data.objects.new(f'submesh{i}', submesh)
        min_face = min(min([[a, b, c] for a, b, c in faces]))
        faces = [(a - min_face, b - min_face, c - min_face) for a, b, c in faces]
        submesh.from_pydata(verts, [], faces)
        uvlayer = submesh.uv_layers.new()
        uvlayer_name = uvlayer.name
        if colors is not None:
            color_layer = submesh.color_attributes.new('vertex_colors', 'FLOAT_COLOR', 'POINT')
        # Creating the color layer has invalidated the reference to the uv layer, so get it again.
        uvlayer = submesh.uv_layers[uvlayer_name]
        for loop_idx in range(len(uvs)):
            uvlayer.uv[loop_idx].vector = uvs[loop_idx]
        if colors is not None:
            for vert_idx in range(len(colors)):
                color_layer.data[vert_idx].color_srgb = colors[vert_idx]
        for material in orig_submesh.materials:
            submesh.materials.append(material)
        if len(submesh.materials) > 0:
            for face in submesh.polygons:
                face.material_index = 0
        for vertex_group in obj.vertex_groups:
            new_group = subobj.vertex_groups.new(name=vertex_group.name)
            for v_idx in range(len(verts_groups)):
                if new_group.name in verts_groups[v_idx]:
                    new_group.add([v_idx], verts_weights[v_idx][new_group.name], 'ADD')
        submeshes.append((submesh, subobj))
    return submeshes

def export_sge(filename, model_type):
    if os.path.exists(filename):
        os.remove(filename)
    f = open(filename, 'x')
    
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.ops.object.select_by_type(type='ARMATURE')
    bpy.ops.transform.mirror(constraint_axis=(False, True, False), orient_type='GLOBAL')
    bpy.ops.object.select_all(action='DESELECT')
    bpy.context.object.matrix_world = bpy.context.object.matrix_world @ Matrix.Rotation(math.radians(-90), 4, 'X')

    model = {"Name": os.path.basename(bpy.data.filepath).split('.')[0]}
    # Header
    model["SgeHeader"] = {}
    model["SgeHeader"]["Version"] = 8
    model["SgeHeader"]["ModelType"] = model_type

    model["SgeAnimations"] = []
    model["TranslateDataEntries"] = [vector_to_json_vector(Vector((0, 0, 0)) / model_scale)]
    model["RotationDataEntries"] = [quaternion_to_json_quaternion(Quaternion((1, 0, 0, 0)))]
    model["ScaleDataEntries"] = [vector_to_json_vector(Vector((1, 1, 1)))]
    model["KeyframeDefinitions"] = [{}]

    model["SgeGXLightingDataTable"] = [{
        "Offset": 1,
        "AmbientR": 1,
        "AmbientG": 1,
        "AmbientB": 1,
        "AmbientA": 1,
        "MaterialR": 1,
        "MaterialG": 1,
        "MaterialB": 1,
        "MaterialA": 0,
        "CombinedR": 0,
        "CombinedG": 0,
        "CombinedB": 0,
        "CombinedA": 0,
        "Unknown30": 0,
        "Unknown34": 0,
        "Unknown38": 0,
        "Unknown3C": 0,
        "Unknown40": 0.1,
        "DefaultLightingEnabled": True
    }]
    model["SubmeshBlendDataTable"] = []
    model["OutlineDataTable"] = []
    model["Unknown4CTable"] = []
    model["BoneAnimationGroups"] = []
    model["Unknown58Table"] = []
    model["SgeMeshes"] = []
    for i in range(10):
        model["SgeMeshes"].append({})

    # Materials/Textures
    tex_folder = os.path.join(os.path.dirname(filename), os.path.splitext(os.path.basename(filename))[0])
    if not os.path.exists(tex_folder):
        os.makedirs(tex_folder)
    model["SgeMaterials"] = []
    tex_idx = 0
    for image in bpy.data.images:
        if image.name == 'Render Result':
            continue
        new_filepath = os.path.join(tex_folder, f'{image.name.split(".")[0]}.png')
        image.save_render(filepath=new_filepath)
        model["SgeMaterials"].append({
            "Index": tex_idx,
            "Name": image.name.split('.')[0],
            "TexturePath": new_filepath
        })
        tex_idx += 1

    # Armature
    model["SgeBones"] = []
    armature_map = {}
    bpy.ops.object.select_by_type(type='ARMATURE')
    bpy.ops.object.mode_set(mode='EDIT')
    obj = bpy.context.object
    armature = obj.data
    for collection in [c for c in list(armature.collections.keys()) if 'AnimationGroup' in c]:
        model["BoneAnimationGroups"].append({ "BoneIndices": [] }) # just prepopulate the list for ease of use
    i = 1
    # Do initial bone map
    for (bone, edit_bone) in zip(armature.bones, armature.edit_bones):
        sge_bone = {}
        sge_bone['BlenderName'] = bone.name
        sge_bone['Address'] = i
        tail = Vector((0, 1, 0))
        if edit_bone.parent:
            tail = edit_bone.head - edit_bone.parent.head
        sge_bone['TailOffset'] = vector_to_json_vector(tail / model_scale)
        sge_bone['HeadPosition'] = vector_to_json_vector(edit_bone.head / model_scale)
        sge_bone['ParentAddress'] = 0
        sge_bone['ChildAddress'] = 0
        sge_bone['NextSiblingAddress'] = 0
        
        if 'NeckBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0002
        if 'FaceBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0004
        if 'ChestBones' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0008
        if 'StomachBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0010
        if 'RightHandBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0020
        if 'LeftHandBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0040
        if 'Unknown0080Group' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0080
        if 'Unknown0100Group' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0100
        if 'RightFootBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0200
        if 'LeftFootBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0400
        if 'EyebrowBones' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x0800
        if 'RightLegBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x1000
        if 'LeftLegBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x2000
        if 'RightCheekBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = 0x4000
        if 'LeftCheekBone' in list(bone.collections.keys()):
            sge_bone['BodyPart'] = -32768 # 0x8000 but since it's a short it has to be negative
        
        if 'EyesAnimationGroup' in list(bone.collections.keys()):
            model["BoneAnimationGroups"][0]["BoneIndices"].append(i - 1)
        if 'MouthAnimationGroup' in list(bone.collections.keys()):
            model["BoneAnimationGroups"][1]["BoneIndices"].append(i - 1)
        for u in range(50): # just an arbitrarily large number; there will never be this many groups
            if f'{u}AnimationGroup' in list(bone.collections.keys()):
                model["BoneAnimationGroups"][u]["BoneIndices"].append(i - 1)

        model["SgeBones"].append(sge_bone)
        armature_map[bone.name] = bone
        i += 1
    # Resolve bone links
    for sge_bone in model["SgeBones"]:
        bone = armature_map[sge_bone['BlenderName']]
        for potential_parent in model["SgeBones"]:
            if bone.parent is not None:
                if potential_parent["BlenderName"] == bone.parent.name:
                    sge_bone["ParentAddress"] = potential_parent["Address"]
                    if potential_parent["ChildAddress"] == 0:
                        potential_parent["ChildAddress"] = sge_bone["Address"]
                    else:
                        for potential_sibling in bone.parent.children:
                            sge_sibling = [s for s in model["SgeBones"] if s["BlenderName"] == potential_sibling.name][0]
                            if sge_sibling["NextSiblingAddress"] == 0:
                                sge_sibling["NextSiblingAddress"] = sge_bone["Address"]
                                break

    # Submeshes
    bpy.ops.object.mode_set(mode='OBJECT')
    model["SgeSubmeshes"] = []
    for collection in bpy.data.collections:
        submesh_group = []
        vtx = 0
        face = 0
        for obj in collection.objects:
            if obj.type == 'MESH':
                submesh_group.append(extract_submesh(obj, obj.data, model, vtx, face))
                vtx = submesh_group[-1]["EndVertex"] + 1
                if model_type == 4:
                    face += submesh_group[-1]["FaceCount"]
                else:
                    face += submesh_group[-1]["FaceCount"] * 3
        if len(submesh_group) > 0:
            model["SgeSubmeshes"].append(submesh_group)
    if len(model["SgeSubmeshes"]) == 0:
        submesh_group = []
        bpy.ops.object.select_by_type(type='MESH')
        vtx = 0
        face = 0
        for obj in bpy.context.selected_objects:
            for submesh, subobj in split_submeshes(obj):
                submesh_group.append(extract_submesh(subobj, submesh, model, vtx, face))
                vtx = submesh_group[-1]["EndVertex"] + 1
                if model_type == 4:
                    face += submesh_group[-1]["FaceCount"]
                else:
                    face += submesh_group[-1]["FaceCount"] * 3
        model["SgeSubmeshes"].append(submesh_group)

    json.dump(model, f)
    f.close()
    
    bpy.context.object.matrix_world = bpy.context.object.matrix_world @ Matrix.Rotation(math.radians(90), 4, 'X')
    bpy.ops.object.select_by_type(type='ARMATURE')
    bpy.ops.transform.mirror(constraint_axis=(False, True, False), orient_type='GLOBAL')
    bpy.ops.object.select_all(action='DESELECT')
    
    return {'FINISHED'}

def vector_to_json_vector(vector):
    return {
        'X': vector[0],
        'Y': vector[1],
        'Z': vector[2]
    }

def vector2_to_json_vector2(vector):
    return {
        'X': vector[0],
        'Y': vector[1]
    }

def quaternion_to_json_quaternion(quaternion):
    return {
        'X': quaternion[1],
        'Y': quaternion[2],
        'Z': quaternion[3],
        'W': quaternion[0]
    }

if __name__ == '__main__':
    input_file = sys.argv[-2]
    model_type = int(sys.argv[-1])

    bpy.ops.wm.open_mainfile(filepath=input_file)

    output_file = os.path.join(os.path.dirname(input_file), f'{os.path.splitext(os.path.basename(input_file))[0]}.sge.json')
    export_sge(output_file, model_type)