using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public GameObject panel;
    }

    public Tab[] tabs;
    private int activeIndex = 0;

    void Start()
    {
        ShowTab(activeIndex);
    }

    public void ShowTab(int index)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            bool isActive = (i == index);
            tabs[i].panel.SetActive(isActive);
        }
        activeIndex = index;
    }
}
