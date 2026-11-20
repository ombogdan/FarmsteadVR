using System.Collections.Generic;
using UnityEngine;

// Makes an animal live a little:
// - wanders slowly around its area and stops for a while to eat (head goes down; chickens peck);
// - when you come close, it stops and looks at you;
// - while talking it faces you (Face);
// - it can walk a route (GoTo / GoVia), e.g. through the coop door to its sleeping spot;
// - at night it sleeps (SetSleeping) and doesn't wander.
// The animal models look along -Z (nose/beak at -Z), so "forward" for them is -transform.forward.
public class Grazer : MonoBehaviour
{
    public Vector2 areaMin = new Vector2(-5f, 1f); // x, z
    public Vector2 areaMax = new Vector2(4f, 8f);
    public float speed = 0.3f;
    public float waitMin = 5f, waitMax = 12f;     // how long it eats before walking on
    public float eatTilt = 35f;                   // how far the head goes down
    public bool pecks;                            // chickens peck quickly instead of grazing
    public float curiousDistance = 2.5f;          // stops and looks at you this close (0 = never)
    public Transform head;

    readonly Queue<Vector3> route = new Queue<Vector3>();
    Vector3 wanderTarget;
    float waitUntil;
    Transform faceTarget;
    bool sleeping;
    float headTilt;
    float peckPhase;
    float mealAfterRoute; // EatAt: how long to eat when the walk is over

    public bool Sleeping => sleeping;

    public void GoTo(Vector3 point)
    {
        GoVia(point);
    }

    // Walks through the points one after another (e.g. in front of the door -> inside -> its spot).
    public void GoVia(params Vector3[] points)
    {
        route.Clear();
        mealAfterRoute = 0f;
        foreach (Vector3 p in points) route.Enqueue(p);
    }

    // Walks there and eats for a while (hens at the feeder).
    public void EatAt(Vector3 point, float seconds)
    {
        GoVia(point);
        mealAfterRoute = seconds;
    }

    // Jumps straight there (used when the game is loaded at night).
    public void PlaceAt(Vector3 point)
    {
        route.Clear();
        transform.position = new Vector3(point.x, transform.position.y, point.z);
    }

    public void SetSleeping(bool value)
    {
        sleeping = value;
        if (!value) waitUntil = Time.time + Random.Range(0.5f, 2f);
    }

    public void Face(Transform who)
    {
        faceTarget = who;
    }

    public void StopFacing()
    {
        faceTarget = null;
    }

    void Start()
    {
        PickWanderTarget();
        waitUntil = Time.time + Random.Range(1f, waitMax);
        peckPhase = Random.Range(0f, 10f);
    }

    void Update()
    {
        bool eating = false;
        float dozing = 0f;
        Camera cam = Camera.main;
        bool playerNear = cam != null && curiousDistance > 0f && FlatDistance(cam.transform.position) < curiousDistance;

        if (route.Count > 0)
        {
            if (MoveTowards(route.Peek(), speed * 2f))
            {
                route.Dequeue();
                if (route.Count == 0 && mealAfterRoute > 0f)
                {
                    waitUntil = Time.time + mealAfterRoute; // "eating" = head down, pecking
                    mealAfterRoute = 0f;
                }
            }
        }
        else if (faceTarget != null)
        {
            TurnTowards(faceTarget.position, 4f);
        }
        else if (sleeping)
        {
            dozing = 20f; // head a little down, eyes closed (in our imagination)
        }
        else if (playerNear)
        {
            TurnTowards(cam.transform.position, 2.5f);
        }
        else if (Time.time < waitUntil)
        {
            eating = true;
        }
        else if (MoveTowards(wanderTarget, speed))
        {
            waitUntil = Time.time + Random.Range(waitMin, waitMax);
            PickWanderTarget();
        }

        float want = dozing;
        if (eating) want = pecks ? (Mathf.Sin(Time.time * 9f + peckPhase) > 0.2f ? eatTilt : eatTilt * 0.2f) : eatTilt;
        headTilt = Mathf.MoveTowards(headTilt, want, (pecks ? 300f : 60f) * Time.deltaTime);
        if (head != null) head.localRotation = Quaternion.Euler(-headTilt, 0f, 0f);
    }

    float FlatDistance(Vector3 point)
    {
        return Vector2.Distance(new Vector2(point.x, point.z), new Vector2(transform.position.x, transform.position.z));
    }

    // Returns true when the point is reached.
    bool MoveTowards(Vector3 point, float moveSpeed)
    {
        Vector3 flat = new Vector3(point.x - transform.position.x, 0f, point.z - transform.position.z);
        if (flat.magnitude < 0.12f) return true;
        TurnTowards(point, 2.5f);
        transform.position += flat.normalized * Mathf.Min(moveSpeed * Time.deltaTime, flat.magnitude);
        return false;
    }

    void TurnTowards(Vector3 point, float turnSpeed)
    {
        Vector3 flat = new Vector3(point.x - transform.position.x, 0f, point.z - transform.position.z);
        if (flat.sqrMagnitude < 0.0001f) return;
        Quaternion want = Quaternion.LookRotation(-flat.normalized, Vector3.up); // nose (-Z) towards the point
        transform.rotation = Quaternion.Slerp(transform.rotation, want, Time.deltaTime * turnSpeed);
    }

    void PickWanderTarget()
    {
        wanderTarget = new Vector3(Random.Range(areaMin.x, areaMax.x), transform.position.y, Random.Range(areaMin.y, areaMax.y));
    }
}
