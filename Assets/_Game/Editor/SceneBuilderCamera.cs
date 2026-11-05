using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using TrenoBambini.Player;

namespace TrenoBambini.EditorTools
{
    /// <summary>Inquadrature del giocatore (Fase 2.4).</summary>
    public enum CamProfilo
    {
        /// <summary>Vicolo: camera vicina, bassa, sopra la spalla (Amerigo spostato a sinistra nell'inquadratura).</summary>
        Esterno,
        /// <summary>Basso di Antonietta (6x5 m): camera più alta e centrata, i muri tra camera e player si nascondono.</summary>
        InternoPiccolo,
        /// <summary>Basso di Antonietta con Amerigo adulto (1,78 m, punto camera a 1,45 m): orbita più bassa e larga, per non inquadrare il soffitto né entrare nei muri.</summary>
        InternoPiccoloAdulto,
        /// <summary>Stanza dei flashback (10x10 m): camera alta e larga.</summary>
        InternoLargo
    }

    public static partial class SceneBuilder
    {
        // Muro* (tranne MuroInvisibile*): la camera li ignora per la collisione, WallFader li nasconde. Le porte (layer PorteVista) restano solide per il player, visibili sempre, e la camera le attraversa.
        static void AssegnaLayerVistaLibera(Scene scene)
        {
            int layer = EnsureLayer(WallFader.LayerName);
            int layerPorte = EnsureLayer(WallFader.LayerPorte);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    bool muro = n.StartsWith("Muro") && !n.StartsWith("MuroInvisibile");
                    bool porta = n.StartsWith("Porta") && t.GetComponent<Collider>() != null;
                    if (porta) t.gameObject.layer = layerPorte; // la camera le attraversa, WallFader non le nasconde mai
                    if (muro && t.GetComponent<Collider>() != null) t.gameObject.layer = layer;
                }
        }

        static int EnsureLayer(string name)
        {
            int existing = LayerMask.NameToLayer(name);
            if (existing >= 0) return existing;
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            for (int i = 8; i < 32; i++)
            {
                var el = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(el.stringValue))
                {
                    el.stringValue = name;
                    tm.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                    if (LayerMask.NameToLayer(name) != i)
                        throw new System.InvalidOperationException("Build interrotto: impossibile creare il layer '" + name + "'.");
                    return i;
                }
            }
            throw new System.InvalidOperationException("Build interrotto: nessun layer libero (8-31) per '" + name + "' in TagManager.");
        }

        static void BuildCameraRig(Camera cam, Transform player, Transform punto, float yaw, CamProfilo profilo)
        {
            int layerVista = EnsureLayer(WallFader.LayerName);
            int layerPorte = EnsureLayer(WallFader.LayerPorte);

            // la Main Camera diventa solo "schermo": il Brain di Cinemachine la muove
            var brain = cam.gameObject.GetComponent<CinemachineBrain>();
            if (brain == null) brain = cam.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);

            var go = new GameObject("CM_Amerigo");
            var vcam = go.AddComponent<CinemachineCamera>();
            vcam.Target.TrackingTarget = punto;
            vcam.Target.LookAtTarget = punto;
            vcam.Lens.NearClipPlane = 0.1f;
            vcam.Lens.FarClipPlane = 150f;

            // Orbita a 3 anelli: l'altezza della camera cambia con l'inclinazione (basso = guarda in su, alto = dall'alto),
            // con la distanza controllata dai raggi degli anelli. Rispetto a ThirdPersonFollow permette di avere forme
            // d'orbita diverse per esterno e interni dal builder e il Deoccluder gestisce le collisioni.
            var orbit = go.AddComponent<CinemachineOrbitalFollow>();
            orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            orbit.TrackerSettings.BindingMode = Unity.Cinemachine.TargetTracking.BindingMode.WorldSpace;
            orbit.TrackerSettings.PositionDamping = new Vector3(0.2f, 0.4f, 0.2f);
            var comp = go.AddComponent<CinemachineRotationComposer>();
            comp.Damping = new Vector2(0.35f, 0.5f);
            comp.CenterOnActivate = true;

            switch (profilo)
            {
                case CamProfilo.InternoPiccolo:
                    vcam.Lens.FieldOfView = 50f;
                    orbit.Orbits = Orbits(top: (3.7f, 1.4f), centro: (2.3f, 3.0f), basso: (1.1f, 2.4f));
                    comp.Composition.ScreenPosition = new Vector2(0f, -0.04f);
                    break;
                case CamProfilo.InternoPiccoloAdulto:
                    // altezze relative al punto camera (1,45 m): camera a ~1,7 / 2,5 / 3,2 m dal suolo, sotto o appena sopra i muri da 3 m
                    vcam.Lens.FieldOfView = 55f;
                    orbit.Orbits = Orbits(top: (1.75f, 1.2f), centro: (1.0f, 2.5f), basso: (0.2f, 2.0f));
                    comp.Composition.ScreenPosition = new Vector2(0f, -0.04f);
                    break;
                case CamProfilo.InternoLargo:
                    vcam.Lens.FieldOfView = 55f;
                    orbit.Orbits = Orbits(top: (4.8f, 1.8f), centro: (2.9f, 4.2f), basso: (1.2f, 3.2f));
                    comp.Composition.ScreenPosition = new Vector2(0f, -0.04f);
                    break;
                default:
                    vcam.Lens.FieldOfView = 52f;
                    orbit.Orbits = Orbits(top: (2.5f, 1.6f), centro: (1.25f, 3.0f), basso: (0.15f, 2.2f));
                    comp.Composition.ScreenPosition = new Vector2(-0.08f, -0.05f); // sopra la spalla destra (ScreenPosition: +-0.5 = bordi dello schermo)
                    break;
            }
            orbit.Orbits.SplineCurvature = 0.5f;

            float off = (profilo == CamProfilo.InternoPiccolo || profilo == CamProfilo.InternoPiccoloAdulto) ? -135f : 0f;
            orbit.HorizontalAxis = new InputAxis
            {
                Value = Mathf.DeltaAngle(0f, yaw + off), Center = Mathf.DeltaAngle(0f, yaw + off),
                Range = new Vector2(-180f, 180f), Wrap = true
            };
            orbit.VerticalAxis = new InputAxis { Value = 0f, Center = 0f, Range = new Vector2(-1f, 1f), Wrap = false };
            orbit.RadialAxis = new InputAxis { Value = 1f, Center = 1f, Range = new Vector2(1f, 1f), Wrap = false };

            // Collisione/occlusione: la camera non attraversa muri e oggetti solidi, tranne Muro*/Porta* (layer VistaLibera,
            // nascosti da WallFader). Il player è escluso via tag.
            var deo = go.AddComponent<CinemachineDeoccluder>();
            deo.CollideAgainst = ~((1 << layerVista) | (1 << layerPorte));
            deo.IgnoreTag = "Player";
            deo.MinimumDistanceFromTarget = 0.5f;
            deo.AvoidObstacles.Enabled = true;
            deo.AvoidObstacles.CameraRadius = 0.3f;
            deo.AvoidObstacles.UseFollowTarget = new CinemachineDeoccluder.ObstacleAvoidance.FollowTargetSettings { Enabled = true, YOffset = 0f };
            deo.AvoidObstacles.Strategy = CinemachineDeoccluder.ObstacleAvoidance.ResolutionStrategy.PullCameraForward;
            deo.AvoidObstacles.Damping = 0.4f;
            deo.AvoidObstacles.DampingWhenOccluded = 0.1f;
            deo.AvoidObstacles.MaximumEffort = 4;
            deo.AvoidObstacles.SmoothingTime = 0.1f;
            deo.ShotQualityEvaluation.Enabled = false;

            var rig = go.AddComponent<CameraRig>();
            rig.vcam = vcam;
            rig.orbit = orbit;
            rig.target = punto;
            // basso: la camera nasce in diagonale (nord-ovest), così la porta d'uscita (parete sud nascosta) resta visibile dietro al player
            rig.snapYawOffset = (profilo == CamProfilo.InternoPiccolo || profilo == CamProfilo.InternoPiccoloAdulto) ? -135f : 0f;

            var fader = go.AddComponent<WallFader>();
            fader.target = punto;

            // posizione iniziale coerente (prima del primo frame del Brain)
            cam.transform.position = player.position + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 2f, -3f);
            cam.transform.LookAt(punto.position);
        }

        static Unity.Cinemachine.Cinemachine3OrbitRig.Settings Orbits((float h, float r) top, (float h, float r) centro, (float h, float r) basso)
        {
            return new Cinemachine3OrbitRig.Settings
            {
                Top = new Cinemachine3OrbitRig.Orbit { Height = top.h, Radius = top.r },
                Center = new Cinemachine3OrbitRig.Orbit { Height = centro.h, Radius = centro.r },
                Bottom = new Cinemachine3OrbitRig.Orbit { Height = basso.h, Radius = basso.r },
                SplineCurvature = 0.5f
            };
        }
    }
}
