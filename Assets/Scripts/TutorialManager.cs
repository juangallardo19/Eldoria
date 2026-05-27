using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// Tutorial — Interior (intro + 4 gates) + Exterior (HoldJump, DropThrough, Durgan)
//          + navigation to Liara's Lookout (HV05) + Ara leads to MTN03 + Sanctuary.
// Pattern: Singleton DDOL + State Machine
// Durgan and Liara: NPCInteract and Collider2D are created at runtime if missing.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    enum Phase
    {
        Inactive,
        IntroDlg,
        GateMove,
        GateJump,
        GateAttack,
        GateCombo,
        ShowDoorArrow,
        GateHoldJump,
        GateDrop,
        DurganApproach,
        DurganDialogue,
        RunHint,
        MapHint,
        LiaraApproach,
        LiaraDialogue,
        AraLeads,
        Done
    }

    Phase          _phase = Phase.Inactive;
    TutorialConfig _cfg;
    NPCInteract    _durganNPC;
    NPCInteract    _liaraNPC;
    bool           _dlgDone;
    bool           _sanctuaryRested;

    static readonly Color c_Ara    = new Color(0.55f, 0.90f, 0.75f);
    static readonly Color c_Kael   = new Color(0.55f, 0.78f, 1.00f);
    static readonly Color c_Durgan = new Color(0.85f, 0.62f, 0.15f);
    static readonly Color c_Liara  = new Color(0.72f, 0.55f, 0.95f);

    static readonly Vector3 DoorWorldPos  = new Vector3(12f, -2f, 0f);
    // Plat_A_LowLeft in HV01_Exterior
    static readonly Vector3 PlatTargetPos = new Vector3(-66.1f, -23.2f, 0f);
    const float PlatGroundThreshold       = -24.5f;

    const float GateX      = 3f;
    const float GateFloorY = -4f;
    const float GateHeight = 7f;
    const float GateWidth  = 0.4f;

    // ── Bootstrap ─────────────────────────────────────────────────────────────
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("[TutorialManager]");
        go.AddComponent<TutorialManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cfg = Resources.Load<TutorialConfig>("TutorialConfig");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ── Scene change detection ────────────────────────────────────────────────

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        SanctuaryFlame.OnRested -= HandleSanctuaryRested;
        _durganNPC       = null;
        _liaraNPC        = null;
        _dlgDone         = false;
        _sanctuaryRested = false;

        if (_phase == Phase.Done || IsTutorialDone())
        {
            _phase = Phase.Done;
            HandleDonePhaseSceneLoad(scene.name);
            return;
        }

        switch (scene.name)
        {
            case EldoriaSceneNames.HV01_Interior:
                if (_phase == Phase.Inactive || _phase == Phase.IntroDlg)
                    StartCoroutine(BeginInteriorTutorial());
                break;

            case EldoriaSceneNames.HV01_Exterior:
                if (_phase == Phase.ShowDoorArrow || _phase == Phase.GateHoldJump ||
                    _phase == Phase.GateDrop      || _phase == Phase.DurganApproach)
                    StartCoroutine(BeginExteriorTutorial());
                else if (_phase == Phase.RunHint || _phase == Phase.MapHint)
                    StartCoroutine(BeginMapTutorial());
                break;

            case EldoriaSceneNames.HV05:
                if (_phase == Phase.LiaraApproach || _phase == Phase.LiaraDialogue)
                    StartCoroutine(BeginLiaraTutorial());
                break;

            case EldoriaSceneNames.MTN03:
                if (_phase == Phase.AraLeads)
                    StartCoroutine(BeginSanctuaryTutorial());
                break;
        }

        // Persistent hint while navigating toward Liara
        if (_phase == Phase.LiaraApproach && scene.name != EldoriaSceneNames.HV05)
        {
            ObjectiveArrow.Hide();
            TutorialHint.Show("Ve al Mirador de Liara — Zona B  [ M ] para el mapa");
            WorldMapController.Instance?.SetTutorialObjective("HUB05");
        }

        // Persistent arrow pointing west while Ara leads toward the Mountains
        if (_phase == Phase.AraLeads && scene.name != EldoriaSceneNames.MTN03)
        {
            TutorialHint.Show("Sigue a Ara  →  Ve hacia el oeste");
            ObjectiveArrow.Show(new Vector3(-9999f, -15f, 0f));
        }
    }

    bool IsTutorialDone()
    {
        if (SaveManager.Instance == null || SaveManager.ActiveSlot < 0) return false;
        var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
        return data != null && data.tutorialDone;
    }

    // ── Interior ─────────────────────────────────────────────────────────────

    IEnumerator BeginInteriorTutorial()
    {
        yield return null;

        PlayerHUD.Instance?.SetVisible(false);
        ObjectiveArrow.Hide();
        TutorialHint.Hide();

        _phase = Phase.IntroDlg;
        if (DialogueManager.Instance != null)
            yield return StartCoroutine(ShowDialogue(BuildSegmentIntro()));

        _phase = Phase.GateMove;
        TutorialHint.Show($"[ {KMove("MoveRight", KeyCode.D, "→")} ] Derecha   [ {KMove("MoveLeft", KeyCode.A, "←")} ] Izquierda");
        SpawnGate(TutorialGate.GateAction.MoveBoth, Phase.GateMove, physical: true);
        yield return new WaitUntil(() => _phase != Phase.GateMove);

        TutorialHint.Hide();
        if (DialogueManager.Instance != null)
            yield return StartCoroutine(ShowDialogue(BuildSegmentJump()));

        _phase = Phase.GateJump;
        TutorialHint.Show($"[ {K("Jump", KeyCode.Z)} ] Saltar");
        SpawnGate(TutorialGate.GateAction.Jump, Phase.GateJump, physical: true);
        yield return new WaitUntil(() => _phase != Phase.GateJump);

        TutorialHint.Hide();
        if (DialogueManager.Instance != null)
            yield return StartCoroutine(ShowDialogue(BuildSegmentAttack()));

        _phase = Phase.GateAttack;
        TutorialHint.Show($"[ {K("Attack", KeyCode.X)} ] Atacar");
        SpawnGate(TutorialGate.GateAction.Attack, Phase.GateAttack, physical: true);
        yield return new WaitUntil(() => _phase != Phase.GateAttack);

        yield return new WaitForSecondsRealtime(0.3f);
        TutorialHint.Show($"[ {K("Attack", KeyCode.X)} × 3 ] Combo completo");
        SpawnGate(TutorialGate.GateAction.Combo3, Phase.GateCombo, physical: true);
        yield return new WaitUntil(() => _phase != Phase.GateCombo);

        TutorialHint.Hide();
        if (DialogueManager.Instance != null)
            yield return StartCoroutine(ShowDialogue(BuildSegmentClose()));

        PlayerHUD.Instance?.SetVisible(true);
        _phase = Phase.ShowDoorArrow;
        ObjectiveArrow.Show(DoorWorldPos);
    }

    // ── Exterior ──────────────────────────────────────────────────────────────

    IEnumerator BeginExteriorTutorial()
    {
        yield return null;
        ObjectiveArrow.Hide();

        // Resume if the player left and returned already in DurganApproach phase
        if (_phase == Phase.DurganApproach)
        {
            SetupDurganNPC();
            yield break;
        }

        // ── HoldJump ─────────────────────────────────────────────────────────
        _phase = Phase.GateHoldJump;
        TutorialHint.Show($"Mantén  [ {K("Jump", KeyCode.Z)} ]  al saltar para alcanzar la plataforma");
        ObjectiveArrow.Show(PlatTargetPos);

        PlayerController player = null;
        while (player == null)
        {
            player = Object.FindObjectOfType<PlayerController>();
            yield return null;
        }

        yield return new WaitUntil(() =>
            player != null &&
            player.IsGrounded &&
            player.transform.position.y >= PlatGroundThreshold);

        // ── DropThrough ──────────────────────────────────────────────────────
        yield return new WaitForSecondsRealtime(0.3f);
        ObjectiveArrow.Hide();
        _phase = Phase.GateDrop;
        TutorialHint.Show("Ahora presiona  [ ↓ ]  para bajar de la plataforma");
        SpawnGate(TutorialGate.GateAction.DropThrough, Phase.GateDrop, physical: false);
        yield return new WaitUntil(() => _phase != Phase.GateDrop);

        // ── NPC Durgan ───────────────────────────────────────────────────────
        yield return new WaitForSecondsRealtime(0.4f);
        TutorialHint.Hide();
        _phase = Phase.DurganApproach;
        SetupDurganNPC();
    }

    // Finds NPC_Durgan by exact name, ensures NPCInteract is present, and subscribes.
    void SetupDurganNPC()
    {
        var go = GameObject.Find("NPC_Durgan");
        if (go == null) return;

        _durganNPC = EnsureNPCInteract(go, size: new Vector2(4f, 3f), offset: new Vector2(0f, 1f));
        _durganNPC.OnInteract += StartDurganDialogue;
        ObjectiveArrow.Show(go.transform.position);
        TutorialHint.Show("Habla con Durgan  [ E ]");
    }

    // ── Mapa + RunHint + Liara ────────────────────────────────────────────────

    IEnumerator BeginMapTutorial()
    {
        yield return null;

        // RunHint: prompt the player to run toward Liara
        _phase = Phase.RunHint;
        TutorialHint.Show($"¡Ve al Mirador de Liara!  Mantén  [ {K("Run", KeyCode.LeftShift)} ]  para correr");
        yield return new WaitForSecondsRealtime(3.5f);

        // MapHint: open WorldMap to locate HV05
        _phase = Phase.MapHint;
        TutorialHint.Show("Presiona  [ M ]  para ver el mapa — localiza el Mirador de Liara");
        WorldMapController.Instance?.SetTutorialObjective("HUB05");

        // Wait for the player to open the map
        yield return new WaitUntil(() =>
            WorldMapController.Instance != null && WorldMapController.Instance.IsOpen);
        TutorialHint.Hide();

        // Wait for the player to close the map
        yield return new WaitUntil(() =>
            WorldMapController.Instance == null || !WorldMapController.Instance.IsOpen);

        yield return new WaitForSecondsRealtime(0.4f);

        // LiaraApproach: arrow toward the interior (KaelHouse door, route to the hub)
        _phase = Phase.LiaraApproach;
        TutorialHint.Show("Ve al Mirador de Liara — Zona B  [ M ] para el mapa");
        ObjectiveArrow.Show(new Vector3(78f, -30f, 0f)); // right exit → HV02 → Liara
    }

    // ── Liara tutorial (HV05) ─────────────────────────────────────────────────

    IEnumerator BeginLiaraTutorial()
    {
        yield return null;
        ObjectiveArrow.Hide();
        TutorialHint.Hide();
        WorldMapController.Instance?.ClearTutorialObjective();

        // Resume if the player left and returned already in LiaraDialogue phase
        if (_phase == Phase.LiaraDialogue)
        {
            SetupLiaraNPC();
            yield break;
        }

        SetupLiaraNPC();
    }

    // Finds NPC_Lyara by exact name, ensures NPCInteract is present, and subscribes.
    void SetupLiaraNPC()
    {
        var go = GameObject.Find("NPC_Lyara");
        if (go == null) return;

        _liaraNPC = EnsureNPCInteract(go, size: new Vector2(4f, 3f), offset: new Vector2(0f, 1f));
        _liaraNPC.OnInteract += StartLiaraDialogue;
        ObjectiveArrow.Show(go.transform.position);
        TutorialHint.Show("Habla con Liara  [ E ]");
    }

    // ── Interior dialogue segments ────────────────────────────────────────────

    DialogueManager.DialoguePage[] BuildSegmentIntro()
    {
        var kael = _cfg?.kaelDialoguePortrait;
        var ara  = _cfg?.araDialoguePortrait;
        return new[]
        {
            DPage("Ara",  c_Ara,  ara,  "Kael... despierta. Algo oscuro avanza desde las profundidades."),
            DPage("Kael", c_Kael, kael, "¿Ara...? ¿Cuándo dejó de salir el sol?"),
            DPage("Ara",  c_Ara,  ara,
                  $"Sin tiempo. Mis energías fluyen a través de ti — muévete con  [ {KMove("MoveRight", KeyCode.D, "→")} ]  y  [ {KMove("MoveLeft", KeyCode.A, "←")} ]."),
        };
    }

    DialogueManager.DialoguePage[] BuildSegmentJump()
    {
        var ara = _cfg?.araDialoguePortrait;
        return new[]
        {
            DPage("Ara", c_Ara, ara, $"Bien. Ahora usa mi impulso — presiona  [ {K("Jump", KeyCode.Z)} ]  para saltar."),
        };
    }

    DialogueManager.DialoguePage[] BuildSegmentAttack()
    {
        var kael = _cfg?.kaelDialoguePortrait;
        var ara  = _cfg?.araDialoguePortrait;
        return new[]
        {
            DPage("Ara",  c_Ara,  ara,  $"El filo de tu espada canaliza mi esencia. Ataca con  [ {K("Attack", KeyCode.X)} ]."),
            DPage("Kael", c_Kael, kael, "Lo siento... hay más fuerza en estos golpes que antes."),
            DPage("Ara",  c_Ara,  ara,  $"Encadena tres golpes rápidos  [ {K("Attack", KeyCode.X)} × 3 ]  para el combo."),
        };
    }

    DialogueManager.DialoguePage[] BuildSegmentClose()
    {
        var kael = _cfg?.kaelDialoguePortrait;
        var ara  = _cfg?.araDialoguePortrait;
        return new[]
        {
            DPage("Ara",  c_Ara,  ara,  "Estás listo, Kael. Ve al exterior — alguien espera allí."),
            DPage("Kael", c_Kael, kael, "Iré. Sea lo que sea que avanza... lo detendré."),
        };
    }

    // ── Durgan dialogue ───────────────────────────────────────────────────────

    void StartDurganDialogue()
    {
        if (_durganNPC != null) _durganNPC.OnInteract -= StartDurganDialogue;
        if (DialogueManager.Instance == null) { StartCoroutine(BeginMapTutorial()); return; }
        _phase = Phase.DurganDialogue;
        ObjectiveArrow.Hide();
        DialogueManager.Instance.Show(BuildDurganDialogue(),
            () => StartCoroutine(BeginMapTutorial()));
    }

    DialogueManager.DialoguePage[] BuildDurganDialogue()
    {
        var kael   = _cfg?.kaelDialoguePortrait;
        var ara    = _cfg?.araDialoguePortrait;
        var durgan = _cfg?.durganDialoguePortrait;
        return new[]
        {
            DPage("Durgan", c_Durgan, durgan, "¡Vaya! ¿Qué fue ese salto? Por fin sales de esa cueva, muchacho."),
            DPage("Kael",   c_Kael,   kael,   "Durgan... ¿qué está pasando? El cielo lleva días sin cambiar."),
            DPage("Durgan", c_Durgan, durgan, "Eso mismo quería preguntarte yo. ¿Qué es eso que te acompaña?"),
            DPage("Ara",    c_Ara,    ara,    "¡Hola! ¡Acabo de despertar! ¿Tú también quieres saber quién soy?"),
            DPage("Durgan", c_Durgan, durgan, "Por los viejos dioses... vi ese brillo al amanecer desde aquí."),
            DPage("Durgan", c_Durgan, durgan, "Kael, hay alguien que necesitas ver. Liara, en su mirador al este."),
            DPage("Kael",   c_Kael,   kael,   "¿Liara? ¿La que estudia el Núcleo?"),
            DPage("Durgan", c_Durgan, durgan, "Lleva cuarenta años mirando hacia el horizonte. Si alguien entiende qué es eso que llevas, es ella."),
            DPage("Durgan", c_Durgan, durgan, "Corre. Y cuídate, muchacho. El cielo no miente."),
        };
    }

    // ── Liara dialogue ────────────────────────────────────────────────────────

    void StartLiaraDialogue()
    {
        if (_liaraNPC != null) _liaraNPC.OnInteract -= StartLiaraDialogue;
        if (DialogueManager.Instance == null) { StartCoroutine(StartAraLeads()); return; }
        _phase = Phase.LiaraDialogue;
        ObjectiveArrow.Hide();
        DialogueManager.Instance.Show(BuildLiaraDialogue(), () => StartCoroutine(StartAraLeads()));
    }

    DialogueManager.DialoguePage[] BuildLiaraDialogue()
    {
        var kael  = _cfg?.kaelDialoguePortrait;
        var ara   = _cfg?.araDialoguePortrait;
        var liara = _cfg?.liaraDialoguePortrait;
        return new[]
        {
            DPage("Liara", c_Liara, liara, "Kael. Sabía que vendrías. Lo sentí al amanecer."),
            DPage("Kael",  c_Kael,  kael,  "¿Sabes qué es esto que llevo?"),
            DPage("Liara", c_Liara, liara, "Sí. El Núcleo tenía seis virtudes. Cuando se fragmentó, cinco se dispersaron por el mundo. Corrompidas. Solas."),
            DPage("Liara", c_Liara, liara, "La sexta nunca la encontramos. Pensamos que se había perdido con el colapso."),
            DPage("Liara", c_Liara, liara, "Ocho años después... está frente a mí."),
            DPage("Ara",   c_Ara,   ara,   "¡Hola! ¿Tú también quieres saber quién soy?"),
            DPage("Liara", c_Liara, liara, "Sé quién eres. Eres la Alegría — la virtud que unía a todas las demás."),
            DPage("Ara",   c_Ara,   ara,   "¿...Alegría? ¡Me gusta! ¡Suena bien!"),
            DPage("Kael",  c_Kael,  kael,  "¿Y qué se supone que haga yo con eso?"),
            DPage("Liara", c_Liara, liara, "Las otras cinco virtudes siguen ahí fuera, corrompidas. Mientras sigan así, Eldoria sigue rota."),
            DPage("Liara", c_Liara, liara, "Ella puede resonar con ellas. Purificarlas. Pero necesita que alguien la lleve."),
            DPage("Kael",  c_Kael,  kael,  "¿Yo."),
            DPage("Liara", c_Liara, liara, "Tú."),
            DPage("Liara", c_Liara, liara, "El primer fragmento está en las Montañas, al oeste. La corrupción allí es Obsesión."),
            DPage("Liara", c_Liara, liara, "Descansa en el Santuario de Ara antes de entrar. Y Kael... escúchala. Ella siente cosas que tú no puedes ver."),
            DPage("Ara",   c_Ara,   ara,   "¡Kael! ¡Somos un equipo!"),
            DPage("Kael",  c_Kael,  kael,  "Eso parece."),
        };
    }

    // ── CompleteTutorial ──────────────────────────────────────────────────────

    void CompleteTutorial()
    {
        _phase = Phase.Done;
        ObjectiveArrow.Hide();
        TutorialHint.Hide();
        WorldMapController.Instance?.ClearTutorialObjective();

        if (SaveManager.Instance != null && SaveManager.ActiveSlot >= 0)
        {
            var data = SaveManager.Instance.Load(SaveManager.ActiveSlot);
            if (data != null)
            {
                data.tutorialDone = true;
                SaveManager.Instance.Save(SaveManager.ActiveSlot, data);
            }
        }
    }

    // ── Ara leads toward the Mountains (post-Liara) ───────────────────────────

    IEnumerator StartAraLeads()
    {
        _phase = Phase.AraLeads;
        if (DialogueManager.Instance != null)
            yield return StartCoroutine(ShowDialogue(BuildAraLeadsDialogue()));
        TutorialHint.Show("Sigue a Ara  →  Ve hacia el oeste");
        ObjectiveArrow.Show(new Vector3(-9999f, -15f, 0f));
    }

    // ── Ara Sanctuary — MTN03 ────────────────────────────────────────────────

    IEnumerator BeginSanctuaryTutorial()
    {
        yield return null;
        ObjectiveArrow.Show(new Vector3(0f, -14.5f, 0f));
        TutorialHint.Show("Descansa en el Santuario de Ara  [ E ]");
        _sanctuaryRested = false;
        SanctuaryFlame.OnRested += HandleSanctuaryRested;
        yield return new WaitUntil(() => _sanctuaryRested);
        SanctuaryFlame.OnRested -= HandleSanctuaryRested;
        ObjectiveArrow.Hide();
        TutorialHint.Hide();
        if (DialogueManager.Instance != null)
            yield return StartCoroutine(ShowDialogue(BuildSanctuaryDialogue()));
        CompleteTutorial();
    }

    void HandleSanctuaryRested() => _sanctuaryRested = true;

    // ── Post-tutorial: NPC idle dialogues ────────────────────────────────────

    void HandleDonePhaseSceneLoad(string sceneName)
    {
        if (sceneName == EldoriaSceneNames.HV01_Exterior)
        {
            var go = GameObject.Find("NPC_Durgan");
            if (go != null)
            {
                _durganNPC = EnsureNPCInteract(go, new Vector2(4f, 3f), new Vector2(0f, 1f));
                AssignIdleDialogue(_durganNPC, BuildDurganIdleDialogue());
            }
        }
        else if (sceneName == EldoriaSceneNames.HV05)
        {
            var go = GameObject.Find("NPC_Lyara");
            if (go != null)
            {
                _liaraNPC = EnsureNPCInteract(go, new Vector2(4f, 3f), new Vector2(0f, 1f));
                AssignIdleDialogue(_liaraNPC, BuildLiaraIdleDialogue());
            }
        }
    }

    // Assigns a persistent dialogue that reactivates after each conversation ends.
    static void AssignIdleDialogue(NPCInteract npc, DialogueManager.DialoguePage[] pages)
    {
        if (npc == null || pages == null || pages.Length == 0) return;
        System.Action cb = null;
        cb = () =>
        {
            if (DialogueManager.Instance == null) return;
            npc.OnInteract -= cb;
            DialogueManager.Instance.Show(pages, () => { if (npc != null) npc.OnInteract += cb; });
        };
        npc.OnInteract += cb;
    }

    // ── Dialogue: Ara leads toward the Mountains ──────────────────────────────

    DialogueManager.DialoguePage[] BuildAraLeadsDialogue()
    {
        var kael = _cfg?.kaelDialoguePortrait;
        var ara  = _cfg?.araDialoguePortrait;
        return new[]
        {
            DPage("Ara",  c_Ara,  ara,  "¡Kael! Siento algo... algo que me llama."),
            DPage("Ara",  c_Ara,  ara,  "Es al oeste. ¡Sígueme!"),
            DPage("Kael", c_Kael, kael, "¿Al oeste? ¿Las Montañas?"),
            DPage("Ara",  c_Ara,  ara,  "¡Sí, eso! ¡Vamos, vamos!"),
        };
    }

    // ── Dialogue: First Sanctuary (MTN03) ────────────────────────────────────

    DialogueManager.DialoguePage[] BuildSanctuaryDialogue()
    {
        var kael = _cfg?.kaelDialoguePortrait;
        var ara  = _cfg?.araDialoguePortrait;
        return new[]
        {
            DPage("Ara",  c_Ara,  ara,  "¡Kael! ¡Ese de ahí! ¡Ese es mío!"),
            DPage("Kael", c_Kael, kael, "¿Cómo sabes que es tuyo?"),
            DPage("Ara",  c_Ara,  ara,  "No sé. Pero lo siento. Es mío."),
            DPage("Kael", c_Kael, kael, "Bien. Aquí podemos descansar."),
            DPage("Ara",  c_Ara,  ara,  "¿Y si morimos, volvemos aquí?"),
            DPage("Kael", c_Kael, kael, "Si yo muero, sí. Tú no mueres."),
            DPage("Ara",  c_Ara,  ara,  "Pues entonces los dos volvemos aquí. Por si acaso."),
            DPage("Ara",  c_Ara,  ara,  "¡Kael! ¡Somos un equipo!"),
            DPage("Kael", c_Kael, kael, "Eso parece."),
        };
    }

    // ── Post-tutorial idle dialogues ──────────────────────────────────────────

    DialogueManager.DialoguePage[] BuildDurganIdleDialogue()
    {
        var durgan = _cfg?.durganDialoguePortrait;
        return new[]
        {
            DPage("Durgan", c_Durgan, durgan, "Volviste. El cielo todavía no ha cambiado."),
            DPage("Durgan", c_Durgan, durgan, "Confío en que tú y eso que llevas en el brazo sabrán qué hacer."),
            DPage("Durgan", c_Durgan, durgan, "Cuídate allá arriba, muchacho."),
        };
    }

    DialogueManager.DialoguePage[] BuildLiaraIdleDialogue()
    {
        var liara = _cfg?.liaraDialoguePortrait;
        return new[]
        {
            DPage("Liara", c_Liara, liara, "¿Cómo van las Montañas?"),
            DPage("Liara", c_Liara, liara, "La corrupción allí lleva años enquistada. No te confíes."),
            DPage("Liara", c_Liara, liara, "Y escúchala. Ara siente cosas antes de que tú las veas."),
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    IEnumerator ShowDialogue(DialogueManager.DialoguePage[] pages)
    {
        _dlgDone = false;
        DialogueManager.Instance.Show(pages, () => _dlgDone = true);
        yield return new WaitUntil(() => _dlgDone);
    }

    void SpawnGate(TutorialGate.GateAction action, Phase currentPhase, bool physical)
    {
        var go = new GameObject($"TutGate_{action}");
        go.transform.position = physical
            ? new Vector3(GateX, GateFloorY + GateHeight * 0.5f, 0f)
            : new Vector3(9999f, 9999f, 0f);

        var gate = go.AddComponent<TutorialGate>();
        gate.requiredAction = action;

        var col       = go.GetComponent<BoxCollider2D>();
        col.isTrigger = !physical;
        if (physical)
        {
            col.size = new Vector2(GateWidth, GateHeight);
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0) go.layer = groundLayer;
        }
        else
        {
            col.size = new Vector2(0.1f, 0.1f);
        }

        var captured = currentPhase;
        gate.onCleared = () => OnGateCleared(captured);
    }

    void OnGateCleared(Phase cleared)
    {
        switch (cleared)
        {
            case Phase.GateMove:   _phase = Phase.GateJump;       break;
            case Phase.GateJump:   _phase = Phase.GateAttack;     break;
            case Phase.GateAttack: _phase = Phase.GateCombo;      break;
            case Phase.GateCombo:  _phase = Phase.ShowDoorArrow;  break;
            case Phase.GateDrop:   _phase = Phase.DurganApproach; break;
        }
    }

    // Finds the first NPCInteract whose name contains 'partialName'
    static NPCInteract FindNPCInteractNamed(string partialName)
    {
        foreach (var npc in Object.FindObjectsOfType<NPCInteract>())
            if (npc.name.IndexOf(partialName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return npc;
        return null;
    }

    // Ensures the GameObject has NPCInteract (and a Collider2D if missing).
    static NPCInteract EnsureNPCInteract(GameObject go, Vector2 size, Vector2 offset)
    {
        var npc = go.GetComponent<NPCInteract>();
        if (npc != null) return npc;

        // Add collider if it doesn't exist
        if (go.GetComponent<Collider2D>() == null)
        {
            var col    = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size      = size;
            col.offset    = offset;
        }
        return go.AddComponent<NPCInteract>();
    }

    static DialogueManager.DialoguePage DPage(string speaker, Color color, Sprite portrait, string text)
        => new DialogueManager.DialoguePage
        {
            speakerName   = speaker,
            nameColor     = color,
            portrait      = portrait,
            portraitBlink = null,
            text          = text
        };

    static string KMove(string actionId, KeyCode def, string arrowSymbol)
    {
        var k = KeyRebindUI.GetKey(actionId, def);
        var n = KeyName(k);
        return n == arrowSymbol ? arrowSymbol : $"{arrowSymbol} / {n}";
    }

    static string K(string actionId, KeyCode def)
        => KeyName(KeyRebindUI.GetKey(actionId, def));

    static string KeyName(KeyCode k)
    {
        switch (k)
        {
            case KeyCode.LeftArrow:  return "←";
            case KeyCode.RightArrow: return "→";
            case KeyCode.UpArrow:    return "↑";
            case KeyCode.DownArrow:  return "↓";
            default:                 return k.ToString().ToUpper();
        }
    }

    static void SetFont(TMP_Text t)
    {
#if UNITY_EDITOR
        var f = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset");
#else
        var f = Resources.Load<TMP_FontAsset>("Fonts/Perfect DOS VGA 437 Win SDF");
#endif
        if (f != null) t.font = f;
    }
}
