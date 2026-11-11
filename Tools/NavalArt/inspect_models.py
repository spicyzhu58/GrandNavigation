import bpy, pathlib, json
from mathutils import Vector
ROOT=pathlib.Path(r'D:\Download\BaiduNetdiskDownload\B301 364艘战舰3D模型_已解压')
OUT=pathlib.Path(__file__).resolve().parents[2]/'Artifacts/NavalArt'
report={}
for name,folder,prefix in [('基林','Gearing','AGS011'),('俾斯麦号','Bismarck','GGM059')]:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.wm.obj_import(filepath=str(ROOT/name/'source'/folder/(folder+'.obj')),use_split_objects=True,use_split_groups=True)
    objects=[o for o in bpy.context.scene.objects if o.type=='MESH']
    points=[o.matrix_world@Vector(c) for o in objects for c in o.bound_box]
    report[folder]={'bounds':[[min(p[i] for p in points) for i in range(3)],[max(p[i] for p in points) for i in range(3)]],'turrets':[]}
    for o in list(objects):
        if not any(m and m.name.startswith(prefix) for m in o.data.materials) or 'turret' not in o.name.lower(): continue
        bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o
        bpy.ops.object.transform_apply(location=False,rotation=True,scale=True)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.remove_doubles(threshold=.00001);bpy.ops.mesh.separate(type='LOOSE');bpy.ops.object.mode_set(mode='OBJECT')
        parts=[]
        for p in bpy.context.selected_objects:
            pts=[p.matrix_world@v.co for v in p.data.vertices]
            lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
            parts.append({'name':p.name,'verts':len(pts),'lo':lo,'hi':hi})
        report[folder]['turrets'].append(parts)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(folder+'_inspection.blend')))
(OUT/'mesh-inspection.json').write_text(json.dumps(report,indent=2),encoding='utf8')
