using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace AoP.Editor
{
    [ScriptedImporter(1, "an")]
    public sealed class FileAnImporter : ScriptedImporter
    {

        [SerializeField] private string fileAniPath = null;
        [SerializeField] private string eventFunctionName = "OnAnimationEvent";

        private readonly SystemNumericsMapper mapper = new();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            FileAn fileAn = new(ctx.assetPath);
            GameObject mainGameObject = CreateMainGameObject(ctx, fileAn);
            ctx.AddObjectToAsset("main", mainGameObject);
            ctx.SetMainObject(mainGameObject);
        }

        private GameObject CreateMainGameObject(AssetImportContext ctx, FileAn fileAn)
        {
            GameObject mainGameObject = new(fileAn.nameWithoutExtension);
            GameObject[] bones = CreateBones(fileAn, mainGameObject.transform);
            Avatar avatar = CreateAvatar(mainGameObject, string.Empty);
            ctx.AddObjectToAsset("avatar", avatar);
            CreateAnimator(mainGameObject, avatar);
            AnimationClip[] animationClips = CreateAnimationClips(fileAn, bones);
            if (animationClips != null && animationClips.Length > 0)
            {
                foreach (var clip in animationClips)
                {
                    ctx.AddObjectToAsset("clip_" + clip.name, clip);
                }
            }
            return mainGameObject;
        }

        private GameObject[] CreateBones(FileAn fileAn, Transform parent)
        {
            GameObject bonesRoot = new("bones");
            Transform bonesRootTransform = bonesRoot.transform;
            bonesRootTransform.parent = parent;
            GameObject[] bones = new GameObject[fileAn.headerData.bonesCount];
            for (int i = 0; i < bones.Length; ++i)
            {
                bones[i] = new GameObject("bone_" + i.ToString("D2"));
            }
            for (int i = 0; i < bones.Length; ++i)
            {
                Transform boneTransform = bones[i].transform;
                int parentIndex = fileAn.bonesData.parentIndices[i];
                if (parentIndex >= 0 && parentIndex < bones.Length)
                {
                    boneTransform.parent = bones[parentIndex].transform;
                }
                else
                {
                    boneTransform.parent = bonesRootTransform;
                }
                Vector3 localPosition = mapper.ToUnity(fileAn.bonesData.startPositions[i]);
                localPosition.x *= -1;
                Quaternion localRotation = mapper.ToUnity(fileAn.framesData.boneRotationByFrames[i, 0]);
                localRotation.x *= -1;
                localRotation.w *= -1;
                boneTransform.localPosition = localPosition;
                boneTransform.localRotation = localRotation;
            }
            return bones;
        }

        private Avatar CreateAvatar(GameObject mainGameObject, string rootMotionTransformName = "")
        {
            Avatar avatar = AvatarBuilder.BuildGenericAvatar(mainGameObject, rootMotionTransformName);
            avatar.name = mainGameObject.name + "Avatar";
            return avatar;
        }

        private Animator CreateAnimator(GameObject mainGameObject, Avatar avatar)
        {
            Animator animator = mainGameObject.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.updateMode = AnimatorUpdateMode.Normal;
            return animator;
        }

        private AnimationClip[] CreateAnimationClips(FileAn fileAn, GameObject[] bones)
        {
            if (string.IsNullOrEmpty(fileAniPath))
            {
                return null;
            }

            FileAni fileAni = new(fileAniPath);
            List<AnimationClip> clips = new();
            string clipNamePrefix = fileAn.nameWithoutExtension + "_";
            foreach (var data in fileAni.dataBySections)
            {
                if (string.IsNullOrEmpty(data.Key))
                {
                    continue;
                }
                var dataBySection = data.Value;
                if (!dataBySection.ContainsKey("start_time") || !dataBySection.ContainsKey("end_time"))
                {
                    Debug.LogWarningFormat("Section [{0}] does not contain 'start_time' or 'end_time'.", data.Key);
                    continue;
                }
                int frameStart = int.Parse(dataBySection["start_time"][0]);
                int frameEnd = int.Parse(dataBySection["end_time"][0]);
                float frameDuration = 1.0F / fileAn.headerData.fps;
                AnimationClip clip = new()
                {
                    name = clipNamePrefix + data.Key,
                    frameRate = fileAn.headerData.fps
                };
                for (int i = 0; i < fileAn.headerData.bonesCount; ++i)
                {
                    AnimationCurve curvePositionX = null;
                    AnimationCurve curvePositionY = null;
                    AnimationCurve curvePositionZ = null;
                    if (i == 0)
                    {
                        curvePositionX = new AnimationCurve();
                        curvePositionY = new AnimationCurve();
                        curvePositionZ = new AnimationCurve();
                    }
                    AnimationCurve curveRotationX = new();
                    AnimationCurve curveRotationY = new();
                    AnimationCurve curveRotationZ = new();
                    AnimationCurve curveRotationW = new();
                    float currentTime = 0;
                    for (int j = frameStart; j <= frameEnd; ++j, currentTime += frameDuration)
                    {
                        if (i == 0)
                        {
                            Vector3 position = mapper.ToUnity(fileAn.framesData.rootBonePositionByFrames[j]);
                            curvePositionX.AddKey(currentTime, -position.x);
                            curvePositionY.AddKey(currentTime, position.y);
                            curvePositionZ.AddKey(currentTime, position.z);
                        }
                        Quaternion rotation = mapper.ToUnity(fileAn.framesData.boneRotationByFrames[i, j]);
                        curveRotationX.AddKey(currentTime, -rotation.x);
                        curveRotationY.AddKey(currentTime, rotation.y);
                        curveRotationZ.AddKey(currentTime, rotation.z);
                        curveRotationW.AddKey(currentTime, -rotation.w);
                    }
                    string path = bones[i].name;
                    if (i == 0)
                    {
                        clip.SetCurve(path, typeof(Transform), "localPosition.x", curvePositionX);
                        clip.SetCurve(path, typeof(Transform), "localPosition.y", curvePositionY);
                        clip.SetCurve(path, typeof(Transform), "localPosition.z", curvePositionZ);
                    }
                    else
                    {
                        path = AnimationUtility.CalculateTransformPath(bones[i].transform, bones[0].transform.parent);
                    }
                    clip.SetCurve(path, typeof(Transform), "localRotation.x", curveRotationX);
                    clip.SetCurve(path, typeof(Transform), "localRotation.y", curveRotationY);
                    clip.SetCurve(path, typeof(Transform), "localRotation.z", curveRotationZ);
                    clip.SetCurve(path, typeof(Transform), "localRotation.w", curveRotationW);
                }
                AnimationEvent[] animationEvents = GetAnimationEvents(dataBySection, frameStart, frameDuration);
                if (animationEvents != null && animationEvents.Length > 0)
                {
                    AnimationUtility.SetAnimationEvents(clip, animationEvents);
                }
                clips.Add(clip);
            }
            return clips.ToArray();
        }

        private AnimationEvent[] GetAnimationEvents(IDictionary<string, List<string>> dataBySection, int frameStart, float frameDuration)
        {
            List<AnimationEvent> animationEvents = new();
            if (dataBySection.ContainsKey("event"))
            {
                var events = dataBySection["event"];
                if (events != null && events.Count > 0)
                {
                    foreach (var currentEvent in events)
                    {
                        string[] eventData = currentEvent.Split(',', System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                        if (eventData.Length > 1)
                        {
                            string stringParameter = eventData[0].Trim('"');
                            if (int.TryParse(eventData[1], out int frameEvent))
                            {
                                float time = (frameEvent - frameStart) * frameDuration;
                                AnimationEvent animationEvent = new()
                                {
                                    time = time,
                                    functionName = eventFunctionName,
                                    stringParameter = stringParameter
                                };
                                animationEvents.Add(animationEvent);
                            }
                            else
                            {
                                Debug.LogWarningFormat("Failed to get event frame: {0}", currentEvent);
                            }
                        }
                    }
                }
            }
            return animationEvents.ToArray();
        }
    }
}
