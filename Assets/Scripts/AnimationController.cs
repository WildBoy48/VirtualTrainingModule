using UnityEngine;
using Autohand;

public class AnimationController : MonoBehaviour
{
    [SerializeField] private GameObject handObject;
    [SerializeField] private GameObject cupObject;
    [SerializeField] private ScoreManager scoreManager;

    private Animator handAnimator;
    private Hand hand;
    private Grabbable cupGrab;

    void Start()
    {
        handAnimator = handObject.GetComponent<Animator>();
        hand = handObject.GetComponent<Hand>();
        cupGrab = cupObject.GetComponent<Grabbable>();
    }

     public void GrabCup()
    {
        cupObject.transform.SetParent(handObject.transform);
        cupObject.transform.localPosition = new Vector3(-0.035f, -0.0345f, 0.0255f);
        cupObject.transform.localRotation = Quaternion.identity;
        hand.GrabPos(0.48f);
    }

    public void ReleaseCup()
    {
        hand.Release();
        hand.BreakGrabConnection();
        cupObject.transform.SetParent(null);
        hand.OpenHand();
    }

    public void AddScore(int amount)
    {
        scoreManager.AddScore(amount);
    }
}