using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;
using Random = UnityEngine.Random;

[SerializeField]
class CardList
{
    public Card[] Cards;
}

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;

    public AudioClip drawSound;
    public AudioClip throwSound;
    public AudioClip clickSound;

    [Header("카드 목록들")]
    public List<Card> cardList = new List<Card>();  //전체카드
    public List<CardObj> selectCardList = new List<CardObj>(); //선택한 카드들

    public bool isCardDrag = false;

    [Header("카드 덱")]
    public GameObject cardPrefab;
    public Transform cardSpawnPosition;

    public Transform player1CardLeft;
    public Transform player1CardRight;
    public Transform player2CardLeft;
    public Transform player2CardRight;
    private bool _onCardArea;

    [Header("점수")]
    public int pareScore;
    public int threepleScore;
    public int seasonScore;
    public int forCardScore;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameManager.OnAddCard += AddCard;
    }

    private void Update()
    {
        if(isCardDrag)
        {
            CardDrag(); 
        }

        DetectCardArea();
    }

    private void OnDestroy()
    {
        GameManager.OnAddCard -= AddCard;
    }

    public Card PopCard()
    {
        Card card = cardList[0];
        cardList.Remove(card);
        return card;
    }

    public void AddCard(Player player)
    {
        SoundManager.Instance.PlayEffectSound(drawSound);
        GameManager.Instance.isLoading = true;
        var cardObj = PoolManager.Instance.Pop("Card");
        cardObj.transform.position = cardSpawnPosition.position;
        cardObj.transform.rotation = Quaternion.identity;
        cardObj.transform.SetParent(player.cardListTrm);
        if (player == GameManager.Instance.player2) cardObj.transform.DOLocalRotate(new Vector3(0, 0, 180), 0);
        CardObj card = cardObj.GetComponent<CardObj>();
        card.Setup(PopCard());
        player.cards.Add(card);

        UIManager.Instance.CardCntUpdate();

        SetOriginOrder(player);
        CardAlignment(player);

    }

    private void CardAlignment(Player player)
    {
        List<PRS> originPRSs = new List<PRS>();
        if(player == GameManager.Instance.player1)
        {
            originPRSs = RoundAligment(player1CardLeft, player1CardRight, player.cards.Count, 0.5f, Vector3.one * 0.5f);
        }
        else
        {
            originPRSs = RoundAligment(player2CardLeft, player2CardRight, player.cards.Count, -0.5f, Vector3.one * 0.5f);
        }

        for (int i = 0; i < player.cards.Count; i++)
        {
            var targetCard = player.cards[i];
            targetCard.originPRS = originPRSs[i];
            targetCard.MoveTrm(targetCard.originPRS, true, .7f);
        }
        if(GameManager.Instance.isGameStart == false)
        {
            StartCoroutine(NextTurnCo());
        }
    }

    private IEnumerator NextTurnCo()
    {
        yield return new WaitForSeconds(0.8f);
        StartCoroutine(GameManager.Instance.NextTurnCoroutine());
    }

    private List<PRS> RoundAligment(Transform leftTrm, Transform rightTrm, int count, float height, Vector3 scale)
    {
        float[] objLerps = new float[count];
        List<PRS> results = new List<PRS>(count);

        if(height < 0)
        {
            switch (count)
            {
                case 1: objLerps = new float[] { 0.5f }; break;
                case 2: objLerps = new float[] { 0.27f, 0.73f }; break;
                case 3: objLerps = new float[] { 0.1f, 0.5f, 0.9f }; break;
                default:
                    float interval = 1f / (count - 1f);
                    for (int i = 0; i < count; i++)
                        objLerps[i] = interval * i;
                    break;
            }
        }
        else
        {
            switch (count)
            {
                case 1: objLerps = new float[] { 0.5f }; break;
                case 2: objLerps = new float[] { 0.73f, 0.27f}; break;
                case 3: objLerps = new float[] { 0.9f, 0.5f, 0.1f }; break;
                default:
                    float interval = 1f / (count - 1f);
                    for (int i = count - 1, j = 0; i >= 0; i--, j++)
                        objLerps[j] = interval * i;
                    break;
            }
        }
        

        for(int i = 0; i < count; i++)
        {
            var targetPos = Vector3.Lerp(leftTrm.position, rightTrm.position, objLerps[i]);
            if (count >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(height, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));
                curve = height >= 0 ? curve : -curve;
                targetPos.y = curve;
                targetPos.y -= height >= 0 ? 3.5f : -3.5f;
            }
            var targetRot = Quaternion.Slerp(leftTrm.rotation, rightTrm.rotation, count >= 4 ? objLerps[i] : 0.5f);

            results.Add(new PRS(targetPos, targetRot, scale));
        }
        return results;
    }

    public void SetOriginOrder(Player player)
    {
        int cnt = player.cards.Count;
        for (int i = 0; i < cnt; i++)
        {
            var targetCard = player.cards[i];
            targetCard.GetComponent<Order>().SetOriginOrder(i);
        }
    }

    public void Shuffle()
    {
        int idx1, idx2;
        Card tempCard;

        //100번 정도 섞음
        for (int i = 0; i < 100; i++)
        {
            idx1 = Random.Range(0, 47);
            idx2 = Random.Range(0, 47);
            tempCard = cardList[idx1];
            cardList[idx1] = cardList[idx2];
            cardList[idx2] = tempCard;
        }
    }


    #region 카드 확대

    public void CardMouseOver(CardObj card)
    {
        EnlargeCard(true, card, GameManager.Instance.TurnPlayer == GameManager.Instance.player1);
    }

    public void CardMouseExit(CardObj card)
    {
        EnlargeCard(false, card, GameManager.Instance.TurnPlayer == GameManager.Instance.player1);
    }

    private void EnlargeCard(bool isEnlarge, CardObj card, bool isPlayer1)
    {
        if(!GameManager.Instance.isLoading && card.spriteRenderer.sprite != UIManager.Instance.backCardSprite)
        {
            if (isEnlarge)
            {
                Vector3 enlargePos = new Vector3(card.originPRS.pos.x, (isPlayer1 ? -3f : 3f), -10f);
                card.MoveTrm(new PRS(enlargePos, card.originPRS.rot, Vector3.one * 0.7f), false);
            }
            else
            {
                card.MoveTrm(card.originPRS, false);
            }

            card.GetComponent<Order>().SetMostFrontOrder(isEnlarge);
        }
    }

    public void CardClick(CardObj card)
    {
        if(GameManager.Instance.isLoading == false)
        {
            if(card.spriteRenderer.sprite != UIManager.Instance.backCardSprite)
        {
            SoundManager.Instance.PlayEffectSound(clickSound);
            isCardDrag = true;
            if (card.IsSelected == false)
            {
                selectCardList.Add(card);
            }
            else
            {
                selectCardList.Remove(card);
            }
            card.IsSelected = !card.IsSelected;
        }
        }
        
    }

    public void CardMouseUp()
    {
        if(isCardDrag == true)
        {
            isCardDrag = false;
            if (!_onCardArea)
            {
                SoundManager.Instance.PlayEffectSound(throwSound);
                Star star = selectCardList[0].card.star;
                WeatherID weather = selectCardList[0].card.weather.weatherId;
                string zodiac = selectCardList[0].card.zodiac;

                int starCnt = 0;
                int weatherCnt = 0;
                int zodiacCnt = 0;
                int nothingCnt = 0;
                
                foreach(CardObj card in selectCardList)
                {
                    if (zodiac == card.card.zodiac) zodiacCnt++;
                    if (weather == card.card.weather.weatherId) weatherCnt++;
                    if (star == card.card.star) starCnt++;

                    //if (card.card.zodiac == "cheonyeo") GameManager.Instance.TurnPlayer.cheonyeoCnt++;
                }

                if (nothingCnt == 0)
                {
                    if (zodiacCnt == 4 && selectCardList.Count == 4) CardThrowSuccess(forCardScore);
                    else if (weatherCnt == 5 && selectCardList.Count == 5) CardThrowSuccess(seasonScore);
                    else if (starCnt == 3 && selectCardList.Count == 3) CardThrowSuccess(threepleScore);
                    else if (starCnt == 2 && selectCardList.Count == 2) CardThrowSuccess(pareScore);

                    //if (GameManager.Instance.TurnPlayer.cheonyeoCnt == 4) GameManager.Instance.TurnPlayer.score -= 5;
                }
            }
        }
    }

    public void CardThrowSuccess(int score)
    {
        GameManager.Instance.TurnPlayer.score += score;
        UIManager.Instance.ScoreUpdate();

        foreach (CardObj card in selectCardList)
        {
            card.transform.position = Utills.MousePos;
            GameManager.Instance.TurnPlayer.cards.Remove(card);
            card.Destroy();
        }
        selectCardList.Clear();
        CardAlignment(GameManager.Instance.TurnPlayer);
    }

    public void CardDrag()
    {
        if(!_onCardArea)
        {
            foreach(CardObj card in selectCardList)
            {
                card.MoveTrm(new PRS(Utills.MousePos, Utills.QI, card.originPRS.scale), false);
            }
        }
    }

    private void DetectCardArea()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(Utills.MousePos, Vector3.forward);
        int layer = LayerMask.NameToLayer("CardArea");
        _onCardArea = Array.Exists(hits, x => x.collider.gameObject.layer == layer);
    }

    #endregion
}
