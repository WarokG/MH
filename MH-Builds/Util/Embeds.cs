﻿using Discord;
using MHBuilds.Settings;

namespace MHBuilds.Util;

public class Embeds
{
    public static EmbedBuilder MakeBuilder()
    {
        var builder = new EmbedBuilder
        {
            Color = Color.Orange,
            Footer = MakeFooter()
        };

        return builder;
    }

    public static EmbedFooterBuilder MakeFooter()
    {
        return new EmbedFooterBuilder
        {
            Text = $"MH-Builds v.{BotVariables.Version}",
            IconUrl = BotVariables.Images.Avatar
        };
    }

    public static EmbedBuilder MakeErrorEmbed()
    {
        var builder = new EmbedBuilder
        {
            Color = Color.Red,
            Footer = MakeFooter(),
            ThumbnailUrl = BotVariables.Images.SadFace
        };

        return builder;
    }
}