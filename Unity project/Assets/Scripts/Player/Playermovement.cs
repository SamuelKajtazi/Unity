using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public Transform playerBody;                // drag your visible capsule/model here
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 20f;
    public float lookSpeed = 2f;
    public float lookXLimit = 85f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    [Header("Sprint FOV")]
    public float sprintFovBoost = 4f;           // how many degrees the FOV grows while sprinting (3 to 5 feels good)
    public float fovChangeSpeed = 8f;           // how fast the FOV changes (higher = snappier)

    [Header("Ground Movement")]
    public float groundAcceleration = 45f;      // speeding up (12 / 45 = about 0.27s to full run speed)
    public float counterAcceleration = 70f;     // cancels sideways and backwards velocity when turning or reversing
    public float stopDeceleration = 50f;        // how fast you stop when you release the keys
    public float overspeedDeceleration = 6f;    // slow bleed when you're above run speed (keeps slide/jump momentum)
    [Range(0f, 1f)]
    public float overspeedCounterMultiplier = 0.25f;  // counter force while above run speed (low = keep momentum through turns)

    [Header("Air Movement")]
    public float airAcceleration = 8f;          // steering in the air
    public float airDrag = 0.3f;                // slowdown in the air with no input
    public float maxSpeed = 30f;                // top speed you can reach by chaining slides and jumps

    [Header("Momentum Jump")]
    public float speedToJumpHeight = 0.25f;     // extra jump power per unit of speed above run speed
    public float maxJumpBonus = 6f;             // cap on the extra jump power (jumpPower + this = highest jump)
    public float jumpForwardBoost = 2.5f;       // speed ADDED in your movement direction on every jump (more distance)
    public float slideJumpBoost = 3f;           // extra speed added when you jump out of a slide

    [Header("Slide")]
    public float slideSpeed = 16f;              // minimum speed at the start of a slide
    public float slideBoost = 4f;               // speed ADDED every time you start a slide
    public float slideFriction = 2f;            // lower = longer slide
    public float slideBufferTime = 0.3f;        // press crouch this long before landing and the slide starts on landing

    [Header("Wall Run")]
    public string wallObjectName = "WallRun";   // only objects with exactly this name can be wall run
    public float wallCheckDistance = 1.2f;      // HOW CLOSE you need to be (reach = this + capsule radius)
    public float wallTapWindow = 0.25f;         // how long a tap of A/D counts (makes timing forgiving)
    public float wallRunSpeed = 14f;            // minimum speed while on the wall
    public float minWallRunEntrySpeed = 4f;     // you need this much speed to start a wall run
    public float minWallRunHeight = 1.2f;       // you must be this high above the ground to start
    public float wallRunMaxTime = 2.5f;         // how long you can stay on the wall
    public float wallRunGravityMultiplier = 0.1f; // 0 = no gravity, 1 = normal gravity
    public float wallStickForce = 1.5f;         // small push into the wall so you stay attached
    public float wallJumpUpForce = 8f;          // upward speed when you jump off the wall
    public float wallJumpSideForce = 9f;        // push away from the wall when you jump off
    public float wallRunCooldown = 0.3f;        // delay before you can grab a wall again
    public float wallRunCamTilt = 12f;          // camera roll while wall running (flip the sign if it tilts the wrong way)

    private Vector3 velocity = Vector3.zero;    // horizontal momentum
    private float verticalVelocity = 0f;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;

    private bool isSliding = false;
    private float slideBuffer = 0f;

    private bool isWallRunning = false;
    private bool wallSideRight = false;
    private float wallRunTimer = 0f;
    private float wallRunCooldownTimer = 0f;
    private float camTilt = 0f;
    private Vector3 wallRunDir = Vector3.forward;   // direction we're running along the wall
    private float tapLeftTimer = 0f;
    private float tapRightTimer = 0f;

    private float cameraStandY;                 // camera height when standing
    private Vector3 controllerStandCenter;      // original CharacterController center
    private float baseFov;                      // FOV from the menu setting

    // extra capsule colliders on the player that need to shrink too
    private CapsuleCollider[] extraColliders;
    private float[] extraStandHeights;
    private Vector3[] extraStandCenters;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraStandY = playerCamera.transform.localPosition.y;
        controllerStandCenter = characterController.center;

        // find extra CapsuleColliders (skip ones under playerBody, those already shrink with its scale)
        CapsuleCollider[] all = GetComponentsInChildren<CapsuleCollider>();
        var list = new System.Collections.Generic.List<CapsuleCollider>();
        foreach (var col in all)
        {
            if (playerBody != null && col.transform.IsChildOf(playerBody)) continue;
            list.Add(col);
        }
        extraColliders = list.ToArray();
        extraStandHeights = new float[extraColliders.Length];
        extraStandCenters = new Vector3[extraColliders.Length];
        for (int i = 0; i < extraColliders.Length; i++)
        {
            extraStandHeights[i] = extraColliders[i].height;
            extraStandCenters[i] = extraColliders[i].center;
        }

        // settings saved from the main menu
        lookSpeed = PlayerPrefs.GetFloat("Sensitivity", 2f);
        baseFov = PlayerPrefs.GetFloat("FOV", 60f);
        playerCamera.fieldOfView = baseFov;
    }

    // raycast that ignores every collider on the player (so we never hit ourselves)
    private bool RaycastNoSelf(Vector3 origin, Vector3 direction, float distance, out RaycastHit best)
    {
        best = default;
        bool found = false;
        float closest = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;   // part of the player, skip
            if (h.distance < closest)
            {
                closest = h.distance;
                best = h;
                found = true;
            }
        }
        return found;
    }

    // looks around you (sides + front diagonals + front) for the closest wall named wallObjectName
    // sideDot: how far to the right (+) or left (-) of you the wall is. Near 0 = head-on.
    private bool FindWall(out Vector3 normal, out bool onRight, out float sideDot)
    {
        normal = Vector3.zero;
        onRight = false;
        sideDot = 0f;

        Vector3 origin = transform.position + characterController.center;
        float dist = characterController.radius + wallCheckDistance;

        Vector3 f = transform.forward;
        Vector3 r = transform.right;
        Vector3[] dirs =
        {
            r, -r,
            (f + r).normalized, (f - r).normalized,
            f
        };

        bool found = false;
        float bestDist = float.MaxValue;

        foreach (Vector3 d in dirs)
        {
            RaycastHit hit;
            if (!RaycastNoSelf(origin, d, dist, out hit)) continue;
            if (hit.collider.gameObject.name != wallObjectName) continue;
            if (Mathf.Abs(hit.normal.y) > 0.3f) continue;            // only vertical walls

            if (hit.distance < bestDist)
            {
                bestDist = hit.distance;
                normal = hit.normal;
                Vector3 toWall = (hit.point - origin);
                toWall.y = 0f;
                toWall = toWall.normalized;
                sideDot = Vector3.Dot(toWall, r);
                onRight = sideDot >= 0f;
                found = true;
            }
        }

        return found;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // dead or paused: no movement

        bool grounded = characterController.isGrounded;

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float inputV = canMove ? Input.GetAxis("Vertical") : 0;
        float inputH = canMove ? Input.GetAxis("Horizontal") : 0;
        bool hasInput = new Vector2(inputH, inputV).sqrMagnitude > 0.01f;

        bool crouchHeld = Input.GetKey(KeyCode.LeftControl) && canMove;
        bool crouchPressed = Input.GetKeyDown(KeyCode.LeftControl) && canMove;
        bool sprintHeld = Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetButton("Jump") && canMove && grounded;

        float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;

        // ---------- slide buffer: pressing crouch in the air slides you on landing ----------
        if (crouchPressed && !grounded) slideBuffer = slideBufferTime;
        if (slideBuffer > 0f) slideBuffer -= Time.deltaTime;

        bool wantsSlide = crouchPressed || (slideBuffer > 0f && crouchHeld);

        // start a slide: grounded + already moving fast. Every slide ADDS speed.
        if (wantsSlide && grounded && !isSliding && horizontalSpeed > walkSpeed + 1f)
        {
            isSliding = true;
            slideBuffer = 0f;
            Vector3 dir = new Vector3(velocity.x, 0, velocity.z).normalized;
            float newSpeed = Mathf.Max(horizontalSpeed, slideSpeed) + slideBoost;
            velocity = dir * Mathf.Min(newSpeed, maxSpeed);
        }

        // end a slide: crouch released, jumped out of it, or slowed down enough
        bool jumpedOutOfSlide = false;
        if (isSliding && (!crouchHeld || jumpPressed || horizontalSpeed <= crouchSpeed + 0.5f))
        {
            jumpedOutOfSlide = jumpPressed;
            isSliding = false;
        }

        bool crouching = crouchHeld || isSliding;
        bool isRunning = sprintHeld && !crouching;
        float currentSpeed = crouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed;

        // ---------- wall running ----------
        if (wallRunCooldownTimer > 0f) wallRunCooldownTimer -= Time.deltaTime;

        // remember taps of A / D for a short time so the timing is forgiving
        if (canMove && Input.GetKeyDown(KeyCode.A)) tapLeftTimer = wallTapWindow;
        if (canMove && Input.GetKeyDown(KeyCode.D)) tapRightTimer = wallTapWindow;
        if (tapLeftTimer > 0f) tapLeftTimer -= Time.deltaTime;
        if (tapRightTimer > 0f) tapRightTimer -= Time.deltaTime;

        Vector3 wallNormal = Vector3.zero;
        Vector3 wallForward = Vector3.zero;
        bool wallOnRight = false;
        float wallSideDot = 0f;
        bool foundWall = !grounded && FindWall(out wallNormal, out wallOnRight, out wallSideDot);

        if (foundWall)
        {
            // direction along the wall: the way you're running (or the way you're moving / facing when starting)
            wallForward = Vector3.Cross(wallNormal, Vector3.up).normalized;

            Vector3 reference;
            if (isWallRunning)
                reference = wallRunDir;
            else if (horizontalSpeed > 0.5f)
                reference = new Vector3(velocity.x, 0f, velocity.z);
            else
                reference = forward;

            if (Vector3.Dot(wallForward, reference) < 0f) wallForward = -wallForward;
        }

        Vector3 bodyCenter = transform.position + characterController.center;
        RaycastHit tmpHit;

        if (!isWallRunning)
        {
            bool highEnough = !RaycastNoSelf(bodyCenter, Vector3.down,
                characterController.height / 2f + minWallRunHeight, out tmpHit);

            // tap A for a wall on your left, D for a wall on your right (head-on: either works)
            bool headOn = Mathf.Abs(wallSideDot) < 0.2f;
            bool tapOK = headOn ? (tapLeftTimer > 0f || tapRightTimer > 0f)
                                : (wallOnRight ? tapRightTimer > 0f : tapLeftTimer > 0f);

            if (foundWall && highEnough && tapOK && horizontalSpeed > minWallRunEntrySpeed
                && !crouching && wallRunCooldownTimer <= 0f)
            {
                isWallRunning = true;
                isSliding = false;
                wallRunTimer = 0f;
                wallSideRight = headOn ? (tapRightTimer > 0f) : wallOnRight;
                wallRunDir = wallForward;
                tapLeftTimer = 0f;
                tapRightTimer = 0f;
                if (verticalVelocity < 0f) verticalVelocity = 0f;   // catch the wall instead of falling
            }
        }
        else
        {
            wallRunTimer += Time.deltaTime;
            bool blockedAhead = foundWall && RaycastNoSelf(bodyCenter, wallForward,
                characterController.radius + 0.4f, out tmpHit);

            if (!foundWall || grounded || wallRunTimer > wallRunMaxTime
                || blockedAhead || horizontalSpeed < 2f || crouchHeld)
            {
                // fell off, ran out of time, hit something, or let go
                isWallRunning = false;
                wallRunCooldownTimer = wallRunCooldown;
            }
            else if (Input.GetButtonDown("Jump") && canMove)
            {
                // wall jump: keep your speed along the wall, push away from it, go up
                float along = Mathf.Max(Vector3.Dot(velocity, wallForward), 0f);
                Vector3 jumpVel = wallForward * along + wallNormal * wallJumpSideForce;
                if (jumpVel.magnitude > maxSpeed) jumpVel = jumpVel.normalized * maxSpeed;

                velocity = new Vector3(jumpVel.x, 0f, jumpVel.z);
                verticalVelocity = wallJumpUpForce;
                horizontalSpeed = velocity.magnitude;

                isWallRunning = false;
                wallRunCooldownTimer = wallRunCooldown;
            }
            else
            {
                wallRunDir = wallForward;
                wallSideRight = Vector3.Dot(-wallNormal, right) >= 0f;
            }
        }

        // ---------- horizontal movement ----------
        Vector3 wishDir = Vector3.ClampMagnitude((forward * inputV) + (right * inputH), 1f);

        if (isWallRunning)
        {
            // run along the wall with a small push into it so you stay attached
            float along = Mathf.Max(Vector3.Dot(velocity, wallForward), wallRunSpeed);
            along = Mathf.Min(along, maxSpeed);
            Vector3 v = wallForward * along - wallNormal * wallStickForce;
            velocity = new Vector3(v.x, 0f, v.z);
        }
        else if (isSliding)
        {
            // friction slowly eats the slide's momentum
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, slideFriction * Time.deltaTime);
        }
        else if (grounded)
        {
            bool overspeed = horizontalSpeed > runSpeed + 0.5f;

            if (hasInput)
            {
                Vector3 dir = wishDir.normalized;
                float targetSpeed = currentSpeed * wishDir.magnitude;

                // split velocity into the part along your input and the part sideways
                float along = Vector3.Dot(velocity, dir);
                Vector3 lateral = velocity - dir * along;

                float counter = counterAcceleration * (overspeed ? overspeedCounterMultiplier : 1f);

                // counter momentum: cancel sideways drift fast
                lateral = Vector3.MoveTowards(lateral, Vector3.zero, counter * Time.deltaTime);

                if (along < 0f)
                    along = Mathf.MoveTowards(along, targetSpeed, counter * Time.deltaTime);                // reversing: strong counter
                else if (along < targetSpeed)
                    along = Mathf.MoveTowards(along, targetSpeed, groundAcceleration * Time.deltaTime);     // speeding up
                else
                    along = Mathf.MoveTowards(along, targetSpeed, overspeedDeceleration * Time.deltaTime);  // slow bleed above target

                velocity = dir * along + lateral;
            }
            else
            {
                // no input: counter your own velocity to stop quickly (slowly if you have slide/jump speed)
                float decel = overspeed ? overspeedDeceleration : stopDeceleration;
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, decel * Time.deltaTime);
            }
        }
        else
        {
            // in the air: keep your speed, only steer
            if (hasInput)
            {
                float limit = Mathf.Max(horizontalSpeed, currentSpeed);
                velocity += wishDir * airAcceleration * Time.deltaTime;
                if (velocity.magnitude > limit)
                    velocity = velocity.normalized * limit;
            }
            else
            {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, airDrag * Time.deltaTime);
            }
        }

        // ---------- vertical + momentum jump ----------
        if (jumpPressed)
        {
            float speedNow = new Vector3(velocity.x, 0, velocity.z).magnitude;

            // faster = higher jump
            float speedAboveRun = Mathf.Max(0f, speedNow - runSpeed);
            float jumpBonus = Mathf.Min(speedAboveRun * speedToJumpHeight, maxJumpBonus);
            verticalVelocity = jumpPower + jumpBonus;

            // faster = further jump: add speed in the direction you're already moving
            if (speedNow > walkSpeed)
            {
                Vector3 dir = new Vector3(velocity.x, 0, velocity.z).normalized;
                float boost = jumpForwardBoost + (jumpedOutOfSlide ? slideJumpBoost : 0f);
                velocity = dir * Mathf.Min(speedNow + boost, maxSpeed);
            }
        }
        else if (isWallRunning)
            verticalVelocity -= gravity * wallRunGravityMultiplier * Time.deltaTime;   // very light gravity on the wall
        else if (grounded && verticalVelocity < 0)
            verticalVelocity = -2f;   // small push down keeps isGrounded stable
        else if (!grounded)
            verticalVelocity -= gravity * Time.deltaTime;

        // ---------- crouch / slide hitbox: shrink from the top so your feet stay on the ground ----------
        float targetHeight = crouching ? crouchHeight : defaultHeight;

        // trying to stand up: stay crouched if something is above you
        if (targetHeight > characterController.height)
        {
            float insideTop = 0.05f; // start the ray slightly inside our own capsule
            Vector3 top = transform.position + characterController.center
                          + Vector3.up * (characterController.height / 2f - insideTop);
            float checkDistance = (defaultHeight - characterController.height) + insideTop;

            if (RaycastNoSelf(top, Vector3.up, checkDistance, out tmpHit))
                targetHeight = characterController.height;   // blocked, stay as you are
        }

        float heightDiff = targetHeight - defaultHeight;   // 0 standing, negative crouching

        characterController.height = targetHeight;
        characterController.center = controllerStandCenter + new Vector3(0f, heightDiff / 2f, 0f);

        // shrink any extra capsule colliders the same way (bottom stays in place)
        float ratio = targetHeight / defaultHeight;
        for (int i = 0; i < extraColliders.Length; i++)
        {
            if (extraColliders[i] == null) continue;
            float newH = extraStandHeights[i] * ratio;
            extraColliders[i].height = newH;
            extraColliders[i].center = extraStandCenters[i] + new Vector3(0f, (newH - extraStandHeights[i]) / 2f, 0f);
        }

        // shrink the visible body along with the hitbox
        if (playerBody != null)
        {
            playerBody.localScale = new Vector3(1f, ratio, 1f);
            playerBody.localPosition = new Vector3(0f, heightDiff / 2f, 0f);
        }

        // lower the camera with the hitbox (smooth)
        float camTargetY = cameraStandY + heightDiff;
        Vector3 camPos = playerCamera.transform.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, camTargetY, 15f * Time.deltaTime);
        playerCamera.transform.localPosition = camPos;

        // camera roll while wall running
        float targetTilt = isWallRunning ? (wallSideRight ? wallRunCamTilt : -wallRunCamTilt) : 0f;
        camTilt = Mathf.Lerp(camTilt, targetTilt, 10f * Time.deltaTime);

        // FOV grows a few degrees while sprinting, then eases back
        bool sprinting = isRunning && hasInput && horizontalSpeed > walkSpeed + 0.5f;
        float targetFov = baseFov + (sprinting ? sprintFovBoost : 0f);
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-fovChangeSpeed * Time.deltaTime));

        Vector3 finalMove = new Vector3(velocity.x, verticalVelocity, velocity.z);
        characterController.Move(finalMove * Time.deltaTime);

        // ---------- look ----------
        if (canMove && !PauseMenu.GameIsPaused)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, camTilt);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}