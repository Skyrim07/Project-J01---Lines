using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SKCell;
public class BoardManager : MonoSingleton<BoardManager>
{
    const int BOARD_SIZE = 9;
    const int START_BALL_COUNT = 5;
    const int SCORE_MULTIPLIER = 50;

    public int ballCount;

    public SKGridLayer gridLayer;
    private Transform ballContainer, cellContainer;
    private Vector2[] dirs8 = new Vector2[] { Vector2.up, -Vector2.up, Vector2.left, Vector2.right, new Vector2(1,1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(-1, -1), };
    private Vector2[] dirs4 = new Vector2[] { Vector2.up,  Vector2.left,  new Vector2(1,1), new Vector2(1, -1) };

    private Cell[,] cells;
    private List<Ball> queuedBalls = new List<Ball>();
    private List<Cell> possibleCells = new List<Cell>();
    private List<Cell> emptyCells = new List<Cell>();
    private void Start()
    {
        cells = new Cell[BOARD_SIZE, BOARD_SIZE];

        gridLayer = CommonReference.instance.gridLayer;
        
        ballContainer = CommonReference.instance.ballContainer;
        cellContainer = CommonReference.instance.cellContainer;

        CommonUtils.InvokeAction(0.3f,()=>
        {
            LoadBoard();
            LoadSavedData();
        });
    }

    public void LoadBoard()
    {
        int x = gridLayer.width;
        int y = gridLayer.height;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                Cell cell = InstantiateCellOnBoard(i, j);
                cells[i, j] = cell;
                emptyCells.Add(cell);
            }
        }

        List<Vector2Int> pos = new List<Vector2Int>();
        for (int i = 0; i < START_BALL_COUNT; i++)
        {
            Vector2Int p = new Vector2Int(Random.Range(0, BOARD_SIZE), Random.Range(0, BOARD_SIZE));

            while (pos.Contains(p))
            {
                p = new Vector2Int(Random.Range(0, BOARD_SIZE), Random.Range(0, BOARD_SIZE));
            }
            pos.Add(p);
            InstantiateBallOnBoard(Random.Range(0,CommonReference.instance.ballPrefabs.Length), p.x, p.y);
        }
        NextTurn();
    }

    private void LoadSavedData()
    {
        CommonReference.instance.bestScoreText.text = FlowManager.instance.bestScore.ToString("d6");
        if (FlowManager.instance.bestTime == int.MaxValue)
            CommonReference.instance.bestTimeText.text = "--:--";
        else
            CommonReference.instance.bestTimeText.text = (FlowManager.instance.bestTime / 60).ToString("d2") + ":" + (FlowManager.instance.bestTime % 60).ToString("d2");
    }
    private void NextTurn()
    {
        for (int i = 0; i < queuedBalls.Count; i++)
        {
            queuedBalls[i].GrowBig();
        }
        queuedBalls.Clear();
        CommonReference.instance.queuedBallsContainer.ClearChildren();

        int queueCount = Mathf.Min(BOARD_SIZE * BOARD_SIZE - ballCount, 3);
        for (int i = 0;i < queueCount;i++)
        {
            Cell cell = emptyCells[Random.Range(0, emptyCells.Count)];
            Vector2Int p = new Vector2Int(cell.gridX, cell.gridY);
            int type = Random.Range(0, CommonReference.instance.ballPrefabs.Length);
            QueueBallOnBoard(type, p.x, p.y);
            QueueBallOnUI(type,i);
        }
        if(emptyCells.Count == 0)
        {
            GameOver();
        }
    }

    public void CheckPattern(Ball ball)
    {
        int x = ball.gridX;
        int y = ball.gridY;
        int type = ball.type;

        List<Ball> balls = new List<Ball>();
        for (int i = 0; i < dirs4.Length; i++)
        {
            balls.Clear();
            int patternCount = ball.skill == BallSKill.Double?2:1;
            balls.Add(ball);
            for (int j = 1; j < BOARD_SIZE; j++)
            {
                int targetX = x + (int)dirs4[i].x * j;
                int targetY = y + (int)dirs4[i].y * j;
                if (targetX < 0 || targetX >= BOARD_SIZE || targetY < 0 || targetY >= BOARD_SIZE)
                    break;
                Ball targetBall = cells[targetX, targetY].ball;
                if (targetBall!=null&&targetBall.type == type)
                {
                    balls.Add(targetBall);
                    patternCount++;
                    if (targetBall.skill == BallSKill.Double)
                        patternCount++;
                }
                else
                {
                    break;
                }
            }

            for (int j = 1; j < BOARD_SIZE; j++)
            {
                int targetX = x - (int)dirs4[i].x * j;
                int targetY = y - (int)dirs4[i].y * j;
                if (targetX < 0 || targetX >= BOARD_SIZE || targetY < 0 || targetY >= BOARD_SIZE)
                    break;
                Ball targetBall = cells[targetX, targetY].ball;
                if (targetBall != null && targetBall.isBig && targetBall.type == type)
                {
                    balls.Add(targetBall);
                    patternCount++;
                    if (targetBall.skill == BallSKill.Double)
                        patternCount++;
                }
                else
                {
                    break;
                }
            }

            if (patternCount >= 5)
            {
                ExplodeBalls(balls.ToArray());
            }
        }
    }

    public void ExplodeBalls(Ball[] balls, bool ignoreSkill = false)
    {
        FlowManager.instance.ChangeScore(SCORE_MULTIPLIER * (balls[0].type + 1) * balls.Length);
        StartCoroutine(ExplodeBallsCR(balls, ignoreSkill));
    }

    IEnumerator ExplodeBallsCR(Ball[] balls, bool ignoreSkill = false)
    {
        for (int i = 0; i < balls.Length; i++)
        {
            RemoveBallOnBoard(balls[i], false);
        }
        for (int i = 0; i < balls.Length; i++)
        {
            Transform tf = CommonUtils.SpawnObject(CommonReference.instance.explosionFx).transform;
            tf.position = balls[i].transform.position;
            CommonUtils.InvokeAction(2f, () =>
            {
                CommonUtils.ReleaseObject(tf.gameObject);
            });

            if (!ignoreSkill)
            {
                if (balls[i].skill == BallSKill.Explode)
                {
                    List<Ball> explodeBalls = new List<Ball>();
                    for (int k = 0; k < BOARD_SIZE; k++)
                    {
                        Cell targetCell = cells[balls[i].gridX, k];
                        if (targetCell.ball != balls[i] && targetCell.ball != null && targetCell.ball.isBig)
                        {
                            explodeBalls.Add(targetCell.ball);
                        }
                        targetCell = cells[k, balls[i].gridY];
                        if (targetCell.ball != balls[i] && targetCell.ball != null && targetCell.ball.isBig)
                        {
                            explodeBalls.Add(targetCell.ball);
                        }
                    }
                    ExplodeBalls(explodeBalls.ToArray());
                }
            }
            RemoveBallOnBoard(balls[i], true);
            yield return new WaitForSeconds(0.1f);
        }
    }
    public void OnSelectPossibleCell(Cell cell)
    {
        Ball ball = BallManager.instance.ballSelected;
        gridLayer.PathfindingSetStartPoint(ball.gridX, ball.gridY);
        gridLayer.PathfindingSetEndPoint(cell.gridX, cell.gridY);
        List<Vector2Int> path = gridLayer.PathfindingStart(ball.skill==BallSKill.Ghost || ball.skill==BallSKill.Killer);

            cell.status = CellStatus.Idle;
            cell.UpdateVisual();
            if (BallManager.instance.ballSelected)
            {
                BallManager.instance.ballSelected.cell.status = CellStatus.Idle;
                BallManager.instance.ballSelected.cell.ball = null;
                BallManager.instance.ballSelected.cell.UpdateVisual();
            }
            ClearPossibleCells();
            if (path!=null)
                MoveBallOnBoard(BallManager.instance.ballSelected, cell.gridX, cell.gridY, path);
            ClearBallSelection();
        return;
    }
    public void OnSelectBall(Ball ball)
    {
        if (ball == null)
            return;

        ClearPossibleCells();
        possibleCells.Clear();

        if (BallManager.instance.ballSelected)
        {
            BallManager.instance.ballSelected.cell.status = CellStatus.Idle;
            BallManager.instance.ballSelected.cell.UpdateVisual();
        }
        BallManager.instance.ballSelected = ball;

        for (int i = 0; i < BOARD_SIZE; i++)
        {
            for (int j = 0; j < BOARD_SIZE; j++)
            {
                Cell cell = cells[i, j];
                if (cell.ball != null)
                {
                    continue;
                }

                possibleCells.Add(cell);
                cell.status = CellStatus.Possible;
                cell.UpdateVisual();
            }
        }
    }

    private void ClearPossibleCells()
    {
        for (int i = 0; i < possibleCells.Count; i++)
        {
            possibleCells[i].status = CellStatus.Idle;
            possibleCells[i].UpdateVisual();
        }
    }

    private Ball InstantiateBallOnBoard(int ballType, int x, int y)
    {
        Ball ball = CommonUtils.SpawnObject(CommonReference.instance.ballPrefabs[ballType]).GetComponent<Ball>();
        ball.transform.SetParent(ballContainer, true);
        Vector3 pos = gridLayer.WorldPosFromCell(x, y);
        pos = new Vector3(pos.x, pos.y, -5);
        ball.transform.position = pos;

        ball.type = ballType;
        ball.gridX = x;
        ball.gridY = y;
        ball.cell = cells[x, y];
        cells[x, y].ball = ball;
        ballCount++;
        emptyCells.Remove(ball.cell);

        ball.GrowBig();
        ball.UpdateVisual();
        return ball;
    }

    private Ball QueueBallOnBoard(int ballType, int x, int y)
    {
        Ball ball = CommonUtils.SpawnObject(CommonReference.instance.ballPrefabs[ballType]).GetComponent<Ball>();
        ball.transform.SetParent(ballContainer, true);
        Vector3 pos = gridLayer.WorldPosFromCell(x, y);
        pos = new Vector3(pos.x, pos.y, -5);
        ball.transform.position = pos;

        float rand = Random.value;
        if (rand > 0.8f)
        {
            ball.skill = (BallSKill)(Random.Range(1, 5));
        }

        ball.isBig = false;
        ball.gridX = x;
        ball.gridY = y;
        ball.cell = cells[x, y];
        ball.type = ballType;
        cells[x, y].ball = ball;
        queuedBalls.Add(ball);
        emptyCells.Remove(ball.cell);

        ballCount++;

        ball.UpdateVisual();
        return ball;
    }
    public void ClearBallSelection()
    {
        BallManager.instance.ballSelected = null;
    }

    public void MoveBallOnBoard(Ball ball, int x, int y, List<Vector2Int> path)
    {
        StartCoroutine(MoveBallCR(ball, x, y, path));
    }

    IEnumerator MoveBallCR(Ball ball, int x, int y, List<Vector2Int> path)
    {
        ball.cell.UpdateVisual();
        emptyCells.Add(ball.cell);
        gridLayer.PathfindingSetCellCost(ball.gridX, ball.gridY, 0);
        gridLayer.PathfindingSetCellCost(x, y, 1);
        ball.gridX = x;
        ball.gridY = y;
        ball.cell = cells[x, y];
        cells[x, y].ball = ball;
        emptyCells.Remove(ball.cell);
        for (int i = 0; i < path.Count - 1; i++)
        {
            if (ball.skill == BallSKill.Killer)
            {
                Ball targetball = cells[path[i].x, path[i].y].ball;
                if (targetball != null && targetball.isBig)
                {
                    ExplodeBalls(new Ball[] { targetball }, true);
                }
            }
        }
            for (int i = 0; i < path.Count - 1; i++)
        {
            if (ball.skill == BallSKill.Killer)
            {
                    Ball targetball = cells[path[i].x, path[i].y].ball;
                    if (targetball != null && targetball.isBig)
                    {
                        ExplodeBalls(new Ball[] { targetball }, true);
                    }
            }
            Vector3 opos = gridLayer.WorldPosFromCell(path[i].x, path[i].y);
            opos = new Vector3(opos.x, opos.y, -5);
            Vector3 pos = gridLayer.WorldPosFromCell(path[i+1].x, path[i+1].y);
            pos = new Vector3(pos.x, pos.y, -5);
            CommonUtils.StartProcedure(SKCurve.QuadraticDoubleIn, 0.05f, (f) =>
            {
                ball.transform.position = opos + (pos - opos) * f;
            });
            yield return new WaitForSeconds(0.06f);
        }
        if (ball.skill == BallSKill.Killer)
        {
            ExplodeBalls(new Ball[] { ball }, true);
        }
        CheckPattern(ball);
        NextTurn();
    }

    private void QueueBallOnUI(int type, int id)
    {
        Ball ball = CommonUtils.SpawnObject(CommonReference.instance.ballPrefabs[type]).GetComponent<Ball>();
        ball.transform.parent = CommonReference.instance.queuedBallsContainer;
        ball.transform.position = CommonReference.instance.queuedBallsContainer.GetChild(id).position;
        ball.UpdateVisual();
    }
    private Cell InstantiateCellOnBoard(int x, int y)
    {
        Cell cell = CommonUtils.SpawnObject(CommonReference.instance.cellPrefab).GetComponent<Cell>();
        cell.transform.SetParent(cellContainer, true);
        Vector3 pos = gridLayer.WorldPosFromCell(x, y);
        pos = new Vector3(pos.x, pos.y, -1);
        cell.transform.position = pos;

        cell.gridX = x;
        cell.gridY = y;
        cell.ball = null;
        return cell;
    }
    private void RemoveBallOnBoard(Ball ball, bool release=true)
    {
        if (release)
        {
            CommonUtils.ReleaseObject(ball.gameObject);
        }
        else
        {
            ballCount--;
            ball.cell.ball = null;
            emptyCells.Add(ball.cell);
            ball.cell.UpdateVisual();
            gridLayer.PathfindingSetCellCost(ball.gridX, ball.gridY, 0);
        }
    }

    private void GameOver()
    {
        FlowManager.instance.GameOver();
    }
}
