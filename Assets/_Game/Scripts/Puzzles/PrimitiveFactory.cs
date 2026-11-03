using UnityEngine;

namespace TrenoBambini.Puzzles
{
    /// <summary>Crea semplici modelli low-poly da primitive (postazioni degli enigmi, oggetti nascosti).</summary>
    public static class PrimitiveFactory
    {
        public static GameObject Create(string modello, string name, Vector3 pos, float size, Color color, Transform parent)
        {
            PrimitiveType type = PrimitiveType.Cube;
            Vector3 scale = Vector3.one * size;
            switch ((modello ?? "").ToLowerInvariant())
            {
                case "sfera": type = PrimitiveType.Sphere; break;
                case "cilindro": type = PrimitiveType.Cylinder; scale = new Vector3(size, size * 0.6f, size); break;
                case "capsula": type = PrimitiveType.Capsule; break;
                case "scarpa":
                case "zoccolo": scale = new Vector3(size * 1.7f, size * 0.7f, size * 0.8f); break;
                default: break; // "cubo" e sconosciuti
            }

            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.material.color = color;
            return go;
        }
    }
}
