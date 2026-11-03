using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>Scena 00_Boot: carica subito il titolo.</summary>
    public class BootLoader : MonoBehaviour
    {
        void Start()
        {
            if (SceneLoader.I != null) SceneLoader.I.Load("01_Titolo", null);
            else Debug.LogWarning("SceneLoader non disponibile.");
        }
    }
}
