from re import U
import bpy
from mathutils import Vector, Matrix
import json

def construct_materials(sge):
    print('Constructing materials...')
    materials = []
    for sge_material in sge['SgeMaterials']:
        material = bpy.data.materials.new(sge_material['Name'])
        material.use_nodes = True
        bsdf = material.node_tree.nodes['Principled BSDF']
        if len(sge_material['TexturePath']) > 0:
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

def construct_mesh(sge, materials):
    print('Constructing mesh...')
    mesh = bpy.data.meshes.new(sge['Name'] + "_Mesh")
    obj = bpy.data.objects.new(sge['Name'], mesh)
    for material in materials:
        obj.data.materials.append(material)

    vertices = []
    normals = []
    uvcoords = []
    for sge_vertex in sge['SgeVertices']:
        vertices.append(json_vector_to_vector(sge_vertex['Position']))
        normals.append(json_vector_to_vector(sge_vertex['Normal']))
        uvcoords.append(json_vector2_to_vector2(sge_vertex['UVCoords']))
    faces = []
    for sge_face in sge['SgeFaces']:
        faces.append((sge_face['Polygon'][0], sge_face['Polygon'][1], sge_face['Polygon'][2]))

    mesh.from_pydata(vertices, [], faces) # Edges are autocalculated by blender so we can pass a blank array
    mesh.normals_split_custom_set_from_vertices(normals)

    uvlayer = mesh.uv_layers.new()
    for face in mesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            uvlayer.data[loop_idx].uv = uvcoords[vert_idx]
    
    for i in range(len(mesh.polygons)):
        mesh.polygons[i].material_index = sge['SgeFaces'][i]['Material']['Index']
    mesh.update()

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
    construct_armature(sge)
    mesh = construct_mesh(sge, materials)

    sge_collection = bpy.data.collections.new('sge_collection')
    bpy.context.scene.collection.children.link(sge_collection)
    sge_collection.objects.link(mesh)

main('D:\\ROMHacking\\WiiHacking\\haruhi_heiretsu\\Heiretsu\\DATA\\files\\seagull_complex.sge.json')