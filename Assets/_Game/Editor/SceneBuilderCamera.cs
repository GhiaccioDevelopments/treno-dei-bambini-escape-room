using UnityEditor;
using UnityEditor.SceneManagement;
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
        // Muro* (tranne MuroInvisibile*) vanno sul layer VistaLibera ("facciata"): da R018 la camera collide anche con loro (vedi CameraGuard),
        // WallFader resta solo come ultima rete per quelli che nonostante tutto stanno tra camera e giocatore.
        // Le porte (layer PorteVista) restano solide anche per la camera (R018: attraversarle mostrava il vuoto fuori dalla stanza) e WallFader non le nasconde mai.
        static void AssegnaLayerVistaLibera(Scene scene)
        {
            int layer = EnsureLayer(WallFader.LayerName);
            int layerPorte = EnsureLayer(WallFader.LayerPorte);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    bool muro = n.StartsWith("Muro") && !n.StartsWith("MuroInvisibile") && !n.Contains("Soffitto"); // R018: i soffitti restano solidi (layer Default)
                    bool porta = n.StartsWith("Porta") && t.GetComponent<Collider>() != null;
                    if (porta) t.gameObject.layer = layerPorte; // esterni: solide anche per la camera; interni: si dissolvono (MuroDissolvibile) come i muri
                    if (muro && t.GetComponent<Collider>() != null) t.gameObject.layer = layer;
                }
        }

        /// <summary>
        /// R018: negli interni (scene con CameraProfileVolume 'dissolviMuri') muri, pareti e porte diventano dissolvibili (MuroDissolvibile = LODGroup
        /// con cross-fade) e i soffitti tornano sul layer Default (sempre solidi). Idempotente: usato sia dai generatori (BuildBasso) sia dal menu
        /// "Treno/Camera/Applica dissolvenza interni" che aggiorna le scene gia' costruite senza rigenerarle (le lightmap restano collegate).
        /// </summary>
        static int PreparaInterniDissolvibili(Scene scene)
        {
            int layerVista = EnsureLayer(WallFader.LayerName), layerPorte = EnsureLayer(WallFader.LayerPorte);
            bool interno = false;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var v in root.GetComponentsInChildren<CameraProfileVolume>(true)) if (v.dissolviMuri) interno = true;
            if (!interno) return 0;
            int tot = 0;
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name;
                    if (n.StartsWith("Muro") && n.Contains("Soffitto")) { t.gameObject.layer = 0; continue; }
                    bool muro = n.StartsWith("Muro") && !n.StartsWith("MuroInvisibile");
                    bool porta = n.StartsWith("Porta") && t.GetComponent<Collider>() != null;
                    if (!(muro || porta)) continue;
                    if (t.GetComponentInChildren<Renderer>(true) == null) continue;
                    if (muro)
                    {
                        t.gameObject.layer = layerVista;
                        // il vetro delle finestre (collider solo per la camera) sta sullo stesso layer del muro: negli interni non spinge la camera e si dissolve con lui
                        foreach (var f in t.GetComponentsInChildren<Transform>(true)) if (f.name == "VetroFinestraCamera") f.gameObject.layer = layerVista;
                    }
                    MuroDissolvibile.Prepara(t.gameObject);
                    tot++;
                }
            return tot;
        }

        [MenuItem("Treno/Camera/Applica dissolvenza interni")]
        public static void ApplicaDissolvenzaInterni()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = ScenesFolder + "/20_Int_BassoAntonietta.unity";
            var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in sc.GetRootGameObjects())
                foreach (var v in root.GetComponentsInChildren<CameraProfileVolume>(true))
                {
                    v.dissolviMuri = true;
                    if (v.name == "CamVol_Cucina") { v.transform.position = new Vector3(3f, 1.5f, 5.8f); v.GetComponent<BoxCollider>().size = new Vector3(3f, 3f, 5f); }
                    if (v.name == "CamVol_Cortile") { v.transform.position = new Vector3(3.75f, 2.1f, 11.1f); v.GetComponent<BoxCollider>().size = new Vector3(5.1f, 4.2f, 6f); }
                }
            int n = PreparaInterniDissolvibili(sc);
            EditorSceneManager.MarkSceneDirty(sc);
            EditorSceneManager.SaveScene(sc);
            Debug.Log("[Camera] Dissolvenza interni applicata a " + n + " muri/porte del basso (scena salvata, lightmap invariate).");
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

        /// <param name="scalaOrbita">Moltiplicatore di altezze e raggi dell'orbita: 0,85 per il bambino MakeHuman (1,0 m invece di 1,2 m) mantiene la stessa proporzione nell'inquadratura.</param>
        static void BuildCameraRig(Camera cam, Transform player, Transform punto, float yaw, CamProfilo profilo, float scalaOrbita = 1f)
        {
            EnsureLayer(WallFader.LayerName);
            int layerPorte = EnsureLayer(WallFader.LayerPorte);

            // la Main Camera diventa solo "schermo": il Brain di Cinemachine la muove
            var brain = cam.gameObject.GetComponent<CinemachineBrain>();
            if (brain == null) brain = cam.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.8f);

            var go = new GameObject("CM_Amerigo");
            var vcam = go.AddComponent<CinemachineCamera>();
            vcam.Target.TrackingTarget = punto;
            vcam.Target.LookAtTarget = punto;
            vcam.Lens.NearClipPlane = CameraRig.NearClip;
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
                    vcam.Lens.FieldOfView = 56f;
                    orbit.Orbits = Orbits(top: (1.6f, 1.3f), centro: (1.2f, 2.4f), basso: (0.8f, 2.0f));
                    comp.Composition.ScreenPosition = new Vector2(0f, -0.04f);
                    break;
                case CamProfilo.InternoPiccoloAdulto:
                    // altezze relative al punto camera (1,45 m): camera a ~1,7 / 2,5 / 3,2 m dal suolo, sotto o appena sopra i muri da 3 m
                    // R018: l'anello alto sta SOTTO il soffitto da 3 m (1,45 + 1,2 = 2,65 m): la camera non esce mai dalla stanza
                    vcam.Lens.FieldOfView = 58f;
                    orbit.Orbits = Orbits(top: (1.2f, 1.2f), centro: (0.9f, 2.4f), basso: (0.2f, 2.0f));
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
            if (Mathf.Abs(scalaOrbita - 1f) > 0.001f)
            {
                var ob = orbit.Orbits;
                ob.Top = new Cinemachine3OrbitRig.Orbit { Height = ob.Top.Height * scalaOrbita, Radius = ob.Top.Radius * scalaOrbita };
                ob.Center = new Cinemachine3OrbitRig.Orbit { Height = ob.Center.Height * scalaOrbita, Radius = ob.Center.Radius * scalaOrbita };
                ob.Bottom = new Cinemachine3OrbitRig.Orbit { Height = ob.Bottom.Height * scalaOrbita, Radius = ob.Bottom.Radius * scalaOrbita };
                orbit.Orbits = ob;
            }

            float off = (profilo == CamProfilo.InternoPiccolo || profilo == CamProfilo.InternoPiccoloAdulto) ? -135f : 0f;
            orbit.HorizontalAxis = new InputAxis
            {
                Value = Mathf.DeltaAngle(0f, yaw + off), Center = Mathf.DeltaAngle(0f, yaw + off),
                Range = new Vector2(-180f, 180f), Wrap = true
            };
            // R018: negli interni l'inclinazione e' limitata (niente anello bassissimo che guarda il soffitto, niente anello troppo alto)
            bool interno = profilo != CamProfilo.Esterno;
            orbit.VerticalAxis = new InputAxis { Value = 0f, Center = 0f, Range = interno ? new Vector2(-0.7f, 1f) : new Vector2(-1f, 1f), Wrap = false };
            orbit.RadialAxis = new InputAxis { Value = 1f, Center = 1f, Range = new Vector2(1f, 1f), Wrap = false };

            // Collisione/occlusione (R018, regola unica documentata in CameraGuard): la camera non attraversa niente di solido,
            // muri, soffitti e porte compresi (layer VistaLibera e PorteVista inclusi); gli unici esclusi sono i trigger e i props piccoli.
            // Il player è escluso via tag. CameraGuard, subito dopo, copre i casi che il Deoccluder non vede.
            var deo = go.AddComponent<CinemachineDeoccluder>();
            deo.CollideAgainst = ~0;
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

            var guard = go.AddComponent<CameraGuard>();
            guard.target = punto;
            guard.collideAgainst = ~0;

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
