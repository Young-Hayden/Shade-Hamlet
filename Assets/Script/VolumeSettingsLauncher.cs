using UnityEngine;

public class VolumeSettingsLauncher : MonoBehaviour
{
    public Sprite panelSprite;
    public Sprite buttonSprite;
    private VolumeSettingsPanel panel;

    public void Open()
    {
        if (panel == null) panel = VolumeSettingsPanel.Create(transform, panelSprite, buttonSprite);
        panel.Open();
    }
}
