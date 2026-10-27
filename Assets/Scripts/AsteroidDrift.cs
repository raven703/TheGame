using UnityEngine;

/// <summary>Inertial dynamic cover; preparation freezes motion without losing momentum.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public class AsteroidDrift : MonoBehaviour
{
    private Rigidbody2D body;
    private Vector2 velocity;
    private float spin;
    private bool suspended = true;
    public void Initialize(Vector2 initialVelocity, float angularVelocity)
    {
        body = GetComponent<Rigidbody2D>(); velocity = initialVelocity; spin = angularVelocity;
        body.bodyType = RigidbodyType2D.Kinematic;
    }
    private void FixedUpdate()
    {
        if(body==null)return;
        bool preparing=CombatSession.IsPreparing;
        if(preparing==suspended)return;
        if(preparing){velocity=body.linearVelocity;spin=body.angularVelocity;body.bodyType=RigidbodyType2D.Kinematic;body.linearVelocity=Vector2.zero;body.angularVelocity=0;}
        else {body.bodyType=RigidbodyType2D.Dynamic;body.linearVelocity=velocity;body.angularVelocity=spin;}
        suspended=preparing;
    }
}