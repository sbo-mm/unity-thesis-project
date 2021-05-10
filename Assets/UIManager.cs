using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private Button Reset, Pause, Resume;

    private void Start()
    {
        Resume.interactable = false;
    }

    public void OnPause()
    {
        Pause.interactable = false;
        Resume.interactable = true;
    }

    public void OnResume()
    {
        Resume.interactable = false;
        Pause.interactable = true;
    }

}
