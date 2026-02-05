using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float intelligence = 0.7f;
    [SerializeField] private float turnSpeed = 20f;
    [SerializeField] private float moveStartAngle = 15f;
    [SerializeField] private float minMoveDistance = 0.5f;

    [Header("Wall Avoidance")]
    [SerializeField] private float sideAvoidTime = 0.3f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpRange = 2.5f;
    [SerializeField] private float edgeDetectionDist = 1.0f;

    [Header("References")]
    [SerializeField] private CharacterController cc;
    [SerializeField] private CharacterAnimator characterAnimator;

    public TargetNode currentTarget;
    public ConnectionAction currentAction;
    private float sideAvoidTimer;
    private float sideDir;
    private float reachCooldown;
   [SerializeField] private MoveState moveState = MoveState.Idle;
    private Vector3 desiredDirection = Vector3.zero;

    private enum MoveState
    {
        Idle,
        Turning,
        Moving
    }

    void Awake()
    {
        if (cc == null) cc = GetComponent<CharacterController>();
        if (characterAnimator == null) characterAnimator = GetComponent<CharacterAnimator>();
    }

    private bool wasBusy;
    private bool hasPerformedInstantJump;

    void Update()
    {
        // 0. Movement guards (match Player.cs)
        if (cc == null || !cc.canMove || cc.beingHit)
        {
            wasBusy = true;
            // Reset input if hit so bots don't "run in place" while on ground
            if (cc != null) cc.SetInput(0, 0);
            UpdateMoveAnimation(0, 0); 
            return;
        }

        // 0.1 Reset targeting upon recovery
        if (wasBusy)
        {
            wasBusy = false;
            AssignInitialTarget(); // Reset to nearest target on recovery
        }

        if (currentTarget == null)
            AssignInitialTarget();

        CheckTargetReached();

        // 1. Calculate Target Direction
        Vector3 targetPos = currentTarget != null ? currentTarget.transform.position : transform.position;
        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        Vector3 inputDir = distance > minMoveDistance ? toTarget.normalized : Vector3.zero;
        float strength = distance > minMoveDistance ? 1f : 0f;

        // 1.1 Edge Detection & Jump Logic
        if (inputDir != Vector3.zero)
        {
            if (currentAction == ConnectionAction.WaitJump)
            {
                bool edgeDetected = CheckForEdge(inputDir);

                if (edgeDetected)
                {
                    // We are at the edge. Should we jump yet?
                    if (distance <= jumpRange)
                    {
                        cc.Jump();
                        // Keep moving forward to cross gap
                        strength = 1f;
                    }
                    else
                    {
                        // Wait at the edge (moving platform or far platform)
                        strength = 0f;
                    }
                }
            }
            else if (currentAction == ConnectionAction.Jump)
            {
                // Instant jump as soon as we head towards this node
                // Only jump if we are grounded and haven't jumped for this specific action yet
                if (cc.grounded && !cc.Jumping && !hasPerformedInstantJump)
                {
                    cc.Jump();
                    hasPerformedInstantJump = true;
                }
            }
        }

        // 2. Decide State
        UpdateMoveState(inputDir, strength);

        // 3. Apply Behavior
        ApplyMoveState(inputDir, strength);

        HandleWallAvoidance();
    }

    private bool CheckForEdge(Vector3 moveDir)
    {
        if (cc == null || !cc.grounded || cc.Jumping) return false;

        // Raycast down slightly in front of the bot
        Vector3 origin = transform.position + Vector3.up * 0.5f + moveDir * edgeDetectionDist;
        bool hasGround = Physics.Raycast(origin, Vector3.down, 1.5f, cc.stepLayerMask);
        
        // No ground = Edge found
        return !hasGround;
    }

    private void OnDrawGizmosSelected()
    {
        if (cc == null) return;
        
        // Draw Jump Range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, jumpRange);

        // Draw Edge Sensor
        Vector3 moveDir = transform.forward;
        Vector3 origin = transform.position + Vector3.up * 0.5f + moveDir * edgeDetectionDist;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * 1.5f);
    }

    private void UpdateMoveState(Vector3 inputDir, float strength)
    {
        // Even if strength is 0 (waiting), we still want to be in a "Turning" or "Idle" state that faces the target
        if (inputDir == Vector3.zero)
        {
            moveState = MoveState.Idle;
            return;
        }

        float angle = Vector3.Angle(transform.forward, inputDir);

        if (angle > moveStartAngle)
            moveState = MoveState.Turning;
        else
            moveState = MoveState.Moving;
    }

    private void ApplyMoveState(Vector3 inputDir, float strength)
    {
        switch (moveState)
        {
            case MoveState.Idle:
                cc.SetInput(0f, 0f);
                UpdateMoveAnimation(0f, 0f);
                break;

            case MoveState.Turning:
                desiredDirection = inputDir;
                RotateTowards(desiredDirection);
                
                cc.SetInput(0f, 0f);
                // Only animate if there's actually intent to move or we are just turning
                UpdateMoveAnimation(0f, strength); 
                break;

            case MoveState.Moving:
                if (inputDir != Vector3.zero)
                    desiredDirection = inputDir;

                RotateTowards(desiredDirection);
                
                // If strength is 0 (waiting at edge), SetInput will be (0,0) but we still face the target
                cc.SetInput(0f, strength);
                UpdateMoveAnimation(0f, strength);
                break;
        }
    }

    private void RotateTowards(Vector3 dir)
    {
        if (dir == Vector3.zero || cc == null) return;
        cc.RotateTowards(dir, turnSpeed);
    }

    private void UpdateMoveAnimation(float x, float y)
    {
        if (characterAnimator == null || cc == null) return;

        characterAnimator.SetHorizontal(x);
        characterAnimator.SetVertical(y);

        if (cc.hasJumped)
            characterAnimator.SetJump(true);

        if (cc.readyToJump && !cc.grounded)
            characterAnimator.SetFall(true);
        else if (cc.grounded)
            characterAnimator.SetFall(false);

        characterAnimator.SetIncline(!cc.OnSlope());
    }

    // ---------------- WALL AVOID ----------------

    void HandleWallAvoidance()
    {
        if (sideAvoidTimer > 0f)
        {
            sideAvoidTimer -= Time.deltaTime;
        }
        else
        {
            sideDir = 0f;
        }
    }

    // ---------------- TARGET LOGIC ----------------

    void AssignInitialTarget()
    {
        if (TargetNodeManager.Instance == null) return;
        currentTarget = TargetNodeManager.Instance.GetNearestTarget(transform.position, true);
        currentAction = ConnectionAction.Walk; // Default for spawn
        hasPerformedInstantJump = false;
    }

    void CheckTargetReached()
    {
        if (currentTarget == null || reachCooldown > Time.time) return;

        Vector3 a = transform.position;
        Vector3 b = currentTarget.transform.position;
        a.y = b.y = 0f;

        if (Vector3.Distance(a, b) <= currentTarget.reachRadius)
        {
            reachCooldown = Time.time + 0.4f;
            OnReachedTarget();
        }
    }

    void OnReachedTarget()
    {
        if (currentTarget == null || TargetNodeManager.Instance == null) return;

        TargetNode oldTarget = currentTarget;
        
        // Find next target connection
        TargetConnection connection = TargetNodeManager.Instance.GetNextConnection(oldTarget, intelligence);

        if (connection != null)
        {
            currentTarget = connection.node;
            currentAction = connection.action;
            hasPerformedInstantJump = false;
        }
        else
        {
            currentTarget = TargetNodeManager.Instance.GetNearestTarget(transform.position, false);
            currentAction = ConnectionAction.Walk;
            hasPerformedInstantJump = false;
        }
    }

    public void SetTarget(TargetNode node)
    {
        currentTarget = node;
        currentAction = ConnectionAction.Walk;
        hasPerformedInstantJump = false;
    }
}