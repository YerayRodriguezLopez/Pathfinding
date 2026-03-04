using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Grid")]
    public int Size;
    public BoxCollider2D Panel;

    [Header("Tokens")]
    public GameObject tokenNode;
    public GameObject tokenOpen;
    public GameObject tokenClosed;
    public GameObject tokenPath;
    public GameObject tokenStart;
    public GameObject tokenEnd;

    [Header("Visualization")]
    public float stepDelay = 0.05f;

    [Header("UI")]
    public Button startButton;
    public Button restartButton;
    public Button exitButton;
    public ScrollRect scrollRect;
    public TextMeshProUGUI logText;

    private Node[,] NodeMatrix;
    private int startPosx, startPosy;
    private int endPosx, endPosy;

    private List<GameObject> spawnedTokens = new List<GameObject>();
    private List<GameObject> persistentTokens = new List<GameObject>();

    private Coroutine astarCoroutine;

    void Awake()
    {
        Instance = this;
        Calculs.CalculateDistances(Panel, Size);
    }

    void Start()
    {
        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartPressed);
        if (exitButton != null) exitButton.onClick.AddListener(OnExitPressed);

        if (restartButton != null) restartButton.interactable = false;

        InitGrid();
    }

    private void InitGrid()
    {
        foreach (GameObject go in spawnedTokens)
            if (go != null) Destroy(go);
        spawnedTokens.Clear();

        foreach (GameObject go in persistentTokens)
            if (go != null) Destroy(go);
        persistentTokens.Clear();

        ClearLog();

        startPosx = Random.Range(0, Size);
        startPosy = Random.Range(0, Size);
        do
        {
            endPosx = Random.Range(0, Size);
            endPosy = Random.Range(0, Size);
        } while (endPosx == startPosx && endPosy == startPosy);

        NodeMatrix = new Node[Size, Size];
        CreateNodes();

        Log($"<b>Grid initialised</b>  {Size}x{Size}");
        Log($"Start -> ({startPosx},{startPosy})   End -> ({endPosx},{endPosy})");

        SpawnPersistent(tokenStart, NodeMatrix[startPosx, startPosy].RealPosition);
        SpawnPersistent(tokenEnd, NodeMatrix[endPosx, endPosy].RealPosition);

        if (startButton != null) startButton.interactable = true;
        if (restartButton != null) restartButton.interactable = false;
    }

    public void CreateNodes()
    {
        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
            {
                NodeMatrix[i, j] = new Node(i, j, Calculs.CalculatePoint(i, j));
                NodeMatrix[i, j].Heuristic = Calculs.CalculateHeuristic(NodeMatrix[i, j], endPosx, endPosy);
            }

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                SetWays(NodeMatrix[i, j], i, j);

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                SpawnToken(tokenNode, NodeMatrix[i, j].RealPosition);

        RaisePeristentTokens();
    }

    public void OnStartPressed()
    {
        if (startButton != null) startButton.interactable = false;
        if (restartButton != null) restartButton.interactable = true;

        ClearLog();
        Log("<b>A* started…</b>");
        astarCoroutine = StartCoroutine(RunAStarVisual());
    }

    public void OnRestartPressed()
    {
        if (astarCoroutine != null)
        {
            StopCoroutine(astarCoroutine);
            astarCoroutine = null;
        }

        if (startButton != null) startButton.interactable = true;
        if (restartButton != null) restartButton.interactable = false;

        InitGrid();
    }

    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator RunAStarVisual()
    {
        List<Node> closedVisited;
        List<Node> openVisited;

        List<Node> path = AStarPathfinder.FindPath(
            NodeMatrix,
            startPosx, startPosy,
            endPosx, endPosy,
            out closedVisited,
            out openVisited);

        Log($"<color=#FFEE00>Exploring open list ({openVisited.Count} nodes)…</color>");
        int openIdx = 0;
        foreach (Node n in openVisited)
        {
            SpawnToken(tokenOpen, n.RealPosition);
            RaisePeristentTokens();
            openIdx++;
            Log($"  Open [{openIdx}]  ({n.PositionX},{n.PositionY})  " +
                $"G={n.GCost:F2}  H={n.Heuristic:F2}  F={n.GCost + n.Heuristic:F2}");
            yield return new WaitForSeconds(stepDelay);
        }

        Log($"<color=#FA00D6>Marking closed list ({closedVisited.Count} nodes)…</color>");
        int closedIdx = 0;
        foreach (Node n in closedVisited)
        {
            SpawnToken(tokenClosed, n.RealPosition);
            RaisePeristentTokens();
            closedIdx++;
            Log($"  Closed [{closedIdx}]  ({n.PositionX},{n.PositionY})");
            yield return new WaitForSeconds(stepDelay);
        }

        if (path != null)
        {
            Log($"<color=#00FA0F><b>Path found! {path.Count} nodes.</b></color>");
            int pathIdx = 0;
            foreach (Node n in path)
            {
                SpawnToken(tokenPath, n.RealPosition);
                RaisePeristentTokens();
                pathIdx++;
                Log($"  Path [{pathIdx}]  ({n.PositionX},{n.PositionY})");
                yield return new WaitForSeconds(stepDelay * 2f);
            }
            Log("<b>Done.</b>");
        }
        else
        {
            Log("<color=red><b>No path found!</b></color>");
        }

        if (restartButton != null) restartButton.interactable = true;
        if (startButton != null) startButton.interactable = false;

        astarCoroutine = null;
    }

    private void SpawnToken(GameObject prefab, Vector2 pos)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        spawnedTokens.Add(go);
    }

    private void SpawnPersistent(GameObject prefab, Vector2 pos)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        persistentTokens.Add(go);
    }

    private void RaisePeristentTokens()
    {
        foreach (GameObject go in persistentTokens)
            if (go != null)
                go.transform.SetAsLastSibling();
    }

    public void SetWays(Node node, int x, int y)
    {
        node.WayList = new List<Way>();
        if (x > 0)
        {
            node.WayList.Add(new Way(NodeMatrix[x - 1, y], Calculs.LinearDistance));
            if (y > 0)
                node.WayList.Add(new Way(NodeMatrix[x - 1, y - 1], Calculs.DiagonalDistance));
        }
        if (x < Size - 1)
        {
            node.WayList.Add(new Way(NodeMatrix[x + 1, y], Calculs.LinearDistance));
            if (y > 0)
                node.WayList.Add(new Way(NodeMatrix[x + 1, y - 1], Calculs.DiagonalDistance));
        }
        if (y > 0)
            node.WayList.Add(new Way(NodeMatrix[x, y - 1], Calculs.LinearDistance));
        if (y < Size - 1)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y + 1], Calculs.LinearDistance));
            if (x > 0)
                node.WayList.Add(new Way(NodeMatrix[x - 1, y + 1], Calculs.DiagonalDistance));
            if (x < Size - 1)
                node.WayList.Add(new Way(NodeMatrix[x + 1, y + 1], Calculs.DiagonalDistance));
        }
    }

    private void Log(string message)
    {
        if (logText == null) return;
        logText.text += message + "\n";
        if (scrollRect != null)
            StartCoroutine(ScrollToBottom());
    }

    private void ClearLog()
    {
        if (logText != null) logText.text = "";
    }

    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;
    }
}