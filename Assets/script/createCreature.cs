using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class createCreature : MonoBehaviour
{
    public GameObject creature;
    private Vector3 pos;
    private Vector3 direction;

    void Start()
    {
        /*for(int i=0; i <10; i++)
        {
            pos.x = Random.Range(60, 90);
            pos.y = Random.Range(0, 10);
            pos.z = Random.Range(-10, 10);

            direction.x = Random.Range(180f, 180f);
            direction.y = Random.Range(180f, 180f);
            direction.z = Random.Range(180f, 180f);

            GameObject creatureA = Instantiate(creature, pos, Quaternion.Euler(direction.x, direction.y, direction.z)); //追尾

            creatureA.tag = "clone";
        }*/
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            pos.x = Random.Range(-5, 5);
            pos.y = Random.Range(5, 8);
            pos.z = Random.Range(-5, 5);

            direction.x = Random.Range(180f, 180f);
            direction.y = Random.Range(180f, 180f);
            direction.z = Random.Range(180f, 180f);

            GameObject creatureA = Instantiate(creature, pos, Quaternion.Euler(direction.x, direction.y, direction.z)); //追尾

            creatureA.tag = "clone";
        }
    }
}
