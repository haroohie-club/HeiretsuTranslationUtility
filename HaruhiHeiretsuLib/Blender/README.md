# SGE Import/Export Scripts

These scripts are intended to be installed as a plugin or run via the Blender executable in headless mode.

Currently, they rely on Blender 4.0 (tested on Blender 4.0.2).

## Preparing a Model for Export
Before exporting a model, you must prepare it for export:

### Character Models
If exporting a character model:

* You must select bones to be demarcated as body parts and add them to bone collections named appropriately. These are not all required, but some have special functions as noted The bone collections are named as follows:
  - NeckBone -- the neck
  - FaceBone -- the face (this bone is where the camera focuses with `CAM_SET` in script mode)
  - ChestBones -- the chest/breasts
  - StomachBone -- the middle of the model's stomach
  - RightHandBone -- center of the right palm
  - LeftHandBone -- center of the left palm (this bone will indicate that a shadow should be cast below it)
  - RightFootBone -- center of the right foot (this bone will indicate that a shadow should be case below it)
  - LeftFootBone -- center of the left foot
  - EybrowBones -- bones controlling the eyebrows of the model
  - RightLegBone -- center of the right leg
  - LeftLegBone -- center of the left leg
  - RightCheekBone -- bone below the right eye
  - LeftCheekBone -- bone below the left eye
* You must also select groups of bones associated with eye and mouth animations. Eye animation bones can be placed in a bone collection called EyesAnimationsGroup while mouth animation bones can be placed in MouthAnimationsGroup. Other animation groups exist but their functions seem to vary depending on the model and are likely not necessary.
* If you want your meshes to have default outlines, you must select each mesh in object mode and give them each two custom properties:
  - OutlineWeight -- this is a float that indicates the thickness of the outline to be drawn around the outer border of the mesh. A typical value is 0.0025
  - OutlineColor -- this is a string that indicates the color of the outline to be drawn. The string is specified as `#ffRRGGBB` where `RR`, `GG`, and `BB` are hexadecimal values between 00 and FF.

## Running in Headless Mode
* Import: `PATH/TO/BLENDER_EXECUTABLE --background -noaudio -P PATH/TO/sge_import.py PATH/TO/model.sge.json`
* Export: `PATH/TO/BLENDER_EXECUTABLE --background -noaudio -P PATH/TO/sge_export.py PATH/TO/model.sge.json MODEL_TYPE`