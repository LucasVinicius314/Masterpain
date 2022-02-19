using Mirror;
using System.Collections.Generic;
using UnityEngine;

public class MinionScript : NetworkBehaviour
{
  Rigidbody rb;
  Transform target;
  GameObject owner;
  List<GameObject> aggro = new List<GameObject>();

  void Start()
  {
    rb = GetComponent<Rigidbody>();
  }

  void Update()
  {
    if (aggro.Count == 0)
    {
      rb.velocity = Vector3.zero;
      rb.angularVelocity = Vector3.zero;

      var distanceToTarget = (transform.position - target.position).magnitude;

      transform.LookAt(target);

      transform.position = Vector3.Lerp(transform.position, target.position, 10 * Time.deltaTime);
    }
    else
    {
      var enemyTarget = aggro[0];

      var direction = (enemyTarget.transform.position + Vector3.up) - transform.position;

      direction.Normalize();

      var rotationAmount = Vector3.Cross(transform.forward, direction);

      rb.angularVelocity = rotationAmount * 1000 * 3 * 3 * Time.deltaTime;

      rb.velocity = transform.forward * 1000 * 3 * Time.deltaTime;

      Debug.DrawLine(transform.position, enemyTarget.transform.position, Color.red);
    }
  }

  public void SetOwner(GameObject player)
  {
    owner = player;
  }

  public void SetTarget(Transform targetTransform)
  {
    target = targetTransform;
  }

  public void SetAggro(List<GameObject> aggroParam)
  {
    aggro = aggroParam;
  }
}
