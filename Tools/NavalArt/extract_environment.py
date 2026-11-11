import tarfile,gzip,pathlib
project=pathlib.Path(__file__).resolve().parents[2]
source=next(pathlib.Path(r'D:\Download\BaiduNetdiskDownload').rglob('Tropical Enviroment Pack.unitypackage'))
dest=project/'Assets/_Game/NavalArt/Source/Tropical'
mapping={}
with gzip.open(source,'rb') as f,tarfile.open(fileobj=f,mode='r|') as t:
    for m in t:
        if not m.name.endswith('/pathname'):continue
        p=t.extractfile(m).read().decode().splitlines()[0]
        rel=p.removeprefix('Assets/Tropical Environment/')
        if rel==p or not rel.startswith(('Models/','Nature/','Textures/')):continue
        if pathlib.Path(rel).suffix.lower() not in ('.fbx','.png','.tga','.tif','.jpg','.prefab','.mat'):continue
        guid=m.name.split('/')[0];mapping[guid]=rel
count=0
with gzip.open(source,'rb') as f,tarfile.open(fileobj=f,mode='r|') as t:
    for m in t:
        parts=m.name.split('/')
        if len(parts)!=2 or parts[0] not in mapping or parts[1] not in ('asset','asset.meta'):continue
        target=dest/mapping[parts[0]]
        # Preserve original GUIDs for new prefab dependencies. Existing imported assets retain their GUIDs.
        if parts[1]=='asset.meta':
            target=pathlib.Path(str(target)+'.meta')
            if target.exists():continue
        target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(t.extractfile(m).read());count+=1
print('Extracted',count,'files')
