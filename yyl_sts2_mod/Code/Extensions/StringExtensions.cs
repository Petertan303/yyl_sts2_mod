using Godot;

namespace yyl_sts2_mod.Code.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    // Godot 的虚拟文件系统 res:// 只认正斜杠; System.IO.Path.Join 在 Windows 下会塞入反斜杠,
    // 导致 ResourceLoader.Exists 找不到资源 (日志里出现 res://yyl_sts2_mod\images\... 就是这原因)。
    // 统一用 '/' 拼接, 并把任何残留的反斜杠也替换掉。
    private static string Join(params string[] parts) =>
        string.Join('/', parts).Replace('\\', '/');

    public static string ImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find image path: " + path);
        return Join(MainFile.ResPath, "images", "card.png");
    }

    public static string CardImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "card_portraits", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find card image path: " + path);
        return Join(MainFile.ResPath, "images", "card_portraits", "card.png");
    }

    public static string BigCardImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "card_portraits", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find big card image path: " + path);
        return Join(MainFile.ResPath, "images", "card_portraits", "big", "card.png");
    }

    public static string PowerImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "powers", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find power image path: " + path);
        return Join(MainFile.ResPath, "images", "powers", "power.png");
    }

    public static string BigPowerImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "powers", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find big power image path: " + path);
        return Join(MainFile.ResPath, "images", "powers", "big", "power.png");
    }

    public static string RelicImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "relics", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find relic image path: " + path);
        return Join(MainFile.ResPath, "images", "relics", "relic.png");
    }

    /// <summary>
    ///     遗物<b>轮廓图</b>（缩略图合成用）。注意实际文件在 relics/outline/ 子目录下，
    ///     与底图 relics/ 不是同一层 —— 早期版本曾漏掉 outline/ 这一级，
    ///     导致所有遗物的缩略图渲染失败回退成 NOPE 占位符（大图不需要轮廓图所以正常）。
    /// </summary>
    public static string RelicOutlineImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "relics", "outline", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find relic outline image path: " + path);
        return Join(MainFile.ResPath, "images", "relics", "outline", "relic_outline.png");
    }

    public static string BigRelicImagePath(this string path)
    {
        path = Join(MainFile.ResPath, "images", "relics", "big", path);
        if (ResourceLoader.Exists(path)) return path;

        MainFile.Logger.Info("Could not find big relic image path: " + path);
        return Join(MainFile.ResPath, "images", "relics", "big", "relic.png");
    }

    public static string CharacterUiPath(this string path)
    {
        return Join(MainFile.ResPath, "images", "charui", path);
    }
}
