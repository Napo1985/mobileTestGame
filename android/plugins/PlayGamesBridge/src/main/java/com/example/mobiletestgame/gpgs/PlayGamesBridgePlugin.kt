package com.example.mobiletestgame.gpgs

import android.os.Handler
import android.os.Looper
import android.util.Log
import org.godotengine.godot.Godot
import org.godotengine.godot.plugin.GodotPlugin
import org.godotengine.godot.plugin.SignalInfo
import org.godotengine.godot.plugin.UsedByGodot

/**
 * Godot Android plugin v2 entry point. Methods are invoked from GDScript via Engine.get_singleton("PlayGamesBridge").
 * Replace the stub bodies with Play Games Services v2 calls (see docs/PLAY_GAMES.md).
 */
class PlayGamesBridgePlugin(godot: Godot) : GodotPlugin(godot) {
    private val mainHandler = Handler(Looper.getMainLooper())

    override fun getPluginName(): String = "PlayGamesBridge"

    override fun getPluginSignals(): MutableSet<SignalInfo> {
        return mutableSetOf(
            SignalInfo(
                "sign_in_result",
                arrayOf(Boolean::class.javaObjectType, String::class.javaObjectType),
            ),
            SignalInfo(
                "leaderboard_submit_result",
                arrayOf(Boolean::class.javaObjectType, String::class.javaObjectType),
            ),
            SignalInfo(
                "snapshot_save_result",
                arrayOf(Boolean::class.javaObjectType, String::class.javaObjectType),
            ),
            SignalInfo(
                "snapshot_load_result",
                arrayOf(Boolean::class.javaObjectType, String::class.javaObjectType),
            ),
        )
    }

    @UsedByGodot
    fun sign_in() {
        Log.i(TAG, "sign_in (stub — wire Play Games Services v2 here)")
        emitOnMainThread("sign_in_result", true, "stub_ok")
    }

    @UsedByGodot
    fun sign_out() {
        Log.i(TAG, "sign_out (stub)")
    }

    @UsedByGodot
    fun submit_leaderboard_score(leaderboardId: String, score: Int) {
        Log.i(TAG, "submit_leaderboard_score id=$leaderboardId score=$score (stub)")
        emitOnMainThread("leaderboard_submit_result", true, "stub_ok")
    }

    @UsedByGodot
    fun save_snapshot(name: String, data: String) {
        Log.i(TAG, "save_snapshot name=$name bytes=${data.length} (stub)")
        emitOnMainThread("snapshot_save_result", true, "stub_ok")
    }

    @UsedByGodot
    fun load_snapshot(name: String) {
        Log.i(TAG, "load_snapshot name=$name (stub)")
        emitOnMainThread("snapshot_load_result", true, "")
    }

    private fun emitOnMainThread(signal: String, vararg args: Any?) {
        mainHandler.post {
            try {
                emitSignal(signal, *args)
            } catch (e: Throwable) {
                Log.e(TAG, "emitSignal failed for $signal", e)
            }
        }
    }

    companion object {
        private const val TAG = "PlayGamesBridge"
    }
}
