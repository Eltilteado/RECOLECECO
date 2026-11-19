using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RecolecEco
{
    /// <summary>
    /// Punto central de la escena BahiaPuerto: estado de la partida, valores derivados de las mejoras,
    /// bloqueo de controles, pausa y secuencia final.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        /// <summary>Lo fija el menú principal: true = continuar partida guardada.</summary>
        public static bool ContinueRequested;
        /// <summary>Lo fija el menú principal cuando se ejecuta con -autotest.</summary>
        public static string AutoTestMode;

        public const string MenuScene = "MainMenu";
        public const string GameScene = "BahiaPuerto";

        [Header("Referencias de escena")]
        public BoatController boat;
        public BoatCamera boatCamera;
        public Transform boatSpawn;
        public DockZone dock;
        public Transform finaleCameraPoint;
        public Transform finaleLookPoint;

        public GameConfig Config { get; private set; }
        public SaveData State { get; private set; }

        public InventorySystem Inventory { get; private set; }
        public RecyclingSystem Recycling { get; private set; }
        public UpgradeSystem Upgrades { get; private set; }
        public QuestManager Quests { get; private set; }
        public DialogueSystem Dialogue { get; private set; }
        public EnvironmentProgress Environment { get; private set; }
        public CollectionSystem Collection { get; private set; }

        public bool Paused { get; private set; }
        public bool Cinematic { get; set; }
        public bool InVictory { get; set; }
        public int ModalCount { get; private set; }

        public bool ControlsLocked => Paused || Cinematic || ModalCount > 0 || (Dialogue != null && Dialogue.IsOpen);

        public float MaxSpeed => State.HasUpgrade(UpgradeType.Motor) ? Config.maxSpeedUpgraded : Config.maxSpeed;
        public float Reach => State.HasUpgrade(UpgradeType.Herramienta) ? Config.reachUpgraded : Config.reach;
        public int Capacity => State.HasUpgrade(UpgradeType.Bodega) ? Config.capacityUpgraded : Config.capacity;

        void Awake()
        {
            I = this;
            GameEvents.Clear();
            InteractionPrompt.Reset();
            Time.timeScale = 1f;
            Waves.ResetGlobals();
            Config = GameConfig.Load();

            SaveData loaded = ContinueRequested ? SaveManager.Load() : null;
            State = loaded ?? new SaveData();
            State.Sanitize();

            Inventory = GetComponent<InventorySystem>();
            Recycling = GetComponent<RecyclingSystem>();
            Upgrades = GetComponent<UpgradeSystem>();
            Quests = GetComponent<QuestManager>();
            Dialogue = GetComponent<DialogueSystem>();
            Environment = GetComponent<EnvironmentProgress>();
            if (boat != null) Collection = boat.GetComponent<CollectionSystem>();
        }

        void Start()
        {
            // Retira del mar los residuos ya recogidos en la partida guardada.
            foreach (var w in CollectibleWaste.All.ToArray())
                if (State.collectedIds.Contains(w.Id)) w.gameObject.SetActive(false);

            if (boat != null && boatSpawn != null) boat.Teleport(boatSpawn.position, boatSpawn.rotation);

            AudioManager.I.PlayGameMusic();
            GameEvents.RaiseStateChanged();

            if (!State.introShown) StartCoroutine(IntroSequence());
            else
            {
                GameEvents.RaiseToast("Partida cargada. ¡Bienvenido de vuelta!");
                if (State.missionIndex >= 5 && !State.finaleShown) StartCoroutine(FinaleSequence());
            }
        }

        void Update()
        {
            if (!Paused) State.playTime += Time.unscaledDeltaTime;
            Waves.PushToShader(Time.timeSinceLevelLoad);
        }

        IEnumerator IntroSequence()
        {
            yield return new WaitForSeconds(0.6f);
            bool done = false;
            Dialogue.Show(DialogueScripts.Intro, () => done = true);
            while (!done) yield return null;
            UIManager.I?.ShowControls(true);
            while (UIManager.I != null && UIManager.I.ControlsOpen) yield return null;
            State.introShown = true;
            SaveManager.Save(State);
            Quests.AnnounceCurrent();
        }

        // ---------- Modales y pausa ----------
        public void PushModal() { ModalCount++; }
        public void PopModal() { ModalCount = Mathf.Max(0, ModalCount - 1); }

        public void SetPaused(bool p)
        {
            Paused = p;
            Time.timeScale = p ? 0f : 1f;
            AudioListener.pause = false;
        }

        public void AddCredits(int amount)
        {
            State.credits += amount;
            if (amount > 0) State.creditsEarned += amount;
            GameEvents.RaiseStateChanged();
        }

        public void Autosave(string reason)
        {
            if (SaveManager.Save(State)) GameEvents.RaiseToast("Partida guardada (" + reason + ")");
        }

        public void RestartNewGame()
        {
            SaveManager.Delete();
            ContinueRequested = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(GameScene);
        }

        public void GoToMenu(bool save = true)
        {
            if (save && State.introShown) SaveManager.Save(State);
            Time.timeScale = 1f;
            SceneManager.LoadScene(MenuScene);
        }

        // ---------- Final (§4.8) ----------
        public void BeginFinale() { StartCoroutine(FinaleSequence()); }

        IEnumerator FinaleSequence()
        {
            yield return new WaitForSeconds(1.2f);
            while (Dialogue.IsOpen || ModalCount > 0) yield return null;
            Cinematic = true;
            if (boatCamera != null && finaleCameraPoint != null)
                boatCamera.SetCinematic(finaleCameraPoint, finaleLookPoint != null ? finaleLookPoint : dock.transform);
            AudioManager.I.PlaySfx(Sfx.Fanfare);
            yield return new WaitForSeconds(1.5f);
            bool done = false;
            Dialogue.Show(DialogueScripts.Finale, () => done = true);
            while (!done) yield return null;
            State.finaleShown = true;
            SaveManager.Save(State);
            InVictory = true;
            UIManager.I?.ShowVictory();
        }

        public void ContinueSailingAfterVictory()
        {
            InVictory = false;
            Cinematic = false;
            if (boatCamera != null) boatCamera.ClearCinematic();
        }
    }
}
