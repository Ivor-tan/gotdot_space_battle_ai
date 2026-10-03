package com.ivortan.godot_taptap_ad;

import android.util.Log;

import com.tapsdk.tapad.AdRequest;
import com.tapsdk.tapad.TapAdConfig;
import com.tapsdk.tapad.TapAdCustomController;
import com.tapsdk.tapad.TapAdManager;
import com.tapsdk.tapad.TapAdNative;
import com.tapsdk.tapad.TapAdSdk;
import com.tapsdk.tapad.TapInterstitialAd;
import com.tapsdk.tapad.TapRewardVideoAd;
import com.tapsdk.tapad.constants.Constants;

import org.godotengine.godot.Godot;
import org.godotengine.godot.plugin.GodotPlugin;
import org.godotengine.godot.plugin.SignalInfo;
import org.godotengine.godot.plugin.UsedByGodot;

import java.util.LinkedHashSet;
import java.util.Set;

public final class GodotTapTapAdPlugin extends GodotPlugin {
    private TapAdNative adNative;
    private TapRewardVideoAd rewardVideoAd;
    private TapInterstitialAd interstitialAd;

    public GodotTapTapAdPlugin(Godot godot) {
        super(godot);
    }

    @Override
    public String getPluginName() {
        return "GodotTapTapAdSDK";
    }

    @Override
    public Set<SignalInfo> getPluginSignals() {
        Set<SignalInfo> signals = new LinkedHashSet<>();
        signals.add(new SignalInfo("initialized"));
        signals.add(new SignalInfo("initialization_failed", String.class));
        signals.add(new SignalInfo("reward_loaded"));
        signals.add(new SignalInfo("reward_load_failed", Integer.class, String.class));
        signals.add(new SignalInfo("reward_shown"));
        signals.add(new SignalInfo("reward_closed"));
        signals.add(new SignalInfo("reward_verified", Boolean.class, Integer.class, String.class, Integer.class, String.class));
        signals.add(new SignalInfo("reward_video_completed"));
        signals.add(new SignalInfo("reward_video_skipped"));
        signals.add(new SignalInfo("reward_video_error"));
        signals.add(new SignalInfo("reward_clicked"));
        signals.add(new SignalInfo("interstitial_loaded"));
        signals.add(new SignalInfo("interstitial_load_failed", Integer.class, String.class));
        signals.add(new SignalInfo("interstitial_shown"));
        signals.add(new SignalInfo("interstitial_closed"));
        signals.add(new SignalInfo("interstitial_error"));
        signals.add(new SignalInfo("interstitial_clicked"));
        return signals;
    }

    @UsedByGodot
    private void initialize(long mediaId, String mediaName, String mediaKey, String tapClientId,
                            boolean debug, boolean personalizedAds, boolean location,
                            boolean phoneState, boolean wifiState, boolean macAddress,
                            boolean bssid, boolean ssid, boolean sensor, boolean writeExternal,
                            boolean appList, boolean androidId, boolean shakeEnabled) {
        // 原生桥接仅记录状态，不记录媒体密钥及账号数据。
        Log.i("TapTapAd", "[TapTapAd] native_initialize_received media_id=" + mediaId);
        runOnUiThread(() -> {
            try {
                Log.i("TapTapAd", "[TapTapAd] native_initialize_ui_thread activity_present=" + (getActivity() != null));
                TapAdCustomController controller = createPrivacyController(
                        location, phoneState, wifiState, macAddress, bssid, ssid,
                        sensor, writeExternal, appList, androidId);
                TapAdConfig.Builder builder = new TapAdConfig.Builder()
                        .withMediaId(mediaId)
                        .withMediaName(mediaName)
                        .withMediaKey(mediaKey)
                        .enableDebug(debug)
                        .shakeEnabled(shakeEnabled)
                        .withCustomController(controller);
                if (tapClientId != null && !tapClientId.trim().isEmpty()) {
                    builder.withTapClientId(tapClientId.trim());
                }
                Log.i("TapTapAd", "[TapTapAd] sdk_init_begin");
                TapAdSdk.init(getActivity().getApplicationContext(), builder.build());
                Log.i("TapTapAd", "[TapTapAd] sdk_init_returned initialized=" + TapAdSdk.isInitialized());
                setPersonalizedAdsEnabled(personalizedAds);
                adNative = TapAdManager.get().createAdNative(getActivity());
                Log.i("TapTapAd", "[TapTapAd] ad_native_created present=" + (adNative != null));
                emitSignal("initialized");
            } catch (Exception exception) {
                Log.e("TapTapAd", "[TapTapAd] native_initialize_failed exception=" + exception.getClass().getSimpleName());
                emitSignal("initialization_failed", safeMessage(exception));
            }
        });
    }

    @UsedByGodot
    private void setPersonalizedAdsEnabled(boolean enabled) {
        TapAdSdk.putMediaGlobalSettings(
                Constants.Personalization.PERSONAL_ADS_TYPE,
                enabled ? Constants.Personalization.PERSONAL_ADS_TYPE_ALLOW
                        : Constants.Personalization.PERSONAL_ADS_TYPE_LIMIT);
    }

    @UsedByGodot
    private void loadRewardVideo(long spaceId, String rewardName, int rewardAmount,
                                 String userId, String extra) {
        runOnUiThread(() -> {
            if (!ensureAdNative("reward_load_failed")) {
                return;
            }
            disposeRewardVideo();
            AdRequest request = new AdRequest.Builder()
                    .withSpaceId(spaceId)
                    .withRewardName(rewardName)
                    .withRewardAmount(rewardAmount)
                    .withUserId(userId)
                    .withExtra1(extra)
                    .build();
            adNative.loadRewardVideoAd(request, new TapAdNative.RewardVideoAdListener() {
                @Override
                public void onError(int code, String message) {
                    emitSignal("reward_load_failed", code, message == null ? "" : message);
                }

                @Override
                public void onRewardVideoAdLoad(TapRewardVideoAd ad) {
                    cacheRewardVideo(ad);
                }

                @Override
                public void onRewardVideoCached(TapRewardVideoAd ad) {
                    cacheRewardVideo(ad);
                }
            });
        });
    }

    @UsedByGodot
    private boolean isRewardVideoReady() {
        return rewardVideoAd != null && rewardVideoAd.isValid();
    }

    @UsedByGodot
    private void showRewardVideo() {
        runOnUiThread(() -> {
            if (!isRewardVideoReady()) {
                emitSignal("reward_load_failed", -2, "Reward video is not ready or has expired.");
                return;
            }
            rewardVideoAd.showRewardVideoAd(getActivity());
        });
    }

    @UsedByGodot
    private void loadInterstitial(long spaceId) {
        runOnUiThread(() -> {
            if (!ensureAdNative("interstitial_load_failed")) {
                return;
            }
            disposeInterstitial();
            AdRequest request = new AdRequest.Builder().withSpaceId(spaceId).build();
            adNative.loadInterstitialAd(request, new TapAdNative.InterstitialAdListener() {
                @Override
                public void onInterstitialAdLoad(TapInterstitialAd ad) {
                    interstitialAd = ad;
                    bindInterstitialListener(ad);
                    emitSignal("interstitial_loaded");
                }

                @Override
                public void onError(int code, String message) {
                    emitSignal("interstitial_load_failed", code, message == null ? "" : message);
                }
            });
        });
    }

    @UsedByGodot
    private boolean isInterstitialReady() {
        return interstitialAd != null && interstitialAd.isValid();
    }

    @UsedByGodot
    private void showInterstitial() {
        runOnUiThread(() -> {
            if (!isInterstitialReady()) {
                emitSignal("interstitial_load_failed", -2, "Interstitial ad is not ready or has expired.");
                return;
            }
            interstitialAd.show(getActivity());
        });
    }

    @UsedByGodot
    private void disposeAds() {
        runOnUiThread(() -> {
            disposeRewardVideo();
            disposeInterstitial();
            if (adNative != null) {
                adNative.dispose();
                adNative = null;
            }
        });
    }

    @Override
    public void onMainDestroy() {
        disposeRewardVideo();
        disposeInterstitial();
        if (adNative != null) {
            adNative.dispose();
            adNative = null;
        }
    }

    private TapAdCustomController createPrivacyController(
            boolean location, boolean phoneState, boolean wifiState, boolean macAddress,
            boolean bssid, boolean ssid, boolean sensor, boolean writeExternal,
            boolean appList, boolean androidId) {
        return new TapAdCustomController() {
            @Override public boolean isCanUseLocation() { return location; }
            @Override public boolean isCanUsePhoneState() { return phoneState; }
            @Override public boolean isCanUseWifiState() { return wifiState; }
            @Override public boolean isCanUsePhoneMacAddress() { return macAddress; }
            @Override public boolean isCanUseBssid() { return bssid; }
            @Override public boolean isCanUseSsid() { return ssid; }
            @Override public boolean isCanUseSensor() { return sensor; }
            @Override public boolean isCanUseWriteExternal() { return writeExternal; }
            @Override public boolean alist() { return appList; }
            @Override public boolean isCanUseAndroidId() { return androidId; }
            @Override public String getDevImei() { return null; }
            @Override public String getDevOaid() { return null; }
        };
    }

    private boolean ensureAdNative(String failureSignal) {
        if (adNative != null) {
            return true;
        }
        emitSignal(failureSignal, -1, "Dirichlet SDK is not initialized.");
        return false;
    }

    private void cacheRewardVideo(TapRewardVideoAd ad) {
        if (rewardVideoAd == ad) {
            return;
        }
        rewardVideoAd = ad;
        ad.setRewardAdInteractionListener(new TapRewardVideoAd.RewardAdInteractionListener() {
            @Override public void onAdShow(TapRewardVideoAd value) { emitSignal("reward_shown"); }
            @Override public void onAdClose(TapRewardVideoAd value) { emitSignal("reward_closed"); disposeRewardVideo(); }
            @Override public void onVideoComplete(TapRewardVideoAd value) { emitSignal("reward_video_completed"); }
            @Override public void onVideoError(TapRewardVideoAd value) { emitSignal("reward_video_error"); }
            @Override public void onRewardVerify(TapRewardVideoAd value, boolean verified, int amount,
                                                  String name, int code, String message) {
                emitSignal("reward_verified", verified, amount, name == null ? "" : name,
                        code, message == null ? "" : message);
            }
            @Override public void onSkippedVideo(TapRewardVideoAd value) { emitSignal("reward_video_skipped"); }
            @Override public void onAdClick(TapRewardVideoAd value) { emitSignal("reward_clicked"); }
            @Override public void onAdValidShow(TapRewardVideoAd value) { }
        });
        emitSignal("reward_loaded");
    }

    private void bindInterstitialListener(TapInterstitialAd ad) {
        ad.setInteractionListener(new TapInterstitialAd.InterstitialAdInteractionListener() {
            @Override public void onAdShow() { emitSignal("interstitial_shown"); }
            @Override public void onAdClose() { emitSignal("interstitial_closed"); disposeInterstitial(); }
            @Override public void onAdError() { emitSignal("interstitial_error"); }
            @Override public void onAdValidShow() { }
            @Override public void onAdClick() { emitSignal("interstitial_clicked"); }
        });
    }

    private void disposeRewardVideo() {
        if (rewardVideoAd != null) {
            rewardVideoAd.dispose();
            rewardVideoAd = null;
        }
    }

    private void disposeInterstitial() {
        if (interstitialAd != null) {
            interstitialAd.dispose();
            interstitialAd = null;
        }
    }

    private static String safeMessage(Exception exception) {
        String message = exception.getMessage();
        return message == null || message.isEmpty() ? exception.getClass().getSimpleName() : message;
    }
}
