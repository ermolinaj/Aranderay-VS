using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
internal sealed class SilhouetteMotion : MonoBehaviour
{
    internal const float ContactTime = 0.085f;
    private const float WindupTime = 0.035f;
    private const float StrikeTime = 0.27f;
    private const float RecoilTime = 0.17f;
    private const float DefeatTime = 0.23f;

    private SpriteRenderer source;
    private SpriteRenderer pose;
    private Vector3 direction;
    private float startedAt;
    private bool reacting;
    private bool creature;
    private bool defeated;
    private Vector3 restingPosition;
    private Quaternion restingRotation;
    private Vector3 restingScale = Vector3.one;
    private Color ink;

    internal SpriteRenderer Sprite => pose;

    internal void Bind(SpriteRenderer renderer)
    {
        source = renderer;
        pose = GetComponent<SpriteRenderer>();
        enabled = false;
    }

    internal void Strike(Vector3 target, bool creatureAttack)
    {
        creature = creatureAttack;
        reacting = false;
        Begin(target - source.transform.position);
    }

    internal void Recoil(Vector3 attacker)
    {
        reacting = true;
        Begin(source.transform.position - attacker);
    }

    private void Begin(Vector3 travel)
    {
        direction = Vector3.ProjectOnPlane(travel, Vector3.up).normalized;
        startedAt = Time.time;
        gameObject.layer = source.gameObject.layer;
        pose.sprite = source.sprite;
        pose.flipX = source.flipX;
        pose.flipY = source.flipY;
        pose.sharedMaterial = source.sharedMaterial;
        pose.sortingLayerID = source.sortingLayerID;
        pose.sortingOrder = source.sortingOrder;
        pose.spriteSortPoint = source.spriteSortPoint;
        pose.maskInteraction = source.maskInteraction;
        ink = source.color;
        pose.color = ink;

        source.forceRenderingOff = true;
        pose.enabled = true;
        enabled = true;
        Present(0, 0, 0, 1, 1);
    }

    internal void FinishDefeat()
    {
        if (!isActiveAndEnabled || !reacting || defeated)
            return;

        // The unit is removed by the rules. Only the fading sprite stays for the hit.
        restingPosition = source.transform.position;
        restingRotation = source.transform.rotation;
        transform.SetParent(null, true);
        restingScale = transform.localScale;
        defeated = true;
        source = null;
    }

    private void Update()
    {
        float elapsed = Time.time - startedAt;
        if (reacting)
            UpdateRecoil(elapsed - ContactTime);
        else
            UpdateStrike(elapsed);
    }

    private void UpdateStrike(float elapsed)
    {
        if (elapsed >= StrikeTime)
        {
            enabled = false;
            return;
        }

        float pullback = creature ? -0.17f : -0.09f;
        float reach = creature ? 0.58f : 0.31f;
        float travel;
        if (elapsed < WindupTime)
            travel = Mathf.SmoothStep(0, pullback, elapsed / WindupTime);
        else if (elapsed < ContactTime)
            travel = Mathf.Lerp(pullback, reach, (elapsed - WindupTime) / (ContactTime - WindupTime));
        else
            travel = Mathf.SmoothStep(reach, 0, (elapsed - ContactTime) / (StrikeTime - ContactTime));

        float lean = travel / reach;
        float lift = Mathf.Max(lean, 0) * (creature ? 0.10f : 0.035f);
        Present(travel, lift, lean * (creature ? 9 : 5), 1 + 0.035f * lean, 1 + 0.055f * lean);
    }

    private void UpdateRecoil(float elapsed)
    {
        float duration = defeated ? DefeatTime : RecoilTime;
        if (elapsed >= duration)
        {
            if (defeated)
                Destroy(gameObject);
            else
                enabled = false;
            return;
        }

        const float joltTime = 0.028f;
        float jolt = elapsed < joltTime
            ? Mathf.Clamp01(elapsed / joltTime)
            : Mathf.SmoothStep(1, 0, (elapsed - joltTime) / (duration - joltTime));
        float fade = defeated ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.055f, duration, elapsed)) : 0;
        var color = ink;
        color.a *= 1 - fade;
        pose.color = color;
        Present(0.32f * jolt, 0.045f * jolt, 7 * jolt, 1 + 0.06f * jolt, 1 - 0.07f * jolt - 0.14f * fade);
    }

    private void Present(float travel, float lift, float lean, float width, float height)
    {
        Vector3 position = restingPosition;
        Quaternion rotation = restingRotation;
        if (source != null)
        {
            position = source.transform.position;
            rotation = source.transform.rotation;
            gameObject.layer = source.gameObject.layer;
        }

        float side = Mathf.Clamp(Vector3.Dot(direction, rotation * Vector3.right) * 1.8f, -1, 1);
        transform.SetPositionAndRotation(position + direction * travel + Vector3.up * lift,
            rotation * Quaternion.AngleAxis(-side * lean, Vector3.forward));
        transform.localScale = Vector3.Scale(restingScale, new Vector3(width, height, 1));
    }

    private void OnDisable()
    {
        if (source != null)
            source.forceRenderingOff = false;
        if (pose != null)
            pose.enabled = false;
    }
}
