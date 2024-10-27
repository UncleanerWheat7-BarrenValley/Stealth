using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CodecFill : MonoBehaviour
{
    [SerializeField]
    Image barFillImage;
    int randomNum;
    float[] barFill = { 1f, 0.90265f, 0.8053f, 0.70795f, 0.6106f, 0.51325f, 0.4159f, 0.31855f, 0.2212f, 0.12382f, 0.06f };


    private void Start()
    {
        barFillImage.fillAmount = 0;
        StartCoroutine(SignalFill());
    }   
    

    IEnumerator SignalFill()
    {

        while (barFillImage.fillAmount < 1.0f)
        {
            barFillImage.fillAmount += 0.1f;
            yield return new WaitForSeconds(0.09735f);
        }
        yield return SignalRand();
        
    }

    IEnumerator SignalRand()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            randomNum = Random.Range(0, 11);
            barFillImage.fillAmount = barFill[randomNum];
        }
    }
}
