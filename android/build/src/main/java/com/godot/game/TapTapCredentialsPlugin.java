package com.godot.game;

import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import org.godotengine.godot.plugin.UsedByGodot;

public final class TapTapCredentialsPlugin extends GodotPlugin {
    public TapTapCredentialsPlugin(Godot godot) {
        super(godot);
    }

    @Override
    public String getPluginName() {
        return "TapTapCredentials";
    }

    @UsedByGodot
    private String getClientId() {
        return BuildConfig.TAPTAP_CLIENT_ID;
    }

    @UsedByGodot
    private String getClientToken() {
        return BuildConfig.TAPTAP_CLIENT_TOKEN;
    }

    @UsedByGodot
    private String getRegion() {
        return BuildConfig.TAPTAP_REGION;
    }
}
