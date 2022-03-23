using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SKCell;
public class Cell : MonoBehaviour
{
    public int gridX, gridY;
    public CellStatus status = CellStatus.Idle;
    public Ball ball;
    private SpriteRenderer sr;
    private Animator anim;

    private void Awake()
    {
        sr=GetComponent<SpriteRenderer>();  
        anim= GetComponent<Animator>(); 
    }
    public void OnSelect()
    {
        status = CellStatus.Selected;
        UpdateAnim(CellStatus.Selected);
    }
    public void OnEnter()
    {
        UpdateAnim(CellStatus.Hover);
    }
    public void OnExit()
    {
        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (status == CellStatus.Selected)
        {
            UpdateAnim(CellStatus.Selected);
        }
        else if (status == CellStatus.Possible)
        {
            UpdateAnim(CellStatus.Possible);
        }
        else
        {
            UpdateAnim(CellStatus.Idle);
        }
    }

    private void OnMouseEnter()
    {
        if (status == CellStatus.Possible)
        {
            UpdateAnim(CellStatus.Selected);
        }
    }

    private void OnMouseExit()
    {
        if (status == CellStatus.Possible)
        {
            UpdateAnim(CellStatus.Possible);
        }
    }

    private void OnMouseUpAsButton()
    {
        if (status == CellStatus.Possible)
        {
            BoardManager.instance.OnSelectPossibleCell(this);
        }
    }

    public void UpdateAnim(CellStatus s)
    {
        anim.ResetTrigger("Idle");
        anim.ResetTrigger("Possible");
        anim.ResetTrigger("Selected");
        anim.ResetTrigger("Hover");

        anim.SetTrigger(s.ToString());
    }
}

public enum CellStatus
{
    Idle,
    Selected,
    Possible,
    Hover
}
