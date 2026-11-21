using UnityEngine;

/// <summary>
/// 主菜单的音量设置入口：首次点击时创建音量面板，之后复用同一实例。
/// </summary>
public class VolumeSettingsLauncher : MonoBehaviour
{
    /// <summary>面板底图。</summary>
    public Sprite panelSprite;
    /// <summary>按钮底图。</summary>
    public Sprite buttonSprite;
    /// <summary>已创建的音量面板。</summary>
    private VolumeSettingsPanel panel;

    /// <summary>打开音量设置面板。</summary>
    public void Open()
    {
        if (panel == null) panel = VolumeSettingsPanel.Create(transform, panelSprite, buttonSprite);
        panel.Open();
    }
}
