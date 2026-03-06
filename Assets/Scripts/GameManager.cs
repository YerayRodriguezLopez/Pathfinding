using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public int Size = 8;
    public BoxCollider2D Panel;

    public GameObject tokenNode;
    public GameObject tokenOpen;
    public GameObject tokenClosed;
    public GameObject tokenPath;
    public GameObject tokenStart;
    public GameObject tokenEnd;

    public float stepDelay = 0.5f;

    public Button startButton;
    public Button restartButton;
    public Button exitButton;

    public ScrollRect scrollRect;
    public TextMeshProUGUI logText;
    public RectTransform logContent;

    private Node[,] nodeMatrix;
    private GameObject[,] tokenObjects;

    private GameObject startTokenObj;
    private GameObject endTokenObj;

    private int startX, startY, endX, endY;
    private bool isRunning = false;

    void Start()
    {
        Calculs.CalculateDistances(Panel, Size);

        startButton.onClick.AddListener(OnStartClicked);
        restartButton.onClick.AddListener(OnRestartClicked);
        exitButton.onClick.AddListener(OnExitClicked);

        InitGrid();
    }

    void InitGrid()
    {
        if (tokenObjects != null)
        {
            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    if (tokenObjects[i, j] != null)
                        Destroy(tokenObjects[i, j]);
        }

        if (startTokenObj != null) Destroy(startTokenObj);
        if (endTokenObj != null) Destroy(endTokenObj);
        startTokenObj = null;
        endTokenObj = null;

        nodeMatrix = new Node[Size, Size];
        tokenObjects = new GameObject[Size, Size];

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
            {
                Vector2 pos = Calculs.CalculatePoint(i, j);
                nodeMatrix[i, j] = new Node(i, j, pos);
            }

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
            {
                nodeMatrix[i, j].WayList = new List<Way>();
                int[] dx = { -1, 1, 0, 0 };
                int[] dy = { 0, 0, -1, 1 };
                for (int d = 0; d < 4; d++)
                {
                    int ni = i + dx[d];
                    int nj = j + dy[d];
                    if (ni >= 0 && ni < Size && nj >= 0 && nj < Size)
                        nodeMatrix[i, j].WayList.Add(new Way(nodeMatrix[ni, nj], 1f));
                }
            }

        startX = Random.Range(0, Size);
        startY = Random.Range(0, Size);
        do
        {
            endX = Random.Range(0, Size);
            endY = Random.Range(0, Size);
        } while (endX == startX && endY == startY);

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
                nodeMatrix[i, j].Heuristic = Calculs.CalculateHeuristic(nodeMatrix[i, j], endX, endY);

        for (int i = 0; i < Size; i++)
            for (int j = 0; j < Size; j++)
            {
                Vector2 pos = Calculs.CalculatePoint(i, j);
                tokenObjects[i, j] = Instantiate(tokenNode, pos, Quaternion.identity);
            }

        startTokenObj = Instantiate(tokenStart, Calculs.CalculatePoint(startX, startY), Quaternion.identity);
        endTokenObj = Instantiate(tokenEnd, Calculs.CalculatePoint(endX, endY), Quaternion.identity);
        SetSortingOrder(startTokenObj, 30);
        SetSortingOrder(endTokenObj, 30);

        logText.text = "";
        AppendLog("Grid initialized. Press Start to run A*.");
        isRunning = false;
    }

    void SetSortingOrder(GameObject obj, int order)
    {
        SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = order;
    }

    void OnStartClicked()
    {
        if (!isRunning)
            StartCoroutine(RunAStar());
    }

    void OnRestartClicked()
    {
        StopAllCoroutines();
        isRunning = false;
        InitGrid();
    }

    void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    IEnumerator RunAStar()
    {
        isRunning = true;
        AppendLog("Running A*...");

        List<Node> closedVisited;
        List<Node> openVisited;

        List<Node> path = AStarPathfinder.FindPath(
            nodeMatrix, startX, startY, endX, endY,
            out closedVisited, out openVisited);

        foreach (Node node in openVisited)
        {
            if (node.PositionX == startX && node.PositionY == startY) continue;
            if (node.PositionX == endX && node.PositionY == endY) continue;
            ReplaceToken(node.PositionX, node.PositionY, tokenOpen);
            EnsureStartEndOnTop();
            AppendLog($"Open: ({node.PositionX}, {node.PositionY}) G={node.GCost:F2} H={node.Heuristic:F2}");
            yield return new WaitForSeconds(stepDelay);
        }

        foreach (Node node in closedVisited)
        {
            if (node.PositionX == startX && node.PositionY == startY) continue;
            if (node.PositionX == endX && node.PositionY == endY) continue;
            ReplaceToken(node.PositionX, node.PositionY, tokenClosed);
            EnsureStartEndOnTop();
            AppendLog($"Closed: ({node.PositionX}, {node.PositionY})");
            yield return new WaitForSeconds(stepDelay);
        }

        if (path != null)
        {
            AppendLog($"Path found! Length: {path.Count}");
            foreach (Node node in path)
            {
                if (node.PositionX == startX && node.PositionY == startY) continue;
                if (node.PositionX == endX && node.PositionY == endY) continue;
                ReplaceToken(node.PositionX, node.PositionY, tokenPath);
                EnsureStartEndOnTop();
                AppendLog($"Path: ({node.PositionX}, {node.PositionY})");
                yield return new WaitForSeconds(stepDelay);
            }
        }
        else
        {
            AppendLog("No path found!");
        }

        EnsureStartEndOnTop();
        AppendLog("Done.");
        isRunning = false;
    }

    void ReplaceToken(int x, int y, GameObject prefab)
    {
        if (tokenObjects[x, y] != null)
            Destroy(tokenObjects[x, y]);
        Vector2 pos = Calculs.CalculatePoint(x, y);
        tokenObjects[x, y] = Instantiate(prefab, pos, Quaternion.identity);
        SetSortingOrder(tokenObjects[x, y], 24);
    }

    void EnsureStartEndOnTop()
    {
        if (startTokenObj != null)
        {
            startTokenObj.transform.position = (Vector3)Calculs.CalculatePoint(startX, startY) + Vector3.back * 0.1f;
            SetSortingOrder(startTokenObj, 30);
        }
        if (endTokenObj != null)
        {
            endTokenObj.transform.position = (Vector3)Calculs.CalculatePoint(endX, endY) + Vector3.back * 0.1f;
            SetSortingOrder(endTokenObj, 30);
        }
    }

    void AppendLog(string message)
    {
        logText.text += message + "\n";
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        // First frame: TMP recalculates preferred height
        yield return new WaitForEndOfFrame();
        // Force ContentSizeFitter to apply the new height
        Canvas.ForceUpdateCanvases();
        // Second frame: ScrollRect recalculates scroll bounds
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}