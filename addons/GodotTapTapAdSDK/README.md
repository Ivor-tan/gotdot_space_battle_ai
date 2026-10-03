# GodotTapTapAdSDK

Godot 4 Android bridge for the official TapADN / Dirichlet Android SDK 5.3.0.1.

## Setup

1. Enable `GodotTapTapAdSDK` in Godot's Plugins settings.
2. Obtain the media ID, media name, media key, and ad-space IDs from Dirichlet.
3. Show the app privacy policy and wait for explicit consent.
4. Only after consent, initialize the SDK:

```gdscript
GodotTapTapAd.initialize(
	123456,
	"Singularity of Star Ocean",
	"YOUR_MEDIA_KEY",
	"YOUR_TAP_CLIENT_ID",
	false, # personalized ads
	false  # SDK debug logs
)
```

All optional device-data permissions default to disabled. Pass a final dictionary only for
capabilities disclosed in the privacy policy and accepted by the player.

## Reward video

```gdscript
GodotTapTapAd.reward_loaded.connect(func(): GodotTapTapAd.show_reward_video())
GodotTapTapAd.reward_verified.connect(_on_reward_verified)
GodotTapTapAd.load_reward_video(SPACE_ID, "星币", 100, PLAYER_ID)
```

Only grant the reward when `reward_verified` reports `verified == true`. For valuable rewards,
use Dirichlet's server-to-server verification instead of trusting the client alone.

## Interstitial

```gdscript
GodotTapTapAd.interstitial_loaded.connect(func(): GodotTapTapAd.show_interstitial())
GodotTapTapAd.load_interstitial(SPACE_ID)
```

Call `dispose_ads()` when the ads are no longer needed. Banner, splash, and feed formats are not
included in this first bridge because their Android `View` lifecycle needs a dedicated Godot UI
container; reward and interstitial formats are safe full-screen entry points.

Official guide: https://ssp.dirichlet.cn/docs/dirichlet-sdk/sdk-guide/
