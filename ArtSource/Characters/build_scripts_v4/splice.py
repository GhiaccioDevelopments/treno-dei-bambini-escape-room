import sys
p, chunk = sys.argv[1], sys.argv[2]
s = open(p, encoding='utf-8').read()
a = s.index('        [MenuItem("Treno/Personaggi/Configura import Amerigo")]')
b = s.index('        /// <summary>Prefab del corpo di Amerigo')
s = s[:a] + open(chunk, encoding='utf-8').read() + s[b:]
open(p, 'w', encoding='utf-8').write(s)
