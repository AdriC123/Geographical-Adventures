using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using UnityEngine;

public class SpiderProceduralAnimation : MonoBehaviour
{
    #region PUBLIC_VAR
    [Tooltip("Every leg target used by the animation rigging package")]
    public Transform[] legTargets;
    [Tooltip("The lenght of every step")]
    public float stepSize = 0.1f;
    [Tooltip("The maximum height of every step")]
    public float stepHeight = 0.05f;
    [Tooltip("How far the leg looks for places to step")]
    public float sphereCastRadius = 0.0085f;
    [Tooltip("How far the leg actually will step")]
    public float raycastRange = 1f;
    [Tooltip("The speed of every leg while performing the step")]
    public float smoothness = 7f;
    [Tooltip("This activates the automatic orientation of the body based on every legs position and orientation. If the controller does not support body orientation, activate it. If it does, you may not need it")]
    public bool bodyOrientation = true;
    [Tooltip("The Enemy to be Procedurally animated.")]
    public SpiderBotAI spiderEnemy;
    //public GameObject spawnHole;
    #endregion

    #region PRIVATE_VAR
    private int spawnCount;
    private Vector3[] defaultLegPositions;
    private Vector3[] lastLegPositions;
    private Vector3 lastBodyUp;
    private Vector3 velocity;
    private Vector3 lastVelocity;
    private Vector3 lastBodyPos;
    private bool[] isAnyLegMoving;
    private bool isThisLegMoving;
    private int numLegs;
    private float velocityMultiplier;
    private LayerMask layerMask = 0;
    /// <summary>   The rig builder attached to this component. </summary>
    private Rig rig;

    #endregion

    private void Start()    //  At the start some necessary variables are set. All of the "last positions" are set to the default values.
    {
        lastBodyUp = transform.up;
        lastBodyPos = transform.position;       

        numLegs = legTargets.Length;    //   Integer "numLegs" (number of legs) is set based on how many targets have been added (1:1 ratio).
        defaultLegPositions = new Vector3[numLegs];
        lastLegPositions = new Vector3[numLegs];    //  
        isAnyLegMoving = new bool[numLegs]; //  Check to see if there is any movement coming from the legs.
        for (int i = 0; i < numLegs; ++i)
        {
            defaultLegPositions[i] = legTargets[i].localPosition;
            lastLegPositions[i] = legTargets[i].position;
            isAnyLegMoving[i] = false;
        }
        if (rig != null) rig.weight = 1f;
    }

    /*public void SpawnHole(int onOff)
    {
        spawnCount = 1;
        if (onOff == 1 && spawnCount == 1)
        {
            spawnHole.SetActive(true);
            spawnHole.transform.SetParent(null);
            spawnCount = 0;
        }
        else
        {
            spawnCount = 0;
            spawnHole.SetActive(false);
            Destroy(spawnHole, 1f);
        }
    }*/

    /// <summary>   Enable or disable the rigBuilder component of this object.  </summary>
    /// <param name="onOff"> Pass 1 to enable the rig and 0 to disable it.  </param>
    public void RigBuilder(int onOff)
    {
        if (onOff == 0)
        {
            Invoke("DeactivateRig", 0f);
        }
        else
        {
            Invoke("ActivateRig", 1f);
        }

    }
    void DeactivateRig()
    {
        rig.weight = 0;
    }

    void ActivateRig()
    {
        rig.weight = 1;
    }

    private void Awake()
    {
        rig = GetComponentInChildren<Rig>();

        if (rig == null)
        {
            Debug.LogError("A Rig component must be placed in " + this.name + " or in its children.");
        }
    }

    void FixedUpdate()
    {
        velocityMultiplier = (2 * smoothness) - 2;
        
        velocity = transform.position - lastBodyPos;
        velocity = (velocity + smoothness * lastVelocity) / (smoothness + 1f);

        if (velocity.magnitude < 0.000025f)
            velocity = lastVelocity;
        else
            lastVelocity = velocity;
        
        Vector3[] desiredPositions = new Vector3[numLegs];
        int indexToMove = -1;
        float maxDistance = stepSize;
        for (int i = 0; i < numLegs; ++i)
        {
            desiredPositions[i] = transform.TransformPoint(defaultLegPositions[i]);

            float distance = Vector3.ProjectOnPlane(desiredPositions[i] + velocity * velocityMultiplier - lastLegPositions[i], transform.up).magnitude;
            if (distance > maxDistance)
            {
                maxDistance = distance;
                indexToMove = i;
            }
        }
        for (int i = 0; i < numLegs; ++i)
            if (i != indexToMove)
                legTargets[i].position = lastLegPositions[i];

        if (indexToMove != -1 && !isThisLegMoving)
        {
            Vector3 targetPoint = desiredPositions[indexToMove] + Mathf.Clamp(velocity.magnitude, 0.0f, 1.5f) * (desiredPositions[indexToMove] - legTargets[indexToMove].position) + velocity * velocityMultiplier;

            Vector3[] positionAndNormalFwd = MatchToSurfaceBelow(targetPoint, ((raycastRange * 0.5f) * velocityMultiplier), (transform.parent.up - velocity * 100).normalized);
            Vector3[] positionAndNormalBwd = MatchToSurfaceBelow(targetPoint, ((raycastRange * 0.5f) * velocityMultiplier), (transform.parent.up + velocity * 75).normalized);
            
            isAnyLegMoving[0] = true;
            
            if (positionAndNormalFwd[1] == Vector3.zero)
            {
                StartCoroutine(PerformStep(indexToMove, positionAndNormalBwd[0]));
            }
            else
            {
                StartCoroutine(PerformStep(indexToMove, positionAndNormalFwd[0]));
            }
        }
        lastBodyPos = transform.position;

        if (numLegs > 3 && bodyOrientation)
        {
            Vector3 v1 = legTargets[0].position - legTargets[1].position;
            Vector3 v2 = legTargets[2].position - legTargets[3].position;
            Vector3 normal = Vector3.Cross(v1, v2).normalized;
            Vector3 up = Vector3.Lerp(lastBodyUp, normal, 1f / (smoothness + 1));
            transform.up = up;
            transform.rotation = Quaternion.LookRotation(transform.parent.forward, up);
            lastBodyUp = transform.up;
        }
    }
    Vector3[] MatchToSurfaceBelow(Vector3 point, float halfRange, Vector3 up)
    {
        Vector3[] landpos = new Vector3[2];
        landpos[1] = Vector3.zero;
        Ray ray = new Ray(point + halfRange * up / 2f, -up);


        if (Physics.SphereCast(ray, sphereCastRadius, out RaycastHit hit, 2f * halfRange, layerMask))
        {
            landpos[0] = hit.point;
            landpos[1] = hit.normal;
        }
        else
        {
            landpos[0] = point;        }
        return landpos;
    }
    IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        /*if (spiderEnemy.footstepSFX != null)
        {
            spiderEnemy.footstepSFX.Post(this.gameObject);
        }*/

        isThisLegMoving = true;
        Vector3 startPos = lastLegPositions[index];
        for (int i = 1; i <= smoothness; ++i)
        {
            legTargets[index].position = Vector3.Lerp(startPos, targetPoint, i / (smoothness + 1f));
            legTargets[index].position += transform.up * Mathf.Sin(i / (smoothness + 1f) * Mathf.PI) * stepHeight;
            yield return new WaitForFixedUpdate();
        }
        legTargets[index].position = targetPoint;
        lastLegPositions[index] = legTargets[index].position;
        isAnyLegMoving[0] = false;
        isThisLegMoving = false;
    }

    /*public void EndAttack()
    {
        spiderEnemy.SendMessage("EndAttack");
    }*/

    /// <summary>   Calls the ExitFromSpawnstate function from the enemy passed.    </summary>
    /*public void ExitFromSpawnstate()
    {
        spiderEnemy.SendMessage("ExitFromSpawnstate");
    }*/

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        for (int i = 0; i < numLegs; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.05f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]), stepSize);
        }
    }
#endif
}
