using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotationTester : MonoBehaviour
{

    public float angle = 0;

    public Vector3 axis;

    public Vector3 up = Vector3.up;
    public Vector3 right = Vector3.right;
    public Vector3 forward = Vector3.forward;

    public float latitude = 0;
    public float longitude = 0;

    // Start is called before the first frame update
    void Start()
    {
        this.axis = new Vector3(Mathf.Sin(Mathf.PI / 2f), Mathf.Cos(Mathf.PI / 2f), 0);
    }

    // Update is called once per frame
    void Update()
    {
        this.forward.x = Mathf.Sin(this.longitude) * Mathf.Cos(this.latitude);
        this.forward.y = Mathf.Sin(this.latitude);
        this.forward.z = Mathf.Cos(this.longitude) * Mathf.Cos(this.latitude);

        this.right = new Vector3(this.forward.z, this.forward.y, this.forward.x);
    }

    private void OnDrawGizmos ( ) {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(this.transform.position, 0.1f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(this.transform.position, this.axis);

        //Gizmos.color = Color.green;
        //Gizmos.DrawWireSphere(this.up, 0.1f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.right, 0.1f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(this.forward, 0.1f);
    }

    private Quaternion ConstructQuaternion(float _angle, Vector3 _point) {
        return Quaternion.identity;
    }
}
