#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AnimationSoundSetup
{
    [MenuItem("Tools/Setup Animation Sounds")]
    public static void SetupAllAnimationSounds()
    {
        SetupPlayerNormalAnimations();
        SetupPlayerGlorboAnimations();
        SetupPlayerOniAnimations();
        
        AssetDatabase.SaveAssets();
        Debug.Log("Animation sounds setup complete!");
    }

    private static void SetupPlayerNormalAnimations()
    {
        AnimationClip chargeAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Prefabs/Player/mc_charge_real.anim");
        AnimationClip dashAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Prefabs/Player/mc_dash_real.anim");
        
        if (chargeAnim != null)
        {
            AnimationEvent[] chargeEvents = new AnimationEvent[1];
            chargeEvents[0] = new AnimationEvent
            {
                time = 0f,
                functionName = "PlaySound",
                stringParameter = "PLAYER_CHARGE"
            };
            AnimationUtility.SetAnimationEvents(chargeAnim, chargeEvents);
            EditorUtility.SetDirty(chargeAnim);
            Debug.Log("Setup charge animation (normal)");
        }
        
        if (dashAnim != null)
        {
            AnimationEvent[] dashEvents = new AnimationEvent[1];
            dashEvents[0] = new AnimationEvent
            {
                time = 0f,
                functionName = "PlaySound",
                stringParameter = "PLAYER_ATTACK"
            };
            AnimationUtility.SetAnimationEvents(dashAnim, dashEvents);
            EditorUtility.SetDirty(dashAnim);
            Debug.Log("Setup dash animation (normal)");
        }
    }

    private static void SetupPlayerGlorboAnimations()
    {
        AnimationClip chargeAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Prefabs/Player/mc_glorbo_charge_real.anim");
        AnimationClip dashAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Prefabs/Player/mc_glorbo_dash_real.anim");
        
        if (chargeAnim != null)
        {
            AnimationEvent[] chargeEvents = new AnimationEvent[1];
            chargeEvents[0] = new AnimationEvent
            {
                time = 0f,
                functionName = "PlaySound",
                stringParameter = "PLAYER_CHARGE"
            };
            AnimationUtility.SetAnimationEvents(chargeAnim, chargeEvents);
            EditorUtility.SetDirty(chargeAnim);
            Debug.Log("Setup charge animation (glorbo)");
        }
        
        if (dashAnim != null)
        {
            AnimationEvent[] dashEvents = new AnimationEvent[1];
            dashEvents[0] = new AnimationEvent
            {
                time = 0f,
                functionName = "PlaySound",
                stringParameter = "GLORBO_ATTACK"
            };
            AnimationUtility.SetAnimationEvents(dashAnim, dashEvents);
            EditorUtility.SetDirty(dashAnim);
            Debug.Log("Setup dash animation (glorbo)");
        }
    }

    private static void SetupPlayerOniAnimations()
    {
        AnimationClip chargeAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Prefabs/Player/mc_oni_charge_real.anim");
        AnimationClip dashAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Prefabs/Player/mc_oni_dash_real.anim");
        
        if (chargeAnim != null)
        {
            AnimationEvent[] chargeEvents = new AnimationEvent[1];
            chargeEvents[0] = new AnimationEvent
            {
                time = 0f,
                functionName = "PlaySound",
                stringParameter = "PLAYER_CHARGE"
            };
            AnimationUtility.SetAnimationEvents(chargeAnim, chargeEvents);
            EditorUtility.SetDirty(chargeAnim);
            Debug.Log("Setup charge animation (oni)");
        }
        
        if (dashAnim != null)
        {
            AnimationEvent[] dashEvents = new AnimationEvent[1];
            dashEvents[0] = new AnimationEvent
            {
                time = 0f,
                functionName = "PlaySound",
                stringParameter = "ONI_ATTACK"
            };
            AnimationUtility.SetAnimationEvents(dashAnim, dashEvents);
            EditorUtility.SetDirty(dashAnim);
            Debug.Log("Setup dash animation (oni)");
        }
    }
}
#endif
