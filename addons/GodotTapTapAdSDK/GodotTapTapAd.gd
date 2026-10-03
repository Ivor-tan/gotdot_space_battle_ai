extends Node

signal initialized
signal initialization_failed(message)
signal reward_loaded
signal reward_load_failed(code, message)
signal reward_shown
signal reward_closed
signal reward_verified(verified, amount, reward_name, code, message)
signal reward_video_completed
signal reward_video_skipped
signal reward_video_error
signal reward_clicked
signal interstitial_loaded
signal interstitial_load_failed(code, message)
signal interstitial_shown
signal interstitial_closed
signal interstitial_error
signal interstitial_clicked

var singleton
var _initialized := false


func _ready():
	if not Engine.has_singleton("GodotTapTapAdSDK"):
		return

	singleton = Engine.get_singleton("GodotTapTapAdSDK")
	for signal_name in [
		"initialized", "reward_loaded", "reward_shown", "reward_closed",
		"reward_video_completed", "reward_video_skipped", "reward_video_error",
		"reward_clicked", "interstitial_loaded", "interstitial_shown",
		"interstitial_closed", "interstitial_error", "interstitial_clicked"
	]:
		singleton.connect(signal_name, Callable(self, "_forward_zero").bind(signal_name))
	singleton.initialization_failed.connect(_forward_one.bind("initialization_failed"))
	singleton.reward_load_failed.connect(_forward_two.bind("reward_load_failed"))
	singleton.interstitial_load_failed.connect(_forward_two.bind("interstitial_load_failed"))
	singleton.reward_verified.connect(_forward_five.bind("reward_verified"))


func initialize(
		media_id: int,
		media_name: String,
		media_key: String,
		tap_client_id: String = "",
		personalized_ads: bool = false,
		debug_log: bool = false,
		privacy_options: Dictionary = {}
	) -> bool:
	if singleton == null:
		push_error("GodotTapTapAdSDK is only available in an Android export with the plugin enabled.")
		return false
	if media_id <= 0 or media_name.is_empty() or media_key.is_empty():
		push_error("Dirichlet media ID, media name, and media key are required.")
		return false

	# Call only after the player has accepted the privacy policy. All optional collection
	# switches default to false so adding the plugin does not silently broaden data access.
	singleton.initialize(
		media_id, media_name, media_key, tap_client_id, debug_log, personalized_ads,
		privacy_options.get("location", false),
		privacy_options.get("phone_state", false),
		privacy_options.get("wifi_state", false),
		privacy_options.get("mac_address", false),
		privacy_options.get("bssid", false),
		privacy_options.get("ssid", false),
		privacy_options.get("sensor", false),
		privacy_options.get("write_external", false),
		privacy_options.get("app_list", false),
		privacy_options.get("android_id", false),
		privacy_options.get("shake", false)
	)
	_initialized = true
	return true


func set_personalized_ads_enabled(enabled: bool):
	if _require_initialization():
		singleton.setPersonalizedAdsEnabled(enabled)


func load_reward_video(space_id: int, reward_name: String, reward_amount: int, user_id: String = "", extra: String = ""):
	if _require_initialization():
		singleton.loadRewardVideo(space_id, reward_name, reward_amount, user_id, extra)


func is_reward_video_ready() -> bool:
	return _require_initialization() and singleton.isRewardVideoReady()


func show_reward_video():
	if _require_initialization():
		singleton.showRewardVideo()


func load_interstitial(space_id: int):
	if _require_initialization():
		singleton.loadInterstitial(space_id)


func is_interstitial_ready() -> bool:
	return _require_initialization() and singleton.isInterstitialReady()


func show_interstitial():
	if _require_initialization():
		singleton.showInterstitial()


func dispose_ads():
	if singleton != null:
		singleton.disposeAds()


func _require_initialization() -> bool:
	if singleton == null:
		push_error("GodotTapTapAdSDK is unavailable on this platform.")
		return false
	if not _initialized:
		push_error("Call GodotTapTapAd.initialize() after privacy consent before requesting ads.")
		return false
	return true


func _forward_zero(signal_name: String):
	Signal(self, signal_name).emit()


func _forward_one(value, signal_name: String):
	Signal(self, signal_name).emit(value)


func _forward_two(value1, value2, signal_name: String):
	Signal(self, signal_name).emit(value1, value2)


func _forward_five(value1, value2, value3, value4, value5, signal_name: String):
	Signal(self, signal_name).emit(value1, value2, value3, value4, value5)
