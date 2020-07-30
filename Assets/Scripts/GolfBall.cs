using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GolfBall : MonoBehaviour, ICanBeFollowed
{
    // Constants 
    public readonly static RigidPreset Air = new RigidPreset(0f, 0f);
    public readonly static RigidPreset Grass = new RigidPreset(0.75f, 1f);

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
    public const float FullPower = 50;
    public const float VelocityMagnitudeCutoffThreshold = 0.25f;

    [Header("Control settings")]
    [Range(0, 1)] public float Power;
    public float Rotation;
    public float Angle;


    private const float Scale = 0.25f;
    public float Radius => Scale / 2;

    /// <summary>
    /// Event called when the GolfBall has finished rolling and the Shooting state has been entered.
    /// </summary>
    public UnityAction OnRollingFinished;


    [Header("References")]
    public Rigidbody rigid;
    public SphereCollider sphereCollider;


    [Header("Line Preview")]
    public Material LinePreviewMaterial;
    private LineRenderer shotPreview;
    public Gradient LineColour;


    private void Awake()
    {
        transform.localScale = new Vector3(Scale, Scale, Scale);

        GameStats = new Stats();
        GameStats.Reset();

        OnRollingFinished += Utils.EMPTY;

        // Set the shot preview
        shotPreview = gameObject.AddComponent<LineRenderer>();
        shotPreview.widthCurve = AnimationCurve.Linear(0f, 0.05f, 1f, 0.05f);
        shotPreview.enabled = false;
        shotPreview.material = LinePreviewMaterial;
        shotPreview.colorGradient = LineColour;
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
        Vector3 force = transform.forward * Power * FullPower;
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



    public void WaitForNextShot()
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




    public void SetShotPreview(bool useRotation, bool useAngle, bool usePower, float lengthMultiplier)
    {
        // Calculate which axis to use
        Vector3 offset = transform.forward;
        offset.x = useRotation ? offset.x : 0;
        offset.z = useRotation ? offset.z : 0;

        offset.y = useAngle ? offset.y : 0;

        // Now ensure it is the correct length
        offset.Normalize();
        offset *= lengthMultiplier;

        // Apply power multiplier if we need to
        offset *= usePower ? Power : 1;

        // Assign the point
        shotPreview.SetPositions(new Vector3[] { transform.position, transform.position + offset });
    }

    public void SetShotPreviewVisible(bool visible)
    {
        shotPreview.enabled = visible;
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
        // Default values
        Power = 0.5f;
        Rotation = 0;
        Angle = 40;

        ValidateValues();
    }

    private void ValidateValues()
    {
        // Clamp the power
        Power = Mathf.Clamp01(Power);

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
        Angle = Mathf.Clamp(Angle, 0, 80);


        transform.rotation = Quaternion.Euler(-Angle, Rotation, 0);
    }


    private void OnValidate()
    {
        ValidateValues();
    }




    private void OnDrawGizmos()
    {
        // Draw the facing
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Forward / 2);
    }






    public class Stats
    {
        public int Shots;

        public void Reset()
        {
            Shots = 0;
        }
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


    public enum Parameters
    {
        Rotation,
        Angle,
        Power,
    }
}
