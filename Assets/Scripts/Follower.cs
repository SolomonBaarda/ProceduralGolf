using UnityEngine;

public class Follower : MonoBehaviour
{
    public readonly Vector3 Up = Vector3.up;

    public GameObject Following;
    private ICanBeFollowed target;

    public Vector3 Offset;


    private void Update()
    {
        if (Following != null)
        {
            Vector3 newPos = Following.transform.position + Offset;

            if (target != null)
            {
                transform.rotation = Quaternion.Euler(target.Forward);


            }

            transform.position = newPos;
        }
    }




    private void OnValidate()
    {
        if (Following != null)
        {
            target = Utils.GetClass<ICanBeFollowed>(Following);
        }
    }

}
