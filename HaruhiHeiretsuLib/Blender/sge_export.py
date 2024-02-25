import bpy
from mathutils import Vector, Matrix, Quaternion
import math
import json
import os
import sys

model_scale = 25.4

def main(filename, model_type):
    if os.path.exists(filename):
        os.remove(filename)
    f = open(filename, 'x')
    
    bpy.ops.object.mode_set(mode='OBJECT')
    bpy.context.object.matrix_world = bpy.context.object.matrix_world @ Matrix.Rotation(math.radians(-90), 4, 'X')
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.transform.mirror(constraint_axis=(False, True, False), orient_type='GLOBAL')
    bpy.ops.object.select_all(action='DESELECT')

    model = {}
    model["SgeHeader"] = {}
    model["SgeHeader"]["Version"] = 8
    model["SgeHeader"]["ModelType"] = model_type

    model["SgeAnimations"] = []
    model["TranslateDataEntries"] = [vector_to_json_vector(Vector((0, 0, 0)))]
    model["RotationDataEntries"] = [quaternion_to_json_quaternion(Quaternion((1, 0, 0, 0)))]
    model["ScaleDataEntries"] = [vector_to_json_vector(Vector((1, 1, 1)))]
    model["KeyframeDefinitions"] = [{}]

    model["SgeGXLightingDataTable"] = []
    model["SubmeshBlendDataTable"] = []
    model["Unknown40Table"] = []
    model["Unknown4CTable"] = []
    model["Unknown50Table"] = []
    model["Unknown58Table"] = []
    model["SgeMeshes"] = []
    for i in range(10):
        model["SgeMeshes"].append({})

    tex_folder = os.path.join(os.path.dirname(filename), os.path.splitext(os.path.basename(input_file))[0])
    if not os.path.exists(tex_folder):
        os.makedirs(tex_folder)
    model["SgeMaterials"] = []
    tex_idx = 0
    for image in bpy.data.images:
        if image.name == 'Render Result':
            continue
        new_filepath = os.path.join(tex_folder, os.path.basename(image.filepath))
        image.save_render(filepath=new_filepath)
        model["SgeMaterials"].append({
            "Index": tex_idx,
            "Name": os.path.basename(image.filepath).split('.')[0],
            "TexturePath": new_filepath
        })
        tex_idx += 1

    model["SgeBones"] = []
    armature_map = {}
    bpy.ops.object.select_by_type(type='ARMATURE')
    obj = bpy.context.object
    armature = obj.data
    i = 0
    # Do initial bone map
    for bone in armature.bones:
        sge_bone = {}
        sge_bone['BlenderName'] = bone.name
        sge_bone['Address'] = i
        sge_bone['Unknown00'] = vector_to_json_vector(bone.tail)
        sge_bone['HeadPosition'] = vector_to_json_vector(bone.head)
        sge_bone['ParentAddress'] = 0
        sge_bone['AddressToBone1'] = 0
        sge_bone['AddressToBone2'] = 0
        
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
                    if potential_parent["AddressToBone1"] == 0:
                        potential_parent["AddressToBone1"] = sge_bone["Address"]
                    else:
                        potential_parent["AddressToBone2"] = sge_bone["Address"]

    json.dump(model, f)
    f.close()

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
    main(output_file, model_type)