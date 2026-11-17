using System.Collections;
using System.Collections.Generic;
using System.Threading;
using RayneoIdle.Todo;   // SystemTodoClient (friend's package, RayNeo system To-do app)
using UnityEngine;
using UnityEngine.UI;

// To-do card in the HUD. Each checked task sharpens the current artwork.
// When all tasks of the round are done, the artwork is added to the gallery and the next round starts.
//
// Use System Todos ON (default): the rows show the todos the player added by voice in the
// To-do setup step (RayNeo system To-do app). Tapping a row marks it finished in the system app too.
// While the HUD is visible the list is re-read every Poll Seconds:
//   - nothing checked yet in this round -> the card follows the system list exactly (new todos appear)
//   - round in progress -> todos finished elsewhere (system app, phone) are crossed out live
//     and count towards the artwork. New todos wait for the next round.
// Use System Todos OFF: the fixed Description texts are used, as before.
public class TodoList : MonoBehaviour
{
    [System.Serializable]
    public class Task
    {
        public Button button;        // the task row (a Legacy Button)
        public Text label;           // its Text (Legacy) child
        public string description;   // fixed text, only used when Use System Todos is off
        [HideInInspector] public bool done;
        [HideInInspector] public bool inUse;      // row has a task this round
        [HideInInspector] public long timestamp;  // system To-do item key (0 = none)
        [HideInInspector] public Image strike;    // cross-out line, created at runtime
        [HideInInspector] public bool removed;    // already deleted from the system To-do app
    }

    [Tooltip("Row pool. The card shows at most this many tasks per round. Duplicate Task_3 and add it here for more rows.")]
    public Task[] tasks;
    public GalleryGrid gallery;              // UIManager's GalleryGrid
    public ArtworkProgress artworkProgress;  // UIManager's ArtworkProgress

    [Header("System To-do (voice)")]
    public bool useSystemTodos = true;
    [Tooltip("0 = no periodic check (the list is still read when the level starts, when the gallery closes and " +
             "when you come back from another app). On the glasses the periodic check runs on a background thread.")]
    public float pollSeconds = 5f;
    [Tooltip("ON: when there are more unfinished todos than rows, show the NEWEST ones (the ones just added by voice). " +
             "OFF: show the oldest ones first.")]
    public bool preferNewest = true;
    [Tooltip("ON: when an artwork unlocks, its to-dos are deleted from the RayNeo To-do app, so only new ones " +
             "show up in TO-DOS. OFF: they stay there as finished. Either way they never come back on the card.")]
    public bool deleteWhenUnlocked = true;
    [Tooltip("ON: a to-do you cross out on the card is deleted from the RayNeo To-do app right away, so it is gone " +
             "from the TO-DOS list too (it stays crossed out on the card). OFF: it is only marked finished there.")]
    public bool deleteWhenChecked = true;
    [Tooltip("Longer task texts are cut with ... so they stay on one line (the cross-out line needs one line).")]
    public int maxChars = 18;

    [Header("Collapsible list")]
    public GameObject taskList;              // the object holding the task rows
    public Text headerText;                  // the header button's text
    public bool startExpanded = true;

    [Header("Look")]
    public Color todoColor = Color.white;
    public Color doneColor = new Color(0.55f, 1f, 0.6f);
    [Tooltip("Cross-out line thickness in canvas px (X3 Pro: at least 2).")]
    public float strikeThickness = 3f;

    private int currentTile = -1;
    private int roundTotal;      // tasks in the current round
    private bool busy;
    private bool wasVisible;
    private float nextPoll;
    private int lastLoggedCount = -1;

    // Background read (device only): the system To-do app is another process; with Wi-Fi on it can take
    // long enough to answer that reading it on the main thread stalls frames. The worker thread only reads;
    // the results are applied on the main thread in Update.
    private Thread readThread;
    private volatile bool readDone;
    private List<RemoteTodo> readOpen, readFinished;

    // The header is visible whenever the HUD is on screen (GameWorld active, gallery closed).
    private bool Visible => headerText == null || headerText.gameObject.activeInHierarchy;

    void Start()
    {
        for (int i = 0; i < tasks.Length; i++)
        {
            int index = i;
            if (tasks[i].button != null)
                tasks[i].button.onClick.AddListener(() => CompleteTask(index, true));
            CreateStrike(tasks[i]);
        }

        if (taskList != null) taskList.SetActive(startExpanded);
        LoadNextArtwork();
    }

    void Update()
    {
        if (!useSystemTodos) return;
        if (!Visible) { wasVisible = false; return; }

        // A background read finished: apply it here, on the main thread.
        if (readThread != null)
        {
            if (!readDone) return;
            readThread = null;
            ApplySync(readOpen, readFinished);
        }

        // Trigger: HUD just became visible (level started, gallery closed) -> read right away.
        if (!wasVisible) { wasVisible = true; nextPoll = 0f; }
        if (Time.unscaledTime < nextPoll) return;
        nextPoll = pollSeconds > 0f ? Time.unscaledTime + pollSeconds : float.MaxValue;
        BeginRead();
    }

    // Reads the system lists without blocking the frame (device), or directly (Editor fake list).
    void BeginRead()
    {
        if (busy || currentTile < 0 || readThread != null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        readDone = false;
        readThread = new Thread(() =>
        {
            AndroidJNI.AttachCurrentThread(); // JNI calls from a non-Unity thread need this
            try
            {
                readOpen = SystemTodoClient.GetUnfinished();
                readFinished = SystemTodoClient.GetFinished();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[TodoList] Background read failed: " + e.Message);
                readOpen = new List<RemoteTodo>();
                readFinished = new List<RemoteTodo>();
            }
            finally
            {
                AndroidJNI.DetachCurrentThread();
                readDone = true;
            }
        }) { IsBackground = true, Name = "TodoListRead" };
        readThread.Start();
#else
        ApplySync(SystemTodoClient.GetUnfinished(), SystemTodoClient.GetFinished());
#endif
    }

    // Trigger: back from another app (e.g. system To-do screen) -> re-read on the next Update.
    void OnApplicationFocus(bool focus)
    {
        if (focus) nextPoll = 0f;
    }

    /// <summary>
    /// Called by FeatureLauncher right when START ISLAND is tapped: re-reads the system To-do list now,
    /// so the voice todos are on the card the moment the island opens (does not rely on visibility polling).
    /// </summary>
    public void ReloadFromSystem()
    {
        Debug.Log($"[TodoList] Reload requested (useSystemTodos={useSystemTodos}, tile={currentTile}, busy={busy})");
        if (!useSystemTodos) return;
        if (currentTile < 0) LoadNextArtwork();   // e.g. artwork was assigned after start
        Sync();                                   // once, right away, so the card is filled on the first frame
        nextPoll = pollSeconds > 0f ? Time.unscaledTime + pollSeconds : float.MaxValue;
    }

    // Hook to the To-Do header button
    public void ToggleList()
    {
        if (taskList == null) return;
        taskList.SetActive(!taskList.activeSelf);
        UpdateHeader();
    }

    // fromPlayer = tapped on the card (also tell the system app); false = already finished in the system app.
    public void CompleteTask(int index, bool fromPlayer)
    {
        if (busy || currentTile < 0) return;
        if (index < 0 || index >= tasks.Length) return;
        Task t = tasks[index];
        if (!t.inUse || t.done) return;

        if (useSystemTodos && t.timestamp != 0)
        {
            // Hidden from the TO-DOS list from now on, even if the system app refuses the call below.
            TodoArchive.Add(t.timestamp);
            if (fromPlayer && deleteWhenChecked)
            {
                t.removed = SystemTodoClient.Delete(t.timestamp);
                Debug.Log($"[TodoList] Checked off \"{t.description}\", deleted in system app: {(t.removed ? "ok" : "FAILED (hidden anyway)")}");
            }
            else if (fromPlayer)
            {
                bool ok = SystemTodoClient.SetFinished(t.timestamp);
                Debug.Log($"[TodoList] Finished \"{t.description}\" in system app: {(ok ? "ok" : "FAILED")}");
            }
        }

        t.done = true;
        int done = DoneCount();
        Refresh();
        artworkProgress.SetProgress(done, roundTotal);

        if (done == roundTotal) StartCoroutine(FinishArtwork());
    }

    // Kept for old button hookups in the Inspector (OnClick -> CompleteTask(int)).
    public void CompleteTask(int index) => CompleteTask(index, true);

    IEnumerator FinishArtwork()
    {
        busy = true;
        while (artworkProgress.IsAnimating) yield return null;   // wait for the final sharpen

        gallery.UnlockTile(currentTile);
        artworkProgress.SetLabel("Added to gallery!");
        ClearUsedTodos();
        yield return new WaitForSeconds(2f);

        LoadNextArtwork();
        busy = false;
    }

    // Trigger: artwork unlocked. The round's to-dos are used up: archive them (never shown again) and,
    // if Delete When Unlocked is on, remove them from the system To-do app.
    void ClearUsedTodos()
    {
        if (!useSystemTodos) return;
        foreach (Task t in tasks)
        {
            if (!t.inUse || t.timestamp == 0) continue;
            TodoArchive.Add(t.timestamp);
            if (deleteWhenUnlocked && !t.removed)
            {
                bool ok = SystemTodoClient.Delete(t.timestamp);
                Debug.Log($"[TodoList] Delete used todo \"{t.description}\": {(ok ? "ok" : "FAILED (hidden anyway)")}");
            }
        }
    }

    void LoadNextArtwork()
    {
        currentTile = gallery.NextLockedIndex();

        if (currentTile < 0)
        {
            artworkProgress.SetArtwork(null);
            artworkProgress.SetLabel("All artworks collected!");
            foreach (Task t in tasks) if (t.button != null) t.button.interactable = false;
            UpdateHeader();
            return;
        }

        artworkProgress.SetArtwork(gallery.tiles[currentTile].artwork);
        StartRound(useSystemTodos ? ReadUnfinished() : null);
    }

    // ------------------------------------------------------------------ rounds

    // Fills the rows for a new round: system todos (oldest first) or the fixed descriptions.
    void StartRound(List<RemoteTodo> systemTodos)
    {
        roundTotal = 0;
        for (int i = 0; i < tasks.Length; i++)
        {
            Task t = tasks[i];
            t.done = false;
            t.removed = false;
            t.timestamp = 0;
            if (systemTodos == null)
            {
                t.inUse = true;
            }
            else if (i < systemTodos.Count)
            {
                t.inUse = true;
                t.timestamp = systemTodos[i].timestamp;
                t.description = systemTodos[i].text;
            }
            else t.inUse = false;
            if (t.inUse) roundTotal++;
        }

        artworkProgress.SetProgress(0, Mathf.Max(1, roundTotal));
        Refresh();

        var names = new List<string>();
        foreach (Task t in tasks) if (t.inUse) names.Add(t.description);
        Debug.Log($"[TodoList] New round ({(systemTodos == null ? "fixed tasks" : "system todos")}): " +
                  string.Join(", ", names));
    }

    // Immediate (blocking) read + apply. Used once when the level starts.
    void Sync()
    {
        if (busy || currentTile < 0)
        {
            Debug.Log($"[TodoList] Sync skipped (busy={busy}, tile={currentTile})");
            return;
        }
        ApplySync(SystemTodoClient.GetUnfinished(), SystemTodoClient.GetFinished());
    }

    // Applies one read of the system lists (raw unfinished + finished) to the card. Main thread only.
    void ApplySync(List<RemoteTodo> rawOpen, List<RemoteTodo> finishedList)
    {
        if (busy || currentTile < 0) return;
        List<RemoteTodo> unfinished = PrepareUnfinished(new List<RemoteTodo>(rawOpen));
        if (unfinished.Count != lastLoggedCount)
        {
            lastLoggedCount = unfinished.Count;
            Debug.Log($"[TodoList] Read {unfinished.Count} unfinished system todos");
        }

        // Nothing checked yet: mirror the system list (todos added or removed since the round started).
        if (DoneCount() == 0)
        {
            if (!SameAsRound(unfinished)) StartRound(unfinished);
            return;
        }

        // Round in progress: cross out todos that were finished outside the game.
        // Only items actually in the finished list count, so a failed read never ticks everything.
        var finished = new HashSet<long>();
        foreach (RemoteTodo r in finishedList) finished.Add(r.timestamp);
        var open = new HashSet<long>();
        foreach (RemoteTodo r in rawOpen) open.Add(r.timestamp); // full list, not trimmed
        // Both reads empty = probably a failed read: change nothing this time.
        bool readOk = finished.Count > 0 || open.Count > 0;

        for (int i = 0; i < tasks.Length; i++)
        {
            Task t = tasks[i];
            if (!t.inUse || t.done) continue;
            if (finished.Contains(t.timestamp)) CompleteTask(i, false);           // finished elsewhere
            else if (readOk && !open.Contains(t.timestamp)) RemoveFromRound(i);   // deleted in the voice list
        }
    }

    // Trigger: a todo of the running round was deleted in the system app. Drop its row; if every
    // remaining task is already done, the artwork is finished.
    void RemoveFromRound(int index)
    {
        if (busy) return;
        Task t = tasks[index];
        t.inUse = false;
        roundTotal--;
        Debug.Log("[TodoList] Removed deleted todo: " + t.description);
        int done = DoneCount();
        Refresh();
        if (roundTotal <= 0) { StartRound(ReadUnfinished()); return; }
        artworkProgress.SetProgress(done, roundTotal);
        if (done == roundTotal) StartCoroutine(FinishArtwork());
    }

    List<RemoteTodo> ReadUnfinished() => PrepareUnfinished(SystemTodoClient.GetUnfinished());

    // Archive filter, oldest-first sort, newest-N trim. Works on the given list.
    List<RemoteTodo> PrepareUnfinished(List<RemoteTodo> list)
    {
        TodoArchive.FilterOut(list); // used ones never return
        list.Sort((a, b) => a.timestamp.CompareTo(b.timestamp)); // oldest first, same order as the voice panel
        // More todos than rows: keep the newest ones (just added in the voice setup), still shown oldest -> newest.
        if (preferNewest && list.Count > tasks.Length)
            list = list.GetRange(list.Count - tasks.Length, tasks.Length);
        return list;
    }

    bool SameAsRound(List<RemoteTodo> list)
    {
        int n = Mathf.Min(list.Count, tasks.Length);
        if (n != roundTotal) return false;
        for (int i = 0; i < n; i++)
            if (tasks[i].timestamp != list[i].timestamp || tasks[i].description != list[i].text) return false;
        return true;
    }

    int DoneCount()
    {
        int n = 0;
        foreach (Task t in tasks) if (t.inUse && t.done) n++;
        return n;
    }

    // ------------------------------------------------------------------ view

    void Refresh()
    {
        for (int i = 0; i < tasks.Length; i++)
        {
            Task t = tasks[i];
            // No todos at all: first row becomes a hint, the others are hidden.
            bool hintRow = roundTotal == 0 && i == 0;
            if (t.button != null)
            {
                t.button.gameObject.SetActive(t.inUse || hintRow);
                t.button.interactable = t.inUse && !t.done;
            }
            if (t.label != null)
            {
                if (hintRow)
                {
                    t.label.text = "No to-dos yet\nAdd them in TO-DOS";
                    t.label.color = todoColor;
                }
                else
                {
                    t.label.text = (t.done ? "[x]  " : "[  ]  ") + Shorten(t.description, maxChars);
                    t.label.color = t.done ? doneColor : todoColor;
                }
            }
            UpdateStrike(t, t.inUse && t.done && !hintRow);
        }

        artworkProgress.SetLabel(roundTotal == 0
            ? "Add to-dos to reveal"
            : DoneCount() + " / " + roundTotal + " done");
        UpdateHeader();
    }

    void UpdateHeader()
    {
        if (headerText == null) return;
        bool open = taskList == null || taskList.activeSelf;
        headerText.text = "To-Do  (" + DoneCount() + "/" + roundTotal + ")   " + (open ? "[-]" : "[+]");
    }

    static string Shorten(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }

    // ------------------------------------------------------------------ cross-out line

    // Legacy Text has no strikethrough, so each label gets a thin Image laid over its text.
    void CreateStrike(Task t)
    {
        if (t.label == null || t.strike != null) return;
        var go = new GameObject("Strike", typeof(RectTransform));
        go.layer = t.label.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(t.label.rectTransform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        var img = go.AddComponent<Image>();    // no sprite = solid bar
        img.raycastTarget = false;             // never blocks the row's button
        img.enabled = false;
        t.strike = img;
    }

    void UpdateStrike(Task t, bool show)
    {
        if (t.strike == null) return;
        t.strike.enabled = show;
        if (!show) return;

        RectTransform labelRt = t.label.rectTransform;
        float boxW = labelRt.rect.width;
        float w = Mathf.Min(t.label.preferredWidth, boxW);
        // Follow the label's horizontal alignment so the line sits on the text, not on empty space.
        int col = (int)t.label.alignment % 3; // 0 left, 1 center, 2 right
        float x = col == 0 ? 0f : col == 1 ? (boxW - w) * 0.5f : boxW - w;

        var rt = t.strike.rectTransform;
        rt.anchoredPosition = new Vector2(x, 0f);
        rt.sizeDelta = new Vector2(w, Mathf.Max(2f, strikeThickness));
        t.strike.color = doneColor;
    }
}