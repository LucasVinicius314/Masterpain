using UnityEngine;

public class TargetScript : MonoBehaviour
{
  int seed = 0;

  void Start()
  {
    seed = Random.Range(0, 359);
  }

  void Update()
  {
    var position = transform.transform.position;

    transform.transform.position = new Vector3(position.x, Mathf.Sin(((Time.frameCount + seed) % 360) * Mathf.Deg2Rad) * .2f + 1.6f, position.z);
  }
}
