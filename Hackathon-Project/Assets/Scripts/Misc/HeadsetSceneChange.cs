using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HeadsetSceneChange : MonoBehaviour
{
    [SerializeField] private int robotSceneId;
    
    private XRGrabInteractable _grabInteractable;
    private bool _triggered = false;

    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !_triggered)
        {
            _triggered = true;
            SceneManager.LoadScene(robotSceneId);
        }
    }
    
}
