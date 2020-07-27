using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GolfBall : MonoBehaviour, ICanBeFollowed
{
    // Constants 
    public readonly static RigidPreset Air = new RigidPreset(0f, 0f);
    public readonly static RigidPreset Grass = new RigidPreset(0.5f, 0.5f);

    // States
    public PlayState State;
    public bool IsOnGround;
    public bool IsFrozen;

    // Statistics
    public Stats GameStats;


    private Vector3 Facing
    {
        get
        {
            if (rigid != null)
            {
                switch (State)
                {
                    case PlayState.Shooting:
                        return transform.forward;
                    // Use the velocity
                    case PlayState.Flying:
                        return rigid.velocity;
                    // Use the velocity
                    case PlayState.Rolling:
                        return rigid.velocity;
                }
            }

            return Vector3.zero;
        }
    }

    public Vector3 Forward => Facing.normalized;


    public Vector3 Position => transform.position;


    // Settings
    public const int Max_Power = 100;
    public float VelocityMagnitudeCutoffThreshold = 0.25f;
    [Header("Control settings")]
    [Range(0, Max_Power)] public float Power = Max_Power / 2;
    public float Rotation;
    public float Angle;


    /// <summary>
    /// Event called when the GolfBall has finished rolling and the Shooting state has been entered.
    /// </summary>
    public UnityAction OnRollingFinished;


    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;




    private void Awake()
    {
        GameStats = new Stats();
    }



    private void FixedUpdate()
    {
        // Update the state
        PlayState stateLastFrame = State;
        State = UpdateState(ref IsOnGround);

        // Check if this is the first frame where the player can start shooting
        if (stateLastFrame == PlayState.Rolling && State == PlayState.Shooting)
        {
            WaitForNextShot();
            OnRollingFinished.Invoke();
        }


        // Update the rigidbody properties
        RigidPreset r = Air;
        // On the ground
        if (IsOnGround)
        {
            r = Grass;
        }

        // Set the values
        rigid.angularDrag = r.AngularDrag;
        rigid.drag = r.Drag;
    }


    public void Shoot()
    {
        // Undo any freezes
        Freeze(false);

        // Apply the force in direction
        Vector3 force = transform.forward * Power;
        rigid.AddForce(force, ForceMode.Impulse);

        GameStats.Shots++;
    }



    public void Reset()
    {
        transform.rotation = Quaternion.identity;

        rigid.velocity = Vector3.zero;
        rigid.angularVelocity = Vector3.zero;

        SetDefaultShotValues();
    }



    private void WaitForNextShot()
    {
        Reset();

        if (!IsFrozen)
        {
            StartCoroutine(FreezeUntilShoot());
        }
    }


    private IEnumerator FreezeUntilShoot()
    {
        Freeze(true);

        int shotsBefore = GameStats.Shots;

        while (shotsBefore == GameStats.Shots)
        {
            yield return null;
        }


        Freeze(false);
    }


    private void Freeze(bool freeze)
    {
        IsFrozen = freeze;

        // Set the freeze mode
        RigidbodyConstraints c = RigidbodyConstraints.None;
        if (freeze)
        {
            c = RigidbodyConstraints.FreezeAll;
        }

        rigid.constraints = c;
    }



    private PlayState UpdateState(ref bool onGround)
    {
        onGround = GroundCheck.IsOnGround(transform.position, sphereCollider.radius + GroundCheck.DEFAULT_RADIUS);
        float velocityMagnitude = rigid.velocity.magnitude;
        PlayState newState;

        if (!onGround)
        {
            newState = PlayState.Flying;
        }
        else
        {
            if (velocityMagnitude < VelocityMagnitudeCutoffThreshold)
            {
                newState = PlayState.Shooting;
            }
            else
            {
                newState = PlayState.Rolling;
            }
        }

        return newState;
    }




    public void SetValues(float rotation, float angle, float power)
    {
        Rotation = rotation;
        Angle = angle;
        Power = power;
        ValidateValues();
    }

    private void SetDefaultShotValues()
    {
        Power = Max_Power / 2;
        Rotation = 0;
        Angle = 0;

        ValidateValues();
    }

    private void ValidateValues()
    {
        // Clamp the power
        Power = Mathf.Clamp(Power, 0, Max_Power);

        // Ensure rotation is between 0 and 360
        while (Rotation < 0)
        {
            Rotation += 360;
        }
        while (Rotation > 360)
        {
            Rotation -= 360;
        }

        // Clamp the angle
        Angle = Mathf.Clamp(Angle, -80, 0);

        // Update the angle
        transform.rotation = Quaternion.Euler(Angle, Rotation, 0);
    }


    private void OnValidate()
    {
        ValidateValues();
    }




    private void OnDrawGizmos()
    {
        // Draw the facing
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + (Power / Max_Power * 10 * Forward));
    }






    public struct Stats
    {
        public int Shots;
    }




    public struct RigidPreset
    {
        public float Drag;
        public float AngularDrag;

        public RigidPreset(float drag, float angularDrag)
        {
            Drag = drag;
            AngularDrag = angularDrag;
        }
    }

    public enum PlayState
    {
        Shooting,
        Flying,
        Rolling,
    }

}
