using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace IdleRPG.EditorTools
{
    /// <summary>
    /// Builds sprite AnimationClips and an AnimatorController per character from the sliced frame strips of a
    /// third-party pack, so a battle unit is a drop-in rather than hand-keyed in the Animation window.
    ///
    /// Per character it produces:
    ///   <Character>_Idle / _Walk / _Attack01 / _Attack02 / _Attack03 / _Hurt / _Death clips (when the strip exists)
    ///   <Character>.controller with: Moving (bool), Attack (trigger), AttackVariant (int), Hurt (trigger), Dead (trigger)
    ///
    /// The controller is the part an idle battle needs: it can sit in Idle forever, play one of several attacks, react to
    /// a hit, and hold a death pose - all driven by parameters, so the sim can drive it later without knowing any clips.
    ///
    /// Frame rate is a constant here because these packs are authored at a fixed rate; 10 fps matches this pack.
    ///
    /// Menu: Tools > Idle RPG > Art > Build Character Animation Sets
    /// </summary>
    public static class CharacterAnimationBuilder
    {
        private const float FrameRate = 10f;
        private const string PackRoot = "Assets/ThirdParty";   // any pack; animation strips are <Character>_<Clip>_anim.png
        private const string OutputRoot = "Assets/IdleRPG/Art/Animations";

        private readonly struct Strip
        {
            public readonly string Character;
            public readonly string Clip;
            public readonly string TexturePath;
            public readonly bool Loop;
            public readonly bool HoldLastFrame;

            public Strip(string character, string clip, string texturePath, bool loop, bool holdLastFrame)
            {
                Character = character;
                Clip = clip;
                TexturePath = texturePath;
                Loop = loop;
                HoldLastFrame = holdLastFrame;
            }
        }

        [MenuItem("Tools/Idle RPG/Art/Build Character Animation Sets", priority = 21)]
        public static void BuildAll()
        {
            var byCharacter = new Dictionary<string, Dictionary<string, AnimationClip>>();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { PackRoot });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);

                // Contract: animation source strips are named <Character>_<Clip>_anim.png (e.g. Goblin_Idle_anim.png).
                if (!file.EndsWith("_anim", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string baseName = file.Substring(0, file.Length - "_anim".Length);
                int sep = baseName.IndexOf('_');

                if (sep <= 0)
                {
                    Debug.LogWarning($"[CharacterAnimationBuilder] '{file}' is not '<Character>_<Clip>_anim'; skipped.");
                    continue;
                }

                string character = baseName.Substring(0, sep);
                string clip = baseName.Substring(sep + 1);
                string stateKey = StateKey(clip);

                if (CollectSprites(path).Length < 2)
                {
                    Debug.LogWarning($"[CharacterAnimationBuilder] '{file}' has fewer than 2 frames; skipped.");
                    continue;
                }

                string outputFolder = OutputRoot + "/" + character;
                EnsureFolder(OutputRoot);
                EnsureFolder(outputFolder);

                bool loop = stateKey == "Idle" || stateKey == "Walk";
                AnimationClip built = BuildClip(new Strip(character, clip, path, loop, !loop), outputFolder);

                if (built == null)
                {
                    continue;
                }

                if (!byCharacter.TryGetValue(character, out Dictionary<string, AnimationClip> clips))
                {
                    clips = new Dictionary<string, AnimationClip>();
                    byCharacter[character] = clips;
                }

                clips[stateKey] = built;
            }

            foreach (KeyValuePair<string, Dictionary<string, AnimationClip>> pair in byCharacter)
            {
                AnimatorController controller = BuildController(pair.Key, pair.Value, OutputRoot + "/" + pair.Key);
                Debug.Log($"[CharacterAnimationBuilder] {pair.Key}: {pair.Value.Count} clip(s), controller '{controller.name}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>Maps source clip names onto the shared controller state keys (Attack -> Attack01, etc.).</summary>
        private static string StateKey(string clip)
        {
            switch (clip.ToLowerInvariant())
            {
                case "idle":    return "Idle";
                case "walk":    return "Walk";
                case "attack":  return "Attack01";
                case "attack2": return "Attack02";
                case "attack3": return "Attack03";
                case "hurt":    return "Hurt";
                case "death":   return "Death";
                default:        return clip;
            }
        }

        private static AnimationClip BuildClip(Strip strip, string outputFolder)
        {
            Sprite[] sprites = CollectSprites(strip.TexturePath);

            if (sprites.Length == 0)
            {
                Debug.LogWarning($"[CharacterAnimationBuilder] no sprites found in '{strip.TexturePath}'; skipped.");
                return null;
            }

            var clip = new AnimationClip
            {
                frameRate = FrameRate,
                wrapMode = strip.Loop ? WrapMode.Loop : WrapMode.Clamp,
            };

            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new List<ObjectReferenceKeyframe>();

            for (int i = 0; i < sprites.Length; i++)
            {
                keys.Add(new ObjectReferenceKeyframe { time = i / FrameRate, value = sprites[i] });
            }

            if (strip.HoldLastFrame)
            {
                // One extra key so a one-shot clip (attack / hurt / death) holds its final pose instead of snapping back.
                keys.Add(new ObjectReferenceKeyframe
                {
                    time = (sprites.Length / FrameRate) + 0.1f,
                    value = sprites[sprites.Length - 1],
                });
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = strip.Loop;
            settings.loopBlend = false;
            settings.stopTime = keys[keys.Count - 1].time + (1f / FrameRate);
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            // CreateAsset over an existing path keeps the asset's GUID, so scene references survive a rebuild.
            AssetDatabase.CreateAsset(clip, outputFolder + "/" + strip.Character + "_" + strip.Clip + ".anim");
            return clip;
        }

        /// <summary>Frames of a strip, left to right, independent of the order the importer hands them over.</summary>
        private static Sprite[] CollectSprites(string texturePath)
        {
            var found = new List<Sprite>();

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(texturePath))
            {
                if (asset is Sprite sprite)
                {
                    found.Add(sprite);
                }
            }

            found.Sort((a, b) => a.rect.x.CompareTo(b.rect.x));
            return found.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
            }
        }

        /// <summary>
        /// The state set an idle battle needs: sits in Idle, plays one of several attacks, reacts to a hit, holds death.
        /// Everything is driven by parameters, so the sim can drive it later without knowing any clip names.
        /// </summary>
        private static AnimatorController BuildController(string character, Dictionary<string, AnimationClip> clips, string outputFolder)
        {
            string path = outputFolder + "/" + character + ".controller";

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackVariant", AnimatorControllerParameterType.Int);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;

            AnimatorState idle = AddState(machine, "Idle", clips, "Idle");
            AnimatorState walk = AddState(machine, "Walk", clips, "Walk");
            AnimatorState attack01 = AddState(machine, "Attack01", clips, "Attack01");
            AnimatorState attack02 = AddState(machine, "Attack02", clips, "Attack02");
            AnimatorState attack03 = AddState(machine, "Attack03", clips, "Attack03");
            AnimatorState hurt = AddState(machine, "Hurt", clips, "Hurt");
            AnimatorState death = AddState(machine, "Death", clips, "Death");

            machine.defaultState = idle;

            if (clips.ContainsKey("Walk"))
            {
                Link(idle, walk, "Moving", AnimatorConditionMode.If, 0f);
                Link(walk, idle, "Moving", AnimatorConditionMode.IfNot, 0f);
            }

            LinkAttack(idle, attack01, 0);
            LinkAttack(idle, attack02, 1);
            LinkAttack(idle, attack03, 2);
            ReturnOnExit(attack01, idle);
            ReturnOnExit(attack02, idle);
            ReturnOnExit(attack03, idle);

            Link(idle, hurt, "Hurt", AnimatorConditionMode.If, 0f);
            ReturnOnExit(hurt, idle);

            // Death can interrupt anything and never transitions out: the unit holds the final pose.
            AnimatorStateTransition toDeath = machine.AddAnyStateTransition(death);
            toDeath.hasExitTime = false;
            toDeath.duration = 0f;
            toDeath.canTransitionToSelf = false;
            toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

            return controller;
        }

        private static AnimatorState AddState(AnimatorStateMachine machine, string name, Dictionary<string, AnimationClip> clips, string clipKey)
        {
            AnimatorState state = machine.AddState(name);
            state.writeDefaultValues = false;

            if (clips.TryGetValue(clipKey, out AnimationClip clip))
            {
                state.motion = clip;
            }

            return state;
        }

        private static void Link(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(mode, threshold, parameter);
        }

        private static void LinkAttack(AnimatorState idle, AnimatorState attack, int variant)
        {
            if (attack.motion == null)
            {
                return;
            }

            AnimatorStateTransition transition = idle.AddTransition(attack);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0f, "Attack");
            transition.AddCondition(AnimatorConditionMode.Equals, variant, "AttackVariant");
        }

        private static void ReturnOnExit(AnimatorState from, AnimatorState to)
        {
            if (from.motion == null)
            {
                return;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
        }

    }
}
