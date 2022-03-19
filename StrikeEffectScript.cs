using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StrikeEffectScript : MonoBehaviour
{
    SpriteRenderer sr;
    public Sprite[] allSprites;
    int currSprite;
    float countdown;

    // Start is called before the first frame update
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (countdown <= 0)
        {
            countdown = Random.value / 5f;
            transform.localPosition = new Vector2(0 + Random.Range(-0.07f, 0.07f), -0.14f + Random.Range(-0.07f, 0.07f));
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Random.Range(0,360f));
            sr.sprite = allSprites[currSprite];
            currSprite = (int)Random.Range(0, allSprites.Length);
        }
        else
            countdown -= Time.deltaTime;
    }
}
