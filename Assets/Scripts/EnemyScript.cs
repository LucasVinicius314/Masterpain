using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

#nullable enable

public class EnemyScript : MonoBehaviour
{
  Text? text;
  Transform? target;
  NavMeshAgent? navMeshAgent;
  Transform? healthBarTransform;

  #region Stats

  float maxHealthPoints = 200;
  float healthPoints = 0;

  #endregion

  void Start()
  {
    healthPoints = maxHealthPoints;

    navMeshAgent = GetComponent<NavMeshAgent>();

    healthBarTransform = transform.Find("Health Bar");

    var canvasTransform = (RectTransform)healthBarTransform.Find("Rotation").GetChild(0);

    text = canvasTransform.Find("Text").GetComponent<Text>();

    StartCoroutine(SetTargetRoutine());

    UpdateHealthBar();
  }

  void OnDestroy()
  {
    StopAllCoroutines();
  }

  void Update()
  {
    if (target != null)
    {
      navMeshAgent?.SetDestination(target.position);

      healthBarTransform?.LookAt(target.GetComponentInChildren<Camera>().transform);
    }
  }

  void UpdateHealthBar()
  {
    if (text != null)
      text.text = $"{healthPoints} / {maxHealthPoints} HP";
  }

  void CalculateTarget()
  {
    target = GameObject.FindGameObjectWithTag("Player")?.transform;
  }

  IEnumerator SetTargetRoutine()
  {
    while (true)
    {
      CalculateTarget();

      yield return new WaitForSeconds(2);
    }
  }
}
