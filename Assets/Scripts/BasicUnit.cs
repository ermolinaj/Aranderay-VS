using TbsFramework.Units;
using UnityEngine;

class BasicUnit : Unit
{
    public Color LeadingColor;

    private Material unitMaterial;
    private Color baseColor;

    public override void Initialize()
    {
        base.Initialize();
        transform.localPosition -= new Vector3(0, 0, 1);
        unitMaterial = GetComponent<Renderer>().material;
        baseColor = LeadingColor.a > 0f ? LeadingColor : unitMaterial.color;
        UnMark();
    }
    public override void MarkAsAttacking(Unit other)
    {
    }

    public override void MarkAsDefending(Unit other)
    {
    }

    public override void MarkAsDestroyed()
    {
    }

    public override void MarkAsFinished()
    {
        UnMark();
    }

    public override void MarkAsFriendly()
    {
        SetTint(LeadingColor + new Color(0.8f, 1, 0.8f));
    }

    public override void MarkAsReachableEnemy()
    {
        SetTint(LeadingColor + Color.red);
    }

    public override void MarkAsSelected()
    {
        SetTint(LeadingColor + Color.green);
    }

    public override void UnMark()
    {
        unitMaterial.color = baseColor;
    }

    private void SetTint(Color color)
    {
        color.a = baseColor.a;
        unitMaterial.color = color;
    }
}
