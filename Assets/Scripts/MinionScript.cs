using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinionScript : MonoBehaviour
{

    Transform target;
    GameObject owner;
    bool onPlayer = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!onPlayer)
        {
            transform.LookAt(owner.transform);
        }
        if ((transform.position - owner.transform.position).magnitude > 1.5f)
        {
            transform.Translate(Vector3.forward * 10 * Time.deltaTime);
        }
        else
        {
            transform.RotateAround(owner.transform.position, Vector3.up, 100 * Time.deltaTime);
        }
    }

    public void SetOwner(GameObject player)
    {
        owner = player;
    }
}
