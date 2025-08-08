using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

namespace SpriteHelper
{
    public class EditorExcuter : Editor
    {
        [MenuItem("Tools/读取任意TextureMeta")]
        public static void ReadTextureMetaSerilizedObject()
        {
            var obj = Selection.objects[0];
            SerializedObject c = new SerializedObject(obj);
            Debug.Log(c);
        }
        public class WindowSetSpritePivot : EditorWindow
        {
            [MenuItem("Tools/批量设置图片Pivot")]
            public static void OpenSetSpritesPivotWindow()
            {
                var window = CreateWindow<WindowSetSpritePivot>("SpritePivotSetter");
                window.Show();
            }
            public static void OpenSetSpritesWithBuilding(List<string> paths)
            {
                var window = CreateWindow<WindowSetSpritePivot>("SpritePivotSetter");
                window.texturePaths = paths;
                window.Show();
            }

            Rect pathLabelHeader_rect = new Rect(20, 5, 80, 25);
            Rect pathDrag_rect = new Rect(100, 5, 300, 25);
            Rect pathObjectField_rect = new Rect(20, 5, 500, 25);

            Rect filterHeader_rect = new Rect(20, 35, 50, 25);
            Rect matchHeader_rect = new Rect(70, 35, 300, 25);

            Rect extter_rect = new Rect(380, 35, 50, 25);
            Rect extterContent_rect = new Rect(430, 35, 120, 25);

            Rect totalSelect_rect = new Rect(20, 75, 120, 25);
            Rect gridCellSizeSliderTitle_rect = new Rect(150, 75, 100, 25);
            Rect gridCellSizeSlider_rect = new Rect(250, 75, 200, 25);
            Rect loadSpriteBtn_rect = new Rect(455, 75, 120, 25);

            Rect workArea_rect = new Rect(10, 110, 220, 500);
            Vector2 startScrollPos = Vector2.zero;
            Rect textPreview_rect = new Rect(250, 110, 500, 500);

            float gridSize = 150;
            List<string> texturePaths;
            TextureEditorData[] texArray;
            string targetObjectPath;
            string matchHeader = "";
            //
            SpriteExtEnum curSelectExtts = SpriteExtEnum._png;
            //
            enum SpriteExtEnum
            {
                _png = 1,
                _jpg = 1 << 1,
                _psd = 1 << 2,
            }
            UnityEngine.Object selectObj;
            private void OnGUI()
            {
                #region Path Loader
                selectObj = EditorGUI.ObjectField(pathObjectField_rect, "Path:", selectObj, typeof(UnityEngine.Object), false);
                 
                #endregion
                #region Filters
                GUI.Label(filterHeader_rect, "Match:");
                matchHeader = GUI.TextField(matchHeader_rect, matchHeader);

                GUI.Label(extter_rect, "Exttes:");
                curSelectExtts = (SpriteExtEnum)EditorGUI.EnumMaskField(extterContent_rect, curSelectExtts);

                #endregion
                #region LoadSprite
                GUI.Label(totalSelect_rect, "总共选择：" + (texturePaths == null ? 0 : texturePaths.Count));
                GUI.Label(gridCellSizeSliderTitle_rect, $"Size:{gridSize}");
                gridSize = Mathf.CeilToInt(GUI.HorizontalSlider(gridCellSizeSlider_rect, gridSize, 88, 256));
                if (GUI.Button(loadSpriteBtn_rect, "LoadSprites"))
                {
                    if (selectObj != null)
                    {
                        targetObjectPath = AssetDatabase.GetAssetPath(selectObj);
                        if (targetObjectPath == "Assets")
                        {
                            targetObjectPath = Application.dataPath;
                        }
                        else
                        {
                            targetObjectPath = targetObjectPath.Substring(targetObjectPath.IndexOf("Assets") + 6);
                            targetObjectPath = Application.dataPath + targetObjectPath;
                        }
                        Debug.Log(targetObjectPath);
                        {
                            //
                            texturePaths = null;
                            texArray = null;
                            //
                            FileInfo[] allFileInfos = null;
                            if (Directory.Exists(targetObjectPath))
                            {
                                DirectoryInfo directory = new DirectoryInfo(targetObjectPath);
                                allFileInfos = directory.GetFiles("*.*", SearchOption.AllDirectories);
                            }
                            else
                            {
                                if (File.Exists(targetObjectPath))
                                {
                                    allFileInfos = new FileInfo[] { new FileInfo(targetObjectPath) };
                                }
                            }
                            if (allFileInfos != null && allFileInfos.Length > 0)
                            {
                                texturePaths = new List<string>();
                                for (int i = 0; i < allFileInfos.Length; i++)
                                {
                                    var fileInfo = allFileInfos[i];
                                    var texPath = fileInfo.FullName.Substring(fileInfo.FullName.IndexOf("Assets"));
                                    var fileExt = Path.GetExtension(texPath);
                                    if (fileExt == ".meta" || fileExt == ".META")
                                    {
                                        continue;
                                    }

                                    if ((fileExt == ".png" || fileExt == ".PNG") && (curSelectExtts & SpriteExtEnum._png) != SpriteExtEnum._png)
                                    {
                                        continue;
                                    }
                                    if ((fileExt == ".jpg" || fileExt == ".JPG") && (curSelectExtts & SpriteExtEnum._jpg) != SpriteExtEnum._jpg)
                                    {
                                        continue;
                                    }
                                    if ((fileExt == ".psd" || fileExt == ".PSD") && (curSelectExtts & SpriteExtEnum._psd) != SpriteExtEnum._psd)
                                    {
                                        continue;
                                    }
                                    if (string.IsNullOrEmpty(matchHeader))
                                    {
                                        texturePaths.Add(texPath);
                                        continue;
                                    }
                                    var fileName = Path.GetFileNameWithoutExtension(texPath);
                                    if (!Regex.IsMatch(fileName, matchHeader, RegexOptions.Compiled))
                                    {
                                        continue;
                                    }
                                    //
                                    texturePaths.Add(texPath);
                                }
                            }
                            Debug.Log("CurrentLoad:" + (texturePaths == null ? 0 : texturePaths.Count));
                        }
                    }
                }
                #endregion

                OnDrawMiniTex_Grid();
            }
            int currenderIndex0 = 0;
            void OnDrawMiniTex_Grid()
            {
                if (texturePaths == null || texturePaths.Count == 0)
                {
                    return;
                }
                workArea_rect.width = this.position.width - 50;
                workArea_rect.height = this.position.height - 150;

                float width = workArea_rect.width;
                float height = workArea_rect.height;
                float celWidth = gridSize;
                float celHeight = gridSize;
                float gap = 10;
                var x_min = workArea_rect.x + 2;
                var y_min = workArea_rect.y + 2;

                var cell_ColNum = Mathf.FloorToInt(width / (celWidth + gap));
                var cell_RowNum = Mathf.FloorToInt(height / (celHeight + gap));

                if (texArray == null) texArray = new TextureEditorData[texturePaths.Count];
                if (currenderIndex0 >= texArray.Length) { currenderIndex0 = texArray.Length - cell_RowNum * cell_ColNum; }
                if (currenderIndex0 <= 0) { currenderIndex0 = 0; }

                for (int row = 0; row < cell_RowNum; row++)
                {
                    var cell_y = y_min + celHeight * row + row * gap;
                    for (int col = 0; col < cell_ColNum; col++)
                    {
                        var cell_x = x_min + celWidth * col + col * gap;
                        var renderRect = new Rect(cell_x, cell_y, celWidth, celHeight);
                        var curRenderIndex = currenderIndex0 + row * cell_ColNum + col;
                        if (curRenderIndex >= texturePaths.Count)
                        {
                            continue;
                        }
                        var curRenderTex_Path = texturePaths[curRenderIndex];
                        var texData = getOrLoadTextureEditorData(curRenderIndex, curRenderTex_Path, true);
                        if (texData != null)
                        {
                            var pivot = texData.GetPivot();
                            var drawRect = getTextDrawRect(texData.tex, renderRect);
                            drawBoundRect(drawRect, new Color(0, 0, 0, .85f));
                            var color = GUI.color;
                            GUI.backgroundColor = Color.gray;
                            GUI.Box(drawRect, texData.tex);
                            if (!texData.isValid)
                            {
                                GUI.Label(new Rect(drawRect.x + drawRect.width * 0.5f - 20,drawRect.y+ drawRect.height*0.5f - 50,100,20), "ERROR");
                            }
                            GUI.backgroundColor = color;
                            //
                            color = GUI.color;
                            GUI.color = Color.yellow;
                            GUI.Label(new Rect(renderRect.x, renderRect.y - 10, renderRect.width + gap, 20), texData.name);
                            GUI.color = color;
                            drawPivot(pivot, drawRect);
                            //检查鼠标点击
                            Event e = Event.current;
                            switch (e.type)
                            {
                                case EventType.MouseDown:
                                    if (e.button == 0)
                                    {
                                        if (drawRect.Contains(e.mousePosition))
                                        {
                                            var mousePos = e.mousePosition;
                                            var pivot_X = (mousePos.x - drawRect.x) / drawRect.width;
                                            var pivot_Y = (mousePos.y - drawRect.y) / drawRect.height;
                                            var newPivot = new Vector2(pivot_X, 1 - pivot_Y);
                                            if (newPivot != pivot)
                                            {
                                                texData.SetPivot(newPivot);
                                                texData.ApplySetting();
                                            }
                                            e.Use();
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }

                var maxRenderIndex = currenderIndex0 + cell_ColNum * cell_RowNum;
                if (maxRenderIndex > texArray.Length) maxRenderIndex = texArray.Length;

                GUILayout.Space(this.position.height - 50);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("<<"))
                {
                    currenderIndex0 -= cell_ColNum * cell_RowNum;
                }
                GUILayout.Space(10);
                GUILayout.Label($"{currenderIndex0 + 1}-{maxRenderIndex}");
                GUILayout.Space(10);
                if (GUILayout.Button(">>"))
                {
                    currenderIndex0 += cell_ColNum * cell_RowNum;
                }
                GUILayout.Space(30);
                if (GUILayout.Button("保存并重新导入"))
                {
                    bool isChanged = false;
                    for (int i = 0; i < texArray.Length; i++)
                    {
                        var texData = texArray[i];
                        if (texData != null && texData.isChanged)
                        {
                            texData.SaveAsset();
                            isChanged = true;
                        }
                    }
                    if (isChanged)
                    {
                        Debug.Log("---Refresh Settings---");
                    }
                }
                GUILayout.Space(20);
                if (GUILayout.Button("清除"))
                {
                    currenderIndex0 = 0;
                    targetObjectPath = null;
                    texturePaths = null;
                    texArray = null;
                }
                GUILayout.EndHorizontal();
            }
            Rect getTextDrawRect(Texture2D tex, Rect canvasRect)
            {
                var w = tex.width;
                var h = tex.height;
                var sMax = canvasRect.width;
                var c = Mathf.Max(h, w) / sMax;
                var lw = w / c;
                var lh = h / c;
                var drawRect = new Rect(canvasRect.x + (sMax - lw) * 0.5f, canvasRect.y + (sMax - lh) * 0.5f, lw, lh);
                return drawRect;
            }
            void drawBoundRect(Rect canvasRect, Color color)
            {
                ///--------------------BOUNDS START---------------------------
                var line_1 = new Rect(canvasRect.x, canvasRect.y - 1, canvasRect.width, 1);
                var line_2 = new Rect(canvasRect.x, canvasRect.y + canvasRect.height, canvasRect.width, 1);
                var line_3 = new Rect(canvasRect.x - 1, canvasRect.y - 1, 1, canvasRect.height + 2);
                var line_4 = new Rect(canvasRect.x + canvasRect.width, canvasRect.y - 1, 1, canvasRect.height + 2);
                EditorGUI.DrawRect(line_1, color);
                EditorGUI.DrawRect(line_2, color);
                EditorGUI.DrawRect(line_3, color);
                EditorGUI.DrawRect(line_4, color);
                ///--------------------BOUNDS END---------------------------
            }
            void drawPivot(Vector2 pivot, Rect canvasRect)
            {
                pivot.y = 1 - pivot.y;
                var pivotArrow_X = canvasRect.x + canvasRect.width * pivot.x;
                var pivotArrow_Y = canvasRect.y + canvasRect.height * pivot.y;
                var pivotArrowRect = new Rect(pivotArrow_X - 2, pivotArrow_Y - 2, 4, 4);
                var pivotArrowRect_1 = new Rect(pivotArrow_X - 10, pivotArrow_Y - 1, 20, 2);
                var pivotArrowRect_2 = new Rect(pivotArrow_X - 1, pivotArrow_Y - 10, 2, 20);
                EditorGUI.DrawRect(pivotArrowRect_1, Color.green);
                EditorGUI.DrawRect(pivotArrowRect_2, Color.green);
                EditorGUI.DrawRect(pivotArrowRect, Color.red);
            }
            TextureEditorData getOrLoadTextureEditorData(int index, string path, bool loadImporter = false)
            {
                TextureEditorData texData = texArray[index];
                if (texData == null)
                {
                    texData = new TextureEditorData();
                    //
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    texData.isValid = tex != null;
                    if(tex == null)
                    {
                        tex = Texture2D.blackTexture;
                    }
                    texData.tex = tex;
                    texData.path = path;
                    texData.name = Path.GetFileNameWithoutExtension(path);
                    if (loadImporter && texData.isValid)
                    {
                        texData.getOrLoadTextureImportSetting();
                    }
                    //
                    texArray[index] = texData;
                }
                return texData;
            }
            //
            private void OnDestroy()
            {
                targetObjectPath = null;
                texturePaths = null;
                texArray = null;
            }
            public class TextureEditorData
            {
                public bool isValid = true;
                public bool isChanged { get; private set; }
                //
                public string path;
                public string name;
                //
                public Texture2D tex;
                //
                public TextureImporter importer;
                public TextureImporterSettings importSetting;
                public bool isImporterLoaded
                {
                    get
                    {
                        return importer != null && importSetting != null;
                    }
                }
                public Vector2 GetPivot()
                {
                    if (importSetting != null)
                    {
                        return importSetting.spritePivot;
                    }
                    return Vector2.one * 0.5f;
                }
                public void SetPivot(Vector2 pivot)
                {
                    isChanged = true;
                    if (importSetting != null)
                    {
                        importSetting.spriteAlignment = (int)SpriteAlignment.Custom;
                        importSetting.spritePivot = pivot;
                    }
                }
                public void getOrLoadTextureImportSetting()
                {
                    if (importer == null || importSetting == null)
                    {
                        importer = (TextureImporter)AssetImporter.GetAtPath(path);
                        importSetting = new TextureImporterSettings();
                        importer.ReadTextureSettings(importSetting);
                    }
                }
                public void ApplySetting()
                {
                    if (importSetting != null && importer != null)
                    {
                        importer.SetTextureSettings(importSetting);
                    }
                }
                public void SaveAsset()
                {
                    if (isChanged)
                    {
                        importer?.SaveAndReimport();
                        isChanged = false;
                    }
                }
            }
        }
        public class TextureImportSettingWindow : EditorWindow
        {
            [MenuItem("Tools/设置导入图片设置")]
            public static void OpenTextureImportWindow()
            {
                var window = CreateWindow<TextureImportSettingWindow>("TextureImportSetting");
                window.Show();
            }
            public void CreateGUI()
            {
                Undo.undoRedoPerformed -= RefreshWindow;
                Undo.undoRedoPerformed += RefreshWindow;

                VisualElement root = this.rootVisualElement;
                // 加载布局文件
                var visualAsset = UxmlLoader.LoadWindowUXML<TextureImportSettingWindow>();
                if (visualAsset == null)
                    return;

                visualAsset.CloneTree(root);
            }
            void RefreshWindow()
            {

            }
            string unityAssetPath = Application.dataPath + "/Assets";
            Vector2 size = new Vector2(32, 32);
            Rect pathLabelHeader_rect = new Rect(20, 5, 80, 25);
            Rect pathDrag_rect = new Rect(100, 5, 300, 25);
            void LoadSetting()
            {
                TextureImporterSettings m_TextureImporterSettings = new TextureImporterSettings()
                {
                    mipmapEnabled = false,
                    mipmapFilter = TextureImporterMipFilter.BoxFilter,
                    sRGBTexture = true,
                    borderMipmap = false,
                    mipMapsPreserveCoverage = false,
                    alphaTestReferenceValue = 0.5f,
                    readable = false,

#if ENABLE_TEXTURE_STREAMING
                    streamingMipmaps = false,
                    streamingMipmapsPriority = 0,
#endif

                    fadeOut = false,
                    mipmapFadeDistanceStart = 1,
                    mipmapFadeDistanceEnd = 3,

                    convertToNormalMap = false,
                    heightmapScale = 0.25F,
                    normalMapFilter = 0,

                    generateCubemap = TextureImporterGenerateCubemap.AutoCubemap,
                    cubemapConvolution = 0,

                    seamlessCubemap = false,

                    npotScale = TextureImporterNPOTScale.ToNearest,

                    spriteMode = (int)SpriteImportMode.Multiple,
                    spriteExtrude = 1,
                    spriteMeshType = SpriteMeshType.Tight,
                    spriteAlignment = (int)SpriteAlignment.Center,
                    spritePivot = new Vector2(0.5f, 0.5f),
                    spritePixelsPerUnit = 100.0f,
                    spriteBorder = new Vector4(0.0f, 0.0f, 0.0f, 0.0f),

                    alphaSource = TextureImporterAlphaSource.FromInput,
                    alphaIsTransparency = true,
                    spriteTessellationDetail = -1.0f,

                    textureType = TextureImporterType.Sprite,
                    textureShape = TextureImporterShape.Texture2D,

                    filterMode = FilterMode.Point,
                    aniso = 1,
                    mipmapBias = 0.0f,
                    wrapModeU = TextureWrapMode.Clamp,
                    wrapModeV = TextureWrapMode.Clamp,
                    wrapModeW = TextureWrapMode.Clamp,
                };
            }

            public class TexDefault
            {
                public TextureImporterType _Texture_Type;
                public TextureImporterShape _Texture_Shape;
                #region Cube
                public int _Mapping = 0;
                public int __Convolution_Type = 0;
                public bool __Fixup_Edge_Seams = false;
                #endregion

                #region 2D_Array
                public int _Columns = 1;
                public int _Rows = 1;
                #endregion

                public bool _sRGB=true;
                public TextureImporterAlphaSource _Alpha_Source;
                public bool _Alpha_Is_Transparency;
                public TextureImporterNPOTScale __Non_Power_of_2;
                public bool __ReadvWrite = false;
                public bool __Virtual_Texture_Only = false;
                public bool __Generate_Mipmaps = false;
                public bool ___Use_Mipmap_Limits = false;
                public bool ___Mip_Sreaming = false;
                public int  ____Priority = 0;
                public TextureImporterMipFilter ___Mipmap_Filtering = TextureImporterMipFilter.BoxFilter;
                public bool ___Preserve_Coverage = false;
               public float ____Alpha_Cutoff = 0.5f;
                public bool ___Replicate_Border = false;
                public int  ____Fade_Range = 1;//1-10
                public bool __Ignore_PNG_Gamma = false;
                public int  __Swizzle_R = 0;
                public int  __Swizzle_G = 1;
                public int  __Swizzle_B = 2;
                public int  __Swizzle_A = 3;
                public TextureWrapMode __Wrap_Mode_U_axis = TextureWrapMode.Repeat;
                public TextureWrapMode __Wrap_Mode_V_axis = TextureWrapMode.Repeat;
                public FilterMode __Filter_Mode = FilterMode.Trilinear;
                public int __Aniso_Level = 1;/*var showAniso = (FilterMode)m_FilterMode.intValue != FilterMode.Point
                    && m_EnableMipMap.intValue > 0
                    && (TextureImporterShape)m_TextureShape.intValue != TextureImporterShape.TextureCube;*/

            }
        }
    }
    public class TextureImportHelper : AssetPostprocessor
    {
        //图片资源导入前
        void OnPreprocessTexture()
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            if (assetPath.Contains("spine") || assetPath.Contains("Spine"))
            {
                //格式化导入的SpineTexture的格式设置
                if (textureImporter != null)
                {
                    //SetUpTexure2DAttribute(textureImporter);

                    var newPlatformSettings_Android = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("Android"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_Android);

                    var newPlatformSettings_iOS = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("iPhone"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_iOS);
                    textureImporter.SaveAndReimport();
                }
                return;
            }
            if (assetPath.Contains("BundleResources/UI")) //批量设置UI导出的图集压缩格式为 ASTC 6x6
            {
                if (textureImporter != null)
                {
                    SetUpTexure2DAttribute(textureImporter);

                    var newPlatformSettings_Android = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("Android"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_Android);

                    var newPlatformSettings_iOS = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("iPhone"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_iOS);
                }
            }
            else
            {
                //SetUpSprite2DAttribute(textureImporter);
            }
        }
        //图片资源重新导入
        void OnPostprocessTexture(Texture2D texture)
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            if (assetPath.Contains("spine") || assetPath.Contains("Spine"))
            {
                //格式化导入的SpineTexture的格式设置
                if (textureImporter != null)
                {
                    //SetUpTexure2DAttribute(textureImporter);

                    var newPlatformSettings_Android = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("Android"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_Android);

                    var newPlatformSettings_iOS = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("iPhone"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_iOS);
                    textureImporter.SaveAndReimport();
                }
                return;
            }

            if (assetPath.Contains("BundleResources/UI")) //批量设置UI导出的图集压缩格式为 ASTC 6x6
            {
                if (textureImporter != null)
                {
                    SetUpTexure2DAttribute(textureImporter);

                    var newPlatformSettings_Android = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("Android"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_Android);

                    var newPlatformSettings_iOS = SetUpOnPostprocessTextureUICompressAndFormat(textureImporter.GetPlatformTextureSettings("iPhone"));
                    textureImporter.SetPlatformTextureSettings(newPlatformSettings_iOS);
                    textureImporter.SaveAndReimport();
                }
            }
            else
            {
                //SetUpSprite2DAttribute(textureImporter);
                //textureImporter.SaveAndReimport();
            }
        }
        public void SetUpTexure2DAttribute(TextureImporter textureImporter)
        {
            //textureImporter.alphaIsTransparency = true;
            //textureImporter.mipmapEnabled = false;
            //textureImporter.textureType = TextureImporterType.Default;
            //textureImporter.spriteImportMode = SpriteImportMode.None;

            TextureImporterSettings setting = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(setting);
            setting.alphaIsTransparency = true;
            setting.mipmapEnabled = false;
            setting.spriteMode = (int)SpriteImportMode.None;
            setting.textureType = TextureImporterType.Default;
            textureImporter.SetTextureSettings(setting);
        }
        public void SetUpSprite2DAttribute(TextureImporter textureImporter)
        {
            //textureImporter.alphaIsTransparency = true;
            //textureImporter.mipmapEnabled = false;
            //textureImporter.textureType = TextureImporterType.Sprite;
            //textureImporter.spriteImportMode = SpriteImportMode.Single;

            TextureImporterSettings setting = new TextureImporterSettings();
            textureImporter.ReadTextureSettings(setting);
            setting.alphaIsTransparency = true;
            setting.mipmapEnabled = false;
            setting.spriteMode = (int)SpriteImportMode.Single;
            setting.textureType = TextureImporterType.Sprite;
            setting.spriteMeshType = SpriteMeshType.FullRect;
            textureImporter.SetTextureSettings(setting);
        }
        public TextureImporterPlatformSettings SetUpOnPostprocessTextureUICompressAndFormat(TextureImporterPlatformSettings platformSettings)
        {
            /*
                有一种压缩方式TextureImporterFormat.ETC2_RGBA8Crunched
                这个压缩格式在iOS上也支持(这是Unity自己改进过的跨平台版本)
                如果按照ASTC_6x6为基准1的话。 ETC2_RGBA8Crunched压缩过后占用为 1 * (CompressQualify / 100.0f)
                经过测试ETC2_RGBA8Crunched在CompressQuality=75时对比ASTC_6x6失真不太明显
                以下时对比数据(RGBA8Crunched是在ETC2_RGBA8(Compressed)基础下再次进行Crunched压缩)
                在Android上因为对ETC格式有CPU优化所以理论上ETC2_RGBA8Crunched格式优于ASTC
                ETC2_RGBA8(Compressed) = 5.3M
                ASTC_6x6 = 2.4M
                ETC2_RGBA8Crunched(Qualty=100) = 0.8M
            */
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            platformSettings.format = TextureImporterFormat.ASTC_6x6;
            platformSettings.crunchedCompression = false;
            platformSettings.overridden = true;
            return platformSettings;
        }
    }
    public class UxmlLoader
    {
        private readonly static Dictionary<System.Type, string> _uxmlDic = new Dictionary<System.Type, string>();

        /// <summary>
        /// 加载窗口的布局文件
        /// </summary>
        public static UnityEngine.UIElements.VisualTreeAsset LoadWindowUXML<TWindow>() where TWindow : class
        {
            var windowType = typeof(TWindow);

            // 缓存里查询并加载
            if (_uxmlDic.TryGetValue(windowType, out string uxmlGUID))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(uxmlGUID);
                if (string.IsNullOrEmpty(assetPath))
                {
                    _uxmlDic.Clear();
                    throw new System.Exception($"Invalid UXML GUID : {uxmlGUID} ! Please close the window and open it again !");
                }
                var treeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(assetPath);
                return treeAsset;
            }

            // 全局搜索并加载
            string[] guids = AssetDatabase.FindAssets(windowType.Name);
            if (guids.Length == 0)
                throw new System.Exception($"Not found any assets : {windowType.Name}");

            foreach (string assetGUID in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (assetType == typeof(UnityEngine.UIElements.VisualTreeAsset))
                {
                    _uxmlDic.Add(windowType, assetGUID);
                    var treeAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.VisualTreeAsset>(assetPath);
                    return treeAsset;
                }
            }
            throw new System.Exception($"Not found UXML file : {windowType.Name}");
        }
    }
}