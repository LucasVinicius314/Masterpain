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

  float maxHealthPoints = 2000;
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
    if (target != null)
      target.GetComponent<PlayerScript>().RemoveAggro(gameObject);

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
    var player = GameObject.FindGameObjectWithTag("Player")?.transform;

    if (player == null) return;

    var playerScript = player.GetComponent<PlayerScript>();

    playerScript.AddAggro(gameObject);

    target = player;
  }

  IEnumerator SetTargetRoutine()
  {
    while (true)
    {
      CalculateTarget();

      yield return new WaitForSeconds(2);
    }
  }

  void OnTriggerEnter(Collider other)
  {
    TakeDamage(40);
  }

  void TakeDamage(float value)
  {
    healthPoints -= value;

    if (healthPoints <= 0)
    {
      Destroy(gameObject);

      return;
    }

    UpdateHealthBar();
  }
}
