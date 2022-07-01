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

def construct_mesh(sge, materials):
    print('Constructing mesh...')
    mesh = bpy.data.meshes.new(sge['Name'] + "_Mesh")
    mesh.validate(verbose=True)
    obj = bpy.data.objects.new(sge['Name'] + "_Mesh", mesh)
    for material in materials:
        obj.data.materials.append(material)

    vertices = []
    normals = []
    uvcoords = []
    colors = []
    for sge_vertex in sge['SgeVertices']:
        vertices.append(json_vector_to_vector(sge_vertex['Position']))
        normals.append(json_vector_to_vector(sge_vertex['Normal']))
        uvcoords.append(json_vector2_to_vector2(sge_vertex['UVCoords']))
        colors.append(sge_vertex['Color'])
    faces = []
    for sge_face in sge['SgeFaces']:
        faces.append((sge_face['Polygon'][0], sge_face['Polygon'][1], sge_face['Polygon'][2])) # Faces are inverted so this is the correct order

    mesh.from_pydata(vertices, [], faces) # Edges are autocalculated by blender so we can pass a blank array
    mesh.normals_split_custom_set_from_vertices(normals)

    uvlayer = mesh.uv_layers.new()
    color_layer = mesh.vertex_colors.new()
    for face in mesh.polygons:
        for vert_idx, loop_idx in zip(face.vertices, face.loop_indices):
            color_layer.data[vert_idx].color = (colors[vert_idx]['R'], colors[vert_idx]['G'], colors[vert_idx]['B'], colors[vert_idx]['A'])
            uvlayer.data[loop_idx].uv = uvcoords[vert_idx]

    for vertex in mesh.vertices:
        color_layer.data
    
    for i in range(len(mesh.polygons)):
        if sge['SgeFaces'][i]['Material'] is not None:
            mesh.polygons[i].material_index = sge['SgeFaces'][i]['Material']['Index']
    mesh.update()
    
    for bone in sge['SgeBones']:
        bone_vertex_group = obj.vertex_groups.new(name='Bone' + str(bone['Address']))
        for vertex in bone['VertexGroup']:
            bone_vertex_group.add([int(vertex)], bone['VertexGroup'][vertex], 'ADD')
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
    mesh = construct_mesh(sge, materials)

    sge_collection = bpy.data.collections.new('sge_collection')
    bpy.context.scene.collection.children.link(sge_collection)
    sge_collection.objects.link(mesh)

    mesh.parent = armature
    modifier = mesh.modifiers.new(type='ARMATURE', name='Armature')
    modifier.object = armature

main('D:\\ROMHacking\\WiiHacking\\haruhi_heiretsu\\Heiretsu\\DATA\\files\\seagull_complex.sge.json')