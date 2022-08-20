using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class Player
{
    public int num;
    public List<CardObj> cards = new List<CardObj>();

    public bool _isFlip = false;

    /// <summary>
    /// ī�� ������ (���� �ٲ�� ī�尡 �����������Ѵ�.)
    /// </summary>
    public IEnumerator CardFlipCoroutine(bool isSkip = false)
    {
        _isFlip = !_isFlip;

        float time = 0.05f;
        if (isSkip == true) time = 0.01f;

        foreach(CardObj card in cards)
        {
            card.transform.DOScaleY(card.transform.localScale.y * -1f, time);
            yield return new WaitForSeconds(time);
            card.spriteRenderer.sprite = _isFlip == true ? UIManager.Instance.backCardSprite : UIManager.Instance.cardSprite[card.card.spriteNum];
        }
    }

    public float GetFlipTime()
    {
        return cards.Count * 0.05f + 0.1f;
    }
}