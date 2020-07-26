using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

public class Follower : MonoBehaviour
{
    public readonly Vector3 Up = Vector3.up;

    public GameObject Following;
    private ICanBeFollowed target;

    [Header("Offset")]
    public float Backwards;
    public float Upwards;
    public float Sideways;


    private void Update()
    {
        if (Following != null)
        {
            Vector3 newPos = Following.transform.position;

            transform.position = newPos;

            if (target != null)
            {
                if(target.Forward != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(target.Forward, Up);
                }
                
                // Offset is backwards + upwards + sideways
                Vector3 relativeOffset = (target.Forward * Backwards) - (Up * Upwards);
                transform.position = transform.position - relativeOffset;
            }
        }
    }




    private void OnValidate()
    {
        if (Following != null)
        {
            if (Utils.GameObjectExtendsClass(Following, out ICanBeFollowed f))
            {
                target = f;
            }
        }

    }

}
