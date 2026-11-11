"""Run with Blender -b. Source OBJ files stay read-only; export deterministic Unity mesh streams."""
import bpy, pathlib, json, struct, shutil, math, sys
from mathutils import Vector
PROJECT=pathlib.Path(__file__).resolve().parents[2]
SOURCE=pathlib.Path(r'D:\Download\BaiduNetdiskDownload\B301 364艘战舰3D模型_已解压')
DEST=PROJECT/'Assets/_Game/NavalArt/Source'
REPORT=json.loads((PROJECT/'Artifacts/NavalArt/mesh-inspection.json').read_text())

def bounds(o):
    p=[o.matrix_world@v.co for v in o.data.vertices]
    return Vector([min(v[i] for v in p) for i in range(3)]),Vector([max(v[i] for v in p) for i in range(3)])

def export_mesh(objects, name, pivot, rear, scale, out, face_filter=None):
    verts=[]; norms=[]; uvs=[]; groups={}; lookup={}
    def convert(p):
        q=Vector((p.x,p.z,-p.y))
        if rear:q.x=-q.x;q.z=-q.z
        return q
    for ob in objects:
        me=ob.data;me.calc_loop_triangles(); uv=me.uv_layers.active
        for tri in me.loop_triangles:
            if face_filter and not face_filter(ob,[ob.matrix_world@me.vertices[me.loops[li].vertex_index].co for li in tri.loops]):continue
            mat=me.materials[tri.material_index].name if len(me.materials)>tri.material_index and me.materials[tri.material_index] else '_default_'
            face=[]
            for li in tri.loops:
                p=convert(ob.matrix_world@me.vertices[me.loops[li].vertex_index].co-pivot)*scale
                n=convert(ob.matrix_world.to_3x3()@me.corner_normals[li].vector).normalized()
                tex=uv.data[li].uv[:] if uv else (0,0)
                key=tuple(round(v,7) for v in (*p,*n,*tex))
                if key not in lookup:
                    lookup[key]=len(verts);verts.append(p);norms.append(n);uvs.append(tex)
                face.append(lookup[key])
            groups.setdefault(mat,[]).extend(reversed(face))
    with (out/(name+'.bytes')).open('wb') as f:
        f.write(struct.pack('<ii',0x4E41564C,len(verts)))
        for p,n,uv in zip(verts,norms,uvs):f.write(struct.pack('<8f',*p,*n,*uv))
        f.write(struct.pack('<i',len(groups)))
        for mat,indices in groups.items():
            encoded=mat.encode('utf8');f.write(struct.pack('<i',len(encoded)));f.write(encoded)
            f.write(struct.pack('<i',len(indices)));f.write(struct.pack('<%di'%len(indices),*indices))
    return {'name':name,'vertices':len(verts),'triangles':sum(map(len,groups.values()))//3}

for cn,ship,length in [('基林','Gearing',20),('俾斯麦号','Bismarck',32)]:
    bpy.ops.wm.open_mainfile(filepath=str(PROJECT/'Artifacts/NavalArt'/(ship+'_inspection.blend')))
    out=DEST/ship;out.mkdir(parents=True,exist_ok=True);(out/'Textures').mkdir(exist_ok=True)
    scale=length/(REPORT[ship]['bounds'][1][1]-REPORT[ship]['bounds'][0][1])
    manifest={'ship':ship,'length':length,'turrets':[],'meshes':[],'materials':[]}
    taken=set()
    for i,parts in enumerate(REPORT[ship]['turrets']):
        obs=[bpy.data.objects[p['name']] for p in parts];taken.update(o.name for o in obs)
        root=obs[0];lo,hi=bounds(root);pivot=(lo+hi)/2;pivot.z=lo.z
        rear=pivot.y>0
        # Identified from welded component geometry: both barrel sections/boots are isolated.
        ids=((28,29),(30,31)) if ship=='Gearing' else ((1,2),(41,42))
        barrel_objects=[[obs[j] for j in pair] for pair in ids]
        barrel_set={o.name for group in barrel_objects for o in group}
        housing=[o for o in obs if o.name not in barrel_set]
        def sleeve(points):
            if ship!='Bismarck':return 0
            forward=[(p.y-pivot.y)*(1 if rear else -1) for p in points]
            if min(forward)<3.3:return 0
            if all(.75<p.x<1.75 for p in points):return 1
            if all(-1.75<p.x<-.75 for p in points):return -1
            return 0
        name='Turret_%d'%i
        manifest['meshes'].append(export_mesh(housing,name+'_Housing',pivot,rear,scale,out,lambda ob,p:not sleeve(p)))
        t={'position':[pivot.x*scale,pivot.z*scale,-pivot.y*scale],'yaw':180 if rear else 0,'barrels':[]}
        for j,group in enumerate(barrel_objects):
            bs=[bounds(o) for o in group];bl=Vector([min(b[0][k] for b in bs) for k in range(3)]);bh=Vector([max(b[1][k] for b in bs) for k in range(3)])
            # Axis through bore center at the breech, slightly inside the armor aperture.
            tube_lo,tube_hi=bounds(group[0] if ship=='Gearing' else group[1])
            bp=Vector(((tube_lo.x+tube_hi.x)/2,bl.y if rear else bh.y,(tube_lo.z+tube_hi.z)/2))
            bp.y+=-.08 if rear else .08
            if ship=='Bismarck':bp.y=pivot.y+(3.55 if rear else -3.55)
            delta=bp-pivot;pos=[delta.x*scale,delta.z*scale,-delta.y*scale]
            if rear:pos[0]=-pos[0];pos[2]=-pos[2]
            muzzle=abs((bh.y if rear else bl.y)-bp.y)*scale
            t['barrels'].append({'position':pos,'length':muzzle})
            side=1 if bp.x>0 else -1
            manifest['meshes'].append(export_mesh(group+housing,name+'_Barrel_'+str(j),bp,rear,scale,out,lambda ob,p:ob in group or sleeve(p)==side))
        manifest['turrets'].append(t)
    hull=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.name not in taken]
    manifest['meshes'].append(export_mesh(hull,'Hull',Vector((0,0,0)),False,scale,out))
    source=SOURCE/cn/'source'/ship
    current=None
    for line in (source/(ship+'.mtl')).read_text().splitlines():
        if line.startswith('newmtl '):
            current={'name':line[7:].strip(),'texture':''};manifest['materials'].append(current)
        elif line.startswith('map_Kd ') and current is not None:
            tex=source/line[7:].strip();shutil.copy2(tex,out/'Textures'/tex.name);current['texture']=tex.name
    (out/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf8')
    print('PREPARED',ship,flush=True)

# Sequential extraction avoids repeated decompression of the large legacy package.
import runpy
if '--ships-only' not in sys.argv:runpy.run_path(str(PROJECT/'Tools/NavalArt/extract_environment.py'))
