﻿using UnityEngine;
using System.Collections;

public class CharacterCollisionHandler : MonoBehaviour
{
    CharacerStats cs;
    void Start()
    {
        cs = GetComponent<CharacerStats>();

    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.tag == "Bullet")
        {
            cs.StartDeathRoutine();
            Camera.main.GetComponent<SoundManager>().enemyHit();
            BackgroundManager.singleton.Hit(other.transform.position);
            Destroy(other.gameObject);
        }
        else if(other.tag == "Laser")
        {
            Camera.main.GetComponent<SoundManager>().enemyHit();
            BackgroundManager.singleton.Hit(transform.position);
            cs.StartDeathRoutine();
        }
    }
}
