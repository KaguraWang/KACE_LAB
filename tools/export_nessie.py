"""Export only the user-provided Nessie scene; never modify the source blend."""
import bpy, pathlib, json
root=pathlib.Path('D:/ChemLabUnity/Assets/Art/Nessie')
root.mkdir(parents=True,exist_ok=True)
textures=root/'Textures'
textures.mkdir(exist_ok=True)
report={'source':bpy.data.filepath,'images':[],'meshes':[]}
used={node.image for obj in bpy.context.scene.objects if obj.type=='MESH' for slot in obj.material_slots if slot.material and slot.material.use_nodes for node in slot.material.node_tree.nodes if node.type=='TEX_IMAGE' and node.image}
for i,img in enumerate(bpy.data.images):
    if img not in used:continue
    if img.type not in {'IMAGE','UV_TEST'} or img.name in {'Render Result','Viewer Node'}:continue
    if not img.has_data:img.reload()
    if not img.has_data and len(img.pixels)==0:raise RuntimeError('Missing texture: '+img.name+' '+img.filepath)
    path=textures/('Texture_%02d.png'%i)
    img.filepath_raw=str(path);img.file_format='PNG';img.save()
    report['images'].append({'name':img.name,'file':path.name,'size':list(img.size)})
bpy.ops.object.select_all(action='DESELECT')
for obj in bpy.context.scene.objects:
    if obj.type in {'MESH','ARMATURE'} and obj.name!='Studio ground':
        obj.select_set(True)
        if obj.type=='MESH':report['meshes'].append({'name':obj.name,'vertices':len(obj.data.vertices)})
bpy.ops.export_scene.fbx(filepath=str(root/'Nessie.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},path_mode='COPY',embed_textures=True,add_leaf_bones=False,bake_anim=False)
(root/'ImportReport.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('NESSIE_EXPORT_OK',report)
# Save a self-contained working copy without touching the supplied original.
for img in used:img.pack()
bpy.ops.wm.save_as_mainfile(filepath='D:/ChemLabUnity/ArtSources/Nessie/LittleNessie_Packed.blend')
