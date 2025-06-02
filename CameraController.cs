using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform Hero;
    private Vector3 pos;

    private void Awake()
    {
        if (!Hero)
        {
            Hero = FindObjectOfType<Hero>().transform;
        }
    }
    private void Update()
    {
        pos = Hero.position;
        pos.z = -10f;
        pos.y += 1f;

        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime);
    }




}