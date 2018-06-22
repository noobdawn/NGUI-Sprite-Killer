using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;

public class SpriteInfo
{
    public UISpriteData Data;
    public UIAtlas Atlas;
    public bool Selected;
    public SpriteInfo(UISpriteData data, UIAtlas atlas)
    {
        Data = data;
        Atlas = atlas;
        Selected = false;
    }
}

public class NGUIUnusedSpriteFound : EditorWindow
{
    #region Param
    private List<SpriteInfo> spriteInfoList;
    private Dictionary<string, SpriteInfo> spriteInfoDic;

    private string atlasPath;
    private string prefabPath;
    #endregion

    #region UI
    private static GUIStyle myStyle;
    [MenuItem("Assets/Noobdawn/检测无效图集")]
    static void ShowWindow()
    {
        NGUIUnusedSpriteFound window = GetWindow<NGUIUnusedSpriteFound>();
        window.Show();
        window.Init();
        window.name = "检测无效图集";

        myStyle = new GUIStyle();
        myStyle.fontSize = 15;
        myStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        GUILayout.BeginVertical();
        if (GUILayout.Button("收集所有图集信息",GUILayout.Width(380)))
        {
            CollectAllSpriteInfo();
        }
        if (GUILayout.Button("开始比对UIPrefab", GUILayout.Width(380)))
        {
            FilterAllSpriteInfo();
        }
        if (GUILayout.Button("删除选中Sprite",   GUILayout.Width(380)))
        {
            DeleteAllSprite();
        }
        DrawSpriteInfo();
        GUILayout.EndVertical();
        GUILayout.BeginVertical();
        GUILayout.TextArea("比对结果仅供参考！\n代码中赋值的图标和表格里配置的图标不计入比对结果！\n请只删除那些确定不会再使用的图片，宁放过勿错过！\n对图集做出修改后请Save Project后再提交SVN。");
        GUILayout.EndVertical();
    }

    public Vector2 mListScrollPos;
    public bool mIsListExpand;
    public bool mIsDicExpand;
    private void DrawSpriteInfo()
    {
        if (spriteInfoList == null) return;
        mListScrollPos = EditorGUILayout.BeginScrollView(mListScrollPos, GUILayout.Width(380));
        mIsListExpand = EditorGUILayout.Foldout(mIsListExpand, "图片列表");
        if (mIsListExpand)
        {
            foreach (var info in spriteInfoList)
            {
                EditorGUILayout.BeginHorizontal(GUILayout.Width(380));
                GUILayout.Space(30);
                GUILayout.Label(info.Atlas.name + " - " + info.Data.name);
                //info.Selected = EditorGUILayout.ToggleLeft(info.SpriteName, info.Selected, GUILayout.Width(240));
                EditorGUILayout.EndHorizontal();
            }
        }
        mIsDicExpand = EditorGUILayout.Foldout(mIsDicExpand, "比对结果");
        if (mIsDicExpand)
        {
            int i = 0;
            foreach (var info in spriteInfoDic)
            {
                GUILayout.Space(65);
                EditorGUILayout.BeginHorizontal(GUILayout.Width(380));
                GUILayout.Space(90);
                Texture2D tex = info.Value.Atlas.texture as Texture2D;
                Rect rect = new Rect(10f, i * 85 + 40, 80f, 80f);
                Rect uv  = new Rect(info.Value.Data.x, info.Value.Data.y, info.Value.Data.width, info.Value.Data.height);
                uv = NGUIMath.ConvertToTexCoords(uv, tex.width, tex.height);
                float scaleX = rect.width / uv.width;
                float scaleY = rect.height / uv.height;

                // Stretch the sprite so that it will appear proper
                float aspect = (scaleY / scaleX) / ((float)tex.height / tex.width);
                Rect clipRect = rect;

                if (aspect != 1f)
                {
                    if (aspect < 1f)
                    {
                        // The sprite is taller than it is wider
                        float padding = 80 * (1f - aspect) * 0.5f;
                        clipRect.xMin += padding;
                        clipRect.xMax -= padding;
                    }
                    else
                    {
                        // The sprite is wider than it is taller
                        float padding = 80 * (1f - 1f / aspect) * 0.5f;
                        clipRect.yMin += padding;
                        clipRect.yMax -= padding;
                    }
                }

                GUI.DrawTextureWithTexCoords(clipRect, tex, uv);
                NGUIEditorTools.DrawOutline(rect, new Color(0.4f, 1f, 0f, 1f));
                info.Value.Selected = EditorGUILayout.ToggleLeft(info.Value.Data.name, info.Value.Selected, myStyle, GUILayout.Width(240));
                EditorGUILayout.EndHorizontal();
                i++;
            }
        }
        EditorGUILayout.EndScrollView();
    }
    #endregion

    private void Init()
    {
        spriteInfoList = new List<SpriteInfo>();
        spriteInfoDic = new Dictionary<string, SpriteInfo>();
        atlasPath = Application.dataPath + "/Game/Atlas/Static";
        prefabPath = Application.dataPath + "/Game/UIPrefab";
    }

    private void CollectAllSpriteInfo()
    {
        spriteInfoList.Clear();
        var atlasFiles = Directory.GetFiles(atlasPath, "*.prefab", SearchOption.AllDirectories);
        EditorUtility.DisplayDialog("", "检测到" + atlasFiles.Length + "个图集", "OK");
        foreach (string atlasFile in atlasFiles)
        {
            UIAtlas atlas = AssetDatabase.LoadAssetAtPath<GameObject>(atlasFile.Substring(atlasFile.IndexOf("Assets"))).GetComponent<UIAtlas>();
            if (atlas == null)
            {
                Debug.LogError(atlasFile + "不是个有效的图集");
                continue;
            }
            foreach(var v in atlas.spriteList)
            {
                spriteInfoList.Add(new SpriteInfo(v, atlas));
            }
        }
        EditorUtility.DisplayDialog("", "总计：" + spriteInfoList.Count + "个Sprite", "OK");
    }

    private void FilterAllSpriteInfo()
    {
        spriteInfoDic.Clear();
        foreach (var v in spriteInfoList)
        {
            if (spriteInfoDic.ContainsKey(v.Data.name))
            {
                Debug.LogError("SpriteName重复：" + v.Data.name);
                continue;
            }
            spriteInfoDic.Add(v.Data.name, v);
        }
        var prefabFiles = Directory.GetFiles(prefabPath, "*.prefab", SearchOption.AllDirectories);
        EditorUtility.DisplayDialog("", "有" + prefabFiles.Length + "个Prefab待比对", "OK");
        foreach (string prefabFile in prefabFiles)
        {
            FilterObjectRef(prefabFile.Substring(prefabFile.IndexOf("Assets")));
        }
    }

    void FilterObjectRef(string assetPath)
    {
        GameObject go = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
        if (null == go)
        {
            return;
        }

        // check Sprite
        UISprite[] sprites = go.GetComponentsInChildren<UISprite>(true);
        foreach (UISprite sprite in sprites)
        {
            if (null == sprite.atlas)
            {
                continue;
            }

            if (spriteInfoDic.ContainsKey(sprite.spriteName))
            {
                spriteInfoDic.Remove(sprite.spriteName);
            }
        }
    }

    void DeleteAllSprite()
    {
        if (EditorUtility.DisplayDialog("警告", "这步操作无法撤销，将直接改变图集，是否确认！", "取消", "我确认这些修改是必须的"))
            return;
        foreach(var kv in spriteInfoDic)
        {
            if (kv.Value.Selected)
            {
                Debug.Log("删除" + kv.Value.Atlas.name + " - " + kv.Value.Data.name);
                DeleteSprite(kv.Value);
            }
        }
        CollectAllSpriteInfo();
        FilterAllSpriteInfo();
    }

    void DeleteSprite (SpriteInfo info)
	{
		if (this == null) return;
		List<UIAtlasMaker.SpriteEntry> sprites = new List<UIAtlasMaker.SpriteEntry>();
		UIAtlasMaker.ExtractSprites(info.Atlas, sprites);

		for (int i = sprites.Count; i > 0; )
		{
			UIAtlasMaker.SpriteEntry ent = sprites[--i];
			if (ent.name == info.Data.name)
				sprites.RemoveAt(i);
		}
		UIAtlasMaker.UpdateAtlas(info.Atlas, sprites);
		NGUIEditorTools.RepaintSprites();
	}
}
