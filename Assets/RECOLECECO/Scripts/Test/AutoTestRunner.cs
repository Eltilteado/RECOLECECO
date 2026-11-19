using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecolecEco
{
    /// <summary>
    /// Prueba automatizada que juega la partida dentro de la BUILD (no solo en el editor) usando
    /// los mismos sistemas que el jugador. Se activa únicamente con argumentos de línea de comandos:
    ///   RECOLECECO.exe -autotest full|save|load [-autotestOut ruta.md] [-autotestShots carpeta]
    /// El piloto automático solo sustituye a las teclas W/A/S/D/E; toda la lógica es la del juego.
    /// </summary>
    public class AutoTestRunner : MonoBehaviour
    {
        public static bool MenuVisited;
        static AutoTestRunner _i;

        string _mode, _outPath, _shotDir;
        readonly List<string> _results = new List<string>();
        readonly List<string> _log = new List<string>();
        int _errors, _exceptions, _assists, _shots;
        float _fpsSum; int _fpsFrames; float _fpsMin = 999f;
        float _start;
        bool _sectorAShot, _recShot;
        float _maxCollectDistance;
        readonly HashSet<CollectibleWaste> _skipped = new HashSet<CollectibleWaste>();
        readonly Dictionary<string, bool> _testStatus = new Dictionary<string, bool>();
        readonly Dictionary<string, string> _testNotes = new Dictionary<string, string>();

        GameManager GM => GameManager.I;

        public static string ModeFromArgs()
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-autotest") return a[i + 1];
            return null;
        }

        static string Arg(string name, string def)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == name) return a[i + 1];
            return def;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string mode = ModeFromArgs();
            if (string.IsNullOrEmpty(mode) || _i != null) return;
            var go = new GameObject("AutoTestRunner");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<AutoTestRunner>();
            _i._mode = mode;
        }

        public static void OnMenuReached() { if (_i != null) _i._menuReached = true; }
        bool _menuReached;

        void Awake()
        {
            Application.logMessageReceived += OnLog;
            _start = Time.realtimeSinceStartup;
        }

        void Start()
        {
            _outPath = Arg("-autotestOut", Path.Combine(Application.persistentDataPath, "autotest_" + _mode + ".md"));
            _shotDir = Arg("-autotestShots", Path.Combine(Application.persistentDataPath, "capturas"));
            Directory.CreateDirectory(_shotDir);
            Log($"Modo {_mode}. Unity {Application.unityVersion}. Plataforma {Application.platform}. Editor: {Application.isEditor}");
            StartCoroutine(Run());
        }

        void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error) { _errors++; _log.Add("ERROR: " + msg); }
            if (type == LogType.Exception) { _exceptions++; _log.Add("EXCEPCIÓN: " + msg + "\n" + stack); }
        }

        void Update()
        {
            if (GM != null && !GM.Paused && Time.unscaledDeltaTime > 0)
            {
                float fps = 1f / Time.unscaledDeltaTime;
                _fpsSum += fps; _fpsFrames++;
                if (Time.realtimeSinceStartup - _start > 8f) _fpsMin = Mathf.Min(_fpsMin, fps);
            }
            // El "jugador" lee los diálogos y cierra la pantalla de controles.
            if (GM != null)
            {
                if (GM.Dialogue.IsOpen && Time.frameCount % 20 == 0) GM.Dialogue.Next();
                if (UIManager.I != null && UIManager.I.ControlsOpen) UIManager.I.ShowControls(false);
            }
        }

        void Log(string s) { _log.Add($"[{Time.realtimeSinceStartup - _start:0.0}s] {s}"); Debug.Log("[AUTOTEST] " + s); }

        void Check(string key, string name, bool ok, string note = "")
        {
            _testStatus[key] = (_testStatus.TryGetValue(key, out var prev) ? prev : true) && ok;
            if (!string.IsNullOrEmpty(note)) _testNotes[key] = (_testNotes.TryGetValue(key, out var n) ? n + "; " : "") + note;
            _results.Add($"- [{(ok ? "OK" : "FALLO")}] {name}{(string.IsNullOrEmpty(note) ? "" : " — " + note)}");
            Log((ok ? "OK: " : "FALLO: ") + name + " " + note);
        }

        IEnumerator WaitGame()
        {
            while (GM == null || SceneManager.GetActiveScene().name != GameManager.GameScene) yield return null;
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator Run()
        {
            yield return WaitGame();
            switch (_mode)
            {
                case "full": yield return FullRun(); break;
                case "save": yield return SaveRun(); break;
                case "load": yield return LoadRun(); break;
            }
            Finish();
        }

        // ------------------------------------------------------------------ helpers de conducción
        IEnumerator DriveTo(Vector3 target, float arrive, float timeout = 70f, Func<bool> done = null)
        {
            var boat = GM.boat;
            float t = 0, best = float.MaxValue, lastImprove = 0;
            int stuck = 0;
            while (true)
            {
                if (done != null && done()) break;
                Vector3 to = target - boat.transform.position; to.y = 0;
                float dist = to.magnitude;
                if (dist <= arrive) break;
                float ang = Vector3.SignedAngle(boat.transform.forward, to, Vector3.up);
                float steer = Mathf.Clamp(ang / 25f, -1f, 1f);
                float th = Mathf.Abs(ang) > 75f ? 0.3f : 1f;
                if (dist < 10f) th = Mathf.Min(th, 0.3f + dist / 14f);

                // Evitar obstáculos delante.
                Vector3 origin = boat.transform.position + Vector3.up * 0.6f;
                if (Physics.SphereCast(origin, 1.4f, boat.transform.forward, out var hit, 9f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.attachedRigidbody == null && dist > hit.distance + 2f)
                {
                    Vector3 local = boat.transform.InverseTransformPoint(hit.point);
                    steer = local.x > 0 ? -1f : 1f;
                    th = 0.5f;
                }
                boat.SetAutopilot(true, th, steer);

                if (dist < best - 0.5f) { best = dist; lastImprove = t; }
                if (t - lastImprove > 7f)
                {
                    stuck++;
                    boat.SetAutopilot(true, -1f, -Mathf.Sign(ang));
                    yield return new WaitForSeconds(1.6f);
                    lastImprove = t;
                    best = float.MaxValue;
                    if (stuck >= 3) { Assist(target, $"atasco al ir a {target}"); break; }
                }
                if (t > timeout) { Assist(target, $"tiempo agotado al ir a {target}"); break; }
                t += Time.deltaTime;
                yield return null;
            }
            boat.SetAutopilot(true, 0, 0);
        }

        void Assist(Vector3 target, string why)
        {
            _assists++;
            var boat = GM.boat;
            Vector3 dir = (target - boat.transform.position); dir.y = 0;
            Vector3 pos = target - dir.normalized * 5f;
            boat.Teleport(pos, Quaternion.LookRotation(dir.normalized));
            Log("ASISTENCIA (teletransporte del piloto automático): " + why);
        }

        IEnumerator WaitDialogues()
        {
            float t = 0;
            while ((GM.Dialogue.IsOpen || (UIManager.I != null && UIManager.I.ControlsOpen)) && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
        }

        CollectibleWaste Nearest(Predicate<CollectibleWaste> f)
        {
            CollectibleWaste best = null; float bd = float.MaxValue;
            foreach (var w in CollectibleWaste.All)
            {
                if (!w.IsAvailable || !f(w) || _skipped.Contains(w)) continue;
                float d = (w.transform.position - GM.boat.transform.position).sqrMagnitude;
                if (d < bd) { bd = d; best = w; }
            }
            return best;
        }

        IEnumerator CollectOne(CollectibleWaste w)
        {
            var col = GM.Collection;
            float reach = GM.Reach;
            yield return DriveTo(w.transform.position, 0f, 60f, () => col.Target == w || (col.Target != null && col.DistanceToTool(col.Target) < reach));
            float waitT = 0;
            while (col.Target == null && waitT < 4f)
            {
                // Pequeña corrección final
                Vector3 to = w.transform.position - GM.boat.transform.position;
                GM.boat.SetAutopilot(true, 0.3f, Mathf.Clamp(Vector3.SignedAngle(GM.boat.transform.forward, to, Vector3.up) / 25f, -1, 1));
                waitT += Time.deltaTime;
                yield return null;
            }
            GM.boat.SetAutopilot(true, 0, 0);
            if (col.Target == null)
            {
                Assist(w.transform.position, "no se alcanzó el residuo " + w.Id);
                yield return null; yield return null;
            }
            yield return WaitDialogues();
            for (int f = 0; f < 10 && col.Target == null; f++) yield return null; // dejar que el sistema detecte el residuo
            if (col.Target == null) { _skipped.Add(w); Log("Residuo omitido por el piloto automático: " + w.Id); yield break; }
            _maxCollectDistance = Mathf.Max(_maxCollectDistance, col.DistanceToTool(col.Target));
            col.TryCollectTarget();
            float t = 0;
            while (col.arm.Busy && t < 6f) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator FillHold(Predicate<CollectibleWaste> filter, int maxItems = 999)
        {
            int got = 0;
            while (!GM.Inventory.IsFull && got < maxItems && GM.State.missionIndex < 5)
            {
                var w = Nearest(filter);
                if (w == null) break;
                int before = GM.Inventory.Used;
                yield return CollectOne(w);
                if (GM.Inventory.Used > before) got++;
                if (!_sectorAShot && GM.Quests.SectorCollected(SectorId.A) >= GM.Quests.SectorTotal(SectorId.A))
                {
                    _sectorAShot = true;
                    yield return ShotFromSector(SectorId.A, "02_sectorA_despues_de_limpiar");
                }
            }
        }

        IEnumerator DockAtPort()
        {
            // Punto de paso frente al muelle (como haría un jugador para evitar barcos anclados y rompeolas).
            Vector3 approach = new Vector3(9f, 0, -28f);
            if (Vector3.Distance(GM.boat.transform.position, GM.dock.transform.position) > 30f)
                yield return DriveTo(approach, 7f, 90f);
            yield return DriveTo(GM.dock.transform.position, GM.dock.radius - 3f, 60f);
            yield return WaitDialogues();
            GM.dock.DockNow();
            float t = 0;
            while (!(GM.boat.IsDocked && UIManager.I.PortOpen) && t < 10f) { t += Time.deltaTime; yield return null; }
        }

        IEnumerator RecycleAll(bool testWrong)
        {
            var ui = UIManager.I;
            ui.OpenRecycling();
            float t = 0;
            while (!ui.RecyclingOpen && t < 20f) { t += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.4f);
            if (!_recShot) { _recShot = true; yield return Shot("04_reciclaje"); }

            int expected = 0;
            for (int i = 0; i < 5; i++) expected += GM.State.inventory[i] * GM.Config.GetWaste((WasteType)i).value;
            int creditsBefore = GM.State.credits;
            int items = GM.Inventory.Used;
            var rec = GM.Recycling;

            while (rec.HasCurrent)
            {
                var cur = rec.Current;
                var info = GM.Config.GetWaste(cur);
                if (testWrong || cur == WasteType.CajaTecnopor)
                {
                    BinType wrong = cur == WasteType.CajaTecnopor ? BinType.OtrosReciclables : (BinType)(((int)info.correctBin + 1) % 5);
                    int c0 = GM.State.credits;
                    ui.Test_SortInto(wrong);
                    bool stillThere = rec.HasCurrent && rec.Current == cur;
                    bool noCredits = GM.State.credits == c0;
                    bool showsCorrect = ui.RecyclingFeedback.Contains(GM.Config.GetBin(info.correctBin).displayName);
                    Check("t3", $"Clasificación incorrecta de «{info.displayName}» en «{GM.Config.GetBin(wrong).displayName}» se rechaza sin créditos y muestra el contenedor correcto",
                        stillThere && noCredits && showsCorrect);
                    if (cur == WasteType.CajaTecnopor) Check("t3", "Tecnopor solo se acepta en «Disposición adecuada» (no reciclable)", !info.recyclable && info.correctBin == BinType.Disposicion);
                    testWrong = false;
                    yield return new WaitForSecondsRealtime(0.4f);
                }
                ui.Test_SortInto(info.correctBin);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            int gained = GM.State.credits - creditsBefore;
            Check("t3", $"Clasificación correcta de {items} residuos mezclados suma {expected} EC esperados y vacía la bodega",
                gained == expected && GM.Inventory.IsEmpty, $"ganados {gained} EC");
            yield return new WaitForSecondsRealtime(0.3f);
            ui.CloseRecycling();
            yield return WaitDialogues();
        }

        IEnumerator BuyUpgrade(UpgradeType t)
        {
            var ui = UIManager.I;
            if (!GM.Upgrades.CanBuy(t)) yield break;
            ui.OpenWorkshop();
            float w = 0;
            while (!ui.WorkshopOpen && w < 20f) { w += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(0.3f);
            if (t == UpgradeType.Motor) yield return Shot("05_taller");
            int c0 = GM.State.credits;
            float speed0 = GM.MaxSpeed, reach0 = GM.Reach; int cap0 = GM.Capacity;
            ui.Test_AskBuy(t);
            yield return new WaitForSecondsRealtime(0.3f);
            ui.Test_ConfirmYes();
            yield return new WaitForSecondsRealtime(0.4f);
            int cost = GM.Config.GetUpgrade(t).cost;
            bool paid = GM.State.credits == c0 - cost && GM.Upgrades.IsOwned(t);
            switch (t)
            {
                case UpgradeType.Motor: Check("t4", "Motor mejorado: velocidad máxima 6 → 8 m/s", paid && Mathf.Approximately(speed0, 6) && Mathf.Approximately(GM.MaxSpeed, 8)); break;
                case UpgradeType.Herramienta: Check("t4", "Herramienta mejorada: alcance 3 → 5 m", paid && Mathf.Approximately(reach0, 3) && Mathf.Approximately(GM.Reach, 5)); break;
                case UpgradeType.Bodega: Check("t4", "Bodega ampliada: capacidad 10 → 18", paid && cap0 == 10 && GM.Capacity == 18 && GM.Inventory.Capacity == 18); break;
            }
            ui.CloseWorkshop();
            yield return WaitDialogues();
        }

        IEnumerator MeasureTopSpeed(float seconds, Action<float> result)
        {
            var boat = GM.boat;
            var rb = boat.GetComponent<Rigidbody>();
            float t = 0, maxV = 0;
            Vector3 last = rb.position;
            while (t < seconds)
            {
                Vector3 dir = (Vector3.zero - boat.transform.position); dir.y = 0; // hacia el centro de la bahía
                float ang = Vector3.SignedAngle(boat.transform.forward, dir, Vector3.up);
                boat.SetAutopilot(true, 1f, Mathf.Clamp(ang / 40f, -0.3f, 0.3f));
                yield return new WaitForFixedUpdate();
                float dt = Time.fixedDeltaTime;
                Vector3 now = rb.position;
                if (t > seconds * 0.6f) maxV = Mathf.Max(maxV, (now - last).magnitude / dt);
                last = now;
                t += dt;
            }
            boat.SetAutopilot(true, 0, 0);
            result(maxV);
        }

        IEnumerator Shot(string name)
        {
            _shots++;
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(_shotDir, name + ".png");
            ScreenCapture.CaptureScreenshot(path);
            yield return null; yield return null;
            Log("Captura: " + path);
        }

        IEnumerator ShotFromSector(SectorId s, string name)
        {
            SectorZone zone = null;
            foreach (var z in GM.Environment.sectors) if (z.id == s) zone = z;
            if (zone == null) yield break;
            var point = new GameObject("ShotPoint").transform;
            var look = new GameObject("ShotLook").transform;
            look.position = zone.transform.position;
            point.position = zone.transform.position + new Vector3(-zone.radius * 0.9f, 16f, -zone.radius * 0.9f);
            GM.boatCamera.SetCinematic(point, look);
            yield return new WaitForSeconds(s == SectorId.A && name.StartsWith("01") ? 2.5f : 4.5f);
            yield return Shot(name);
            GM.boatCamera.ClearCinematic();
            Destroy(point.gameObject); Destroy(look.gameObject);
            yield return new WaitForSeconds(0.8f);
        }

        // ------------------------------------------------------------------ escenarios
        IEnumerator FullRun()
        {
            Time.timeScale = 1f;
            yield return WaitDialogues();
            while (!GM.State.introShown) yield return null;
            Check("t1", "Inicio de partida desde el menú (nueva partida) y presentación de Don Mateo", GM.State.missionIndex == 0);
            yield return ShotFromSector(SectorId.A, "01_sectorA_al_inicio");
            yield return ShotFromSector(SectorId.B, "02a_sectorB_al_inicio");
            yield return Shot("03_hud_navegando");

            // Velocidad base
            float v0 = 0;
            yield return MeasureTopSpeed(5f, v => v0 = v);
            Check("t4", "Velocidad máxima medida sin mejora ≈ 6 m/s", Mathf.Abs(v0 - 6f) < 0.35f, $"{v0:0.00} m/s");

            // Misión 1
            yield return DriveTo(GM.Quests.mission1Point.position, GM.Config.mission1Radius - 3f);
            yield return WaitMission(1);
            Check("t1", "Misión 1 «Primer viaje» completada", GM.State.missionIndex >= 1);

            // Misión 2 + prueba de bodega llena
            yield return FillHold(w => w.Sector == SectorId.A);
            Check("t1", "Misión 2 «Una bahía más limpia» completada", GM.State.missionIndex >= 2);
            var extra = Nearest(w => true);
            if (extra != null && GM.Inventory.IsFull)
            {
                yield return DriveTo(extra.transform.position, 0f, 60f, () => GM.Collection.Target != null);
                GM.boat.SetAutopilot(true, 0, 0);
                yield return null;
                string prompt = InteractionPrompt.Text ?? "";
                float before = GM.Collection.LastFullWarningTime;
                bool started = GM.Collection.TryCollectTarget();
                Check("t2", "Con la bodega llena aparece «Bodega llena, vuelve al puerto» y no se recoge", !started && GM.Collection.LastFullWarningTime > before && prompt.Contains("Bodega llena"), $"aviso: «{prompt}»");
            }
            else Check("t2", "Prueba de bodega llena", false, "no se pudo llenar la bodega");

            yield return DockAtPort();
            Check("t2", "Tras llenar la bodega se puede volver y atracar (E) en el muelle", GM.boat.IsDocked);
            Check("t6", "Guardado automático al atracar (archivo JSON creado)", SaveManager.Exists, SaveManager.FilePath);
            yield return Shot("06_panel_puerto");
            yield return RecycleAll(true);
            yield return WaitMission(3);
            Check("t1", "Misión 3 «Nada se desperdicia» completada", GM.State.missionIndex >= 3);
            yield return BuyUpgrade(UpgradeType.Motor);
            yield return WaitMission(4);
            Check("t1", "Misión 4 «Una chalana mejor» completada", GM.State.missionIndex >= 4);
            UIManager.I.Sail();
            yield return new WaitForSeconds(1.5f);
            float v1 = 0;
            yield return MeasureTopSpeed(5f, v => v1 = v);
            Check("t4", "Velocidad máxima medida con motor mejorado ≈ 8 m/s (se nota al jugar)", Mathf.Abs(v1 - 8f) < 0.4f, $"{v1:0.00} m/s");

            // Resto de la campaña
            int trips = 0;
            bool invShot = false;
            while (GM.State.missionIndex < 5 && trips < 14)
            {
                trips++;
                yield return FillHold(w => w.Sector != SectorId.A || GM.Quests.SectorCollected(SectorId.B) >= GM.Quests.SectorTarget(SectorId.B) && GM.Quests.SectorCollected(SectorId.C) >= GM.Quests.SectorTarget(SectorId.C));
                if (!invShot) { invShot = true; UIManager.I.ToggleInventory(); yield return new WaitForSeconds(0.3f); yield return Shot("07_inventario"); UIManager.I.ToggleInventory(); }
                if (GM.State.missionIndex >= 5) break;
                yield return DockAtPort();
                yield return RecycleAll(false);
                if (!GM.Upgrades.IsOwned(UpgradeType.Herramienta)) yield return BuyUpgrade(UpgradeType.Herramienta);
                if (!GM.Upgrades.IsOwned(UpgradeType.Bodega)) yield return BuyUpgrade(UpgradeType.Bodega);
                UIManager.I.Sail();
                yield return new WaitForSeconds(1.2f);
            }
            if (GM.Upgrades.IsOwned(UpgradeType.Herramienta))
                Check("t4", "Con la herramienta mejorada se recogen residuos a más de 3 m", _maxCollectDistance > 3.05f, $"máx. {_maxCollectDistance:0.00} m");
            Check("t1", "Misión 5 «Guardianes de la bahía» completada (≥80 % en B y C)", GM.State.missionIndex >= 5,
                $"B {GM.Quests.SectorCollected(SectorId.B)}/{GM.Quests.SectorTotal(SectorId.B)}, C {GM.Quests.SectorCollected(SectorId.C)}/{GM.Quests.SectorTotal(SectorId.C)}");

            float vt = 0;
            while (!UIManager.I.VictoryOpen && vt < 60f) { vt += Time.unscaledDeltaTime; yield return null; }
            yield return new WaitForSecondsRealtime(1f);
            Check("t1", "Pantalla final con estadísticas y botones «Seguir navegando» / «Volver al menú»", UIManager.I.VictoryOpen);
            yield return Shot("08_victoria");
            Check("t1", "Limpieza de la bahía al final", true, $"{GM.Environment.CleanPercent} % · {GM.State.creditsEarned} EC ganados · mejoras {GM.State.UpgradeCount}/3");

            UIManager.I.Test_ContinueSailing();
            yield return new WaitForSeconds(1f);
            yield return ShotFromSector(SectorId.B, "02b_sectorB_despues_de_limpiar");
            yield return ShotFromSector(SectorId.A, "02c_sectorA_al_final");
            Check("t1", "«Seguir navegando» devuelve el control de la chalana", !GM.ControlsLocked);

            // Prueba 5: pausa → reiniciar → menú
            UIManager.I.SetPause(true);
            yield return new WaitForSecondsRealtime(0.5f);
            Check("t5", "Esc abre la pausa y detiene el juego", UIManager.I.PauseOpen && Time.timeScale == 0f);
            yield return Shot("09_pausa");
            int oldMission = GM.State.missionIndex;
            GM.RestartNewGame();
            yield return new WaitForSecondsRealtime(1f);
            yield return WaitGame();
            yield return WaitDialogues();
            Check("t5", "Pausa → «Reiniciar partida» empieza de cero", GM.State.missionIndex == 0 && GM.State.credits == 0 && GM.State.collectedIds.Count == 0 && oldMission == 5);
            float it = 0;
            while (!GM.State.introShown && it < 30f) { it += Time.unscaledDeltaTime; yield return null; }
            UIManager.I.SetPause(true);
            yield return new WaitForSecondsRealtime(0.3f);
            GM.GoToMenu(true);
            float mt = 0;
            while (!_menuReached && mt < 15f) { mt += Time.unscaledDeltaTime; yield return null; }
            Check("t5", "Pausa → «Menú principal» vuelve al menú", SceneManager.GetActiveScene().name == GameManager.MenuScene);
            yield return Shot("00_menu_principal");
        }

        IEnumerator WaitMission(int index)
        {
            float t = 0;
            while (GM.State.missionIndex < index && t < 10f) { t += Time.deltaTime; yield return null; }
            yield return WaitDialogues();
        }

        string ExpectedPath => Path.Combine(Application.persistentDataPath, "autotest_esperado.json");

        IEnumerator SaveRun()
        {
            yield return WaitDialogues();
            while (!GM.State.introShown) yield return null;
            yield return DriveTo(GM.Quests.mission1Point.position, GM.Config.mission1Radius - 3f);
            yield return WaitMission(1);
            yield return FillHold(w => w.Sector == SectorId.A, 8);
            yield return DockAtPort();
            yield return RecycleAll(false);
            yield return BuyUpgrade(UpgradeType.Motor);
            yield return WaitMission(4);
            // El progreso ya se guardó automáticamente (atraque + misiones completadas).
            var s = GM.State;
            var exp = new SaveData
            {
                credits = s.credits, missionIndex = s.missionIndex, upgrades = (bool[])s.upgrades.Clone(),
                collectedIds = new List<string>(s.collectedIds), totalSortedCorrect = s.totalSortedCorrect
            };
            File.WriteAllText(ExpectedPath, JsonUtility.ToJson(exp, true));
            Check("t6", "Partida jugada y guardada automáticamente antes de cerrar", SaveManager.Exists,
                $"misión {s.missionIndex + 1}, {s.credits} EC, {s.collectedIds.Count} residuos retirados, motor={s.HasUpgrade(UpgradeType.Motor)}");
        }

        IEnumerator LoadRun()
        {
            yield return new WaitForSeconds(1f);
            var exp = File.Exists(ExpectedPath) ? JsonUtility.FromJson<SaveData>(File.ReadAllText(ExpectedPath)) : null;
            if (exp == null) { Check("t6", "Cargar partida", false, "no existe el archivo esperado de la ejecución anterior"); yield break; }
            var s = GM.State;
            bool sameIds = s.collectedIds.Count == exp.collectedIds.Count && exp.collectedIds.TrueForAll(id => s.collectedIds.Contains(id));
            int hiddenOk = 0;
            foreach (var w in CollectibleWaste.All) if (s.collectedIds.Contains(w.Id) && !w.gameObject.activeSelf) hiddenOk++;
            Check("t6", "Al reabrir el .exe y elegir «Continuar» se mantienen misión, EcoCréditos y mejoras",
                s.missionIndex == exp.missionIndex && s.credits == exp.credits && s.HasUpgrade(UpgradeType.Motor) == exp.HasUpgrade(UpgradeType.Motor) && Mathf.Approximately(GM.MaxSpeed, exp.HasUpgrade(UpgradeType.Motor) ? 8 : 6),
                $"misión {s.missionIndex + 1}, {s.credits} EC, velocidad máx. {GM.MaxSpeed}");
            Check("t6", "Los residuos retirados siguen fuera del mar tras reabrir", sameIds && hiddenOk == exp.collectedIds.Count, $"{hiddenOk}/{exp.collectedIds.Count}");
            yield return Shot("10_partida_cargada");
        }

        void Finish()
        {
            Time.timeScale = 1f;
            var sb = new StringBuilder();
            sb.AppendLine($"# Resultado de prueba automatizada ({_mode})");
            sb.AppendLine();
            sb.AppendLine($"- Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"- Unity: {Application.unityVersion} · Plataforma: {Application.platform} · Build (no editor): {!Application.isEditor}");
            sb.AppendLine($"- Resolución: {Screen.width}x{Screen.height} · GPU: {SystemInfo.graphicsDeviceName}");
            sb.AppendLine($"- Duración real: {Time.realtimeSinceStartup - _start:0} s");
            if (_fpsFrames > 0) sb.AppendLine($"- FPS medio: {_fpsSum / _fpsFrames:0} · FPS mínimo (tras carga): {_fpsMin:0}");
            sb.AppendLine($"- Errores en consola: {_errors} · Excepciones: {_exceptions}");
            sb.AppendLine($"- Asistencias del piloto automático (teletransporte por atasco): {_assists}");
            sb.AppendLine();
            sb.AppendLine("## Pruebas");
            foreach (var kv in _testStatus) sb.AppendLine($"- Prueba {kv.Key}: **{(kv.Value ? "SUPERADA" : "FALLIDA")}**");
            sb.AppendLine();
            sb.AppendLine("## Detalle");
            foreach (var r in _results) sb.AppendLine(r);
            sb.AppendLine();
            sb.AppendLine("## Registro");
            foreach (var l in _log) sb.AppendLine("    " + l.Replace("\n", "\n    "));
            try { File.WriteAllText(_outPath, sb.ToString()); } catch (Exception e) { Debug.LogError(e); }
            Debug.Log("[AUTOTEST] Terminado: " + _outPath);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(_testStatus.ContainsValue(false) || _exceptions > 0 ? 1 : 0);
#endif
        }
    }
}
