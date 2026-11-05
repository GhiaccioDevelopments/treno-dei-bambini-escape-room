import sys, os, soundfile as sf, numpy as np
src, dst, lvl = sys.argv[1], sys.argv[2], float(sys.argv[3])
ids = sys.argv[4:]
os.makedirs(dst, exist_ok=True)
tot = 0
for i in ids:
    d, sr = sf.read(os.path.join(src, i + ".wav"), dtype="float32")
    out = os.path.join(dst, i + ".ogg")
    with sf.SoundFile(out, "w", samplerate=sr, channels=1, format="OGG", subtype="VORBIS", compression_level=lvl) as f:
        for k in range(0, len(d), 4096): f.write(d[k:k+4096])
    n = os.path.getsize(out); tot += n
    r, _ = sf.read(out, dtype="float32")
    L = min(len(r), len(d))
    err = float(np.sqrt(np.mean((r[:L]-d[:L])**2)) / (np.sqrt(np.mean(d**2)) + 1e-9))
    print(f"{i}\t{n/1024:.1f} KB\tlen {len(r)}/{len(d)}\trel_err {err:.3f}")
print("TOTALE %.1f KB" % (tot/1024))
