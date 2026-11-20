using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[DefaultExecutionOrder(200)]   // runs after the katana flip and the camera move, so the rope sticks to the handle
public class KatanaGrapple : MonoBehaviour
{
    [Header("Setup")]
    public Transform handleTip;          // empty object at the END of the handle (the rope starts here)
    public Transform cam;                // leave empty, found from PlayerMovement
    public Transform player;             // leave empty, found from PlayerMovement
    public LayerMask whatIsGrappleable;  // set this to your "Slash" layer ONLY
    public int mouseButton = 1;          // 1 = right click (same button as the flip)
    public float fireDelay = 0.1f;       // wait for the flip before firing
    public float maxDistance = 25f;      // the radius: how close you have to be

    [Header("Pull (SpringJoint)")]
    public float spring = 4.5f;
    public float damper = 7f;
    public float massScale = 4.5f;
    [Range(0.05f, 1f)] public float maxDistanceMultiplier = 0.8f;   // lower = pulls harder
    [Range(0f, 1f)] public float minDistanceMultiplier = 0.25f;

    [Header("Rope look")]
    public int quality = 100;
    public float ropeDamper = 14f;
    public float ropeStrength = 800f;
    public float ropeVelocity = 15f;
    public float waveCount = 3f;
    public float waveHeight = 1f;
    public AnimationCurve affectCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
    public float ropeCatchUpSpeed = 12f;

    LineRenderer lr;
    SpringJoint joint;
    Vector3 grapplePoint;
    Vector3 currentGrapplePosition;
    RopeSpring ropeSpring;
    bool pending;
    float pressTime;

    public bool IsGrappling() { return joint != null; }
    public Vector3 GetGrapplePoint() { return grapplePoint; }

    // true when you are in range AND your crosshair points at something on the grapple layer
    // (KatanaSwing uses this to decide if the katana is allowed to flip)
    public bool LookingAtTarget()
    {
        Vector3 p;
        return FindGrapplePoint(out p);
    }

    void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;

        ropeSpring = new RopeSpring();
        ropeSpring.SetTarget(0f);

        if (PlayerMovement.Instance != null)
        {
            if (player == null) player = PlayerMovement.Instance.transform;
            if (cam == null) cam = PlayerMovement.Instance.playerCam;
        }
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        currentGrapplePosition = HandlePosition();
    }

    Vector3 HandlePosition()
    {
        return handleTip != null ? handleTip.position : transform.position;
    }

    void Update()
    {
        if (player == null || cam == null) return;

        // paused or dead: let go
        if (Time.timeScale == 0f || PauseMenu.GameIsPaused)
        {
            pending = false;
            StopGrapple();
            return;
        }

        // only start if you are looking at a grapple block in range
        if (Input.GetMouseButtonDown(mouseButton))
        {
            pending = LookingAtTarget();
            pressTime = Time.time;
        }

        // fire once the katana has flipped (right click is still held)
        if (pending && Input.GetMouseButton(mouseButton) && Time.time >= pressTime + fireDelay)
        {
            pending = false;
            StartGrapple();
        }

        if (Input.GetMouseButtonUp(mouseButton))
        {
            pending = false;
            StopGrapple();
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void OnDisable()
    {
        pending = false;
        StopGrapple();
    }

    // true if this collider belongs to the player or the camera/weapons
    bool IsOwn(Transform t)
    {
        if (player != null && t.IsChildOf(player)) return true;
        if (cam != null && t.root == cam.root) return true;
        return false;
    }

    // finds the first thing your crosshair hits (any layer, so walls block it).
    // it only counts if that thing is on the grapple layer and within the radius.
    bool FindGrapplePoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (cam == null) return false;

        RaycastHit[] hits = Physics.RaycastAll(cam.position, cam.forward, maxDistance,
            ~0, QueryTriggerInteraction.Ignore);

        bool found = false;
        float best = float.MaxValue;
        RaycastHit nearest = new RaycastHit();

        foreach (RaycastHit h in hits)
        {
            if (IsOwn(h.collider.transform)) continue;
            if (h.distance < best)
            {
                best = h.distance;
                nearest = h;
                found = true;
            }
        }

        if (!found) return false;

        // the nearest thing must be on the grapple layer
        if ((whatIsGrappleable.value & (1 << nearest.collider.gameObject.layer)) == 0) return false;

        point = nearest.point;
        return true;
    }

    void StartGrapple()
    {
        if (joint != null) return;
        if (!FindGrapplePoint(out grapplePoint)) return;

        joint = player.gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = grapplePoint;

        float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);

        // the distance the grapple tries to keep from the grapple point
        joint.maxDistance = distanceFromPoint * maxDistanceMultiplier;
        joint.minDistance = distanceFromPoint * minDistanceMultiplier;

        joint.spring = spring;
        joint.damper = damper;
        joint.massScale = massScale;

        currentGrapplePosition = HandlePosition();
    }

    void StopGrapple()
    {
        if (joint != null) Destroy(joint);
        joint = null;
    }

    void DrawRope()
    {
        Vector3 start = HandlePosition();

        // not grappling: no rope
        if (joint == null)
        {
            currentGrapplePosition = start;
            if (ropeSpring != null) ropeSpring.Reset();
            if (lr != null && lr.positionCount > 0) lr.positionCount = 0;
            return;
        }

        int q = Mathf.Max(2, quality);

        if (lr.positionCount != q + 1)
        {
            ropeSpring.SetVelocity(ropeVelocity);
            lr.positionCount = q + 1;
        }

        ropeSpring.SetDamper(ropeDamper);
        ropeSpring.SetStrength(ropeStrength);
        ropeSpring.Update(Time.deltaTime);

        Vector3 dir = grapplePoint - start;
        if (dir.sqrMagnitude < 0.0001f) return;

        Vector3 up = Quaternion.LookRotation(dir.normalized) * Vector3.up;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, grapplePoint, Time.deltaTime * ropeCatchUpSpeed);

        for (int i = 0; i < q + 1; i++)
        {
            float delta = i / (float)q;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI)
                             * ropeSpring.Value * affectCurve.Evaluate(delta);
            lr.SetPosition(i, Vector3.Lerp(start, currentGrapplePosition, delta) + offset);
        }
    }

    // the rope wobble (plain class, so Unity doesn't warn like it did with the MonoBehaviour Spring)
    class RopeSpring
    {
        float strength, damper, target, velocity, value;

        public void Update(float deltaTime)
        {
            float direction = target - value >= 0f ? 1f : -1f;
            float force = Mathf.Abs(target - value) * strength;
            velocity += (force * direction - velocity * damper) * deltaTime;
            value += velocity * deltaTime;
        }

        public void Reset() { velocity = 0f; value = 0f; }
        public void SetTarget(float t) { target = t; }
        public void SetDamper(float d) { damper = d; }
        public void SetStrength(float s) { strength = s; }
        public void SetVelocity(float v) { velocity = v; }
        public float Value { get { return value; } }
    }
}