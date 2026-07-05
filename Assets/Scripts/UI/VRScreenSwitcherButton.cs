using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class VRScreenSwitcherButton : MonoBehaviour
{
    public GameObject screenToShow;
    public GameObject screenToHide;
    public string screenToShowName = "ExperimentSelectionScreen";
    public string screenToHideName = "VR_REAL_3D_MENU";

    public bool switchOnSelectExit = true;

    UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    void OnEnable()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (!interactable)
            interactable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        if (switchOnSelectExit)
            interactable.selectExited.AddListener(OnSelectExited);
        else
            interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDisable()
    {
        if (!interactable) return;
        interactable.selectExited.RemoveListener(OnSelectExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
    }

    void OnSelectEntered(SelectEnterEventArgs args) => SwitchScreens();
    void OnSelectExited(SelectExitEventArgs args) => SwitchScreens();

public void SwitchScreens()
    {
        if (!screenToShow && !string.IsNullOrEmpty(screenToShowName))
            screenToShow = FindByNameIncludingInactive(screenToShowName);
        if (!screenToHide && !string.IsNullOrEmpty(screenToHideName))
            screenToHide = FindByNameIncludingInactive(screenToHideName);

        if (screenToHide) screenToHide.SetActive(false);
        if (screenToShow) screenToShow.SetActive(true);
    }

    GameObject FindByNameIncludingInactive(string objectName)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var go in all)
        {
            if (go.name == objectName && go.scene.IsValid())
                return go;
        }
        return null;
    }
}
