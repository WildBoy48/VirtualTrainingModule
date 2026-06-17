using UnityEngine;
using Autohand;

public class AnimationController : MonoBehaviour
{
    private Animator animator;

    [Header("Auto Hand")]
    [SerializeField] private Hand hand;          // Assign the hand in Inspector
    [SerializeField] private Grabbable cupGrab;  // Assign the cup's Grabbable component
    [SerializeField] private GameObject cup;     // Assign the cup GameObject in Inspector
    [SerializeField] private GameObject handObject;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

     public void GrabCup()
    {
        cup.transform.SetParent(handObject.transform);
        cup.transform.localPosition = new Vector3(-0.035f, -0.0345f, 0.0255f);
        cup.transform.localRotation = Quaternion.identity;
        hand.CloseHand();
        hand.CreateGrabConnection(cupGrab, true);
    }

    public void ReleaseCup()
    {
        cup.transform.SetParent(null);
        hand.Release();
        hand.OpenHand();
    }
}