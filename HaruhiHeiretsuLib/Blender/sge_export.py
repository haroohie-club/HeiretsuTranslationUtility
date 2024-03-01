import bpy
from mathutils import Vector, Matrix, Quaternion
import math
import json
import os
import sys

model_scale = 25.4

def extract_submesh(obj, model, start_vertex, start_face, skip_to = 0):
    submesh = obj.data
    sge_submeshes = []
    sge_submesh = {}
    sge_submesh["SubmeshVertices"] = []
    bone_palette = []
    submesh.calc_normals_split()
    cut_off_vertex = -1
    i = 0
    for vert in submesh.vertices:
        if skip_to > 0 and i <= skip_to:
            i += 1
            continue
        if len(list(set(bone_palette))) + len(list(set(vert.groups))) > 16:
            cut_off_vertex = i - 1
            break
        for group in vert.groups:
            bone = [b for b in model["SgeBones"] if b["BlenderName"] == obj.vertex_groups[group.group].name][0]
            bone_palette.append(bone['Address'])
        sge_submesh["SubmeshVertices"].append({
            "Position": vector_to_json_vector(vert.co / model_scale),
            "Unknown2": 65535
        })
        i += 1
    bone_palette = list(set(bone_palette))
    while len(bone_palette) < 16:
        bone_palette.append(0) # will be -1 when we subtract below
    i = 0
    for vert in submesh.vertices:
        if skip_to > 0 and i <= skip_to:
            i += 1
            continue
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
        sge_submesh["SubmeshVertices"][vert.index - skip_to - 1 if skip_to > 0 else vert.index]["BoneIndices"] = bone_indices
        sge_submesh["SubmeshVertices"][vert.index - skip_to - 1 if skip_to > 0 else vert.index]["Weight"] = weight
        if i == cut_off_vertex:
            break
        i += 1
    bone_palette = [b - 1 for b in bone_palette] # modify bone palette to reflect actual indices
    sge_submesh["BonePalette"] = bone_palette
    uv_layer = submesh.uv_layers[0]
    color = None
    if len(submesh.color_attributes) > 0:
        color = submesh.color_attributes[0]
    sge_submesh["SubmeshFaces"] = []
    sge_submesh["Material"] = None
    for face in submesh.polygons:
        if (cut_off_vertex > 0 and any([v >= cut_off_vertex for v in face.vertices])) or (skip_to > 0 and any([v <= skip_to for v in face.vertices])):
            continue
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            sge_submesh["SubmeshVertices"][vert_idx - skip_to - 1 if skip_to > 0 else vert_idx]["Normal"] = vector_to_json_vector(submesh.loops[loop_idx].normal)
            sge_submesh["SubmeshVertices"][vert_idx - skip_to - 1 if skip_to > 0 else vert_idx]["UVCoords"] = vector2_to_json_vector2(uv_layer.uv[loop_idx].vector)
            if color is not None:
                sge_submesh["SubmeshVertices"][vert_idx]["Color"] = {
                    "R": color.data[vert_idx - skip_to - 1 if skip_to > 0 else vert_idx].color_srgb[0],
                    "G": color.data[vert_idx - skip_to - 1 if skip_to > 0 else vert_idx].color_srgb[1],
                    "B": color.data[vert_idx - skip_to - 1 if skip_to > 0 else vert_idx].color_srgb[2],
                    "A": color.data[vert_idx - skip_to - 1 if skip_to > 0 else vert_idx].color_srgb[3]
                }
            else:
                sge_submesh["SubmeshVertices"][vert_idx - skip_to - 1 if skip_to > 0 else vert_idx]["Color"] = {
                    "R": 1,
                    "G": 1,
                    "B": 1,
                    "A": 1
                }
        sge_submesh["SubmeshFaces"].append({
            "Polygon": [ int(face.vertices[0]) - skip_to - 1 if skip_to > 0 else int(face.vertices[0]), int(face.vertices[1]) - skip_to - 1 if skip_to > 0 else int(face.vertices[1]), int(face.vertices[2]) - skip_to - 1 if skip_to > 0 else int(face.vertices[2]) ],
        })
        if sge_submesh["Material"] == None:
            if submesh.materials[face.material_index] and submesh.materials[face.material_index].use_nodes:
                for n in submesh.materials[face.material_index].node_tree.nodes:
                    if n.type == 'TEX_IMAGE':
                        sge_submesh["Material"] = [m for m in model["SgeMaterials"] if m["Name"] == n.image.name][0]
    
    sge_submesh["GXLightingAddress"] = 1
    sge_submesh["StartVertex"] = skip_to + 1 if skip_to > 0 else start_vertex
    sge_submesh["EndVertex"] = cut_off_vertex if cut_off_vertex > 0 else start_vertex + len(sge_submesh["SubmeshVertices"]) - 1
    sge_submesh["StartFace"] = start_face
    sge_submesh["FaceCount"] = len(sge_submesh["SubmeshFaces"])
    sge_submeshes.append(sge_submesh)
    if cut_off_vertex > 0:
        for next_submesh in extract_submesh(obj, model, cut_off_vertex + 1, start_face + len(sge_submesh["SubmeshFaces"]), cut_off_vertex + 1):
            sge_submeshes.append(next_submesh)
    return sge_submeshes

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
    model["Unknown40Table"] = []
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
                for submesh in extract_submesh(obj, model, vtx, face):
                    submesh_group.append(submesh)
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
            for submesh in extract_submesh(obj, model, vtx, face):
                submesh_group.append(submesh)
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