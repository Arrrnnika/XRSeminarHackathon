using UnityEngine;

public class ScrollingWater : MonoBehaviour
{
    [SerializeField] private Vector2 speed = new Vector2(0.01f, 0.005f);

    private Material _material;

    private void Start()
    {
        _material = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        _material.SetTextureOffset("_BumpMap", speed * Time.time);
    }
}