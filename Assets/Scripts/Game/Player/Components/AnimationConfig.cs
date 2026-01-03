using UnityEngine;

/// <summary>
/// 动画配置数据
/// 存储动画参数名称和触发器名称
/// </summary>
[System.Serializable]
public class AnimationConfig
{
    [Header("动画参数名称")]
    public string isGroundedParam = "IsGrounded";
    public string comboIndexParam = "ComboIndex";
    public string skillStateParam = "IsInSkill";
    public string speedParam = "Speed";

    // 缓存的哈希值
    [System.NonSerialized] public int isGroundedHash;
    [System.NonSerialized] public int comboIndexHash;
    [System.NonSerialized] public int skillStateHash;
    [System.NonSerialized] public int speedHash;

    /// <summary>
    /// 初始化哈希值
    /// </summary>
    public void InitializeHashes()
    {
        isGroundedHash = Animator.StringToHash(isGroundedParam);
        comboIndexHash = Animator.StringToHash(comboIndexParam);
        skillStateHash = Animator.StringToHash(skillStateParam);
        speedHash = Animator.StringToHash(speedParam);
    }
}

