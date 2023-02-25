using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Animations;

namespace AoP.Editor
{
    [ScriptedImporter(1, "gm")]
    public sealed class FileGmImporter : ScriptedImporter
    {
        [SerializeField] private bool isMeshReadWrite = false;
        [SerializeField] private bool flipUvVertical = false;
        [SerializeField] private bool createLocatorConstraint = true;
        [SerializeField][HideInInspector] private string materialsExtractPath = null;
        [SerializeField][HideInInspector] private string animationFilePath = null;
        [SerializeField][HideInInspector] private bool hasAnimatedMesh = false;
        [SerializeField][HideInInspector] private bool hasLocators = false;

        private readonly SystemNumericsMapper mapper = new();

        public override bool SupportsRemappedAssetType(System.Type type)
        {
            return type.IsAssignableFrom(typeof(Material));
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            FileGm fileGm = new(ctx.assetPath);
            FileAn fileAn = null;
            if (!string.IsNullOrEmpty(animationFilePath))
            {
                fileAn = new FileAn(animationFilePath);
            }
            CreateMainGameObject(ctx, fileGm, fileAn);
        }

        private void CreateMainGameObject(AssetImportContext ctx, FileGm fileGm, FileAn fileAn)
        {
            GameObject mainGameObject = new(fileGm.nameWithoutExtension);
            Transform mainTransform = mainGameObject.transform;
            Material[] materials = FindOrCreateMaterials(ctx, fileGm);
            Transform[] bones = CreateBones(fileAn, mainTransform);
            CreateMeshObjects(ctx, fileGm, mainTransform, materials, bones);
            CreateLocators(fileGm, mainTransform, bones);
            CreateAnimator(mainGameObject, bones);
            ctx.AddObjectToAsset("main", mainGameObject);
            ctx.SetMainObject(mainGameObject);
        }

        private Material[] FindOrCreateMaterials(AssetImportContext ctx, FileGm fileGm)
        {
            FileGm.MaterialsData.Material[] materialsGm = fileGm.materialsData.materials;
            Material[] materials = new Material[materialsGm.Length];
            Shader shader = null;
            Dictionary<string, Texture2D> texturesByNames = null;
            for (int i = 0; i < materialsGm.Length; ++i)
            {
                FileGm.MaterialsData.Material materialGm = materialsGm[i];
                Material material = null;
                bool found = true;
                if (!string.IsNullOrEmpty(materialsExtractPath))
                {
                    material = FindAndLoadAsset<Material>(
                        assetName: materialGm.name,
                        filter: "t:material",
                        path: materialsExtractPath);
                }
                if (material == null)
                {
                    found = false;
                    if (shader == null)
                    {
                        shader = FindShader();
                    }
                    if (texturesByNames == null)
                    {
                        texturesByNames = FindTextures(fileGm);
                    }
                    material = CreateMaterialFromMaterialGm(fileGm, materialGm, shader, texturesByNames);
                }
                if (material != null)
                {
                    materials[i] = material;

                    if (!found)
                    {
                        ctx.AddObjectToAsset($"material_{material.name}", material);
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to create material '{materialGm.name}'.");
                }
            }
            return materials;
        }

        private Material CreateMaterialFromMaterialGm(
            FileGm fileGm,
            FileGm.MaterialsData.Material materialGm,
            Shader shader,
            Dictionary<string, Texture2D> texturesByNames)
        {
            if (shader == null)
            {
                Debug.LogWarning($"Failed to create material '{materialGm.name}': shader is null.");
                return null;
            }

            string[] texturesNames = fileGm.texturesData.names;
            Material material = new(shader)
            {
                name = materialGm.name
            };
            for (int i = 0; i < materialGm.textureIndices.Length; ++i)
            {
                int textureIndex = materialGm.textureIndices[i];
                if (textureIndex >= 0 && textureIndex < texturesNames.Length)
                {
                    string textureName = texturesNames[textureIndex];
                    if (texturesByNames.ContainsKey(textureName))
                    {
                        Texture2D texture2D = texturesByNames[textureName];
                        switch (materialGm.textureTypes[i])
                        {
                            case FileGm.MaterialsData.Material.TextureTypes.Main:
                                {
                                    material.mainTexture = texture2D;
                                    break;
                                }
                            case FileGm.MaterialsData.Material.TextureTypes.Bump:
                                {
                                    material.SetTexture(Shader.PropertyToID("_ParallaxMap"), texture2D);
                                    break;
                                }
                        }
                    }
                    else
                    {
                        Debug.Log($"Texture '{textureName}' was not found for material '{materialGm.name}'.");
                    }
                }
            }
            return material;
        }

        private Dictionary<string, Texture2D> FindTextures(FileGm fileGm)
        {
            Dictionary<string, Texture2D> texturesByNames = new();
            foreach (string textureName in fileGm.texturesData.names)
            {
                Texture2D texture = FindAndLoadAsset<Texture2D>(assetName: textureName, filter: "t:texture2D");
                if (texture == null)
                {
                    texture = FindAndLoadAsset<Texture2D>(
                        assetName: Path.GetFileNameWithoutExtension(textureName),
                        filter: "t:texture2D");
                }
                if (texture != null)
                {
                    texturesByNames[textureName] = texture;
                }
            }
            return texturesByNames;
        }

        private Shader FindShader()
        {
            string[] shadersNames =
            {
                "HDRP/Lit",
                "Universal Render Pipeline/Lit",
                "Standard"
            };
            foreach (string shaderName in shadersNames)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    return shader;
                }
            }
            return null;
        }

        private Transform[] CreateBones(FileAn fileAn, Transform parent)
        {
            int bonesCount = fileAn != null ? fileAn.headerData.bonesCount : 0;
            if (bonesCount <= 0)
            {
                return null;
            }
            GameObject bonesRoot = new("bones");
            Transform bonesRootTransform = bonesRoot.transform;
            bonesRootTransform.parent = parent;
            GameObject[] bones = new GameObject[bonesCount];
            Transform[] bonesTransforms = new Transform[bones.Length];
            for (int i = 0; i < fileAn.headerData.bonesCount; ++i)
            {
                GameObject bone = new("bone_" + i.ToString("D2"));
                bones[i] = bone;
                bonesTransforms[i] = bone.transform;
            }
            for (int i = 0; i < bones.Length; ++i)
            {
                int parentIndex = fileAn.bonesData.parentIndices[i];
                if (parentIndex >= 0 && parentIndex < bones.Length)
                {
                    bonesTransforms[i].parent = bonesTransforms[parentIndex];
                }
                else
                {
                    bonesTransforms[i].parent = bonesRootTransform;
                }
                Vector3 localPosition = mapper.ToUnity(fileAn.bonesData.startPositions[i]);
                localPosition.x *= -1;
                Quaternion localRotation = mapper.ToUnity(fileAn.framesData.bonesRotations[i, 0]);
                localRotation.x *= -1;
                localRotation.w *= -1;
                bonesTransforms[i].localPosition = localPosition;
                bonesTransforms[i].localRotation = localRotation;
            }
            return bonesTransforms;
        }

        private void CreateMeshObjects(
            AssetImportContext ctx,
            FileGm fileGm,
            Transform parent,
            Material[] materials,
            Transform[] bones)
        {
            FileGm.MeshObjectsData.MeshObject[] meshObjectsGm = fileGm.meshObjectsData.meshObjects;
            if (meshObjectsGm.Length <= 0)
            {
                return;
            }
            GameObject meshesRoot = new("meshes");
            Transform meshesRootTransform = meshesRoot.transform;
            meshesRootTransform.parent = parent;
            Dictionary<string, GameObject> groups = new();
            Dictionary<string, int> meshesCountByNames = new();
            for (int i = 0; i < meshObjectsGm.Length; ++i)
            {
                FileGm.MeshObjectsData.MeshObject meshObjectGm = meshObjectsGm[i];
                if (!groups.ContainsKey(meshObjectGm.groupName))
                {
                    GameObject group = new(meshObjectGm.groupName);
                    group.transform.parent = meshesRootTransform;
                    groups[meshObjectGm.groupName] = group;
                }
                Transform groupTransform = groups[meshObjectGm.groupName].transform;
                if (meshesCountByNames.ContainsKey(meshObjectGm.name))
                {
                    meshesCountByNames[meshObjectGm.name] += 1;
                }
                else
                {
                    meshesCountByNames[meshObjectGm.name] = 1;
                }
                int countOfMeshWithCurrentName = meshesCountByNames[meshObjectGm.name];
                string meshName = meshObjectGm.name;
                if (countOfMeshWithCurrentName > 1)
                {
                    meshName = $"{meshName}_{countOfMeshWithCurrentName:D2}";
                }
                GameObject meshObject = new(meshName);
                meshObject.transform.parent = groupTransform;
                CreateMesh(ctx, fileGm, meshObjectGm, meshObject, materials, bones);
            }
        }

        private void CreateMesh(
            AssetImportContext ctx,
            FileGm fileGm,
            FileGm.MeshObjectsData.MeshObject meshObjectGm,
            GameObject target,
            Material[] materials,
            Transform[] bones)
        {
            int verticesCount = meshObjectGm.verticesCount;
            int vertexBufferIndex = meshObjectGm.vertexBufferIndex;
            var buffer = fileGm.vertexBuffersData.vertexBuffers[vertexBufferIndex];
            bool isAnimated = buffer.isAnimated;
            Vector3[] vertices = new Vector3[verticesCount];
            Vector3[] normals = new Vector3[verticesCount];
            Vector2[] uv = new Vector2[verticesCount];
            Vector2[] uv2 = null;
            if (buffer.hasUv2)
            {
                uv2 = new Vector2[verticesCount];
            }
            BoneWeight[] boneWeights = null;
            if (isAnimated)
            {
                hasAnimatedMesh = true;
                if (bones == null)
                {
                    Debug.LogWarning($"Mesh '{meshObjectGm.name}' is animated, but bones not loaded. Select .an file to load bones data.");
                }
                boneWeights = new BoneWeight[verticesCount];
            }
            for (int i = 0; i < meshObjectGm.verticesCount; ++i)
            {
                var vertex = fileGm.verticesData.verticesByBufferIndices[vertexBufferIndex][i + meshObjectGm.verticesOffset];
                vertices[i] = mapper.ToUnity(vertex.position);
                normals[i] = mapper.ToUnity(vertex.normal);
                if (buffer.isAnimated)
                {
                    vertices[i].x *= -1;
                    normals[i].x *= -1;
                    if (vertex.bone1 == null || vertex.bone2 == null || vertex.weight1 == null || vertex.weight2 == null)
                    {
                        Debug.LogWarning("buffer.isAnimated && (vertex.bone1 == null || vertex.bone2 == null || vertex.weight1 == null || vertex.weight2 == null)");
                    }
                    else
                    {
                        boneWeights[i].boneIndex0 = (int)vertex.bone1;
                        boneWeights[i].boneIndex1 = (int)vertex.bone2;
                        boneWeights[i].weight0 = (float)vertex.weight1;
                        boneWeights[i].weight1 = (float)vertex.weight2;
                    }
                }
                uv[i] = mapper.ToUnity(vertex.uv);
                if (flipUvVertical)
                {
                    uv[i].y *= -1;
                }
                if (uv2 != null)
                {
                    if (vertex.uv2 == null)
                    {
                        Debug.LogWarning("buffer.hasUv2 && vertex.uv1 == null");
                    }
                    else
                    {
                        uv2[i] = mapper.ToUnity((System.Numerics.Vector2)vertex.uv2);
                        if (flipUvVertical)
                        {
                            uv2[i].y *= -1;
                        }
                    }
                }
            }
            int[] triangles = new int[meshObjectGm.trianglesCount * 3];
            for (int i = 0, j = 0; i < meshObjectGm.trianglesCount; ++i, j += 3)
            {
                var triangle = fileGm.trianglesData.triangles[i + meshObjectGm.trianglesOffset];
                triangles[j] = triangle.v1;
                triangles[j + 1] = triangle.v2;
                triangles[j + 2] = triangle.v3;
            }
            Matrix4x4[] bindposes = null;
            Transform rootBone = null;
            if (bones != null && bones.Length > 0)
            {
                rootBone = bones[0];
                bindposes = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; ++i)
                {
                    bindposes[i] = bones[i].worldToLocalMatrix;
                }
            }
            Mesh mesh = new()
            {
                name = target.name,
                vertices = vertices,
                normals = normals,
                triangles = triangles,
                uv = uv,
                uv2 = uv2,
                boneWeights = boneWeights,
                bindposes = bindposes
            };
            mesh.RecalculateTangents();
            mesh.UploadMeshData(!isMeshReadWrite);
            if (isAnimated && bones != null)
            {
                SkinnedMeshRenderer skinnedMeshRenderer = target.AddComponent<SkinnedMeshRenderer>();
                skinnedMeshRenderer.sharedMesh = mesh;
                skinnedMeshRenderer.material = materials[meshObjectGm.materialIndex];
                skinnedMeshRenderer.bones = bones;
                skinnedMeshRenderer.rootBone = rootBone;
                skinnedMeshRenderer.quality = SkinQuality.Bone2;
            }
            else
            {
                MeshFilter meshFilter = target.AddComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                MeshRenderer meshRenderer = target.AddComponent<MeshRenderer>();
                meshRenderer.material = materials[meshObjectGm.materialIndex];
            }
            ctx.AddObjectToAsset($"mesh_{mesh.name}", mesh);
        }

        private void CreateLocators(FileGm fileGm, Transform parent, Transform[] bones)
        {
            FileGm.LocatorsData.Locator[] locatorsGm = fileGm.locatorsData.locators;
            hasLocators = locatorsGm.Length > 0;
            if (!hasLocators)
            {
                return;
            }
            GameObject locatorsRoot = new("locators");
            Transform locatorsRootTransform = locatorsRoot.transform;
            locatorsRootTransform.parent = parent;
            Dictionary<string, GameObject> groups = new();
            Dictionary<string, Dictionary<int, GameObject>> constraintsByGroup = new();
            for (int i = 0; i < locatorsGm.Length; ++i)
            {
                FileGm.LocatorsData.Locator locatorGm = locatorsGm[i];
                GameObject locator = new(locatorGm.name);
                Transform locatorTransform = locator.transform;

                string groupName = locatorGm.groupName;
                if (!groups.ContainsKey(groupName))
                {
                    GameObject group = new(groupName);
                    group.transform.parent = locatorsRootTransform;
                    groups[groupName] = group;
                }
                Matrix4x4 matrix = mapper.ToUnity(locatorGm.matrix);
                Vector3 position = new(matrix[0, 3], matrix[1, 3], matrix[2, 3]);
                Quaternion rotation = matrix.rotation;
                Transform locatorTransformParent;
                if (hasAnimatedMesh)
                {
                    position.x *= -1;
                    rotation.x *= -1;
                    rotation.w *= -1;

                    if (bones == null || !createLocatorConstraint)
                    {
                        locatorTransformParent = groups[groupName].transform;

                        if (createLocatorConstraint)
                        {
                            Debug.LogWarning($"Failed to create constraint for locator '{locatorGm.name}', because bones not loaded. Select .an file to load bones data.");
                        }
                    }
                    else
                    {
                        if (!constraintsByGroup.ContainsKey(groupName))
                        {
                            constraintsByGroup[groupName] = new Dictionary<int, GameObject>();
                        }
                        Dictionary<int, GameObject> constraints = constraintsByGroup[groupName];
                        int boneIndex = locatorGm.boneIndices[0];
                        if (!constraints.ContainsKey(boneIndex))
                        {
                            Transform bone = bones[boneIndex];
                            GameObject constraintBone = new($"{bone.name}_constraint");
                            constraintBone.transform.parent = groups[groupName].transform;
                            constraintBone.transform.SetPositionAndRotation(bone.position, bone.rotation);
                            ParentConstraint constraint = constraintBone.AddComponent<ParentConstraint>();
                            ConstraintSource constraintSource = new()
                            {
                                sourceTransform = bone,
                                weight = 1.0F
                            };
                            constraint.AddSource(constraintSource);
                            constraint.locked = true;
                            constraint.constraintActive = false;
                            constraints[boneIndex] = constraintBone;
                        }
                        locatorTransformParent = constraints[boneIndex].transform;
                    }
                }
                else
                {
                    locatorTransformParent = groups[groupName].transform;
                }
                locatorTransform.parent = locatorTransformParent;
                locatorTransform.SetPositionAndRotation(position, rotation);
            }
        }

        private void CreateAnimator(GameObject mainGameObject, Transform[] bones)
        {
            if (bones != null)
            {
                Avatar avatar = null;
                if (!string.IsNullOrEmpty(animationFilePath))
                {
                    avatar = AssetDatabase.LoadAssetAtPath<Avatar>(animationFilePath);
                }
                Animator animator = mainGameObject.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.updateMode = AnimatorUpdateMode.Normal;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            }
        }

        private T FindAndLoadAsset<T>(string assetName, string filter, string path = null) where T : Object
        {
            string[] guids;
            if (string.IsNullOrEmpty(path))
            {
                guids = AssetDatabase.FindAssets($"{assetName} {filter}".Trim());
            }
            else
            {
                guids = AssetDatabase.FindAssets($"{assetName} {filter}".Trim(), new[] { path });
            }
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (string.Equals(asset.name, assetName))
                {
                    return asset;
                }
            }
            return null;
        }
    }
}
