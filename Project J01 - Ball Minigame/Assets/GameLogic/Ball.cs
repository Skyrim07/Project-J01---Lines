using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SKCell;
public class Ball : MonoBehaviour
{
    public int type;
    public BallSKill skill;
    public int gridX, gridY;
    public Cell cell;
    public bool isBig = false;

    private Animator anim;
    private GameObject ghost, doubleIcon, explode, killer;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        ghost = transform.Find("Ghost").gameObject;
        doubleIcon = transform.Find("Double").gameObject;
        explode = transform.Find("Explode").gameObject;
        killer = transform.Find("Killer").gameObject;
    }
    private void OnMouseEnter()
    {
        if (!isBig)
            return;
        cell.OnEnter();
    }
    private void OnMouseExit()
    {
        if (!isBig)
            return;
        cell.OnExit();
    }
    private void OnMouseUpAsButton()
    {
        if (!isBig)
            return;
        cell.OnSelect();
        BoardManager.instance.OnSelectBall(this);
    }

    public void GrowBig()
    {
        isBig = true;
        anim.SetBool("Big", true);
        BoardManager.instance.gridLayer.PathfindingSetCellCost(gridX, gridY, 1);
        BoardManager.instance.CheckPattern(this);
    }

    public void UpdateVisual()
    {
        ghost.SetActive(skill == BallSKill.Ghost);
        doubleIcon.SetActive(skill == BallSKill.Double);
        explode.SetActive(skill == BallSKill.Explode);
        killer.SetActive(skill == BallSKill.Killer);
    }
}

public enum BallSKill
{
    None,
    Ghost,
    Double,
    Explode,
    Killer
}
