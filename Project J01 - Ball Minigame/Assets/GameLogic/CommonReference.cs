using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SKCell;
public class CommonReference : MonoSingleton<CommonReference>
{
    public GameObject[] ballPrefabs;
    public GameObject cellPrefab;
    public GameObject explosionFx;
    public GameObject gameOver;
    public SKGridLayer gridLayer;
    public Transform ballContainer, cellContainer, queuedBallsContainer;
    public Text scoreText, timeText, bestScoreText, bestTimeText;

    public Color cellNormalColor, cellSelectedColor, cellPossibleColor, cellHoverColor;
}
