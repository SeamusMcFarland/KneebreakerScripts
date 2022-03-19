using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkScript : MonoBehaviour
{
    SpriteRenderer sr;
    public Sprite[] allSpr;
    float timePassed;
    const float ANIMATION_DELAY = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        timePassed += Time.deltaTime;
        if (timePassed >= ANIMATION_DELAY)
        {
            timePassed = 0;
            sr.sprite = allSpr[(int)Random.Range(0, allSpr.Length)];
        }
    }
}
