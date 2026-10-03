@tool
extends EditorPlugin

var export_plugin: AndroidExportPlugin


func _enter_tree():
	export_plugin = AndroidExportPlugin.new()
	add_export_plugin(export_plugin)
	add_autoload_singleton("GodotTapTapAd", "res://addons/GodotTapTapAdSDK/GodotTapTapAd.gd")


func _exit_tree():
	remove_export_plugin(export_plugin)
	export_plugin = null


class AndroidExportPlugin extends EditorExportPlugin:
	const PLUGIN_NAME := "GodotTapTapAdSDK"

	func _supports_platform(platform):
		return platform is EditorExportPlatformAndroid

	func _get_android_libraries(platform, debug):
		var variant := "debug" if debug else "release"
		return PackedStringArray([
			"res://addons/%s/bin/%s/%s-%s.aar" % [PLUGIN_NAME, variant, PLUGIN_NAME, variant],
			"res://addons/%s/libs/dirichlet_ad_5.3.0.1.aar" % PLUGIN_NAME,
		])

	func _get_android_dependencies(platform: EditorExportPlatform, debug: bool) -> PackedStringArray:
		return PackedStringArray(["com.squareup.okhttp3:okhttp:3.12.1"])

	func _get_name():
		return PLUGIN_NAME
