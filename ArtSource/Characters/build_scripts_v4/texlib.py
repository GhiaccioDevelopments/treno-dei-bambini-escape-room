import numpy as np, zlib, struct

def write_png(path, arr):
    """arr: HxWxC uint8, row 0 = TOP of image."""
    arr = np.ascontiguousarray(arr.astype(np.uint8))
    h, w = arr.shape[:2]
    c = 1 if arr.ndim == 2 else arr.shape[2]
    ct = {1: 0, 3: 2, 4: 6}[c]
    raw = b''.join(b'\x00' + arr[y].tobytes() for y in range(h))
    def chunk(t, d):
        x = struct.pack('>I', len(d)) + t + d
        return x + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', w, h, 8, ct, 0, 0, 0)) \
        + chunk(b'IDAT', zlib.compress(raw, 6)) + chunk(b'IEND', b'')
    open(path, 'wb').write(png)

def read_png_rgba(path):
    """minimal reader for 8bit PNG written by us / non-interlaced"""
    d = open(path, 'rb').read()
    pos = 8; idat = b''
    while pos < len(d):
        ln = struct.unpack('>I', d[pos:pos+4])[0]; t = d[pos+4:pos+8]
        body = d[pos+8:pos+8+ln]
        if t == b'IHDR':
            w, h, bd, ct = struct.unpack('>IIBB', body[:10])
        elif t == b'IDAT':
            idat += body
        pos += 12 + ln
    ch = {0: 1, 2: 3, 6: 4}[ct]
    raw = zlib.decompress(idat)
    st = w * ch + 1
    out = np.zeros((h, w * ch), np.uint8)
    prev = np.zeros(w * ch, np.int32)
    for y in range(h):
        f = raw[y*st]; line = np.frombuffer(raw[y*st+1:(y+1)*st], np.uint8).astype(np.int32)
        if f == 0: cur = line
        elif f == 1:
            cur = line.copy()
            for i in range(ch, len(cur)): cur[i] = (cur[i] + cur[i-ch]) & 255
        elif f == 2: cur = (line + prev) & 255
        elif f == 3:
            cur = line.copy()
            for i in range(len(cur)):
                a = cur[i-ch] if i >= ch else 0
                cur[i] = (cur[i] + ((a + prev[i]) >> 1)) & 255
        else:
            cur = line.copy()
            for i in range(len(cur)):
                a = cur[i-ch] if i >= ch else 0; b = prev[i]; c = prev[i-ch] if i >= ch else 0
                p = a + b - c; pa = abs(p-a); pb = abs(p-b); pc = abs(p-c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                cur[i] = (cur[i] + pr) & 255
        out[y] = cur; prev = cur
    return out.reshape(h, w, ch)

def noise(h, w, scale, seed, octaves=1, persist=0.5):
    """tileable fractal-ish noise in [0,1] using FFT-filtered white noise. scale = feature size in px"""
    rng = np.random.default_rng(seed)
    tot = np.zeros((h, w)); amp = 1.0; norm = 0
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    r = np.sqrt(fx*fx + fy*fy) + 1e-9
    for o in range(octaves):
        s = max(scale / (2 ** o), 1.5)
        wn = rng.standard_normal((h, w))
        F = np.fft.fft2(wn) * np.exp(-(r * s) ** 2 * 2.0)
        n = np.real(np.fft.ifft2(F))
        n = (n - n.mean()) / (n.std() + 1e-9)
        tot += amp * n; norm += amp; amp *= persist
    tot /= norm
    return np.clip(0.5 + tot * 0.22, 0, 1)

def height_to_normal(hm, strength=2.0):
    gx = (np.roll(hm, -1, 1) - np.roll(hm, 1, 1)) * strength
    gy = (np.roll(hm, -1, 0) - np.roll(hm, 1, 0)) * strength
    # image row 0 is top => v up = -row. Use OpenGL convention (Y+ up) as Blender/Unity expect.
    nx = -gx; ny = gy; nz = np.ones_like(hm)
    l = np.sqrt(nx*nx + ny*ny + nz*nz)
    n = np.stack([nx/l, ny/l, nz/l], -1)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)

def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a + 1e-9), 0, 1)
    return t * t * (3 - 2 * t)

def dilate_rgb(rgb, alpha, iters=8):
    """spread colors into transparent pixels (bleeding) to avoid dark halos"""
    rgb = rgb.copy(); a = alpha > 0.5
    for _ in range(iters):
        acc = np.zeros_like(rgb); cnt = np.zeros(a.shape)
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1)):
            m = np.roll(a, (dy, dx), (0, 1))
            acc += np.roll(rgb, (dy, dx), (0, 1)) * m[..., None]; cnt += m
        fill = (~a) & (cnt > 0)
        rgb[fill] = acc[fill] / cnt[fill][:, None]
        a = a | fill
    return rgb
