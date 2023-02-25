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
        [SerializeField] private string eventFunctionName = "OnAnimationEvent";
        [SerializeField][HideInInspector] private string fileAniPath = null;

        private readonly SystemNumericsMapper mapper = new();

        public override void OnImportAsset(AssetImportContext ctx)
        {
            FileAni fileAni = null;
            if (!string.IsNullOrEmpty(fileAniPath))
            {
                fileAni = new FileAni(fileAniPath);
            }
            CreateMainGameObject(ctx, new FileAn(ctx.assetPath), fileAni);
        }

        private void CreateMainGameObject(AssetImportContext ctx, FileAn fileAn, FileAni fileAni)
        {
            GameObject mainGameObject = new(fileAn.nameWithoutExtension);
            Transform mainTransform = mainGameObject.transform;
            GameObject[] bones = CreateBones(fileAn, mainTransform);
            Avatar avatar = CreateAvatar(ctx, mainGameObject, string.Empty);
            CreateAnimator(mainGameObject, avatar);
            CreateAnimationClips(ctx, fileAn, fileAni, mainTransform, bones);
            ctx.AddObjectToAsset("main", mainGameObject);
            ctx.SetMainObject(mainGameObject);
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
                Quaternion localRotation = mapper.ToUnity(fileAn.framesData.bonesRotations[i, 0]);
                localRotation.x *= -1;
                localRotation.w *= -1;
                boneTransform.localPosition = localPosition;
                boneTransform.localRotation = localRotation;
            }
            return bones;
        }

        private Avatar CreateAvatar(
            AssetImportContext ctx,
            GameObject mainGameObject,
            string rootMotionTransformName = "")
        {
            Avatar avatar = AvatarBuilder.BuildGenericAvatar(mainGameObject, rootMotionTransformName);
            avatar.name = mainGameObject.name + "Avatar";
            ctx.AddObjectToAsset("avatar", avatar);
            return avatar;
        }

        private Animator CreateAnimator(GameObject mainGameObject, Avatar avatar)
        {
            Animator animator = mainGameObject.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            return animator;
        }

        private void CreateAnimationClips(
            AssetImportContext ctx,
            FileAn fileAn,
            FileAni fileAni,
            Transform parent,
            GameObject[] bones)
        {
            if (bones == null)
            {
                throw new System.ArgumentException("Bones is null.", "bones");
            }
            if (fileAni == null)
            {
                Debug.LogWarning("Failed to create animation clips. Select .ani file to load animation clips data.");
                return;
            }
            string clipNamePrefix = fileAn.nameWithoutExtension + "_";
            foreach (KeyValuePair<string, IDictionary<string, IList<string>>> data in fileAni.dataBySections)
            {
                if (string.IsNullOrEmpty(data.Key))
                {
                    continue;
                }
                IDictionary<string, IList<string>> dataBySection = data.Value;
                if (!dataBySection.ContainsKey("start_time") || !dataBySection.ContainsKey("end_time"))
                {
                    Debug.LogWarning($"Section [{data.Key}] does not contain 'start_time' or 'end_time'.");
                    continue;
                }
                CreateAnimationClip(
                    ctx,
                    clipNamePrefix + data.Key,
                    fileAn.headerData.fps,
                    dataBySection,
                    bones,
                    parent,
                    mapper.ToUnity(fileAn.framesData.rootBonePositions),
                    mapper.ToUnity(fileAn.framesData.bonesRotations));
            }
        }

        private void CreateAnimationClip(
            AssetImportContext ctx,
            string name,
            float frameRate,
            IDictionary<string, IList<string>> data,
            GameObject[] bones,
            Transform parent,
            Vector3[] rootBonePositions,
            Quaternion[,] bonesRotations)
        {
            AnimationClip clip = new()
            {
                name = name,
                frameRate = frameRate
            };
            int frameStart = int.Parse(data["start_time"][0]);
            int frameEnd = int.Parse(data["end_time"][0]);
            int framesCount = frameEnd - frameStart + 1;
            float frameDuration = 1.0F / frameRate;
            bool loop = false;
            if (data.TryGetValue("loop", out IList<string> loopData))
            {
                bool.TryParse(loopData[0], out loop);
            }
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                string path = AnimationUtility.CalculateTransformPath(bones[boneIndex].transform, parent);
                if (boneIndex == 0)
                {
                    SetupAnimationCurvesRootBonePosition(
                        clip,
                        frameStart,
                        frameEnd,
                        framesCount,
                        frameDuration,
                        rootBonePositions,
                        path);
                }
                SetupAnimationCurvesRotations(
                    clip,
                    boneIndex,
                    frameStart,
                    frameEnd,
                    framesCount,
                    frameDuration,
                    bonesRotations,
                    path);
            }
            AnimationClipSettings clipSettings = new()
            {
                loopTime = loop,
                loopBlend = loop,
                startTime = 0.0F,
                stopTime = (frameEnd - frameStart) * frameDuration
            };
            AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AnimationEvent[] animationEvents = GetAnimationEvents(data, frameStart, frameDuration);
            if (animationEvents != null && animationEvents.Length > 0)
            {
                AnimationUtility.SetAnimationEvents(clip, animationEvents);
            }
            ctx.AddObjectToAsset("clip_" + clip.name, clip);
        }

        private void SetupAnimationCurvesRootBonePosition(
            AnimationClip clip,
            int frameStart,
            int frameEnd,
            int framesCount,
            float frameDuration,
            Vector3[] bonePositions,
            string path)
        {
            List<Keyframe> keyframesX = new(framesCount);
            List<Keyframe> keyframesY = new(framesCount);
            List<Keyframe> keyframesZ = new(framesCount);
            float currentTime = 0;
            for (int i = frameStart; i <= frameEnd; ++i, currentTime += frameDuration)
            {
                Vector3 position = bonePositions[i];
                keyframesX.Add(new Keyframe()
                {
                    time = currentTime,
                    value = -position.x
                });
                keyframesY.Add(new Keyframe()
                {
                    time = currentTime,
                    value = position.y
                });
                keyframesZ.Add(new Keyframe()
                {
                    time = currentTime,
                    value = position.z
                });
            }
            AnimationCurve positionCurveX = new(keyframesX.ToArray());
            AnimationCurve positionCurveY = new(keyframesY.ToArray());
            AnimationCurve positionCurveZ = new(keyframesZ.ToArray());
            CalculateTangents(positionCurveX);
            CalculateTangents(positionCurveY);
            CalculateTangents(positionCurveZ);
            clip.SetCurve(path, typeof(Transform), "localPosition.x", positionCurveX);
            clip.SetCurve(path, typeof(Transform), "localPosition.y", positionCurveY);
            clip.SetCurve(path, typeof(Transform), "localPosition.z", positionCurveZ);
        }

        private void SetupAnimationCurvesRotations(
            AnimationClip clip,
            int boneIndex,
            int frameStart,
            int frameEnd,
            int framesCount,
            float frameDuration,
            Quaternion[,] bonesRotations,
            string path)
        {
            List<Keyframe> keyframesX = new(framesCount);
            List<Keyframe> keyframesY = new(framesCount);
            List<Keyframe> keyframesZ = new(framesCount);
            float currentTime = 0;
            Vector3 previousEulerAngles = Vector3.zero;
            Vector3 offsetEulerAngles = Vector3.zero;
            for (int i = frameStart; i <= frameEnd; ++i, currentTime += frameDuration)
            {
                Quaternion rotation = bonesRotations[boneIndex, i];
                Vector3 eulerAngles = new Quaternion(-rotation.x, rotation.y, rotation.z, -rotation.w).eulerAngles;
                eulerAngles += offsetEulerAngles;
                if (i > frameStart)
                {
                    if (Mathf.Abs(eulerAngles.x - previousEulerAngles.x) > 270)
                    {
                        if (eulerAngles.x - offsetEulerAngles.x > 180)
                        {
                            eulerAngles.x -= 360;
                            offsetEulerAngles.x -= 360;
                        }
                        else
                        {
                            eulerAngles.x += 360;
                            offsetEulerAngles.x += 360;
                        }
                    }
                    if (Mathf.Abs(eulerAngles.y - previousEulerAngles.y) > 270)
                    {
                        if (eulerAngles.y - offsetEulerAngles.y > 180)
                        {
                            eulerAngles.y -= 360;
                            offsetEulerAngles.y -= 360;
                        }
                        else
                        {
                            eulerAngles.y += 360;
                            offsetEulerAngles.y += 360;
                        }
                    }
                    if (Mathf.Abs(eulerAngles.z - previousEulerAngles.z) > 270)
                    {
                        if (eulerAngles.z - offsetEulerAngles.z > 180)
                        {
                            eulerAngles.z -= 360;
                            offsetEulerAngles.z -= 360;
                        }
                        else
                        {
                            eulerAngles.z += 360;
                            offsetEulerAngles.z += 360;
                        }
                    }
                }
                keyframesX.Add(new Keyframe()
                {
                    time = currentTime,
                    value = eulerAngles.x
                });
                keyframesY.Add(new Keyframe()
                {
                    time = currentTime,
                    value = eulerAngles.y
                });
                keyframesZ.Add(new Keyframe()
                {
                    time = currentTime,
                    value = eulerAngles.z
                });
                previousEulerAngles = eulerAngles;
            }
            AnimationCurve rotationCurveX = new(keyframesX.ToArray());
            AnimationCurve rotationCurveY = new(keyframesY.ToArray());
            AnimationCurve rotationCurveZ = new(keyframesZ.ToArray());
            CalculateTangents(rotationCurveX);
            CalculateTangents(rotationCurveY);
            CalculateTangents(rotationCurveZ);
            clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x", rotationCurveX);
            clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.y", rotationCurveY);
            clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.z", rotationCurveZ);
        }

        private void CalculateTangents(AnimationCurve curve)
        {
            int length = curve.length;
            for (int i = 0; i < length; ++i)
            {
                AnimationUtility.SetKeyBroken(curve, i, true);
                if (i > 0)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                }
                if (i < length - 1)
                {
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                }
            }
        }

        private AnimationEvent[] GetAnimationEvents(
            IDictionary<string, IList<string>> data,
            int frameStart,
            float frameDuration)
        {
            List<AnimationEvent> animationEvents = new();
            if (data.TryGetValue("event", out IList<string> events))
            {
                if (events != null && events.Count > 0)
                {
                    foreach (string currentEvent in events)
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
                                Debug.LogWarning($"Failed to get event frame: {currentEvent}");
                            }
                        }
                    }
                }
            }
            return animationEvents.ToArray();
        }
    }
}
