using UnityEngine;

public class SpinAndBob : MonoBehaviour
{
    [Header("Random Spin")]
    public float minSpinSpeed = 60f;       // degrees per second
    public float maxSpinSpeed = 180f;
    public float changeInterval = 2.5f;    // seconds between picking a new random spin
    public float smoothing = 1.5f;         // how quickly it blends into the new spin (higher = faster)

    [Header("Bob")]
    public bool bob = true;
    public float bobHeight = 0.25f;
    public float bobSpeed = 2f;

    private Vector3 startPosition;
    private Vector3 currentSpin;   // degrees per second on each axis
    private Vector3 targetSpin;
    private float timer;
    private float bobOffset;

    void Start()
    {
        startPosition = transform.position;
        bobOffset = Random.Range(0f, 10f);   // so multiple milks don't bob in sync

        targetSpin = RandomSpin();
        currentSpin = targetSpin;
        timer = changeInterval;
    }

    void Update()
    {
        // pick a new random spin every few seconds
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            targetSpin = RandomSpin();
            timer = changeInterval * Random.Range(0.7f, 1.3f);
        }

        // blend smoothly toward the new spin
        currentSpin = Vector3.Lerp(currentSpin, targetSpin, smoothing * Time.deltaTime);

        float speed = currentSpin.magnitude;
        if (speed > 0.01f)
            transform.Rotate(currentSpin / speed, speed * Time.deltaTime, Space.World);

        if (bob)
        {
            float offset = Mathf.Sin((Time.time + bobOffset) * bobSpeed) * bobHeight;
            transform.position = startPosition + new Vector3(0f, offset, 0f);
        }
    }

    Vector3 RandomSpin()
    {
        return Random.onUnitSphere * Random.Range(minSpinSpeed, maxSpinSpeed);
    }
}