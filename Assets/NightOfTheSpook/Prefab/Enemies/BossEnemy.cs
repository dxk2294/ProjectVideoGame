using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEnemy : MonoBehaviour
{
    /// <summary>
    /// Point to the attackable that we're upgrading to a boss. Needed to access its health.
    /// </summary>
    public Attackable Attackable;

    /// <summary>
    /// List of locations that the boss can teleport to.
    /// </summary>
    public List<Transform> TeleportLocations;

    /// <summary>
    /// How much DPS the boss takes before jumping somewhere.
    /// </summary>
    public float DamagePerSecondBeforeTeleport = 10.0f;

    /// <summary>
    /// How long the teleport animation plays for. Defaults to a second.
    /// </summary>
    public float TeleportAnimationDuration = 1.0f;

    /// <summary>
    /// Animation curve to use when playing the teleport animation arbl garbl warble.
    /// </summary>
    public AnimationCurve TeleportAnimationCurve = AnimationCurve.EaseInOut(1.0f, 0.0f, 0.0f, 1.0f);

    public enum FaceState
    {
        Idle,
        Pain
    }
    public GameObject FaceIdle;
    public GameObject FacePain;

    public enum BodyState
    {
        Raw,
        Rare,
        Medium,
        WellDone
    }
    public List<GameObject> BodyStates;
    private BodyState _currentBodyState = BodyState.Raw;

    private BossTeleportAnimation _teleportAnimation;
    private float _previousDPSCheckTime = 0.0f;
    private float _previousHealthValue = 0.0f;

    void Start()
    {
        _previousHealthValue = Attackable.Health;
        _previousDPSCheckTime = Time.time;
        SetFace(FaceState.Idle);
        SetBody(BodyState.Raw);
        ConfigureTeleportAnimation();
    }

    void Update()
    {
        UpdateBodyState();
        UpdateTeleport();
        UpdateWinState();
    }

    private void UpdateBodyState()
    {
        var healthPercentage = Attackable.Health / Attackable.TotalHealth;
        if(healthPercentage >= 0.75)
        {
            if (_currentBodyState != BodyState.Raw)
            {
                SetBody(BodyState.Raw);
            }
        }
        else if (healthPercentage >= 0.5)
        {
            if (_currentBodyState != BodyState.Rare)
            {
                SetBody(BodyState.Rare);
            }
        }
        else if (healthPercentage >= 0.25)
        {
            if (_currentBodyState != BodyState.Medium)
            {
                SetBody(BodyState.Medium);
            }
        }
        else
        {
            SetBody(BodyState.WellDone);
        }
    }

    private void UpdateTeleport()
    {
        if (IsTeleporting)
        {
            // need to wait for the callback to complete.
            return;
        }

        if (ShouldTeleport())
        {
            SetFace(FaceState.Pain);
            _teleportAnimation.enabled = true;
        }
    }

    private void UpdateWinState()
    {
        if (IsDead)
        {
            // Cancel the teleportation animation so we don't try to yeet the boss
            // somewhere that won't exist momentarily.
            _teleportAnimation.enabled = false;

            // TODO: play a "boss is kill" animation here?

            // Show the end screen.
            var gm = GameManager.instance;
            if(gm != null)
            {
                var sgm = FindObjectOfType<SpookyGameManager>();
                gm.ShowWinScreen(sgm.state);
            }
        }
    }

    private bool ShouldTeleport()
    {
        // This will result in a check every second, so there will be a slight delay before the boss jumps.
        if ((_previousDPSCheckTime + 1.0f) > Time.time)
        {
            return false;
        }

        var result = (_previousHealthValue - Attackable.Health) > DamagePerSecondBeforeTeleport;
        _previousHealthValue = Attackable.Health;
        _previousDPSCheckTime = Time.time;
        return result;
    }

    private bool IsTeleporting
    {
        get { return _teleportAnimation != null && _teleportAnimation.enabled; }
    }

    private void HandleTeleportAnimationFinished()
    {
        var nextLocationIndex = Random.Range(0, TeleportLocations.Count);
        var nextLocation = TeleportLocations[nextLocationIndex];
        gameObject.transform.position = nextLocation.position;

        // Reset the face back to normal.
        SetFace(FaceState.Idle);

        // The previous animation will destroy itself so we need a new one.
        ConfigureTeleportAnimation();
    }

    private void ConfigureTeleportAnimation()
    {
        _teleportAnimation = gameObject.AddComponent<BossTeleportAnimation>();
        _teleportAnimation.scaleCurve = TeleportAnimationCurve;
        _teleportAnimation.duration = TeleportAnimationDuration;
        _teleportAnimation.OnAnimationFinished = HandleTeleportAnimationFinished;
        _teleportAnimation.enabled = false;
    }

    private bool IsDead
    {
        get { return Attackable.Health <= 0.0f; }
    }

    private void SetFace(FaceState face)
    {
        FaceIdle.SetActive(false);
        FacePain.SetActive(false);
        switch (face)
        {
            case FaceState.Pain:
                FacePain.SetActive(true);
                break;
            case FaceState.Idle:
            default:
                FaceIdle.SetActive(true);
                break;
        }
    }

    private void SetBody(BodyState body)
    {
        // Avoid churn by activating/deactivating unneccesarily.
        if(body == _currentBodyState) { return; }

        foreach (var state in BodyStates)
        {
            state.SetActiveRecursively(false); //(false);
        }
        switch (body)
        {
            case BodyState.WellDone:
                BodyStates[3].SetActiveRecursively(true);
                break;
            case BodyState.Medium:
                BodyStates[2].SetActiveRecursively(true);
                break;
            case BodyState.Rare:
                BodyStates[1].SetActiveRecursively(true);
                break;
            case BodyState.Raw:
            default:
                BodyStates[0].SetActiveRecursively(true);
                break;
        }
        _currentBodyState = body;
    }
}