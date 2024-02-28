bl_info = {
    "name": "SGE Model Importer",
    "author": "Jonko",
    "version": (1, 0),
    "blender": (4, 0, 2),
    "location": "File > Import-Export",
    "description": "Import/Export Shade Graphics Environment (SGE) JSON intermediary files from games like Suzumiya Haruhi no Heiretsu",
    "tracker_url": "https://github.com/haroohie-club/HeiretsuTranslationUtility/issues/",
    "category": "Import-Export"
}

import bpy
from . import sge_import, sge_export
from bpy_extras.io_utils import ImportHelper, ExportHelper
from bpy.props import StringProperty, IntProperty

class ImportSgeJson(bpy.types.Operator, ImportHelper):
    bl_idname = "import.sge_json_data"
    bl_label = "Import SGE JSON data"
    bl_options = {'PRESET'}
    filename_ext = ".json"
    filter_glob = StringProperty(default="*.sge.json", options={'HIDDEN'})

    def execute(self, context):
        return sge_import.import_sge(self.filepath)

class ExportSgeJson(bpy.types.Operator, ExportHelper):
    bl_idname = "export.sge_json_data"
    bl_label = "Export SGE JSON data"
    bl_options = {'PRESET'}
    filename_ext= ".json"
    filter_glob = StringProperty(defaults="*.sge.json", options=('HIDDEN'))

    export_sge_model_type: IntProperty(
        name='SGE Model Type',
        description='The model type of the SGE (0: object, 3: character, 4: map, 5: unknown)',
        default=3,
        min=0,
        max=5,
    )

    def execute(self, context):
        return sge_export.export_sge(self.filepath, self.export_sge_model_type)

def menu_func_import(self, context):
    self.layout.operator(ImportSgeJson.bl_idname, text="SGE JSON (.sge.json)")
def menu_func_export(self, context):
    self.layout.operator(ExportSgeJson.bl_idname, text="SGE JSON (.sge.json)")

def register():
    bpy.utils.register_class(ImportSgeJson)
    bpy.types.TOPBAR_MT_file_import.append(menu_func_import)
    bpy.utils.register_class(ExportSgeJson)
    bpy.types.TOPBAR_MT_file_export.append(menu_func_export)

def unregister():
    bpy.utils.unregister_class(ImportSgeJson)
    bpy.types.TOPBAR_MT_file_import.remove(menu_func_import)
    bpy.utils.unregister_class(ExportSgeJson)
    bpy.types.TOPBAR_MT_file_export.remove(menu_func_export)

if __name__ == '__main__':
    register()