bl_info = {
    "name": "SGE Model Importer",
    "author": "Jonko",
    "version": (0, 1),
    "blender": (3, 2, 0),
    "location": "File > Import-Export",
    "description": "Import Shade Graphics Environment (SGE) files from games like Suzumiya Haruhi no Heiretsu",
    "category": "Import-Export"
}

import bpy
from . import sge_import
from bpy_extras.io_utils import ImportHelper
from bpy.props import StringProperty

class ImportSgeJson(bpy.types.Operator, ImportHelper):
    bl_idname = "import.sge_json_data"
    bl_label = "Import SGE JSON data"
    bl_options = {'PRESET'}
    filename_ext = ".json"
    filter_glob = StringProperty(default="*.sge.json", options={'HIDDEN'})

    def execute(self, context):
        return sge_import.main(self.filepath)

def menu_func_import(self, context):
    self.layout.operator(ImportSgeJson.bl_idname, text="SGE JSON (.sge.json)")

def register():
    bpy.utils.register_class(ImportSgeJson)
    bpy.types.TOPBAR_MT_file_import.append(menu_func_import)

def unregister():
    bpy.utils.unregister_class(ImportSgeJson)
    bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)

if __name__ == '__main__':
    register()