using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TrenoBambini.EditorTools
{
    /// <summary>
    /// Kit modulare generato da codice (Fase 2.3): costruttore di mesh a scatole con UV in metri
    /// ancorate al mondo (la texture PBR continua senza giunture tra un pezzo e l'altro),
    /// tangenti per le normal map e UV lightmap per il bake. Una sola mesh per facciata, con sottomesh per materiale.
    /// </summary>
    internal sealed class KitBuilder
    {
        public const int NX = 1, PX = 2, NY = 4, PY = 8, NZ = 16, PZ = 32;
        public const string Folder = "Assets/_Game/Generated/Kit";
        /// <summary>Percorsi scritti nella ricostruzione corrente (il resto e' orfano e si cancella a fine costruzione).</summary>
        public static readonly HashSet<string> Used = new HashSet<string>();

        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Vector3> n = new List<Vector3>();
        readonly List<Vector2> uv = new List<Vector2>();
        readonly List<int>[] tris;
        public readonly string[] matNames;

        public KitBuilder(params string[] materials)
        {
            matNames = materials;
            tris = new List<int>[materials.Length];
            for (int i = 0; i < tris.Length; i++) tris[i] = new List<int>();
        }

        public int VertexCount { get { return v.Count; } }

        // R017: facce delle scatole gia' emesse, per evitare che due facce con la stessa normale, sullo stesso piano e con materiali diversi
        // si sovrappongano (z-fighting). La scatola nuova, a stare sopra, viene allargata di pochi mm verso l'esterno sulla faccia in conflitto.
        struct FaceRec { public int axis, sign, sub; public float plane, a0, a1, b0, b1; }
        readonly List<FaceRec> faceRecs = new List<FaceRec>();
                int noGrowAxis = -1; float endLo, endHi;
        /// <summary>Sottomesh dei corpi massicci (intonaco del guscio): le sue scatole non si spostano mai, sono gli altri pezzi (pietra, legno, ferro) a staccarsi.</summary>
        public int SubFisso = -1;
        /// <summary>Le facce perpendicolari a 'axis' poste alle estremita' [lo, hi] (testate della parete) non si spostano mai: allungherebbero il modulo e lo farebbero sovrapporre ai vicini.</summary>
        public void ProteggiTestate(int axis, float lo, float hi) { noGrowAxis = axis; endLo = Mathf.Min(lo, hi); endHi = Mathf.Max(lo, hi); }
        /// <summary>Distanza (m) sotto la quale due facce parallele e uguali nel verso si considerano complanari.</summary>
        public const float Complanare = 0.002f;
        /// <summary>Sporgenza (m) con cui la faccia nuova si stacca da quella sottostante: maggiore della tolleranza di VerificaSuperfici.</summary>
        public const float Sporgenza = 0.0035f;

        static float Ax(Vector3 p, int axis) { return axis == 0 ? p.x : axis == 1 ? p.y : p.z; }
        static void SetAx(ref Vector3 p, int axis, float val) { if (axis == 0) p.x = val; else if (axis == 1) p.y = val; else p.z = val; }
        static void Rect(Vector3 mn, Vector3 mx, int axis, out float a0, out float a1, out float b0, out float b1)
        {
            if (axis == 0) { a0 = mn.y; a1 = mx.y; b0 = mn.z; b1 = mx.z; }
            else if (axis == 1) { a0 = mn.x; a1 = mx.x; b0 = mn.z; b1 = mx.z; }
            else { a0 = mn.x; a1 = mx.x; b0 = mn.y; b1 = mx.y; }
        }

        bool InConflitto(int axis, int sign, float plane, Vector3 mn, Vector3 mx, int sub)
        {
            float a0, a1, b0, b1;
            Rect(mn, mx, axis, out a0, out a1, out b0, out b1);
            string mat = matNames[sub];
            foreach (var f in faceRecs)
            {
                if (f.axis != axis || f.sign != sign || Mathf.Abs(f.plane - plane) >= Complanare) continue;
                if (matNames[f.sub] == mat) continue;
                if (Mathf.Min(a1, f.a1) - Mathf.Max(a0, f.a0) < 0.003f || Mathf.Min(b1, f.b1) - Mathf.Max(b0, f.b0) < 0.003f) continue;
                return true;
            }
            return false;
        }

        /// <summary>Scatola allineata agli assi. tile = lato in metri della texture; skip = bit delle facce da omettere.</summary>
        public void Box(Vector3 a, Vector3 b, int sub, float tile, int skip = 0)
        {
            Vector3 mn = Vector3.Min(a, b), mx = Vector3.Max(a, b);
            if (mx.x - mn.x < 1e-4f && mx.y - mn.y < 1e-4f) return;
            // risolve le facce complanari con scatole precedenti (vedi sopra); i corpi massicci (SubFisso) non si muovono
            bool sottile = sub != SubFisso;
            for (int axis = 0; axis < 3 && sottile; axis++)
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    int bit = axis == 0 ? (sg < 0 ? NX : PX) : axis == 1 ? (sg < 0 ? NY : PY) : (sg < 0 ? NZ : PZ);
                    if ((skip & bit) != 0) continue;
                    for (int guard = 0; guard < 4; guard++)
                    {
                        float plane = sg > 0 ? Ax(mx, axis) : Ax(mn, axis);
                        if (axis == noGrowAxis && (Mathf.Abs(plane - endLo) < 0.01f || Mathf.Abs(plane - endHi) < 0.01f)) break;
                        if (!InConflitto(axis, sg, plane, mn, mx, sub)) break;
                        if (sg > 0) SetAx(ref mx, axis, plane + Sporgenza); else SetAx(ref mn, axis, plane - Sporgenza);
                    }
                }
            for (int axis = 0; axis < 3; axis++)
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    int bit = axis == 0 ? (sg < 0 ? NX : PX) : axis == 1 ? (sg < 0 ? NY : PY) : (sg < 0 ? NZ : PZ);
                    if ((skip & bit) != 0) continue;
                    var fr = new FaceRec { axis = axis, sign = sg, sub = sub, plane = sg > 0 ? Ax(mx, axis) : Ax(mn, axis) };
                    Rect(mn, mx, axis, out fr.a0, out fr.a1, out fr.b0, out fr.b1);
                    faceRecs.Add(fr);
                }
            if ((skip & NX) == 0) Face(0, -1, mn, mx, sub, tile);
            if ((skip & PX) == 0) Face(0, 1, mn, mx, sub, tile);
            if ((skip & NY) == 0) Face(1, -1, mn, mx, sub, tile);
            if ((skip & PY) == 0) Face(1, 1, mn, mx, sub, tile);
            if ((skip & NZ) == 0) Face(2, -1, mn, mx, sub, tile);
            if ((skip & PZ) == 0) Face(2, 1, mn, mx, sub, tile);
        }

        void Face(int axis, int sign, Vector3 mn, Vector3 mx, int sub, float tile)
        {
            Vector3 p0, p1, p2, p3, nrm;
            switch (axis)
            {
                case 0:
                    {
                        float x = sign > 0 ? mx.x : mn.x;
                        p0 = new Vector3(x, mn.y, mn.z); p1 = new Vector3(x, mx.y, mn.z);
                        p2 = new Vector3(x, mx.y, mx.z); p3 = new Vector3(x, mn.y, mx.z);
                        nrm = new Vector3(sign, 0, 0);
                        break;
                    }
                case 1:
                    {
                        float y = sign > 0 ? mx.y : mn.y;
                        p0 = new Vector3(mn.x, y, mn.z); p1 = new Vector3(mn.x, y, mx.z);
                        p2 = new Vector3(mx.x, y, mx.z); p3 = new Vector3(mx.x, y, mn.z);
                        nrm = new Vector3(0, sign, 0);
                        break;
                    }
                default:
                    {
                        float z = sign > 0 ? mx.z : mn.z;
                        p0 = new Vector3(mn.x, mn.y, z); p1 = new Vector3(mn.x, mx.y, z);
                        p2 = new Vector3(mx.x, mx.y, z); p3 = new Vector3(mx.x, mn.y, z);
                        nrm = new Vector3(0, 0, sign);
                        break;
                    }
            }
            // verso orario per Unity: cross(p1-p0, p2-p0) deve coincidere con la normale
            bool flip = Vector3.Dot(Vector3.Cross(p1 - p0, p2 - p0), nrm) < 0f;
            int b = v.Count;
            Vector3[] ps = { p0, p1, p2, p3 };
            for (int i = 0; i < 4; i++)
            {
                v.Add(ps[i]);
                n.Add(nrm);
                uv.Add(UvOf(axis, ps[i], tile));
            }
            var t = tris[sub];
            if (!flip) { t.Add(b); t.Add(b + 1); t.Add(b + 2); t.Add(b); t.Add(b + 2); t.Add(b + 3); }
            else { t.Add(b); t.Add(b + 2); t.Add(b + 1); t.Add(b); t.Add(b + 3); t.Add(b + 2); }
        }

        /// <summary>R006: quadrilatero libero (a,b,c,d in ordine); il verso viene scelto in modo che il fronte guardi 'nrm'. UV proiettate sull'asse dominante della normale.</summary>
        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 nrm, int sub, float tile)
        {
            nrm = nrm.normalized;
            int axis = Mathf.Abs(nrm.x) >= Mathf.Abs(nrm.y) && Mathf.Abs(nrm.x) >= Mathf.Abs(nrm.z) ? 0 : (Mathf.Abs(nrm.y) >= Mathf.Abs(nrm.z) ? 1 : 2);
            bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), nrm) < 0f;
            int bi = v.Count;
            Vector3[] ps = { a, b, c, d };
            for (int i = 0; i < 4; i++) { v.Add(ps[i]); n.Add(nrm); uv.Add(UvOf(axis, ps[i], tile)); }
            var t = tris[sub];
            if (!flip) { t.Add(bi); t.Add(bi + 1); t.Add(bi + 2); t.Add(bi); t.Add(bi + 2); t.Add(bi + 3); }
            else { t.Add(bi); t.Add(bi + 2); t.Add(bi + 1); t.Add(bi); t.Add(bi + 3); t.Add(bi + 2); }
        }

        /// <summary>R006: triangolo con normale 'nrm' (usato per le campiture curve degli archi).</summary>
        public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 nrm, int sub, float tile)
        {
            nrm = nrm.normalized;
            int axis = Mathf.Abs(nrm.x) >= Mathf.Abs(nrm.y) && Mathf.Abs(nrm.x) >= Mathf.Abs(nrm.z) ? 0 : (Mathf.Abs(nrm.y) >= Mathf.Abs(nrm.z) ? 1 : 2);
            bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), nrm) < 0f;
            int bi = v.Count;
            Vector3[] ps = { a, b, c };
            for (int i = 0; i < 3; i++) { v.Add(ps[i]); n.Add(nrm); uv.Add(UvOf(axis, ps[i], tile)); }
            var t = tris[sub];
            if (!flip) { t.Add(bi); t.Add(bi + 1); t.Add(bi + 2); } else { t.Add(bi); t.Add(bi + 2); t.Add(bi + 1); }
        }

        /// <summary>R006: tubo a sezione poligonale tra due punti (telai di biciclette, aste, cerchi).</summary>
        public void Tube(Vector3 p0, Vector3 p1, float radius, int sides, int sub, float tile)
        {
            Vector3 dir = (p1 - p0);
            if (dir.sqrMagnitude < 1e-8f) return;
            dir.Normalize();
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 w = Vector3.Cross(dir, u);
            for (int i = 0; i < sides; i++)
            {
                float a0 = i * Mathf.PI * 2f / sides, a1 = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 o0 = (u * Mathf.Cos(a0) + w * Mathf.Sin(a0)), o1 = (u * Mathf.Cos(a1) + w * Mathf.Sin(a1));
                Vector3 nm = (o0 + o1).normalized;
                Quad(p0 + o0 * radius, p0 + o1 * radius, p1 + o1 * radius, p1 + o0 * radius, nm, sub, tile);
            }
        }

        /// <summary>R006: anello (ruota, cerchio) nel piano ortogonale a 'axis', raggio R, sezione r.</summary>
        public void Torus(Vector3 center, Vector3 axis, float R, float r, int segs, int sub, float tile)
        {
            axis.Normalize();
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 w = Vector3.Cross(axis, u);
            for (int i = 0; i < segs; i++)
            {
                float a0 = i * Mathf.PI * 2f / segs, a1 = (i + 1) * Mathf.PI * 2f / segs;
                Vector3 c0 = center + (u * Mathf.Cos(a0) + w * Mathf.Sin(a0)) * R, c1 = center + (u * Mathf.Cos(a1) + w * Mathf.Sin(a1)) * R;
                Tube(c0, c1, r, 4, sub, tile);
            }
        }

        static Vector2 UvOf(int axis, Vector3 p, float tile)
        {
            float k = 1f / Mathf.Max(0.01f, tile);
            if (axis == 0) return new Vector2(p.z * k, p.y * k);
            if (axis == 1) return new Vector2(p.x * k, p.z * k);
            return new Vector2(p.x * k, p.y * k);
        }

        /// <summary>Crea la mesh, la salva in Assets/_Game/Generated/Kit e restituisce l'asset.</summary>
        public Mesh Build(string name, bool lightmapUv = true)
        {
            string path = Folder + "/" + name + ".asset";
            // asset esistente: si riempie la stessa mesh, cosi' GUID e .meta restano stabili tra le ricostruzioni
            var m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool exists = m != null;
            if (exists) m.Clear(); else m = new Mesh();
            m.name = name;
            m.indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.subMeshCount = tris.Length;
            for (int i = 0; i < tris.Length; i++) m.SetTriangles(tris[i], i);
            m.RecalculateBounds();
            m.RecalculateTangents();
            if (lightmapUv && v.Count > 0) Unwrapping.GenerateSecondaryUVSet(m);

            if (exists) EditorUtility.SetDirty(m);
            else AssetDatabase.CreateAsset(m, path);
            Used.Add(path);
            return m;
        }
    }

    /// <summary>Sistema di riferimento di una parete: o = origine sul piano frontale, a = direzione lungo la parete (u),
    /// n = normale uscente verso chi guarda, d = profondita' nel muro (verso -n), y = altezza.</summary>
    internal struct WallFrame
    {
        public Vector3 o, a, n;
        public WallFrame(Vector3 origin, Vector3 along, Vector3 normal)
        {
            o = origin; a = along.normalized; n = normal.normalized;
        }
        public Vector3 W(float u, float d, float y) { return o + a * u - n * d + Vector3.up * y; }
        public float U(Vector3 world) { return Vector3.Dot(world - o, a); }
        /// <summary>Asse mondo (0=x, 2=z) lungo il quale corre la parete.</summary>
        public int AxisU { get { return Mathf.Abs(a.x) >= Mathf.Abs(a.z) ? 0 : 2; } }

        static int Bit(Vector3 dir)
        {
            float ax = Mathf.Abs(dir.x), ay = Mathf.Abs(dir.y), az = Mathf.Abs(dir.z);
            if (ax >= ay && ax >= az) return dir.x > 0 ? KitBuilder.PX : KitBuilder.NX;
            if (ay >= az) return dir.y > 0 ? KitBuilder.PY : KitBuilder.NY;
            return dir.z > 0 ? KitBuilder.PZ : KitBuilder.NZ;
        }

        // skip locali: 1=-u 2=+u 4=fronte(d0) 8=dietro(d1) 16=-y 32=+y
        public int SkipWorld(int local)
        {
            int s = 0;
            if ((local & 1) != 0) s |= Bit(-a);
            if ((local & 2) != 0) s |= Bit(a);
            if ((local & 4) != 0) s |= Bit(n);
            if ((local & 8) != 0) s |= Bit(-n);
            if ((local & 16) != 0) s |= Bit(Vector3.down);
            if ((local & 32) != 0) s |= Bit(Vector3.up);
            return s;
        }

        public void Box(KitBuilder kb, float u0, float u1, float d0, float d1, float y0, float y1, int sub, float tile, int skipLocal = 0)
        {
            kb.Box(W(u0, d0, y0), W(u1, d1, y1), sub, tile, SkipWorld(skipLocal));
        }

        /// <summary>Estremi mondo (AABB) di un blocco locale.</summary>
        public void Bounds(float u0, float u1, float d0, float d1, float y0, float y1, out Vector3 center, out Vector3 size)
        {
            Vector3 a0 = W(u0, d0, y0), a1 = W(u1, d1, y1);
            Vector3 mn = Vector3.Min(a0, a1), mx = Vector3.Max(a0, a1);
            center = (mn + mx) * 0.5f; size = mx - mn;
        }
    }

    /// <summary>Apertura in una parete (finestra o porta). u = centro lungo la parete.</summary>
    internal struct WallOpening
    {
        public float u, w, y0, y1;
        public bool door;
        public WallOpening(float u, float w, float y0, float y1, bool door = false)
        { this.u = u; this.w = w; this.y0 = y0; this.y1 = y1; this.door = door; }
        public float U0 { get { return u - w * 0.5f; } }
        public float U1 { get { return u + w * 0.5f; } }
    }
}
