import bpy
from mathutils import Vector, Matrix
import json

def construct_armature(sge):
    print('Construction armature...')
    bpy.ops.object.add(
        type='ARMATURE',
        enter_editmode=True,
        location=(json_vector_to_vector(sge['SgeArmature']['RootBone']['Position']))
    )
    obj = bpy.context.object
    obj.name = sge['Name']
    armature = obj.data
    armature.name = sge['Name'] + "_Armature"
    add_bone_and_children(armature, sge['SgeArmature']['RootBone'], None)

def add_bone_and_children(armature, bone, parent_bone):
    new_bone = armature.edit_bones.new("Bone" + str(bone['Address']))
    new_bone.head = json_vector_to_vector(bone['Position'])
    if (parent_bone is not None):
        new_bone.parent = parent_bone
        new_bone.tail = parent_bone.head
    else:
        new_bone.tail = json_vector_to_vector(bone['Position']) + Vector((0, 0.1, 0))
    if (len(bone['ChildBones']) > 0):
        for child_bone in bone['ChildBones']:
            add_bone_and_children(armature, child_bone, new_bone)

def json_vector_to_vector(json_vector):
    print(json_vector)
    return Vector((float(json_vector['X']), float(json_vector['Z']), float(json_vector['Y'])))

def main(filename):
    f = open(filename)
    sge = json.load(f)
    construct_armature(sge)
    
main('D:\\ROMHacking\\WiiHacking\\haruhi_heiretsu\\Heiretsu\\DATA\\files\\seagull_complex.sge.json')