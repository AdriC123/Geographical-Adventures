using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderBotAI : MonoBehaviour
{

    public float _speed = 3f;
    public float smoothness = 5f;
    public int raysNb = 8;
    public float raysEccentricity = 0.2f;
    public float outerRaysOffset = 2f;
    public float innerRaysOffset = 25f;

    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastPosition;
    private Vector3 forward;
    private Vector3 upward;
    private Quaternion lastRot;
    private Vector3[] pn;

    static Vector3[] GetClosestPoint(Vector3 point, Vector3 forward, Vector3 up, float halfRange, float eccentricity, float offset1, float offset2, int rayAmount)
    {
        Vector3[] res = new Vector3[2] { point, up };
        Vector3 right = Vector3.Cross(up, forward);
        float normalAmount = 1f;
        float positionAmount = 1f;

        Vector3[] dirs = new Vector3[rayAmount];
        float angularStep = 2f * Mathf.PI / (float)rayAmount;
        float currentAngle = angularStep / 2f;
        for(int i = 0; i < rayAmount; ++i)
        {
            dirs[i] = -up + (right * Mathf.Cos(currentAngle) + forward * Mathf.Sin(currentAngle)) * eccentricity;
            currentAngle += angularStep;
        }

        foreach (Vector3 dir in dirs)
        {
            RaycastHit hit;
            Vector3 largener = Vector3.ProjectOnPlane(dir, up);
            Ray ray = new Ray(point - (dir + largener) * halfRange + largener.normalized * offset1 / 100f, dir);
            //Debug.DrawRay(ray.origin, ray.direction);
            if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
            {
                res[0] += hit.point;
                res[1] += hit.normal;
                normalAmount += 1;
                positionAmount += 1;
            }
            ray = new Ray(point - (dir + largener) * halfRange + largener.normalized * offset2 / 100f, dir);
            //Debug.DrawRay(ray.origin, ray.direction, Color.green);
            if (Physics.SphereCast(ray, 0.01f, out hit, 2f * halfRange))
            {
                res[0] += hit.point;
                res[1] += hit.normal;
                normalAmount   += 1;
                positionAmount += 1;
            }
        }
        res[0] /= positionAmount;
        res[1] /= normalAmount;
        return res;
    }

    // Start is called before the first frame update
    void Start()
    {
        velocity = new Vector3();
        forward = transform.forward;
        upward = transform.up;
        lastRot = transform.rotation;
    }

    // Update is called once per frame
    void FixedUpdate()
    {

        velocity = (smoothness * velocity + (transform.position - lastPosition)) / (1f + smoothness);
        if (velocity.magnitude < 0.00025f)
            velocity = lastVelocity;
        lastPosition = transform.position;
        lastVelocity = velocity;
        
        float multiplier = 1f;
        if (Input.GetKey(KeyCode.LeftShift))
            multiplier = 2f;

        float valueY = Input.GetAxis("Vertical");
        if (valueY != 0)
            transform.position += transform.forward * valueY * _speed * multiplier * Time.fixedDeltaTime;
        float valueX = Input.GetAxis("Horizontal");
        if (valueX != 0)
            transform.position += Vector3.Cross(transform.up, transform.forward) * valueX * _speed * multiplier * Time.fixedDeltaTime;

        if (valueX != 0 || valueY != 0)
        {
            pn = GetClosestPoint(transform.position, transform.forward, transform.up, 0.5f, 0.1f, 30, -30, 4);
            //        pn = GetClosestPointIco(transform.position, transform.up, 0.2f);

            upward = pn[1];

            Vector3[] pos = GetClosestPoint(transform.position, transform.forward, transform.up, 0.5f, raysEccentricity, innerRaysOffset, outerRaysOffset, raysNb);
            transform.position = Vector3.Lerp(lastPosition, pos[0], 1f / (1f + smoothness));

            forward = velocity.normalized;
            Quaternion q = Quaternion.LookRotation(forward, upward);
            transform.rotation = Quaternion.Lerp(lastRot, q, 1f / (1f + smoothness));
        }

        lastRot = transform.rotation;

    }
}
