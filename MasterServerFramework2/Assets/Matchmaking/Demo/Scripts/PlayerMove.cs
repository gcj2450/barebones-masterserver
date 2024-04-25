using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerMove : NetworkBehaviour
{

    private float speed = 5f;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isLocalPlayer)
        {
            UnityEngine.Random.InitState((int)(System.DateTime.Now.Ticks % 10000));

            transform.position = new Vector3(Random.Range(-20, 20), 0, Random.Range(-20, 20));
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            transform.localPosition += transform.TransformDirection(Vector3.forward) * speed * Time.deltaTime;
            transform.Rotate(new Vector3(0, speed, 0) * 15f * Time.deltaTime);
        }
    }
}
