using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class wallCollisionDetector : MonoBehaviour
{
    // 壁に当たった際の処理
    void OnTriggerEnter(Collider other)
    {
        GameObject body = other.gameObject;
        avoidWallMoveCreature tmp = body.GetComponent<avoidWallMoveCreature>();
        tmp.collisionDetect = true;
        body.tag = "corpse";
    }
}
