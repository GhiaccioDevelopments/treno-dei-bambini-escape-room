import os
ROOT = os.environ.get("TRENO_ROOT", r".")  # cartella del progetto Il Treno dei Bambini
SCRATCH = os.environ.get("TRENO_SCRATCH", os.path.join(ROOT, "ArtSource", "tmp"))
import re,sys
main=ROOT
for name in ["Assembly-CSharp","Assembly-CSharp-Editor"]:
    s=open(main+"/"+name+".csproj",encoding='utf-8-sig').read()
    s=re.sub(r'<Compile Include="[^"]*" />','',s)
    s=re.sub(r'<HintPath>(Library[^<]*)</HintPath>',lambda m:'<HintPath>'+main+'/'+m.group(1)+'</HintPath>',s)
    s=re.sub(r'<ProjectReference Include="([^"]*)"',lambda m:'<ProjectReference Include="'+main+'/'+m.group(1)+'"',s)
    glob = 'Assets/_Game/Scripts/**/*.cs' if name=="Assembly-CSharp" else 'Assets/_Game/Editor/**/*.cs'
    s=s.replace('</Project>','<ItemGroup><Compile Include="'+glob+'" /></ItemGroup></Project>')
    open("tmp_"+name+".csproj","w",encoding='utf-8').write(s)
