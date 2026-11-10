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
        Color color = mark switch
        {
            Mark.Path => isDangerous ? new Color(1f, 0.55f, 0f) : Color.green,
            Mark.Reachable => isDangerous ? new Color(1f, 0.25f, 0f) : Color.yellow,
            Mark.Highlighted => isDangerous ? Color.red : new Color(0.75f, 0.75f, 0.75f),
            _ => isDangerous ? Color.red : Color.white
        };
        GetComponent<Renderer>().material.color = color;
    }
}
