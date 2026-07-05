using UnityEngine;

public class VRExperimentSelectionController : MonoBehaviour
{
    [Header("Screen refs")]
    public GameObject mainMenuRoot;
    public GameObject selectionScreenRoot;

    [Header("Selection")]
    public int selectedExperiment = 0;
    public VRExperiment3DButton[] experimentCards;

    public void SelectExperiment(int index)
    {
        selectedExperiment = index;
        if (experimentCards == null) return;
        for (int i = 0; i < experimentCards.Length; i++)
            if (experimentCards[i]) experimentCards[i].SetSelected(i == index);
    }

    public void ShowSelection()
    {
        if (mainMenuRoot) mainMenuRoot.SetActive(false);
        if (selectionScreenRoot) selectionScreenRoot.SetActive(true);
        SelectExperiment(selectedExperiment);
    }

    public void BackToMainMenu()
    {
        if (selectionScreenRoot) selectionScreenRoot.SetActive(false);
        if (mainMenuRoot) mainMenuRoot.SetActive(true);
    }

    public void StartSelectedExperiment()
    {
        Debug.Log("Starting selected experiment index: " + selectedExperiment);
        // Hook real experiment loading here. 0 = Titulación ácido-base, 1 = Mezcla con cambio de color.
    }
}
