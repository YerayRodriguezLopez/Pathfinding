using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private Node[,] NodeMatrix;
    private int startPosx, startPosy;
    private int endPosx, endPosy;
    private List<GameObject> spawnedTokens = new List<GameObject>();
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

        startPosx = Random.Range(0, Size);
        startPosy = Random.Range(0, Size);
        do
        {
            endPosx = Random.Range(0, Size);
            endPosy = Random.Range(0, Size);
        } while (endPosx == startPosx || endPosy == startPosy);

        NodeMatrix = new Node[Size, Size];
        CreateNodes();

        Debug.Log($"Start: ({startPosx},{startPosy})  End: ({endPosx},{endPosy})");
        SpawnToken(tokenStart, NodeMatrix[startPosx, startPosy].RealPosition);
        SpawnToken(tokenEnd, NodeMatrix[endPosx, endPosy].RealPosition);

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
    }

    public void OnStartPressed()
    {
        if (startButton != null) startButton.interactable = false;
        if (restartButton != null) restartButton.interactable = true;

        astarCoroutine = StartCoroutine(RunAStarVisual());
    }

    public void OnRestartPressed()
    {
        if (astarCoroutine != null)
        {
            StopCoroutine(astarCoroutine);
            astarCoroutine = null;
        }
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

        foreach (Node n in openVisited)
        {
            SpawnToken(tokenOpen, n.RealPosition);
            yield return new WaitForSeconds(stepDelay);
        }

        foreach (Node n in closedVisited)
        {
            SpawnToken(tokenClosed, n.RealPosition);
            yield return new WaitForSeconds(stepDelay);
        }

        if (path != null)
        {
            Debug.Log($"Path found! {path.Count} nodes.");
            foreach (Node n in path)
            {
                SpawnToken(tokenPath, n.RealPosition);
                yield return new WaitForSeconds(stepDelay * 2f);
            }
        }
        else
        {
            Debug.LogWarning("No path found.");
        }

        astarCoroutine = null;
    }

    private void SpawnToken(GameObject prefab, Vector2 pos)
    {
        if (prefab == null) return;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        spawnedTokens.Add(go);
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
}