using UnityEngine;

public class MinionScript : MonoBehaviour
{
  Transform target;
  GameObject owner;

  void Update()
  {
    var distanceToTarget = (transform.position - target.position).magnitude;

    if (distanceToTarget > 1.4 && distanceToTarget <= 1.5)
    {
      // maybe
      // transform.LookAt(owner.transform);
    }
    else
    {
      transform.LookAt(target);
    }

    transform.position = Vector3.Lerp(transform.position, target.position, 10 * Time.deltaTime);
  }

  public void SetOwner(GameObject player)
  {
    owner = player;
  }

  public void SetTarget(Transform targetTransform)
  {
    target = targetTransform;
  }
}
