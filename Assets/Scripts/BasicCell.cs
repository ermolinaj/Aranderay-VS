using TbsFramework.Cells;
using UnityEngine;

class BasicCell : Square
{
    private enum Mark
    {
        None,
        Highlighted,
        Reachable,
        Path
    }

    private Mark mark;
    private bool isDangerous;

    public override Vector3 GetCellDimensions()
    {
        return GetComponent<Renderer>().bounds.size;
    }

    public override void MarkAsHighlighted()
    {
        SetMark(Mark.Highlighted);
    }

    public override void SetDanger(bool isDangerous)
    {
        this.isDangerous = isDangerous;
        RefreshColor();
    }

    public override void MarkAsPath()
    {
        SetMark(Mark.Path);
    }

    public override void MarkAsReachable()
    {
        SetMark(Mark.Reachable);
    }

    public override void UnMark()
    {
        SetMark(Mark.None);
    }

    private void SetMark(Mark value)
    {
        mark = value;
        RefreshColor();
    }

    private void RefreshColor()
    {
        // Keep the Scout layer visible; route and reach markers are drawn separately.
        Color color = isDangerous ? Color.red : mark switch
        {
            Mark.Path => Color.green,
            Mark.Reachable => Color.yellow,
            Mark.Highlighted => new Color(0.75f, 0.75f, 0.75f),
            _ => Color.white
        };
        GetComponent<Renderer>().material.color = color;
    }
}
