from pathlib import Path
import re,shutil,json,hashlib
project=Path(__file__).resolve().parents[2]
source=Path(r'E:\Unity\Stylized Water\Assets')
dest=project/'Assets/_Game/WaterIntegration'
records=[]
def copy(p,q):
    q.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,q)
    meta=Path(str(p)+'.meta')
    if meta.exists():shutil.copy2(meta,Path(str(q)+'.meta'))
    records.append({'source':str(p),'destination':str(q.relative_to(project)),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()})
for name in ['Water.shader','Water_Sea_Mat.mat']:
    copy(source/'Shader'/name,dest/'Imported'/name)
for folder in ['SSR','ShallowWater']:
    for p in (source/'Shader'/folder).iterdir():
        if p.suffix in ['.cs','.shader','.hlsl','.compute']:copy(p,dest/'Imported'/folder/p.name)
guids=set(re.findall(r'guid: ([a-f0-9]{32})',(source/'Shader/Water_Sea_Mat.mat').read_text()))
for p in source.rglob('*.meta'):
    m=re.search(r'guid: ([a-f0-9]{32})',p.read_text(errors='ignore'))
    asset=Path(str(p)[:-5])
    if m and m[1] in guids and asset.suffix not in ['.shader'] and asset.is_file():copy(asset,dest/'Imported/Textures'/asset.name)
copy(Path(r'E:\Unity\Water Asset\Assets\FFTOcean\FFTOcean.compute'),dest/'Imported/FFT/FFTOcean.compute')
(dest/'source-manifest.json').write_text(json.dumps(records,indent=2),encoding='utf8')
print('Imported',len(records),'selected source files')
