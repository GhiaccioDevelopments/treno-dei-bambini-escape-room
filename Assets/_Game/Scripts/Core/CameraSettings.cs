using UnityEngine;

namespace TrenoBambini.Core
{
    /// <summary>
    /// Impostazioni della camera salvate in PlayerPrefs (la UI arriverà nella sotto-fase 2.7).
    /// Chiavi:
    ///   treno_cam_sens  float, moltiplicatore di sensibilità (mouse, stick destro, touch). Default 1, intervallo 0.2-3.
    ///   treno_cam_invy  int 0/1, inversione dell'asse Y. Default 0 (spingere in su = guardare in su).
    /// </summary>
    public static class CameraSettings
    {
        public const string KeySensibilita = "treno_cam_sens";
        public const string KeyInvertiY = "treno_cam_invy";
        public const float SensibilitaDefault = 1f;
        public const float SensibilitaMin = 0.2f;
        public const float SensibilitaMax = 3f;

        public static float Sensibilita
        {
            get { return Mathf.Clamp(PlayerPrefs.GetFloat(KeySensibilita, SensibilitaDefault), SensibilitaMin, SensibilitaMax); }
            set { PlayerPrefs.SetFloat(KeySensibilita, Mathf.Clamp(value, SensibilitaMin, SensibilitaMax)); }
        }

        public static bool InvertiY
        {
            get { return PlayerPrefs.GetInt(KeyInvertiY, 0) != 0; }
            set { PlayerPrefs.SetInt(KeyInvertiY, value ? 1 : 0); }
        }
    }
}
