using TbsFramework.Units;
using UnityEngine;

class BasicUnit : Unit
{
    public Color LeadingColor;

    private Material unitMaterial;
    private Color baseColor;
    private SpriteRenderer sprite;
    private SpriteRenderer feedbackRim;
    private SilhouetteMotion motion;

    internal SpriteRenderer PresentedSprite =>
        motion != null && motion.isActiveAndEnabled && motion.Sprite.enabled ? motion.Sprite : sprite;

    public override void Initialize()
    {
        base.Initialize();
        var renderer = GetComponent<Renderer>();
        sprite = renderer as SpriteRenderer;
        unitMaterial = renderer.material;
        baseColor = LeadingColor.a > 0f ? LeadingColor : unitMaterial.color;
        UnMark();
    }

    // The sprite's pivot is at its centre, one world unit above the board.
    protected override Vector3 GetCellPlacementOffset() => Vector3.up;

    internal void ShowFeedbackRim(Material material, Color color)
    {
        var presented = PresentedSprite;
        if (presented == null || material == null)
            return;

        if (feedbackRim == null)
        {
            var rimObject = new GameObject("Tactical rim");
            rimObject.transform.SetParent(transform, false);
            feedbackRim = rimObject.AddComponent<SpriteRenderer>();
            feedbackRim.sharedMaterial = material;
            feedbackRim.sortingLayerID = sprite.sortingLayerID;
            feedbackRim.sortingOrder = sprite.sortingOrder + 1;
            feedbackRim.spriteSortPoint = sprite.spriteSortPoint;
            feedbackRim.maskInteraction = sprite.maskInteraction;
        }

        feedbackRim.gameObject.layer = gameObject.layer;
        feedbackRim.sprite = presented.sprite;
        feedbackRim.flipX = presented.flipX;
        feedbackRim.flipY = presented.flipY;
        feedbackRim.color = color;
        var rim = feedbackRim.transform;
        if (presented == sprite)
        {
            rim.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            rim.localScale = Vector3.one;
        }
        else
        {
            rim.SetLocalPositionAndRotation(presented.transform.localPosition, presented.transform.localRotation);
            rim.localScale = presented.transform.localScale;
        }
        feedbackRim.enabled = presented.enabled && !presented.forceRenderingOff && presented.color.a > 0;
    }

    internal void HideFeedbackRim()
    {
        if (feedbackRim != null)
            feedbackRim.enabled = false;
    }

    public override void MarkAsAttacking(Unit other)
    {
        if (other != null && CanAnimate())
            GetMotion().Strike(other.transform.position, PlayerNumber != 0);
    }

    public override void MarkAsDefending(Unit other)
    {
        if (other != null && CanAnimate())
            GetMotion().Recoil(other.transform.position);
    }

    public override void MarkAsDestroyed()
    {
        if (motion != null)
            motion.FinishDefeat();
    }

    private bool CanAnimate() => sprite != null && sprite.enabled && sprite.sprite != null &&
        (!sprite.forceRenderingOff || (motion != null && motion.isActiveAndEnabled));

    private SilhouetteMotion GetMotion()
    {
        if (motion == null)
        {
            var visual = new GameObject("Combat silhouette");
            visual.transform.SetParent(transform, false);
            motion = visual.AddComponent<SilhouetteMotion>();
            motion.Bind(sprite);
        }
        return motion;
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
